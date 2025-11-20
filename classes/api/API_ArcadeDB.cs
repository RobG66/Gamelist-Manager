using GamelistManager.classes.io;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;

namespace GamelistManager.classes.api
{
    internal class API_ArcadeDB
    {
        private readonly string apiURL = "http://adb.arcadeitalia.net/service_scraper.php";
        private readonly HttpClient _httpClientService;
        private readonly FileTransfer _fileTransfer;

        public API_ArcadeDB(HttpClient httpClientService)
        {
            _httpClientService = httpClientService;
            _fileTransfer = new FileTransfer(_httpClientService);
        }

        public async Task<string> ScrapeGame(string url)
        {
            try
            {
                HttpResponseMessage httpResponse = await _httpClientService.GetAsync(url).ConfigureAwait(false);
                return await httpResponse.Content.ReadAsStringAsync();
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

            using JsonDocument document = JsonDocument.Parse(jsonString);
            // Check if the root element is an array or object
            if (document.RootElement.ValueKind == JsonValueKind.Array || document.RootElement.ValueKind == JsonValueKind.Object)
            {
                var elementValue = document.RootElement
                    .GetProperty("result")
                    .EnumerateArray()
                    .Where(game => game.TryGetProperty(elementName, out JsonElement element) && element.ValueKind != JsonValueKind.Null)
                    .Select(game => game.GetProperty(elementName).ToString())
                    .FirstOrDefault();

                // Return the Value or an empty string if not found
                return elementValue ?? string.Empty;
            }
            // Return empty string if the root element is not an array or object
            return string.Empty;
        }

        public static void SaveJsonStringToCacheFile(string jsonString, string cacheFolder, string romName)
        {
            try
            {
                if (!Directory.Exists(cacheFolder))
                {
                    Directory.CreateDirectory(cacheFolder);
                }
                string cacheFilePath = Path.Combine(cacheFolder, $"{romName}.json");
                File.WriteAllText(cacheFilePath, jsonString);
            }
            catch
            {
                // Ignore write errors
            }
        }

        private static string ReadJsonFromCache(string romName, string cacheFolder)
        {
            if (!Directory.Exists(cacheFolder))
            {
                return string.Empty;
            }

            string cacheFile = Path.Combine(cacheFolder, $"{romName}.json");

            if (!File.Exists(cacheFile))
            {
                return string.Empty;
            }

            string jsonString = File.ReadAllText(cacheFile);
            return jsonString;
        }

        public async Task<bool> ScrapeArcadeDBAsync(DataRowView rowView, ScraperParameters scraperParameters)
        {
            if (scraperParameters.ElementsToScrape == null || scraperParameters.ElementsToScrape.Count == 0)
            {
                return false; // Nothing to scrape
            }
                        
            string jsonResponse = string.Empty;

            // Scrape by cache if selected
            if (scraperParameters.ScrapeByCache && !string.IsNullOrEmpty(scraperParameters.CacheFolder))
            {
                jsonResponse = ReadJsonFromCache(scraperParameters.RomFileNameWithoutExtension!, scraperParameters.CacheFolder);
                if (string.IsNullOrEmpty(jsonResponse) && scraperParameters.SkipNonCached)
                {
                    return false;
                }
            }

            // Scrape from website or if there was no cache file
            if (string.IsNullOrEmpty(jsonResponse))
            {
                string url = $"{apiURL}?ajax=query_mame&game_name={scraperParameters.RomFileNameWithoutExtension}";
                jsonResponse = await ScrapeGame(url);

                if (string.IsNullOrEmpty(jsonResponse))
                {
                    return false; // Scrape failed
                }

                // Validate json
                try
                {
                    JsonDocument.Parse(jsonResponse);
                }
                catch
                {
                    return false;
                }


                // Save json response to cache
                SaveJsonStringToCacheFile(jsonResponse, scraperParameters.CacheFolder!, scraperParameters.RomFileNameWithoutExtension!);
            }

            string downloadURL;

            foreach (var element in scraperParameters.ElementsToScrape)
            {
                switch (element)
                {
                    case "publisher":
                        string publisher = GetJsonElementValue(jsonResponse, "manufacturer");
                        UpdateMetadata(rowView, "Publisher", publisher, scraperParameters.OverwriteMetadata);
                        break;

                    case "players":
                        string players = GetJsonElementValue(jsonResponse, "players");
                        UpdateMetadata(rowView, "Players", players, scraperParameters.OverwriteMetadata);
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
                            UpdateMetadata(rowView, "Rating", rating, scraperParameters.OverwriteMetadata);
                        }
                        break;

                    case "desc":
                        string description = GetJsonElementValue(jsonResponse, "history");
                        UpdateMetadata(rowView, "Description", description, scraperParameters.OverwriteMetadata);
                        break;

                    case "name":
                        string name = GetJsonElementValue(jsonResponse, "title");
                        UpdateMetadata(rowView, "Name", name, scraperParameters.OverwriteName);
                        break;

                    case "genre":
                        string genre = GetJsonElementValue(jsonResponse, "genre");
                        UpdateMetadata(rowView, "Genre", genre, scraperParameters.OverwriteMetadata);
                        break;

                    case "releasedate":
                        string releasedate = GetJsonElementValue(jsonResponse, "year");
                        UpdateMetadata(rowView, "Release Date", releasedate, scraperParameters.OverwriteMetadata);
                        break;

                    case "image":
                        downloadURL = GetJsonElementValue(jsonResponse, scraperParameters.ImageSource!);
                        await DownloadFile(downloadURL, rowView, scraperParameters.RomFileNameWithoutExtension!, "image", "Image", scraperParameters);
                        break;

                    case "thumbnail":
                        downloadURL = GetJsonElementValue(jsonResponse, scraperParameters.ThumbnailSource!);
                        await DownloadFile(downloadURL, rowView, scraperParameters.RomFileNameWithoutExtension!, "thumbnail", "Thumbnail", scraperParameters);
                        break;

                    case "bezel":
                        downloadURL = GetJsonElementValue(jsonResponse, "url_image_bezel");
                        await DownloadFile(downloadURL, rowView, scraperParameters.RomFileNameWithoutExtension!, "bezel", "Bezel", scraperParameters);
                        break;

                    case "manual":
                        downloadURL = GetJsonElementValue(jsonResponse, "url_manual");
                        await DownloadFile(downloadURL, rowView, scraperParameters.RomFileNameWithoutExtension!, "manual", "Manual", scraperParameters);
                        break;

                    case "titleshot":
                        downloadURL = GetJsonElementValue(jsonResponse, "url_image_title");
                        await DownloadFile(downloadURL, rowView, scraperParameters.RomFileNameWithoutExtension!, "titleshot", "Titleshot", scraperParameters);
                        break;

                    case "marquee":
                        downloadURL = GetJsonElementValue(jsonResponse, scraperParameters.MarqueeSource!);
                        await DownloadFile(downloadURL, rowView, scraperParameters.RomFileNameWithoutExtension!, "marquee", "Marquee", scraperParameters);
                        break;

                    case "video":
                        downloadURL = GetJsonElementValue(jsonResponse, scraperParameters.VideoSource!);
                        if (string.IsNullOrEmpty(downloadURL))
                        {
                            downloadURL = GetJsonElementValue(jsonResponse, "url_video_shortplay");
                        }
                        await DownloadFile(downloadURL, rowView, scraperParameters.RomFileNameWithoutExtension!, "video", "Video", scraperParameters);
                        break;

                }
            }
            return true;
        }

        private static void UpdateMetadata(DataRowView rowView, string column, string newValue, bool overwrite)
        {
            if (string.IsNullOrEmpty(newValue))
            {
                return;
            }

            var currentValue = rowView[column];

            // Check if the current Value is DBNull or null, and if overwrite is allowed
            if (overwrite || currentValue == DBNull.Value || string.IsNullOrEmpty(currentValue.ToString()))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    rowView[column] = newValue;
                });
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
            if (currentValue != DBNull.Value && currentValue != null && !string.IsNullOrEmpty(currentValue.ToString()) && !overwriteMedia)
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

            // Convert forward slashes to backslashes for Windows file system
            string destinationFolderWindows = destinationFolder.Replace('/', '\\');

            string fileName = $"{romName}-{mediaName}.{extension}";
            string downloadPath = Path.Combine(parentFolderPath, destinationFolderWindows);
            string fileToDownload = Path.Combine(downloadPath, fileName);

            bool downloadSuccessful = await _fileTransfer.DownloadFile(verify, fileToDownload, downloadURL, string.Empty);
            // true is a successful download
            if (downloadSuccessful)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    rowView[mediaType] = $"./{destinationFolder}/{fileName}";
                });
            }
        }
    }
}
