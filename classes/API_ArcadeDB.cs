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
            GetJsonResponse getJsonResponse = new GetJsonResponse();

            try
            {
                // Fetch the JSON response using the GetJsonResponse class
                string jsonResponse = await getJsonResponse.GetJsonResponseAsync(url);
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

        public async Task<MetaDataList> ScrapeArcadeDBAsync(ScraperParameters scraperParameters)
        {
            string romName = scraperParameters.RomFileNameWithoutExtension;
            string cacheFolder = scraperParameters.CacheFolder;
            bool scrapeByCache = scraperParameters.ScrapeByCache;
            string jsonResponse = string.Empty;

            // Scrape by cache if selected
            if (scrapeByCache == true)
            {
                jsonResponse = ReadJsonFromCache(romName, cacheFolder);
            }

            // Scrape from website or if there was no cache file
            if (string.IsNullOrEmpty(jsonResponse))
            {
                jsonResponse = await ScrapeGame(romName);

                if (string.IsNullOrEmpty(jsonResponse))
                {
                    return null!;
                }

                // Save json response to cache
                SaveJsonStringToCacheFile(jsonResponse, cacheFolder, romName);
            }

            var elementsToScrape = scraperParameters.ElementsToScrape!;
            var mediaPaths = scraperParameters.MediaPaths!;
            bool overwrite = scraperParameters.Overwrite;
            bool verify = scraperParameters.Verify;
            string parentFolderPath = scraperParameters.ParentFolderPath!;
            string remoteDownloadURL = string.Empty;
            string destinationFolder = string.Empty;
            string fileName = string.Empty;
            string downloadPath = string.Empty;
            string fileToDownload = string.Empty;
            bool downloadResult;
            string propertyName = string.Empty;

            MetaDataList metaDataList = new MetaDataList();

            foreach (var element in elementsToScrape)
            {
                switch (element)
                {
                    case "publisher":
                        string publisher = GetJsonElementValue(jsonResponse, "manufacturer");
                        if (!string.IsNullOrEmpty(publisher))
                        {
                            metaDataList.SetMetadataValue(MetaDataKeys.publisher, publisher);
                        }
                        break;

                    case "players":
                        string players = GetJsonElementValue(jsonResponse, "players");
                        if (!string.IsNullOrEmpty(players))
                        {
                            metaDataList.SetMetadataValue(MetaDataKeys.players, players);
                        }
                        break;
                            
                    case "rating":
                        string rating = GetJsonElementValue(jsonResponse, "rate");
                        if (!string.IsNullOrEmpty(rating))
                        {
                            if (int.TryParse(rating, out int parseResult))
                            {
                                if (parseResult == 100)
                                {
                                    rating = "1";
                                }
                                else if (parseResult > 0 && parseResult < 100)
                                {
                                    rating = "." + parseResult.ToString().TrimStart('0');
                                }
                                metaDataList.SetMetadataValue(MetaDataKeys.rating, rating);
                            }
                        }
                        break;

                    case "desc":
                        string description = GetJsonElementValue(jsonResponse, "history");
                        if (!string.IsNullOrEmpty(description))
                        {
                            metaDataList.SetMetadataValue(MetaDataKeys.desc, description);
                        }
                        break;

                    case "name":
                        string name = GetJsonElementValue(jsonResponse, "title");
                        if (!string.IsNullOrEmpty(name))
                        {
                            metaDataList.SetMetadataValue(MetaDataKeys.name, name);
                        }
                        break;

                    case "genre":
                        string genre = GetJsonElementValue(jsonResponse, "genre");
                        if (!string.IsNullOrEmpty(genre))
                        {
                            metaDataList.SetMetadataValue(MetaDataKeys.genre, genre);
                        }
                        break;

                    case "releasedate":
                        string releasedate = GetJsonElementValue(jsonResponse, "year");
                        if (!string.IsNullOrEmpty(releasedate))
                        {
                            string isoDate = ISO8601Converter.ConvertToISO8601(releasedate);
                            metaDataList.SetMetadataValue(MetaDataKeys.releasedate, isoDate);
                        }
                        break;

                    case "image":
                        string imageSource = scraperParameters.ImageSource;
                        remoteDownloadURL = GetJsonElementValue(jsonResponse, imageSource);
                        if (!string.IsNullOrEmpty(remoteDownloadURL))
                        {
                            destinationFolder = mediaPaths["image"].ToString();
                            fileName = $"{romName}-image.png";
                            downloadPath = $"{parentFolderPath}\\{destinationFolder}";
                            fileToDownload = $"{downloadPath}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult)
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.image, $"./{destinationFolder}/{fileName}");
                            }
                        }
                        break;

                    case "thumbnail":
                        string boxSource = scraperParameters.BoxSource;
                        remoteDownloadURL = GetJsonElementValue(jsonResponse, boxSource);
                        if (!string.IsNullOrEmpty(remoteDownloadURL))
                        {
                            destinationFolder = mediaPaths["thumbnail"].ToString();
                            fileName = $"{romName}-thumb.png";
                            downloadPath = $"{parentFolderPath}\\{destinationFolder}";
                            fileToDownload = $"{downloadPath}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult)
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.thumbnail, $"./{destinationFolder}/{fileName}");
                            }
                        }
                        break;
                        
                        case "bezel":
                        remoteDownloadURL = GetJsonElementValue(jsonResponse, "url_image_bezel");
                        if (!string.IsNullOrEmpty(remoteDownloadURL))
                        {
                            destinationFolder = mediaPaths["bezel"].ToString();
                            fileName = $"{romName}-bezel.png";
                            downloadPath = $"{parentFolderPath}\\{destinationFolder}";
                            fileToDownload = $"{downloadPath}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult)
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.bezel, $"./{destinationFolder}/{fileName}");
                            }
                        }
                        break;

                    case "manual":
                        remoteDownloadURL = GetJsonElementValue(jsonResponse, "url_manual");
                        if (!string.IsNullOrEmpty(remoteDownloadURL))
                        {
                            destinationFolder = mediaPaths["manual"].ToString();
                            fileName = $"{romName}-manual.pdf";
                            downloadPath = $"{parentFolderPath}\\{destinationFolder}";
                            fileToDownload = $"{downloadPath}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult)
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.manual, $"./{destinationFolder}/{fileName}");
                            }
                        }
                        break;




                    case "titleshot":
                        remoteDownloadURL = GetJsonElementValue(jsonResponse, "url_image_title");
                        if (!string.IsNullOrEmpty(remoteDownloadURL))
                        {
                            destinationFolder = mediaPaths["titleshot"].ToString();
                            fileName = $"{romName}-titleshot.png";
                            downloadPath = $"{parentFolderPath}\\{destinationFolder}";
                            fileToDownload = $"{downloadPath}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult)
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.titleshot, $"./{destinationFolder}/{fileName}");
                            }
                        }
                        break;

                    case "marquee":
                        string logoSource = scraperParameters.LogoSource;
                        remoteDownloadURL = GetJsonElementValue(jsonResponse, logoSource);
                        if (!string.IsNullOrEmpty(remoteDownloadURL))
                        {
                            destinationFolder = mediaPaths["marquee"].ToString();
                            fileName = $"{romName}-marquee.png";
                            downloadPath = $"{parentFolderPath}\\{destinationFolder}";
                            fileToDownload = $"{downloadPath}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult)
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.marquee, $"./{destinationFolder}/{fileName}");
                            }
                        }
                        break;

                    case "video":
                        string videoSource = scraperParameters.VideoSource;
                        remoteDownloadURL = GetJsonElementValue(jsonResponse, videoSource);
                        if (string.IsNullOrEmpty(remoteDownloadURL))
                        {
                            remoteDownloadURL = GetJsonElementValue(jsonResponse, "url_video_shortplay");
                        }
                        if (!string.IsNullOrEmpty(remoteDownloadURL))
                        {
                            destinationFolder = mediaPaths["video"] as string;
                            fileName = $"{romName}-video.mp4";
                            downloadPath = $"{parentFolderPath}\\{destinationFolder}";
                            fileToDownload = $"{downloadPath}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult)
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.video, $"./{destinationFolder}/{fileName}");
                            }
                        }
                        break;
                }
            }

            return metaDataList;
        }
    }
}
