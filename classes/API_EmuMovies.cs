using System.Data;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamelistManager.classes
{
    internal class API_EmuMovies
    {
        private readonly string _apiURL = "https://api3.emumovies.com/api";
        private readonly string _bearerToken = "";
        private readonly HttpClient _httpClientService;
        private readonly FileTransfer _fileTransfer;

        public API_EmuMovies(HttpClient httpClientService)
        {
            _httpClientService = httpClientService;
            _fileTransfer = new FileTransfer(_httpClientService);
        }


        public async Task<bool> ScrapeEmuMoviesAsync(DataRowView rowView, ScraperParameters scraperParameters, Dictionary<string, List<string>> mediaLists)
        {
            var elementsToScrape = scraperParameters.ElementsToScrape!;

            foreach (string element in elementsToScrape)
            {
                switch (element)
                {
                    case "fanart":
                        await DownloadFile(rowView, "fanart", "Fanart", "Background", scraperParameters, mediaLists);
                        break;

                    case "boxback":
                        await DownloadFile(rowView, "boxback", "Boxback", "BoxBack", scraperParameters, mediaLists);
                        break;

                    case "boxart":
                        await DownloadFile(rowView, "boxart", "Boxart", scraperParameters.BoxartSource!, scraperParameters, mediaLists);
                        break;

                    case "wheel":
                        await DownloadFile(rowView, "wheel", "Wheel", scraperParameters.WheelSource!, scraperParameters, mediaLists);
                        break;

                    case "manual":
                        await DownloadFile(rowView, "manual", "Manual", "Manual", scraperParameters, mediaLists);
                        break;

                    case "music":
                        await DownloadFile(rowView, "music", "Music", "Music", scraperParameters, mediaLists);
                        break;

                    case "image":
                        await DownloadFile(rowView, "image", "Image", scraperParameters.ImageSource!, scraperParameters, mediaLists);
                        break;

                    case "titleshot":
                        await DownloadFile(rowView, "titleshot", "Titleshot", "Title", scraperParameters, mediaLists);
                        break;

                    case "thumbnail":
                        await DownloadFile(rowView, "thumbnail", "Thumbnail", scraperParameters.ThumbnailSource!, scraperParameters, mediaLists);
                        break;

                    case "marquee":
                        await DownloadFile(rowView, "marquee", "Marquee", scraperParameters.MarqueeSource!, scraperParameters, mediaLists);
                        break;

                    case "cartridge":
                        await DownloadFile(rowView, "cartridge", "Cartridge", scraperParameters.CartridgeSource!, scraperParameters, mediaLists);
                        break;

                    case "video":
                        await DownloadFile(rowView, "video", "Video", scraperParameters.VideoSource!, scraperParameters, mediaLists);
                        break;
                }
            }
            return true;
        }

        private string FindString(List<string> searchText, List<string> mediaList)
        {
            string result = string.Empty;

            foreach (string item in searchText)
            {
                result = TextSearchHelper.FindTextMatch(item, mediaList);
                if (!string.IsNullOrEmpty(result))
                {
                    break;
                }
            }

            return result;
        }

        private async Task DownloadFile(DataRowView rowView, string mediaName, string mediaType, string remoteMediaType, ScraperParameters scraperParameters, Dictionary<string, List<string>> mediaLists)
        {
            bool overwriteMedia = scraperParameters.OverwriteMedia;

            var currentValue = rowView[mediaType];
            if (currentValue != DBNull.Value && currentValue != null && !string.IsNullOrEmpty(currentValue.ToString()) && !overwriteMedia)
            {
                return;
            }

            string name = scraperParameters.Name!;
            string romFileNameWithoutExtension = scraperParameters.RomFileNameWithoutExtension!;

            List<string> mediaList = mediaLists[remoteMediaType];

            List<string> items = new List<string> { name, romFileNameWithoutExtension };
            string remoteFileName = FindString(items, mediaList);
            if (string.IsNullOrEmpty(remoteFileName))
            {
                return;
            }

            string fileFormat = Path.GetExtension(remoteFileName);

            if (string.IsNullOrEmpty(fileFormat))
            {
                return;
            }

            var mediaPaths = scraperParameters.MediaPaths!;
            string destinationFolder = mediaPaths[mediaName];
            string parentFolderPath = scraperParameters.ParentFolderPath!;
            bool verify = scraperParameters.Verify;

            if (mediaName == "thumbnail")
            {
                mediaName = "thumb";
            }

            string fileName = $"{romFileNameWithoutExtension}-{mediaName}{fileFormat}";
            string downloadPath = $"{parentFolderPath}\\{destinationFolder}";
            string fileToDownload = $"{downloadPath}\\{fileName}";

            string downloadURL = $"{_apiURL}/Media/Download?accessToken={scraperParameters.UserAccessToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={UrlEncoder.UrlEncodeFileName(remoteFileName)}";
            bool downloadResult = await _fileTransfer.DownloadFile(verify, fileToDownload, downloadURL, _bearerToken);

            // Music can be downloaded, but I do not create gamelist entries for it
            if (mediaType == "music")
            {
                return;
            }

            if (downloadResult)
            {
                rowView[mediaType] = $"./{destinationFolder}/{fileName}";
            }
        }
        public async Task<string> AuthenticateEmuMoviesAsync(string username, string password)
        {
            var credentials = new
            {
                username,
                password
            };

            string url = $"{_apiURL}/User/authenticate";

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var jsonContent = JsonSerializer.Serialize(credentials, options);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

            // Add the Authorization header
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);

            using HttpResponseMessage httpResponse = await _httpClientService.SendAsync(request);

            if (!httpResponse.IsSuccessStatusCode)
            {
                return string.Empty; // Authentication failed
            }

            string responseBody = await httpResponse.Content.ReadAsStringAsync();

            try
            {
                using JsonDocument doc = JsonDocument.Parse(responseBody);
                JsonElement root = doc.RootElement;

                // Extract "accessToken" from the "data" object
                if (root.TryGetProperty("data", out JsonElement data) &&
                    data.TryGetProperty("acessToken", out JsonElement accessTokenElement))
                {
                    return accessTokenElement.GetString() ?? string.Empty;
                }
            }
            catch (JsonException)
            {
                // Handle cases where the response isn't valid JSON
                return responseBody;
            }

            return responseBody;
        }


        public async Task<List<string>> GetMediaTypes(string system)
        {
            string url = $"{_apiURL}/Media/MediaTypes?systemName={system}";
            string jsonString = string.Empty;
            try
            {
                _httpClientService.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
                HttpResponseMessage httpResponse = await _httpClientService.GetAsync(url).ConfigureAwait(false);
                jsonString = await httpResponse.Content.ReadAsStringAsync();
            }
            catch
            {
                return null!;
            }

            if (string.IsNullOrEmpty(jsonString))
            {
                return null!;
            }

            var mediaTypes = DeserializeJSON(jsonString);
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
            string jsonString = string.Empty;

            string url = $"{_apiURL}/Media/MediaList?systemName={system}&mediaType={mediaTitle}&mediaSet=default";

            try
            {
                _httpClientService.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
                HttpResponseMessage httpResponse = await _httpClientService.GetAsync(url).ConfigureAwait(false);
                jsonString = await httpResponse.Content.ReadAsStringAsync();
            }
            catch
            {
                return null!;
            }

            if (string.IsNullOrEmpty(jsonString))
            {
                return null!;
            }

            var mediaList = DeserializeJSON(jsonString);
            return mediaList;
        }

    }
}