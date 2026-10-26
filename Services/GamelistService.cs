using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Gamelist_Manager.Services
{
    public class GamelistService
    {
        public static (ObservableCollection<GameMetadataRow> Games, List<string> Duplicates) LoadGamelist(string xmlFilePath, bool ignoreDuplicates = false)
        {
            var games = new ObservableCollection<GameMetadataRow>();
            var duplicates = new List<string>();

            XDocument xmlDoc;
            try
            {
                xmlDoc = XDocument.Load(xmlFilePath);
            }
            catch
            {
                return (games, duplicates);
            }

            // Only direct game entries belong to the editor. Nested game elements in
            // unknown metadata must remain untouched during a round trip.
            var gameElements = xmlDoc.Root?.Elements("game") ?? Enumerable.Empty<XElement>();
            if (!gameElements.Any())
                return (games, duplicates);

            var uniqueRomPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var boolFields = MetadataService.GetAllBoolMetadata();
            var viewableFields = MetadataService.GetAllViewableFields();

            foreach (var gameElement in gameElements)
            {
                var game = new GameMetadataRow();

                foreach (var metaDecl in boolFields)
                    game.SetValue(metaDecl.Key, false);

                foreach (var metaDecl in viewableFields)
                {
                    var element = gameElement.Element(metaDecl.Type);
                    if (element == null || string.IsNullOrEmpty(element.Value))
                        continue;

                    string value = element.Value;

                    if (metaDecl.DataType == MetaDataType.Bool)
                    {
                        game.SetValue(metaDecl.Key, bool.TryParse(value, out bool boolValue) && boolValue);
                    }
                    else
                    {
                        if (metaDecl.Key == MetaDataKeys.releasedate || metaDecl.Key == MetaDataKeys.lastplayed)
                        {
                            // Preserve seconds when converting timestamps for display and saving.
                            string formattedDate = Iso8601Helper.ConvertFromIso8601(value, "yyyy-MM-dd HH:mm:ss");
                            game.SetValue(metaDecl.Key, !string.IsNullOrEmpty(formattedDate) ? formattedDate : value);
                        }
                        else
                        {
                            game.SetValue(metaDecl.Key, value);
                        }
                    }
                }

                var gameIdAttr = gameElement.Attribute("id")?.Value;
                if (!string.IsNullOrEmpty(gameIdAttr))
                    game.SetValue(MetaDataKeys.id, gameIdAttr);

                var gamePath = game.Path;

                if (!string.IsNullOrEmpty(gamePath))
                {
                    if (!uniqueRomPaths.Add(gamePath))
                    {
                        duplicates.Add(gamePath);
                        if (ignoreDuplicates)
                            continue;
                    }
                }

                games.Add(game);
            }

            return (games, duplicates);
        }

        public static void PopulateMediaPaths(IList<GameMetadataRow> games, string mediaDirectory, string? romDirectory = null)
        {
            if (string.IsNullOrEmpty(mediaDirectory)) return;

            var mediaDecls = MetadataService.GetAllMediaFolderTypes();

            foreach (var game in games)
            {
                var romPath = game.Path;
                if (string.IsNullOrEmpty(romPath)) continue;

                string? fullRomPath = !string.IsNullOrEmpty(romDirectory)
                    ? FilePathHelper.GamelistPathToFullPath(romPath, romDirectory)
                    : null;
                var romName = FilePathHelper.NormalizeMediaRomName(romPath, fullRomPath, preserveDirectoryExtension: true);
                if (string.IsNullOrEmpty(romName)) continue;

                foreach (var decl in mediaDecls)
                {
                    if (!decl.IsEsDeSupported) continue;

                    var folder = FilePathHelper.MediaFolderForRom(
                        Path.Combine(mediaDirectory, decl.EsDeFolderName), fullRomPath, romDirectory, esDeProfile: true);

                    var extensions = decl.DataType switch
                    {
                        MetaDataType.Video => MediaExtensions.EsDeVideoExtensions,
                        MetaDataType.Document => MediaExtensions.ManualExtensions,
                        MetaDataType.Music => MediaExtensions.MusicExtensions,
                        _ => MediaExtensions.EsDeImageExtensions
                    };

                    string? resolved = null;
                    foreach (var ext in extensions)
                    {
                        var candidate = Path.Combine(folder, romName + ext);
                        if (File.Exists(candidate))
                        {
                            resolved = candidate;
                            break;
                        }
                    }

                    game.SetValue(decl.Key, resolved ?? string.Empty);
                }
            }
        }

        public static bool SaveGamelist(string xmlFilePath, ObservableCollection<GameMetadataRow> games, bool saveMediaPaths)
        {
            string? temporaryFile = null;
            try
            {
                var originalExists = File.Exists(xmlFilePath);
                var savedItems = new HashSet<string>(StringComparer.Ordinal);
                var doc = originalExists
                    ? XDocument.Load(xmlFilePath, LoadOptions.PreserveWhitespace)
                    : new XDocument(new XElement("gameList"));
                var root = doc.Root;
                if (root == null || root.Name != "gameList")
                    return false;

                var layout = GetXmlLayout(xmlFilePath, root, originalExists);

                // The caller supplies the media persistence capability. Keep media
                // values in the model and existing XML intact when it is disabled.
                var persistedFields = SessionState.Instance.XmlPersistedFields
                    .Where(decl => saveMediaPaths || !decl.IsMedia)
                    .ToList();

                if (originalExists)
                {
                    // Keep the original document, including folders, unknown metadata,
                    // attributes, comments and processing instructions, in place.
                    var existingGames = root.Elements("game").ToList();
                    var gameLookup = games.ToDictionary(g => g.Path, g => g, StringComparer.Ordinal);

                    foreach (var gameElement in existingGames)
                    {
                        var pathElement = gameElement.Element("path");
                        if (pathElement == null)
                        {
                            // Entries without a path cannot be matched safely. Preserve
                            // them rather than recreating the corresponding empty row.
                            savedItems.Add(string.Empty);
                            continue;
                        }

                        var pathValue = pathElement.Value;
                        if (!savedItems.Add(pathValue))
                        {
                            // Retain the existing duplicate-path cleanup behavior.
                            gameElement.Remove();
                            continue;
                        }

                        if (gameLookup.TryGetValue(pathValue, out var game))
                        {
                            UpdateGameElement(gameElement, game, persistedFields);
                        }
                        else
                            gameElement.Remove();
                    }
                }

                foreach (var game in games)
                {
                    if (!savedItems.Contains(game.Path))
                    {
                        root.Add(CreateGameElement(game, persistedFields));
                    }
                }

                NormalizeEditableWhitespace(root, layout);

                var fullPath = Path.GetFullPath(xmlFilePath);
                var directory = Path.GetDirectoryName(fullPath);
                if (string.IsNullOrEmpty(directory))
                    return false;

                // Use a unique file beside the destination so concurrent or failed
                // saves cannot reuse stale output and the final rename stays on the
                // same filesystem.
                temporaryFile = Path.Combine(
                    directory,
                    $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

                using (var stream = new FileStream(
                    temporaryFile,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.None))
                {
                    SaveDocument(doc, stream, layout);
                    stream.Flush(flushToDisk: true);
                }

                // Never replace the user's gamelist until the complete output can be
                // parsed again and contains exactly the path-bearing rows requested.
                if (!ValidateSavedGamelist(temporaryFile, games))
                    return false;

                File.Move(temporaryFile, fullPath, overwrite: true);
                temporaryFile = null;

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (temporaryFile != null)
                {
                    try
                    {
                        File.Delete(temporaryFile);
                    }
                    catch
                    {
                        // A failed cleanup must not hide the original save result.
                    }
                }
            }
        }

        private static bool ValidateSavedGamelist(string filePath, IEnumerable<GameMetadataRow> games)
        {
            var savedDocument = XDocument.Load(filePath);
            var root = savedDocument.Root;
            if (root == null || root.Name != "gameList")
                return false;

            var expectedPaths = games
                .Select(game => game.Path)
                .Where(path => !string.IsNullOrEmpty(path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();

            var savedPaths = root.Elements("game")
                .Select(game => game.Element("path")?.Value)
                .Where(path => !string.IsNullOrEmpty(path))
                .Select(path => path!)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();

            return expectedPaths.SequenceEqual(savedPaths, StringComparer.Ordinal);
        }

        private static XmlLayout GetXmlLayout(string xmlFilePath, XElement root, bool originalExists)
        {
            if (!originalExists)
                return new XmlLayout(Environment.NewLine, "\t", "\t\t", EmitUtf8Bom: false);

            string sourceText = File.ReadAllText(xmlFilePath);
            string newLine = DetectNewLine(sourceText);
            string rootIndent = FindExistingIndent(root) ?? "\t";
            string indentUnit = rootIndent.All(character => character == '\t')
                ? "\t"
                : new string(' ', Math.Max(1, rootIndent.Length));
            string childIndent = root.Elements("game")
                .Select(FindExistingIndent)
                .FirstOrDefault(indent => !string.IsNullOrEmpty(indent))
                ?? rootIndent + indentUnit;

            return new XmlLayout(newLine, rootIndent, childIndent, HasUtf8Bom(xmlFilePath));
        }

        private static string DetectNewLine(string text)
        {
            int lineFeed = text.IndexOf('\n');
            if (lineFeed >= 0)
                return lineFeed > 0 && text[lineFeed - 1] == '\r' ? "\r\n" : "\n";

            return text.Contains('\r') ? "\r" : Environment.NewLine;
        }

        private static string? FindExistingIndent(XElement parent)
        {
            foreach (var textNode in parent.Nodes().OfType<XText>())
            {
                if (!string.IsNullOrWhiteSpace(textNode.Value) || textNode.NextNode == null)
                    continue;

                int lineBreak = textNode.Value.LastIndexOfAny(['\r', '\n']);
                if (lineBreak < 0 || lineBreak == textNode.Value.Length - 1)
                    continue;

                string indent = textNode.Value[(lineBreak + 1)..];
                if (indent.All(char.IsWhiteSpace))
                    return indent;
            }

            return null;
        }

        private static bool HasUtf8Bom(string xmlFilePath)
        {
            Span<byte> prefix = stackalloc byte[3];
            using var stream = new FileStream(xmlFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return stream.Read(prefix) == prefix.Length &&
                   prefix[0] == 0xEF && prefix[1] == 0xBB && prefix[2] == 0xBF;
        }

        private static void NormalizeEditableWhitespace(XElement root, XmlLayout layout)
        {
            foreach (var gameElement in root.Elements("game").ToList())
                NormalizeElementWhitespace(gameElement, layout.NewLine, layout.ChildIndent, layout.RootIndent);

            NormalizeElementWhitespace(root, layout.NewLine, layout.RootIndent, string.Empty);
        }

        private static void NormalizeElementWhitespace(
            XElement element,
            string newLine,
            string childIndent,
            string closingIndent)
        {
            // The nearest xml:space declaration wins, including an explicit
            // default that overrides an ancestor's preserve instruction.
            var space = element.AncestorsAndSelf().Attributes(XNamespace.Xml + "space").FirstOrDefault();
            if (space?.Value == "preserve")
                return;

            var nodes = element.Nodes().ToList();

            // CDATA is content even when it contains only whitespace. Leave
            // all direct text in place when this element has mixed content.
            if (nodes.OfType<XText>().Any(node => node is XCData || !string.IsNullOrWhiteSpace(node.Value)))
                return;

            var contentNodes = nodes
                .Where(node => node is not XText text || !string.IsNullOrWhiteSpace(text.Value))
                .ToList();

            element.RemoveNodes();
            foreach (var node in contentNodes)
            {
                element.Add(new XText(newLine + childIndent));
                element.Add(node);
            }

            if (contentNodes.Count > 0)
                element.Add(new XText(newLine + closingIndent));
        }

        private static void SaveDocument(XDocument document, FileStream stream, XmlLayout layout)
        {
            var encoding = new UTF8Encoding(layout.EmitUtf8Bom, throwOnInvalidBytes: true);
            var writerSettings = new XmlWriterSettings
            {
                Encoding = encoding,
                Indent = false,
                NewLineChars = layout.NewLine,
                NewLineHandling = NewLineHandling.Replace,
                CloseOutput = false
            };

            using (var writer = XmlWriter.Create(stream, writerSettings))
                document.Save(writer);

            if (!EndsWithLineBreak(stream))
            {
                stream.Position = stream.Length;
                stream.Write(encoding.GetBytes(layout.NewLine));
            }
        }

        private static bool EndsWithLineBreak(FileStream stream)
        {
            if (stream.Length == 0)
                return false;

            stream.Position = stream.Length - 1;
            int lastByte = stream.ReadByte();
            return lastByte is (byte)'\r' or (byte)'\n';
        }

        private sealed record XmlLayout(
            string NewLine,
            string RootIndent,
            string ChildIndent,
            bool EmitUtf8Bom);

        private static void UpdateGameElement(XElement gameElement, GameMetadataRow game, IReadOnlyList<MetaDataDecl> persistedFields)
        {
            var gameId = game.GetValue(MetaDataKeys.id)?.ToString();
            if (string.IsNullOrEmpty(gameId))
                gameElement.Attribute("id")?.Remove();
            else
                gameElement.SetAttributeValue("id", gameId);

            foreach (var metaDecl in persistedFields)
            {
                if (metaDecl.Key == MetaDataKeys.id) continue;

                var elementName = metaDecl.Type;
                var value = game.GetValue(metaDecl.Key);
                var stringValue = GetStringValue(value, metaDecl);
                var element = gameElement.Element(elementName);

                if (element == null && !string.IsNullOrEmpty(stringValue))
                    gameElement.Add(new XElement(elementName, stringValue));
                else if (element != null && string.IsNullOrEmpty(stringValue))
                    element.Remove();
                else if (element != null && !string.IsNullOrEmpty(stringValue))
                    element.Value = stringValue;
            }
        }

        private static XElement CreateGameElement(GameMetadataRow game, IReadOnlyList<MetaDataDecl> persistedFields)
        {
            var gameElement = new XElement("game");

            var gameId = game.GetValue(MetaDataKeys.id)?.ToString();
            if (!string.IsNullOrEmpty(gameId))
                gameElement.SetAttributeValue("id", gameId);

            foreach (var metaDecl in persistedFields)
            {
                if (metaDecl.Key == MetaDataKeys.id) continue;

                var elementName = metaDecl.Type;
                var value = game.GetValue(metaDecl.Key);
                var stringValue = GetStringValue(value, metaDecl);

                if (!string.IsNullOrEmpty(stringValue))
                    gameElement.Add(new XElement(elementName, stringValue));
            }

            return gameElement;
        }

        private static string? GetStringValue(object? value, MetaDataDecl metaDecl)
        {
            if (value == null) return null;

            if (metaDecl.DataType == MetaDataType.Bool)
                return value is bool b && b ? "true" : null;

            var stringValue = value.ToString() ?? string.Empty;

            if (metaDecl.Key == MetaDataKeys.rating)
                stringValue = NormalizeRatingForXml(stringValue);
            else if (metaDecl.Key == MetaDataKeys.releasedate || metaDecl.Key == MetaDataKeys.lastplayed)
                stringValue = Iso8601Helper.ConvertToIso8601(stringValue);

            return string.IsNullOrWhiteSpace(stringValue) ? null : stringValue;
        }

        private static string NormalizeRatingForXml(string value)
        {
            if (!value.Contains(',') || value.Contains('.'))
                return value;

            string normalized = value.Replace(',', '.');
            if (decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal rating) &&
                rating >= 0m && rating <= 1m)
            {
                return normalized;
            }

            return value;
        }
    }
}
