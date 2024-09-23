using GamelistManager.classes.GamelistManager;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamelistManager.classes
{
    internal class API_EmuMovies
    {
        private readonly ConcurrentDictionary<(string, string), int> memo = new ConcurrentDictionary<(string, string), int>();
        private readonly string apiURL = "http://api3.emumovies.com/api";
        private readonly string apiKey = "";
      
        public async Task<MetaDataList> ScrapeEmuMoviesAsync(ScraperParameters scraperParameters, Dictionary<string, List<string>> emumoviesMediaLists)
        {
            string destinationFolder = string.Empty;
            string remoteDownloadURL = string.Empty;
            string remoteFileName = string.Empty;
            string fileName = string.Empty;
            string fileToDownload = string.Empty;
            bool downloadResult = false;
            bool overwrite = scraperParameters.Overwrite;
            bool verify = scraperParameters.Verify;
            string remoteMediaType = string.Empty;
            List<string> mediaList = [];
            string fileFormat = string.Empty;
            string romFileNameWithoutExtension = scraperParameters.RomFileNameWithoutExtension!; 
            string name = scraperParameters.Name!;
            var mediaPaths = scraperParameters.MediaPaths!;
            var elementsToScrape = scraperParameters.ElementsToScrape!;

            MetaDataList metaDataList = new MetaDataList();

            foreach (string element in elementsToScrape)
            {
                switch (element)
                {
                    case "fanart":
                        destinationFolder = mediaPaths["fanart"];
                        remoteMediaType = "Background";
                        mediaList = emumoviesMediaLists[remoteMediaType];
                        if (mediaList == null)
                        {
                            continue;
                        }
                        remoteFileName = FuzzySearch(name, mediaList);
                        if (string.IsNullOrEmpty(remoteFileName))
                        {
                            remoteFileName = FuzzySearch(romFileNameWithoutExtension, mediaList);
                            if (string.IsNullOrEmpty(remoteFileName))
                            {
                                continue;
                            }
                        }
                        fileFormat = Path.GetExtension(remoteFileName);
                        if (string.IsNullOrEmpty(fileFormat))
                        {
                            continue;
                        }
                        fileName = $"{romFileNameWithoutExtension}-{element}{fileFormat}";
                        fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                        remoteDownloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserAccessToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
                        downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                        if (downloadResult == true)
                        {
                            metaDataList.SetMetadataValue(MetaDataKeys.fanart, $"./{destinationFolder}/{fileName}");
                        }
                        break;

                    case "boxback":
                        destinationFolder = mediaPaths["boxback"];
                        remoteMediaType = "BoxBack";
                        mediaList = emumoviesMediaLists[remoteMediaType];
                        if (mediaList == null)
                        {
                            // No media
                            continue;
                        }
                        remoteFileName = FuzzySearch(name, mediaList);
                        if (string.IsNullOrEmpty(remoteFileName))
                        {
                            remoteFileName = FuzzySearch(romFileNameWithoutExtension, mediaList);
                            if (string.IsNullOrEmpty(remoteFileName))
                            {
                                continue;
                            }
                        }
                        fileFormat = Path.GetExtension(remoteFileName);
                        if (string.IsNullOrEmpty(fileFormat))
                        {
                            continue;
                        }
                        fileName = $"{romFileNameWithoutExtension}-{element}{fileFormat}";
                        fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                        remoteDownloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserAccessToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
                        downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                        if (downloadResult == true)
                        {
                            metaDataList.SetMetadataValue(MetaDataKeys.boxback, $"./{destinationFolder}/{fileName}");
                        }
                        break;

                    case "manual":
                        destinationFolder = mediaPaths["manual"];
                        remoteMediaType = "Manual";
                        mediaList = emumoviesMediaLists[remoteMediaType];
                        if (mediaList == null)
                        {
                            // No media
                            continue;
                        }
                        remoteFileName = FuzzySearch(name, mediaList);
                        if (string.IsNullOrEmpty(remoteFileName))
                        {
                            remoteFileName = FuzzySearch(romFileNameWithoutExtension, mediaList);
                            if (string.IsNullOrEmpty(remoteFileName))
                            {
                                continue;
                            }
                        }
                        fileFormat = Path.GetExtension(remoteFileName);
                        if (string.IsNullOrEmpty(fileFormat))
                        {
                            continue;
                        }
                        fileName = $"{romFileNameWithoutExtension}-{element}{fileFormat}";
                        fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                        remoteDownloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserAccessToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
                        downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                        if (downloadResult == true)
                        {
                            metaDataList.SetMetadataValue(MetaDataKeys.manual, $"./{destinationFolder}/{fileName}");
                        }
                        break;

                    case "music":
                        destinationFolder = mediaPaths["music"];
                        remoteMediaType = "Music";
                        mediaList = emumoviesMediaLists[remoteMediaType];
                        if (mediaList == null)
                        {
                            // No media
                            continue;
                        }
                        remoteFileName = FuzzySearch(name, mediaList);
                        if (string.IsNullOrEmpty(remoteFileName))
                        {
                            remoteFileName = FuzzySearch(romFileNameWithoutExtension, mediaList);
                            if (string.IsNullOrEmpty(remoteFileName))
                            {
                                continue;
                            }
                        }

                        fileFormat = Path.GetExtension(remoteFileName);
                        if (string.IsNullOrEmpty(fileFormat))
                        {
                            continue;
                        }
                        fileName = $"{romFileNameWithoutExtension}-{element}{fileFormat}";
                        fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                        remoteDownloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserAccessToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
                        downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                        if (downloadResult == true)
                        {
                            metaDataList.SetMetadataValue(MetaDataKeys.music, $"./{destinationFolder}/{fileName}");
                        }
                        break;


                    case "image":
                        destinationFolder = mediaPaths["image"];
                        remoteMediaType = scraperParameters.ImageSource;
                        mediaList = emumoviesMediaLists[remoteMediaType];
                        if (mediaList == null)
                        {
                            // No media
                            continue;
                        }
                        remoteFileName = FuzzySearch(name, mediaList);
                        if (string.IsNullOrEmpty(remoteFileName))
                        {
                            remoteFileName = FuzzySearch(romFileNameWithoutExtension, mediaList);
                            if (string.IsNullOrEmpty(remoteFileName))
                            {
                                continue;
                            }
                        }

                        fileFormat = Path.GetExtension(remoteFileName);
                        if (string.IsNullOrEmpty(fileFormat))
                        {
                            continue;
                        }
                        fileName = $"{romFileNameWithoutExtension}-{element}{fileFormat}";
                        fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                        remoteDownloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserAccessToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
                        downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                        if (downloadResult == true)
                        {
                            metaDataList.SetMetadataValue(MetaDataKeys.image, $"./{destinationFolder}/{fileName}");
                        }
                        break;

                    case "titleshot":
                        destinationFolder = mediaPaths["titleshot"];
                        remoteMediaType = "Title";
                        mediaList = emumoviesMediaLists[remoteMediaType];
                        if (mediaList == null)
                        {
                            // No media
                            continue;
                        }
                        remoteFileName = FuzzySearch(name, mediaList);
                        if (string.IsNullOrEmpty(remoteFileName))
                        {
                            remoteFileName = FuzzySearch(romFileNameWithoutExtension, mediaList);
                            if (string.IsNullOrEmpty(remoteFileName))
                            {
                                continue;
                            }
                        }

                        fileFormat = Path.GetExtension(remoteFileName);
                        if (string.IsNullOrEmpty(fileFormat))
                        {
                            continue;
                        }
                        fileName = $"{romFileNameWithoutExtension}-{element}{fileFormat}";
                        fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                        remoteDownloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserAccessToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
                        downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                        if (downloadResult == true)
                        {
                            metaDataList.SetMetadataValue(MetaDataKeys.titleshot, $"./{destinationFolder}/{fileName}");
                        }
                        break;

                    case "thumbnail":
                        destinationFolder = mediaPaths["thumbnail"];
                        remoteMediaType = scraperParameters.BoxSource;
                        mediaList = emumoviesMediaLists[remoteMediaType];
                        if (mediaList == null)
                        {
                            // No media
                            continue;
                        }
                        remoteFileName = FuzzySearch(name, mediaList);
                        if (string.IsNullOrEmpty(remoteFileName))
                        {
                            remoteFileName = FuzzySearch(romFileNameWithoutExtension, mediaList);
                            if (string.IsNullOrEmpty(remoteFileName))
                            {
                                continue;
                            }
                        }

                        fileFormat = Path.GetExtension(remoteFileName);
                        if (string.IsNullOrEmpty(fileFormat))
                        {
                            continue;
                        }
                        // Batocera uses thumb instead of thumbnail, so I do the same
                        fileName = $"{romFileNameWithoutExtension}-thumb{fileFormat}";
                        fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                        remoteDownloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserAccessToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
                        downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                        if (downloadResult == true)
                        {
                            metaDataList.SetMetadataValue(MetaDataKeys.thumbnail, $"./{destinationFolder}/{fileName}");
                        }
                        break;

                    case "marquee":
                        remoteMediaType = scraperParameters.LogoSource;
                        mediaList = emumoviesMediaLists[remoteMediaType];
                        destinationFolder = mediaPaths["marquee"];
                        if (mediaList == null)
                        {
                            // No media
                            continue;
                        }
                        remoteFileName = FuzzySearch(name, mediaList);
                        if (string.IsNullOrEmpty(remoteFileName))
                        {
                            remoteFileName = FuzzySearch(romFileNameWithoutExtension, mediaList);
                            if (string.IsNullOrEmpty(remoteFileName))
                            {
                                continue;
                            }
                        }
                        fileFormat = Path.GetExtension(remoteFileName);
                        if (string.IsNullOrEmpty(fileFormat))
                        {
                            continue;
                        }
                        fileName = $"{romFileNameWithoutExtension}-{element}{fileFormat}";
                        fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                        remoteDownloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserAccessToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
                        downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                        if (downloadResult == true)
                        {
                            metaDataList.SetMetadataValue(MetaDataKeys.marquee, $"./{destinationFolder}/{fileName}");
                        }
                        break;

                    case "cartridge":
                        remoteMediaType = scraperParameters.CartridgeSource;
                        mediaList = emumoviesMediaLists[remoteMediaType];
                        destinationFolder = mediaPaths["cartridge"];
                        if (mediaList == null)
                        {
                            // No media
                            continue;
                        }
                        remoteFileName = FuzzySearch(name, mediaList);
                        if (string.IsNullOrEmpty(remoteFileName))
                        {
                            remoteFileName = FuzzySearch(romFileNameWithoutExtension, mediaList);
                            if (string.IsNullOrEmpty(remoteFileName))
                            {
                                continue;
                            }
                        }
                        fileFormat = Path.GetExtension(remoteFileName);
                        if (string.IsNullOrEmpty(fileFormat))
                        {
                            continue;
                        }
                        fileName = $"{romFileNameWithoutExtension}-{element}{fileFormat}";
                        fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                        remoteDownloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserAccessToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
                        downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                        if (downloadResult == true)
                        {
                            metaDataList.SetMetadataValue(MetaDataKeys.cartridge, $"./{destinationFolder}/{fileName}");
                        }
                        break;


                    case "video":
                        remoteMediaType = scraperParameters.VideoSource;
                        mediaList = emumoviesMediaLists[remoteMediaType];
                        destinationFolder = mediaPaths["video"];
                        if (mediaList == null)
                        {
                            // No media
                            continue;
                        }
                        remoteFileName = FuzzySearch(name, mediaList);
                        if (string.IsNullOrEmpty(remoteFileName))
                        {
                            remoteFileName = FuzzySearch(romFileNameWithoutExtension, mediaList);
                            if (string.IsNullOrEmpty(remoteFileName))
                            {
                                continue;
                            }
                        }
                        fileFormat = Path.GetExtension(remoteFileName);
                        if (string.IsNullOrEmpty(fileFormat))
                        {
                            continue;
                        }
                        fileName = $"{romFileNameWithoutExtension}-{element}{fileFormat}";
                        fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                        remoteDownloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserAccessToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
                        downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                        if (downloadResult == true)
                        {
                            metaDataList.SetMetadataValue(MetaDataKeys.video, $"./{destinationFolder}/{fileName}");
                        }
                        break;
                }
            }
            return metaDataList;
        }

        private string FuzzySearch(string searchName, List<string> names)
        {
            var fuzzySearchHelper = new FuzzySearchHelper();
            string bestMatch = fuzzySearchHelper.FuzzySearch(searchName, names);
            return bestMatch;
        }

        /* This method is no longer used, but will keep for reference
        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            return client;
        }
        */

        public async Task<string> AuthenticateEmuMoviesAsync(string username, string password)
        {
            var credentials = new
            {
                username,
                password
            };

            string url = $"{apiURL}/User/authenticate";

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var jsonContent = JsonSerializer.Serialize(credentials, options);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            // Use the singleton instance
            var client = HttpClientSingleton.Instance;

            HttpResponseMessage response = await client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    JsonElement root = doc.RootElement;
                    JsonElement data = root.GetProperty("data");

                    JsonElement accessTokenElement = data.GetProperty("acessToken");
                    return accessTokenElement.GetString() ?? string.Empty;
                }
            }
            else
            {
                return string.Empty;
            }
        }


        public async Task<List<string>> GetMediaTypes(string system)
        {
            string url = $"{apiURL}/Media/MediaTypes?systemName={system}";
            GetJsonResponse getJsonResponse = new GetJsonResponse();
            string jsonResponse = await getJsonResponse.GetJsonResponseAsync(url);
            if (string.IsNullOrEmpty(jsonResponse))
            {
                return null!;
            }

            var mediaTypes = DeserializeJSON(jsonResponse);
            return mediaTypes;
        }

        public List<string> DeserializeJSON(string jsonString)
        {
            try
            {
                JsonDocument doc = JsonDocument.Parse(jsonString);
                JsonElement root = doc.RootElement;
                JsonElement dataElement = root.GetProperty("data");
                List<string> dataList = new List<string>();
                foreach (JsonElement element in dataElement.EnumerateArray())
                {
                    dataList.Add(element.GetString()!);
                }
                return dataList;
            }
            catch
            {
                return null!;
            }
        }

        public async Task<List<string>> GetMediaList(string system, string mediaTitle)
        {
            string url = $"{apiURL}/Media/MediaList?systemName={system}&mediaType={mediaTitle}&mediaSet=default";
            GetJsonResponse getJsonResponse = new GetJsonResponse();
            string jsonResponse = await getJsonResponse.GetJsonResponseAsync(url);

            if (string.IsNullOrEmpty(jsonResponse))
            {
                return null!;
            }

            var mediaList = DeserializeJSON(jsonResponse);
            return mediaList;
        }
    }
}