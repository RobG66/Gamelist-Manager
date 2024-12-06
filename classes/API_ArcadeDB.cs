using System.Data;
using System.IO;
using System.Text.Json;

namespace GamelistManager.classes
{
    internal class API_ArcadeDB
    {
        private readonly string apiURL = "http://adb.arcadeitalia.net/service_scraper.php";

        public async Task<string> ScrapeGame(string romName)
        {
            string url = $"{apiURL}?ajax=query_mame&game_name={romName}";
            
            try
            {
                // Fetch the JSON response using the GetJsonResponse class
                string jsonResponse = await GetJSONResponse.GetJsonResponseAsync(string.Empty, url);
                return jsonResponse;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string GetJsonElementValue(string jsonString, string elementName)
        {
            if (jsonString == null || elementName == null)
            {
                return string.Empty;
            }

            using (JsonDocument document = JsonDocument.Parse(jsonString))
            {
                // Check if the root element is an array or object
                if (document.RootElement.ValueKind == JsonValueKind.Array || document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var elementValue = document.RootElement
                        .GetProperty("result")
                        .EnumerateArray()
                        .Where(game => game.TryGetProperty(elementName, out JsonElement element) && element.ValueKind != JsonValueKind.Null)
                        .Select(game => game.GetProperty(elementName).ToString())
                        .FirstOrDefault();

                    // Return the value or an empty string if not found
                    return elementValue ?? string.Empty;
                }
                // Return empty string if the root element is not an array or object
                return string.Empty;
            }
        }

        public void SaveJsonStringToCacheFile(string jsonString, string cacheFolder, string romName)
        {
            if (!Directory.Exists(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }
            string cacheFilePath = Path.Combine(cacheFolder, $"{romName}.json");
            File.WriteAllText(cacheFilePath, jsonString);
        }

        private string ReadJsonFromCache(string romName, string cacheFolder)
        {
            if (!Directory.Exists(cacheFolder))
            {
                return string.Empty;
            }

            string cacheFilePath = Path.Combine(cacheFolder, $"{romName}.json");

            if (!Path.Exists(cacheFilePath))
            {
                return string.Empty;
            }

            string jsonString = File.ReadAllText(cacheFilePath);
            return jsonString;
        }

        public async Task<bool> ScrapeArcadeDBAsync(DataRowView rowView, ScraperParameters scraperParameters)
        {
          
            string romPath = rowView["Rom Path"].ToString()!;
            string romName = Path.GetFileNameWithoutExtension(romPath);
            string gameName = rowView["Name"]?.ToString()!; // Never empty
          
            string cacheFolder = scraperParameters.CacheFolder!;
            bool scrapeByCache = scraperParameters.ScrapeByCache;
            bool skipNonCached = scraperParameters.SkipNonCached;
            string jsonResponse = string.Empty;

            // Scrape by cache if selected
            if (scrapeByCache)
            {
                jsonResponse = ReadJsonFromCache(romName, cacheFolder);
                if (string.IsNullOrEmpty(jsonResponse) && skipNonCached)
                {
                    return false;
                }
            }

            // Scrape from website or if there was no cache file
            if (string.IsNullOrEmpty(jsonResponse))
            {
                jsonResponse = await ScrapeGame(romName);

                if (string.IsNullOrEmpty(jsonResponse))
                {
                    return false;
                }

                // Save json response to cache
                SaveJsonStringToCacheFile(jsonResponse, cacheFolder, romName);
            }

            var elementsToScrape = scraperParameters.ElementsToScrape!;
            bool overwriteMetaData = scraperParameters.OverwriteMetadata;
            string downloadURL = string.Empty;

            foreach (var element in elementsToScrape)
            {
                switch (element)
                {
                    case "publisher":
                        string publisher = GetJsonElementValue(jsonResponse, "manufacturer");
                        UpdateMetadata(rowView, "Publisher", publisher, overwriteMetaData);
                        break;

                    case "players":
                        string players = GetJsonElementValue(jsonResponse, "players");
                        UpdateMetadata(rowView, "Players", players, overwriteMetaData);
                        break;

                    case "rating":
                        string rating = GetJsonElementValue(jsonResponse, "rate");
                        if (int.TryParse(rating, out int parseResult))
                        {
                            rating = parseResult == 100
                                ? "1"
                                : parseResult > 0 && parseResult < 100
                                    ? "." + parseResult.ToString().TrimStart('0')
                                    : rating;
                            UpdateMetadata(rowView, "Rating", rating, overwriteMetaData);
                        }
                        break;

                    case "desc":
                        string description = GetJsonElementValue(jsonResponse, "history");
                        UpdateMetadata(rowView, "Description", description, overwriteMetaData);
                        break;

                    case "name":
                        string name = GetJsonElementValue(jsonResponse, "title");
                        UpdateMetadata(rowView, "Name", name, overwriteMetaData);
                        break;

                    case "genre":
                        string genre = GetJsonElementValue(jsonResponse, "genre");
                        UpdateMetadata(rowView, "Genre", genre, overwriteMetaData);
                        break;

                    case "releasedate":
                        string releasedate = GetJsonElementValue(jsonResponse, "year");
                        UpdateMetadata(rowView, "Release Date", releasedate, overwriteMetaData);
                        break;

                    case "image":
                        downloadURL = GetJsonElementValue(jsonResponse, scraperParameters.ImageSource!);
                        await DownloadFile(downloadURL, rowView, romName, "image", "Image", scraperParameters);
                        break;

                    case "thumbnail":
                        downloadURL = GetJsonElementValue(jsonResponse, scraperParameters.BoxSource!);
                        await DownloadFile(downloadURL, rowView, romName, "thumbnail", "Box", scraperParameters);
                        break;

                    case "bezel":
                        downloadURL = GetJsonElementValue(jsonResponse, "url_image_bezel");
                        await DownloadFile(downloadURL, rowView, romName, "bezel", "Bezel", scraperParameters);
                        break;

                    case "manual":
                        downloadURL = GetJsonElementValue(jsonResponse, "url_manual");
                        await DownloadFile(downloadURL, rowView, romName, "manual", "Manual", scraperParameters);
                        break;

                    case "titleshot":
                        downloadURL = GetJsonElementValue(jsonResponse, "url_image_title");
                        await DownloadFile(downloadURL, rowView, romName, "titleshot", "Title Shot", scraperParameters);
                        break;

                    case "marquee":
                        downloadURL = GetJsonElementValue(jsonResponse, scraperParameters.LogoSource!);
                        await DownloadFile(downloadURL, rowView, romName, "marquee", "Logo", scraperParameters);
                        break;

                    case "video":
                        downloadURL = GetJsonElementValue(jsonResponse, scraperParameters.VideoSource!)
                            ?? GetJsonElementValue(jsonResponse, "url_video_shortplay");
                        await DownloadFile(downloadURL, rowView, romName, "video", "Video", scraperParameters);
                        break;

                }
            }
            return true;
        }

        private void UpdateMetadata(DataRowView rowView, string column, string newValue, bool overwrite)
        {
            if (string.IsNullOrEmpty(newValue))
            {
                return;
            }

            var currentValue = rowView[column];

            // Check if the current value is DBNull or null, and if overwrite is allowed
            if (overwrite || currentValue == DBNull.Value || string.IsNullOrEmpty(currentValue.ToString()))
            {
                rowView[column] = newValue;
            }
        }

        private async Task DownloadFile(string downloadURL, DataRowView rowView, string romName, string mediaName, string mediaType, ScraperParameters scraperParameters)
        {
            if (string.IsNullOrEmpty(downloadURL))
            { 
                return;
            }

            bool overwriteMedia = scraperParameters.OverwriteMedia;

            var currentValue = rowView[mediaType];
            if (currentValue != DBNull.Value && !string.IsNullOrEmpty(currentValue.ToString()) && !overwriteMedia)
            {
                return;
            }

            var mediaPaths = scraperParameters.MediaPaths!;
            string destinationFolder = mediaPaths[mediaName];
            string parentFolderPath = scraperParameters.ParentFolderPath!;
            bool verify = scraperParameters.Verify;
                       
            string extension = "png";
            if (mediaName == "video")
            {
                extension = "mp4";
            }
            if (mediaName == "manual")
            {
                extension = "pdf";
            }

            if (mediaName == "thumbnail")
            {
                mediaName = "thumb";
            }

            string fileName = $"{romName}-{mediaName}.{extension}";
            string downloadPath = $"{parentFolderPath}\\{destinationFolder}";
            string fileToDownload = $"{downloadPath}\\{fileName}";
            bool downloadSuccessful = await FileTransfer.DownloadFile(verify, overwriteMedia, fileToDownload, downloadURL);

            // true is a successful download
            if (downloadSuccessful)
            {
                rowView[mediaType] = $"./{destinationFolder}/{fileName}";
            }
        }
    }
}
