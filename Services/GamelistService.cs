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

            var uniqueRomPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var gameElement in gameElements)
            {
                var game = new GameMetadataRow();

                foreach (var metaDecl in metaDataDict.Values.Where(d => d.DataType == MetaDataType.Bool))
                    game.SetValue(metaDecl.Key, false);

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

        public static bool SaveGamelist(string xmlFilePath, ObservableCollection<GameMetadataRow> games)
        {
            try
            {
                var originalExists = File.Exists(xmlFilePath);
                var savedItems = new HashSet<string>(StringComparer.Ordinal);
                var newFile = xmlFilePath + ".tmp";
                var root = new XElement("gameList");

                if (originalExists)
                {
                    var existingDoc = XDocument.Load(xmlFilePath);
                    var existingGames = existingDoc.Descendants("game").ToList();
                    var gameLookup = games.ToDictionary(g => g.Path, g => g, StringComparer.Ordinal);

                    foreach (var gameElement in existingGames)
                    {
                        var pathElement = gameElement.Element("path");
                        if (pathElement == null) continue;

                        var pathValue = pathElement.Value;
                        if (!savedItems.Add(pathValue)) continue;

                        if (gameLookup.TryGetValue(pathValue, out var game))
                        {
                            UpdateGameElement(gameElement, game);
                            root.Add(gameElement);
                        }
                    }
                }

                foreach (var game in games)
                {
                    if (!savedItems.Contains(game.Path))
                    {
                        root.Add(CreateGameElement(game));
                    }
                }

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
            var gameId = game.GetValue(MetaDataKeys.id)?.ToString();
            if (string.IsNullOrEmpty(gameId))
                gameElement.Attribute("id")?.Remove();
            else
                gameElement.SetAttributeValue("id", gameId);

            foreach (var metaDecl in SessionState.Instance.XmlPersistedFields)
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

        private static XElement CreateGameElement(GameMetadataRow game)
        {
            var gameElement = new XElement("game");

            var gameId = game.GetValue(MetaDataKeys.id)?.ToString();
            if (!string.IsNullOrEmpty(gameId))
                gameElement.SetAttributeValue("id", gameId);

            foreach (var metaDecl in SessionState.Instance.XmlPersistedFields)
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

            if (metaDecl.Key == MetaDataKeys.releasedate || metaDecl.Key == MetaDataKeys.lastplayed)
                stringValue = Iso8601Helper.ConvertToIso8601(stringValue);

            return string.IsNullOrWhiteSpace(stringValue) ? null : stringValue;
        }
    }
}