using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml.Serialization;

namespace GamelistManager
{
    [XmlRoot("gameList")]
    public class GameList
    {
        [XmlElement("game")]
        public List<Game> Games { get; set; } = new List<Game>();
    }

    [XmlRoot("game")]
    public class Game
    {
        [XmlAttribute("id")]
        public string id { get; set; }
        public string path { get; set; }
        public string name { get; set; }
        public string desc { get; set; }
        public string hidden { get; set; }
        public string favorite { get; set; }
        public string image { get; set; }
        public string video { get; set; }
        public string marquee { get; set; }
        public string fanart { get; set; }
        public string arcadesystemname { get; set; }
        public string boxback { get; set; }
        public string thumbnail { get; set; }
        public string rating { get; set; }
        public string releasedate { get; set; }
        public string developer { get; set; }
        public string publisher { get; set; }
        public string genre { get; set; }
        public string players { get; set; }
        public string playcount { get; set; }
        public string lastplayed { get; set; }
        public string md5 { get; set; }
        public string gametime { get; set; }
        public string lang { get; set; }
        public string region { get; set; }
        public string genreid { get; set; }

        [XmlElement("scrap")]
        public List<Scrap> Scraps { get; set; } = new List<Scrap>();

        public static Game FromDataRow(DataRow row)
        {
            Game game = new Game
            {
                id = GetNonNullableStringValue(row, "id"),
                Scraps = GetScrapsFromRow(row)
            };

            foreach (DataColumn column in row.Table.Columns)
            {
                string propertyName = column.ColumnName.ToLower(); // Convert to lowercase
                string propertyValue = GetNonNullableStringValue(row, column.ColumnName);

                // Check if the property should be serialized
                if (!string.IsNullOrEmpty(propertyValue))
                {
                    game.GetType().GetProperty(propertyName)?.SetValue(game, propertyValue);
                }
            }

            return game;
        }

        private static List<Scrap> GetScrapsFromRow(DataRow row)
        {
            List<Scrap> scraps = new List<Scrap>();

            foreach (DataColumn column in row.Table.Columns)
            {
                if (column.ColumnName.StartsWith("scrap_"))
                {
                    string scrapName = column.ColumnName.Substring("scrap_".Length);
                    string scrapValue = GetNonNullableStringValue(row, column.ColumnName);
                    if (!string.IsNullOrEmpty(scrapValue))
                    {
                        scraps.Add(new Scrap { Name = scrapName, Date = scrapValue });
                    }
                }
            }

            return scraps;
        }

        private static string GetNonNullableStringValue(DataRow row, string columnName)
        {
            return row.Table.Columns.Contains(columnName) ? row[columnName]?.ToString() : string.Empty;
        }
    }

    public class Scrap
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("date")]
        public string Date { get; set; }
    }

    public static class GamelistUtility
    {
        public static void ExportDataSetToGameList(DataSet dataSet, string filename)
        {
            GameList gameList = new GameList();

            DataTable dataTable = dataSet.Tables["game"];

            foreach (DataRow row in dataTable.Rows)
            {
                gameList.Games.Add(Game.FromDataRow(row));
            }

            // Serialize GameList to XML
            SaveGameListToXml(gameList, filename);
        }

        public static void SaveGameListToXml(GameList gameList, string xmlFilePath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(GameList));
                using (StreamWriter writer = new StreamWriter(xmlFilePath))
                {
                    serializer.Serialize(writer, gameList);
                }

                Console.WriteLine($"GameList saved to {xmlFilePath} successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
