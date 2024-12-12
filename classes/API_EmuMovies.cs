using GamelistManager.classes.GamelistManager;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamelistManager.classes
{
    internal class API_EmuMovies
    {
        private readonly string apiURL = "http://api3.emumovies.com/api";
        private readonly string bearerToken = "";

        public async Task<bool> ScrapeEmuMoviesAsync(DataRowView rowView, ScraperParameters scraperParameters, Dictionary<string, List<string>> mediaLists)
        {
            var elementsToScrape = scraperParameters.ElementsToScrape!;

            foreach (string element in elementsToScrape)
            {
                switch (element)
                {
                    case "fanart":
                        await DownloadFile(rowView, "fanart", "Fan Art", "Background", scraperParameters, mediaLists);
                        break;

                    case "boxback":
                        await DownloadFile(rowView, "boxback", "Box Back", "BoxBack", scraperParameters, mediaLists);
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
                        await DownloadFile(rowView, "titleshot", "Title Shot", "Title", scraperParameters, mediaLists);
                        break;

                    case "thumbnail":
                        await DownloadFile(rowView, "thumbnail", "Box", scraperParameters.BoxSource!, scraperParameters, mediaLists);
                        break;

                    case "marquee":
                        await DownloadFile(rowView, "marquee", "Logo", scraperParameters.LogoSource!, scraperParameters, mediaLists);
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
                result = TextSearch.FindTextMatch(item, mediaList);
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
            if (currentValue != DBNull.Value && !string.IsNullOrEmpty(currentValue.ToString()) && !overwriteMedia)
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
            bool overwrite = scraperParameters.OverwriteMedia;

            if (mediaName == "thumbnail")
            {
                mediaName = "thumb";
            }

            string fileName = $"{romFileNameWithoutExtension}-{mediaName}{fileFormat}";
            string downloadPath = $"{parentFolderPath}\\{destinationFolder}";
            string fileToDownload = $"{downloadPath}\\{fileName}";

            string downloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserAccessToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
            bool result = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, downloadURL);

            // Music can be downloaded, but I do not create gamelist entries for it
            if (mediaType == "music")
            {
                return;
            }

            if (result)
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
            HttpClientSingleton.SetBearerToken(bearerToken);

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
            string jsonResponse = await GetJSONResponse.GetJsonResponseAsync(bearerToken, url);
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
            string jsonResponse = await GetJSONResponse.GetJsonResponseAsync(bearerToken, url);

            if (string.IsNullOrEmpty(jsonResponse))
            {
                return null!;
            }

            var mediaList = DeserializeJSON(jsonResponse);
            return mediaList;
        }
    }
}