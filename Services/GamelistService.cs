using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Gamelist_Manager.Services
{
    public class GamelistService
    {
        private static readonly Lazy<IReadOnlyDictionary<MetaDataKeys, MetaDataDecl>> s_metaDataDict =
            new(() => GamelistMetaData.GetMetaDataDictionary());

        public static (ObservableCollection<GameMetadataRow> Games, List<string> Duplicates) LoadGamelist(string xmlFilePath, bool ignoreDuplicates = false)
        {
            var games = new ObservableCollection<GameMetadataRow>();
            var duplicates = new List<string>();

            XDocument xmlDoc;
            try
            {
                xmlDoc = XDocument.Load(xmlFilePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load gamelist: {ex.Message}");
                return (games, duplicates);
            }

            var gameElements = xmlDoc.Descendants("game");
            if (!gameElements.Any())
                return (games, duplicates);

            var metaDataDict = s_metaDataDict.Value;
            var parentFolderPath = Path.GetDirectoryName(xmlFilePath);

            var uniqueRomPaths = new HashSet<string>(FilePathHelper.PathComparer);

            foreach (var gameElement in gameElements)
            {
                var game = new GameMetadataRow();

                // Pre-initialize all bool fields to false so missing XML elements default correctly
                foreach (var metaDecl in metaDataDict.Values.Where(d => d.DataType == MetaDataType.Bool))
                    game.SetValue(metaDecl.Key, false);

                // Parse all metadata fields
                foreach (var metaDecl in metaDataDict.Values.Where(d => d.Viewable))
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
                            string formattedDate = Iso8601Helper.ConvertFromIso8601(value);
                            game.SetValue(metaDecl.Key, !string.IsNullOrEmpty(formattedDate) ? formattedDate : value);
                        }
                        else
                        {
                            game.SetValue(metaDecl.Key, value);
                        }
                    }
                }

                // id is an XML attribute on <game>, not a child element
                var gameIdAttr = gameElement.Attribute("id")?.Value;
                if (!string.IsNullOrEmpty(gameIdAttr))
                    game.SetValue(MetaDataKeys.id, gameIdAttr);

                var normalizedFilePath = FilePathHelper.GamelistPathToFullPath(game.Path, parentFolderPath!);

                if (!string.IsNullOrEmpty(normalizedFilePath))
                {
                    if (!uniqueRomPaths.Add(normalizedFilePath))
                    {
                        duplicates.Add(normalizedFilePath);
                        if (ignoreDuplicates)
                            continue;
                    }
                }

                games.Add(game);
            }

            return (games, duplicates);
        }

        public static bool SaveGamelist(string xmlFilePath, ObservableCollection<GameMetadataRow> games)
        {
            try
            {
                var originalExists = File.Exists(xmlFilePath);

                var savedItems = new HashSet<string>(StringComparer.Ordinal);
                var newFile = xmlFilePath + ".tmp";

                var root = new XElement("gameList");

                // If original exists, preserve order and update existing entries
                if (originalExists)
                {
                    var existingDoc = XDocument.Load(xmlFilePath);
                    var existingGames = existingDoc.Descendants("game").ToList();

                    var gameLookup = games.ToDictionary(g => g.Path, g => g, StringComparer.Ordinal);

                    foreach (var gameElement in existingGames)
                    {
                        var pathElement = gameElement.Element("path");
                        if (pathElement == null)
                            continue;

                        var pathValue = pathElement.Value;

                        if (!savedItems.Add(pathValue))
                            continue; // Duplicate, skip

                        if (gameLookup.TryGetValue(pathValue, out var game))
                        {
                            // Update existing game node
                            UpdateGameElement(gameElement, game);
                            root.Add(gameElement);
                        }
                    }
                }

                // Add new games not in original file
                foreach (var game in games)
                {
                    if (!savedItems.Contains(game.Path))
                    {
                        var gameElement = CreateGameElement(game);
                        root.Add(gameElement);
                    }
                }

                // Save to temp file then replace atomically
                var doc = new XDocument(root);
                doc.Save(newFile);

                File.Move(newFile, xmlFilePath, overwrite: true);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save gamelist: {ex.Message}");
                return false;
            }
        }

        private static void UpdateGameElement(XElement gameElement, GameMetadataRow game)
        {
            var metaDataDict = s_metaDataDict.Value;

            // Update id attribute
            var gameId = game.GetValue(MetaDataKeys.id)?.ToString();
            if (string.IsNullOrEmpty(gameId))
            {
                gameElement.Attribute("id")?.Remove();
            }
            else
            {
                gameElement.SetAttributeValue("id", gameId);
            }

            // Update elements — id is saved as an attribute above, not a child element.
            // In ES-DE mode, skip media fields — they are populated at load time from the
            // filesystem and must not be written back into the XML.
            var isEsDe = SharedDataService.Instance.IsEsDeMode;
            foreach (var metaDecl in metaDataDict.Values.Where(d => d.Viewable))
            {
                if (metaDecl.Key == MetaDataKeys.id) continue;
                if (isEsDe && metaDecl.IsMedia) continue;

                var elementName = metaDecl.Type;
                var value = game.GetValue(metaDecl.Key);
                var stringValue = GetStringValue(value, metaDecl);

                var element = gameElement.Element(elementName);

                if (element == null && !string.IsNullOrEmpty(stringValue))
                {
                    // Add new element
                    gameElement.Add(new XElement(elementName, stringValue));
                }
                else if (element != null && string.IsNullOrEmpty(stringValue))
                {
                    // Remove element
                    element.Remove();
                }
                else if (element != null && !string.IsNullOrEmpty(stringValue))
                {
                    // Update element
                    element.Value = stringValue;
                }
            }
        }

        private static XElement CreateGameElement(GameMetadataRow game)
        {
            var gameElement = new XElement("game");
            var metaDataDict = s_metaDataDict.Value;

            // Add id attribute if exists
            var gameId = game.GetValue(MetaDataKeys.id)?.ToString();
            if (!string.IsNullOrEmpty(gameId))
            {
                gameElement.SetAttributeValue("id", gameId);
            }

            // Add all metadata elements — id is saved as an attribute above, not a child element.
            // In ES-DE mode, skip media fields (runtime-only, resolved from filesystem at load time).
            var isEsDe = SharedDataService.Instance.IsEsDeMode;
            foreach (var metaDecl in metaDataDict.Values.Where(d => d.Viewable))
            {
                if (metaDecl.Key == MetaDataKeys.id) continue;
                if (isEsDe && metaDecl.IsMedia) continue;

                var elementName = metaDecl.Type;
                var value = game.GetValue(metaDecl.Key);
                var stringValue = GetStringValue(value, metaDecl);

                if (!string.IsNullOrEmpty(stringValue))
                {
                    gameElement.Add(new XElement(elementName, stringValue));
                }
            }

            return gameElement;
        }

        private static string? GetStringValue(object? value, MetaDataDecl metaDecl)
        {
            if (value == null)
                return null;

            if (metaDecl.DataType == MetaDataType.Bool)
            {
                return value is bool b && b ? "true" : null;
            }

            var stringValue = value.ToString() ?? string.Empty;

            // Date conversions
            if (metaDecl.Key == MetaDataKeys.releasedate || metaDecl.Key == MetaDataKeys.lastplayed)
            {
                stringValue = Iso8601Helper.ConvertToIso8601(stringValue);
            }

            return string.IsNullOrWhiteSpace(stringValue) ? null : stringValue;
        }

        #region ES-DE Helpers

        private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg"];
        private static readonly string[] VideoExtensions = [".mp4", ".avi", ".mkv"];
        private static readonly string[] ManualExtensions = [".pdf"];

        // Resolves ES-DE media paths from the filesystem and writes them into the row values so
        // all downstream consumers (grid, preview, statistics) can read them without branching.
        internal static void PopulateMediaPaths(IList<GameMetadataRow> games, string mediaDirectory)
        {
            if (string.IsNullOrEmpty(mediaDirectory)) return;

            var mediaDecls = GamelistMetaData.GetAllMediaFolderTypes();

            foreach (var game in games)
            {
                var romPath = game.Path;
                if (string.IsNullOrEmpty(romPath)) continue;

                var romName = FilePathHelper.NormalizeRomName(romPath);
                if (string.IsNullOrEmpty(romName)) continue;

                foreach (var decl in mediaDecls)
                {
                    if (!decl.IsEsDeSupported) continue;

                    var folder = Path.Combine(mediaDirectory, decl.EsDeFolderName);

                    var extensions = decl.DataType switch
                    {
                        MetaDataType.Video => VideoExtensions,
                        MetaDataType.Document => ManualExtensions,
                        _ => ImageExtensions
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

        internal static string? ReadRomDirectoryFromEsDeSettings(string esDeRoot)
        {
            if (string.IsNullOrWhiteSpace(esDeRoot)) return null;

            var settingsPath = Path.Combine(esDeRoot, "settings", "es_settings.xml");
            if (File.Exists(settingsPath))
            {
                try
                {
                    var doc = XDocument.Load(settingsPath);
                    var romDir = doc.Descendants("string")
                        .FirstOrDefault(e => string.Equals(e.Attribute("name")?.Value, "ROMDirectory", StringComparison.Ordinal))
                        ?.Attribute("value")?.Value;

                    if (!string.IsNullOrWhiteSpace(romDir))
                    {
                        var trimmed = Path.TrimEndingDirectorySeparator(romDir);
                        if (Directory.Exists(trimmed))
                            return trimmed;
                    }
                }
                catch { }
            }

            // Default location is one level up from the ES-DE root, in a folder named ROMS
            var parentDir = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(esDeRoot));
            if (parentDir != null)
            {
                var fallback = Path.Combine(parentDir, "ROMS");
                if (Directory.Exists(fallback))
                    return fallback;
            }

            return null;
        }

        #endregion

    }
}
