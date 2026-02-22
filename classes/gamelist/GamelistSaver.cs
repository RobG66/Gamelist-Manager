using GamelistManager.classes.core;
using GamelistManager.classes.helpers;
using System.Data;
using System.IO;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace GamelistManager.classes.gamelist
{
    public static class GamelistSaver
    {
        public static bool SaveGamelist(string xmlFilename)
        {
            try
            {
                bool originalExists = File.Exists(xmlFilename);

                // Build lookup dictionary once for O(1) access instead of O(n) search per game
                var rowLookup = SharedData.DataSet.Tables[0].AsEnumerable()
                    .ToDictionary(r => r.Field<string>("Rom Path")!, r => r, StringComparer.Ordinal);

                // Track saved items with HashSet for O(1) lookups instead of List
                var savedItems = new HashSet<string>(StringComparer.Ordinal);

                string newfile = xmlFilename + ".tmp";

                var writerSettings = new XmlWriterSettings
                {
                    Indent = true,
                    CloseOutput = true
                };

                using (XmlWriter writer = XmlWriter.Create(newfile, writerSettings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("gameList");

                    if (originalExists)
                    {
                        var readerSettings = new XmlReaderSettings
                        {
                            IgnoreWhitespace = true,
                            IgnoreComments = true
                        };

                        using (XmlReader reader = XmlReader.Create(xmlFilename, readerSettings))
                        {
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.Element && reader.Name == "game")
                                {
                                    XElement gameNode = ReadGameNode(reader);

                                    if (gameNode == null)
                                        continue;

                                    var pathElement = gameNode.Element("path");
                                    if (pathElement == null)
                                        continue;

                                    string pathValue = pathElement.Value;

                                    // HashSet lookup is O(1)
                                    if (!savedItems.Add(pathValue))
                                        continue; // Duplicate, skip

                                    // Dictionary lookup is O(1) instead of FirstOrDefault which is O(n)
                                    if (!rowLookup.TryGetValue(pathValue, out DataRow? row))
                                        continue;

                                    UpdateGameNode(gameNode, row);
                                    gameNode.WriteTo(writer);
                                }
                            }
                        }
                    }

                    // Save new items - already have rowLookup, just filter
                    foreach (var kvp in rowLookup)
                    {
                        if (!savedItems.Contains(kvp.Key))
                        {
                            WriteNewGameNode(writer, kvp.Value);
                        }
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }

                // Atomic file replacement
                if (File.Exists(xmlFilename))
                    File.Delete(xmlFilename);
                File.Move(newfile, xmlFilename);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private static void UpdateGameNode(XElement gameNode, DataRow row)
        {
            // Update id attribute
            var gameId = row["Game Id"];
            if (gameId == DBNull.Value || string.IsNullOrEmpty(gameId.ToString()))
            {
                gameNode.Attribute("id")?.Remove();
            }
            else
            {
                gameNode.SetAttributeValue("id", gameId);
            }

            // Update elements
            foreach (var item in GamelistMetaData.NameToTypeMap)
            {
                string elementName = item.Key;
                string columnName = item.Value;

                if (elementName == "id")
                    continue;

                string columnValue = GetColumnValue(row, columnName);
                XElement? element = gameNode.Element(elementName);
                bool elementExists = element != null;

                if (!elementExists && !string.IsNullOrEmpty(columnValue))
                {
                    // Add new element
                    if (elementName == "releasedate" || elementName == "lastplayed")
                        columnValue = ISO8601Helper.ConvertToISO8601(columnValue);

                    gameNode.Add(new XElement(elementName, columnValue));
                }
                else if (elementExists && string.IsNullOrEmpty(columnValue))
                {
                    // Remove element
                    element!.Remove();
                }
                else if (elementExists && !string.IsNullOrEmpty(columnValue))
                {
                    // Update element
                    if (elementName == "releasedate" || elementName == "lastplayed")
                        columnValue = ISO8601Helper.ConvertToISO8601(columnValue);

                    element!.Value = columnValue;
                }
            }
        }

        private static void WriteNewGameNode(XmlWriter writer, DataRow row)
        {
            writer.WriteStartElement("game");

            // Add 'id' if it exists
            var gameId = row["Game Id"];
            if (gameId != DBNull.Value && !string.IsNullOrEmpty(gameId.ToString()))
            {
                writer.WriteAttributeString("id", gameId.ToString());
            }

            foreach (var item in GamelistMetaData.NameToTypeMap)
            {
                string elementName = item.Key;
                string columnName = item.Value;

                if (elementName == "id")
                    continue;

                string columnValue = GetColumnValue(row, columnName);

                if (string.IsNullOrEmpty(columnValue))
                    continue;

                if (elementName == "releasedate" || elementName == "lastplayed")
                    columnValue = ISO8601Helper.ConvertToISO8601(columnValue);

                writer.WriteElementString(elementName, columnValue);
            }

            writer.WriteEndElement();
        }

        private static string GetColumnValue(DataRow row, string columnName)
        {
            var datasetValue = row[columnName];

            if (datasetValue == DBNull.Value || datasetValue == null)
                return string.Empty;

            if (datasetValue is bool boolValue)
                return boolValue ? "true" : string.Empty;

            string stringValue = datasetValue.ToString()!;
            return string.IsNullOrWhiteSpace(stringValue) ? string.Empty : stringValue;
        }

        public static XElement? ReadGameNode(XmlReader reader)
        {
            if (reader.NodeType != XmlNodeType.Element || reader.Name != "game")
                return null;

            // Use XNode.ReadFrom to properly parse the entire subtree
            XElement gameElement = (XElement)XNode.ReadFrom(reader);
            return gameElement;
        }

        public static void BackupGamelist(string system, string fileName)
        {
            if (string.IsNullOrEmpty(SharedData.XMLFilename) || !File.Exists(SharedData.XMLFilename))
                return;

            string backupDirectory = $"{SharedData.ProgramDirectory}\\gamelist backup\\{system}";
            if (!Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string dateTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFileName = $"{fileNameWithoutExtension}_{dateTime}.xml";
            string backupFilePath = Path.Combine(backupDirectory, backupFileName);

            File.Copy(SharedData.XMLFilename, backupFilePath);
        }
    }
}