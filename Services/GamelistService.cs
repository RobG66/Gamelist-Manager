using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
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
            try
            {
                var originalExists = File.Exists(xmlFilePath);
                var savedItems = new HashSet<string>(StringComparer.Ordinal);
                var newFile = xmlFilePath + ".tmp";
                var doc = originalExists
                    ? XDocument.Load(xmlFilePath, LoadOptions.PreserveWhitespace)
                    : new XDocument(new XElement("gameList"));
                var root = doc.Root;
                if (root == null || root.Name != "gameList")
                    return false;

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

                doc.Save(newFile, originalExists ? SaveOptions.DisableFormatting : SaveOptions.None);
                File.Move(newFile, xmlFilePath, overwrite: true);

                return true;
            }
            catch
            {
                return false;
            }
        }

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
