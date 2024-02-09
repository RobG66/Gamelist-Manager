﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GamelistManager
{
    public class ScrapeArcadeDB
    {
        ScraperForm scraperForm = new ScraperForm();
        GamelistManagerForm gamelistManagerForm = new GamelistManagerForm();

        public ScrapeArcadeDB(ScraperForm scraperForm)
        {
            this.scraperForm = scraperForm ?? throw new ArgumentNullException(nameof(scraperForm));
            this.gamelistManagerForm = gamelistManagerForm ?? throw new ArgumentNullException(nameof(gamelistManagerForm));
        }

        public async
        Task
        ScrapeArcadeDBAsync(string XMLFilename, DataSet dataSet, bool overWriteData, List<string> elementsToScrape, List<string> romPaths, CancellationToken cancellationToken)
        {
            int total = romPaths.Count;
            int count = 0;


            Dictionary<string, string> Metadata = new Dictionary<string, string>();
            Metadata.Add("name", "title");
            Metadata.Add("desc", "history");
            Metadata.Add("genre", "genre");
            Metadata.Add("releasedate", "year");
            Metadata.Add("players", "players");
            Metadata.Add("rating", "rate");
            Metadata.Add("lang", "languages");
            Metadata.Add("publisher", "manufacturer");
            Metadata.Add("marquee", "url_image_marquee");
            Metadata.Add("image", "url_image_ingame");
            Metadata.Add("video", "url_video_shortplay_hd");

            int batchSize = 50;

            string scraperBaseURL = "http://adb.arcadeitalia.net/service_scraper.php?ajax=query_mame&game_name=";
            string parentFolderPath = Path.GetDirectoryName(XMLFilename);

            // Start to process selected datagridview rows
            for (int i = 0; i < romPaths.Count; i += batchSize)
            {
                // Take the next batch of roms
                string[] batchArray = romPaths.Skip(i).Take(batchSize).ToArray();

                // Construct a semicolon-separated string of ROM names for the current batch
                string joinedRomNames = string.Join(";", batchArray.Select(path => gamelistManagerForm.ExtractFileNameNoExtension(path)));

                // Construct the scraper URL with the batch of ROM names
                string scraperRequestURL = $"{scraperBaseURL}{(joinedRomNames)}";

                // Get the JSON response from the website
                string jsonResponse = await GetJsonResponseAsync(scraperRequestURL, cancellationToken);

                if (jsonResponse == null)
                {
                    scraperForm.AddToLog("Scraper returned no data!");
                    count += batchArray.Length;
                    continue;
                }

                // Deserialize the JSON to names and values
                ScrapeArcadeDBResponse deserializedJSON = null;
                deserializedJSON = await DeserializeJsonAsync<ScrapeArcadeDBResponse>(jsonResponse, cancellationToken);

                if (deserializedJSON == null)
                {
                    scraperForm.AddToLog("JSON deserialization failed!");
                    count += batchArray.Length;
                    continue;
                }

                // Loop through the returned data and process it
                for (int j = 0; j < batchArray.Length; j++)
                {
                    count++;
                    scraperForm.UpdateProgressBar();
                    scraperForm.UpdateLabel(count, total);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        // Perform cleanup or handle cancellation if needed
                        scraperForm.AddToLog("Scraping canceled by user.");
                        return;
                    }

                    string currentRomPath = batchArray[j];
                    string currentRomName = gamelistManagerForm.ExtractFileNameNoExtension(currentRomPath);

                    ScrapeArcadeDBItem scraperData = deserializedJSON.result[j];

                    scraperForm.AddToLog($"Scraping item {currentRomName}");

                    // Loop through the Metadata dictionary
                    foreach (var kvp in Metadata)
                    {
                        string localPropertyName = kvp.Key;
                        string remotePropertyName = kvp.Value;
                        string fullRomPath = Path.Combine(parentFolderPath, currentRomPath.Replace("./", "").Replace("/", Path.DirectorySeparatorChar.ToString()));

                        if (!File.Exists(fullRomPath))
                        {
                            scraperForm.AddToLog($"File {fullRomPath} is missing!");
                            continue;
                        }

                        if (!elementsToScrape.Contains(localPropertyName))
                        {
                            continue;
                        }

                        // Get the returned property value from its string name
                        PropertyInfo property = typeof(ScrapeArcadeDBItem).GetProperty(remotePropertyName);
                        object propertyValue = property.GetValue(scraperData);
                        string scrapedValue = propertyValue != null ? propertyValue.ToString() : null;

                        // Find the datarow we want to work with
                        DataRow tableRow = dataSet.Tables[0].AsEnumerable()
                            .FirstOrDefault(row => row.Field<string>("path") == currentRomPath);

                        // Generate MD5
                        string md5 = null;
                        md5 = ChecksumCreator.CreateMD5(fullRomPath);
                        tableRow["md5"] = md5;

                        string columnName = localPropertyName;
                        object cellValueObject = tableRow[columnName];
                        string cellValueString = (cellValueObject != null) ? cellValueObject.ToString() : string.Empty;

                        // Check for empty scrape value
                        if (string.IsNullOrEmpty(scrapedValue))
                        {
                            continue;
                        }

                        // Fix rating
                        if (columnName == "rating")
                        {
                            string convertedRating = null;

                            if (scrapedValue == "100")
                            {
                                convertedRating = "1";
                            }

                            if (scrapedValue == "0")
                            {
                                // Don't even bother with a zero rating
                                return;
                            }

                            if (convertedRating == null)
                            {
                                scrapedValue = "." + scrapedValue.TrimStart('0');
                            }
                            else
                            {
                                scrapedValue = convertedRating;
                            }
                        }

                        // If it's not an image property, we stop here
                        if (!remotePropertyName.Contains("url_"))
                        {
                            // Check if we are allowed to overwrite
                            // Write regardless if the local value is empty
                            if ((!overWriteData && string.IsNullOrEmpty(cellValueString)) || overWriteData)
                            {
                                tableRow[columnName] = scrapedValue;
                            }
                            continue;
                        }


                        // Image URL handling
                        string remoteDownloadURL = scrapedValue;

                        if (string.IsNullOrEmpty(remoteDownloadURL))
                        {
                            continue;
                        }

                        // What are we downloading, a video or image?                                     
                        string fileName = null;
                        string folderName = null;

                        if (localPropertyName == "video")
                        {
                            folderName = "videos";
                            fileName = $"{currentRomName}-video.mp4";
                        }
                        else
                        {
                            folderName = "images";
                            fileName = $"{currentRomName}-{localPropertyName}.png";
                        }

                        // Setup the download
                        string downloadPath = $"{parentFolderPath}\\{folderName}";
                        string fileToDownload = $"{downloadPath}\\{fileName}";
                        bool result = await FileTransfer.DownloadFile(overWriteData, fileToDownload, remoteDownloadURL);
                        // Returns true on success
                        if (result == true)
                        {
                            scraperForm.AddToLog($"Downloaded {fileName}");
                            tableRow[columnName] = $"./{folderName}/{fileName}";
                        }
                        else
                        {
                            scraperForm.AddToLog($"Failed to download {fileName}");
                        }
                    }
                }
            }
        }

        private async Task<string> GetJsonResponseAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] responseBytes = await client.GetByteArrayAsync(url);
                    return Encoding.UTF8.GetString(responseBytes);
                }
            }
            catch
            {
                return null;
            }
        }

        private async Task<T> DeserializeJsonAsync<T>(string json, CancellationToken cancellationToken)
        {
            try
            {
                return await Task.Run(() => JsonConvert.DeserializeObject<T>(json));
            }
            catch
            {
                return default;
            }
        }


        public class ScrapeArcadeDBItem
        {
            public int index { get; set; }
            public string url { get; set; }
            public string game_name { get; set; }
            public string title { get; set; }
            public string cloneof { get; set; }
            public string manufacturer { get; set; }
            public string url_image_ingame { get; set; }
            public string url_image_title { get; set; }
            public string url_image_marquee { get; set; }
            public string url_image_cabinet { get; set; }
            public string url_image_flyer { get; set; }
            public string url_icon { get; set; }
            public string genre { get; set; }
            public int players { get; set; }
            public string year { get; set; }
            public string status { get; set; }
            public string history { get; set; }
            public string history_copyright_short { get; set; }
            public string history_copyright_long { get; set; }
            public string youtube_video_id { get; set; }
            public string url_video_shortplay { get; set; }
            public string url_video_shortplay_hd { get; set; }
            public int emulator_id { get; set; }
            public string emulator_name { get; set; }
            public string languages { get; set; }
            public int rate { get; set; }
            public string short_title { get; set; }
            public string nplayers { get; set; }
            public string input_controls { get; set; }
            public int input_buttons { get; set; }
            public string buttons_colors { get; set; }
            public string serie { get; set; }
            public string screen_orientation { get; set; }
            public string screen_resolution { get; set; }
        }


        public class ScrapeArcadeDBResponse
        {
            public List<ScrapeArcadeDBItem> result { get; set; }
        }

    }

}
