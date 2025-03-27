﻿using System.Data;
using System.IO;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace GamelistManager.classes
{
    public static class GamelistSaver
    {
        public static bool SaveGamelist(string xmlFilename)
        {

            // Create a map of XML element names to dataset column names

            List<string> savedItems = new List<string>();

            // Rewriting the new gamelist based off the original gamelist
            // This preserves structure and elements that are not managed or unknown
            try
            {
                string newfile = xmlFilename + ".tmp";
                using (XmlReader reader = XmlReader.Create(xmlFilename))
                using (XmlWriter writer = XmlWriter.Create(newfile, new XmlWriterSettings { Indent = true }))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("gameList"); // Start root element

                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "game")
                        {
                            // Read the <game> element as an XElement
                            XElement gameNode = ReadGameNode(reader);

                            if (gameNode == null)
                            {
                                continue;
                            }

                            // Get the 'path' attribute to match rows in the dataset
                            var pathValue = gameNode.Element("path")!.Value;

                            // Do not save duplicates!
                            if (savedItems.Contains(pathValue))
                            {
                                continue;
                            }

                            savedItems.Add(pathValue);

                            // Find the corresponding row in the dataset
                            DataRow row = SharedData.DataSet.Tables[0].AsEnumerable()
                            .FirstOrDefault(r => r.Field<string>("Rom Path") == pathValue)!;

                            if (row == null)
                            {
                                continue; // Skip if no matching row is found
                            }

                            var gameId = row["Game Id"];

                            if (gameId == DBNull.Value || string.IsNullOrEmpty(gameId.ToString()))
                            {
                                gameNode.Attribute("id")?.Remove();
                            }
                            else
                            {
                                gameNode.SetAttributeValue("id", gameId);
                            }

                            // Update the XML elements based on dataset values

                            foreach (var item in GamelistMetaData.NameToTypeMap)
                            {
                                string elementName = item.Key;
                                string columnName = item.Value;

                                // skip id, it's a game attribute, not an element
                                if (elementName == "id")
                                {
                                    continue;
                                }

                                string columnValue = row[columnName] == null || row[columnName] is DBNull
                                 ? string.Empty
                                 : row[columnName] is bool boolValue
                                     ? (boolValue ? "true" : string.Empty)
                                     : string.IsNullOrWhiteSpace(row[columnName]?.ToString())
                                         ? string.Empty
                                         : row[columnName]!.ToString()!;

                                bool elementExists = gameNode.Element(elementName) != null;

                                if (!elementExists && !string.IsNullOrEmpty(columnValue))
                                {
                                    gameNode.Add(new XElement(elementName, columnValue));
                                    continue;
                                }

                                if (elementExists && string.IsNullOrEmpty(columnValue))
                                {
                                    gameNode.Element(elementName)!.Remove();
                                    continue;
                                }

                                if (elementExists && !string.IsNullOrEmpty(columnValue))
                                {
                                    if (elementName == "releasedate")
                                    {
                                        columnValue = ISO8601Converter.ConvertToISO8601(columnValue);
                                    }
                                    gameNode.Element(elementName)!.Value = columnValue;
                                    continue;
                                }

                            }

                            // Write the updated <game> node to the new XML
                            gameNode.WriteTo(writer);
                        }
                    }

                    // Save new items now, if any
                    var newRows = from row in SharedData.DataSet.Tables[0].AsEnumerable()
                                  where !savedItems.Contains(row.Field<string>("Rom Path")!)
                                  select row;

                    foreach (var row in newRows)
                    {
                        // Start the <game> element
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
                            {
                                continue;
                            }

                            var datasetValue = row[columnName];

                            // Skip if the value is null or empty
                            if (datasetValue == DBNull.Value || datasetValue == null || string.IsNullOrEmpty(datasetValue.ToString()))
                            {
                                continue;
                            }

                            // Handle boolean values (only save 'true' booleans)
                            if (datasetValue is bool boolValue)
                            {
                                if (!boolValue) continue; // Skip 'false' boolean values
                                datasetValue = boolValue.ToString().ToLower();
                            }

                            // Add the element with its value
                            writer.WriteElementString(elementName, datasetValue.ToString());

                        }

                        // End the <game> element
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement(); // End root element
                    writer.WriteEndDocument();
                    reader.Close();
                    writer.Close();
                    File.Delete(xmlFilename);
                    File.Move(newfile, newfile.Replace(".tmp", string.Empty));
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;

        }


        // Method to read an entire <game> node and its attributes and children
        public static XElement ReadGameNode(XmlReader reader)
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "game")
            {
                XElement gameElement = new XElement("game");

                // Copy attributes
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        gameElement.SetAttributeValue(reader.Name, reader.Value);
                    }
                    reader.MoveToElement(); // Return to the <game> element
                }

                // Process inner elements within the <game> element
                while (reader.Read() && !(reader.NodeType == XmlNodeType.EndElement && reader.Name == "game"))
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        XElement childElement = new XElement(reader.Name);

                        // Copy attributes of child elements
                        if (reader.HasAttributes)
                        {
                            while (reader.MoveToNextAttribute())
                            {
                                childElement.SetAttributeValue(reader.Name, reader.Value);
                            }
                            reader.MoveToElement(); // Move back to the element
                        }

                        // Read inner content if the element is not empty
                        if (!reader.IsEmptyElement)
                        {
                            string elementValue = reader.ReadInnerXml();
                            childElement.Value = elementValue;
                        }

                        gameElement.Add(childElement); // Add the child element to <game>
                    }
                }

                return gameElement;
            }

            return null!; // Return null if not positioned at a <game> element
        }

        public static void BackupGamelist(string system, string fileName)
        {
            string backupDirectory = $"{SharedData.ProgramDirectory}\\gamelist backup\\{system}";
            if (!Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            // Get the current date and time in the format "yyyyMMdd_HHmmss"
            string dateTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Create the backup filename with the date and time appended
            string backupFileName = $"{fileNameWithoutExtension}_{dateTime}.xml";

            // Combine the directory and backup filename to create the full backup file path
            string backupFilePath = Path.Combine(backupDirectory, backupFileName);
            File.Copy(SharedData.XMLFilename, backupFilePath);
        }


        // Load the original XML document
        private static XDocument LoadXmlDocument(string xmlFilename)
        {
            return XDocument.Load(xmlFilename);
        }
    }
}
