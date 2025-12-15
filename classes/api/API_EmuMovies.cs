using GamelistManager.classes.helpers;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamelistManager.classes.api
{
    internal class API_EmuMovies
    {
        private const string ApiUrl = "https://api3.emumovies.com/api";
        private readonly string _bearerToken;
        private readonly HttpClient _httpClient;

        public API_EmuMovies(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _bearerToken = ConfigurationManager.AppSettings["EmuMovies_bearerToken"]
               ?? throw new InvalidOperationException("EmuMovies_bearerToken not found in App.config");
        }

        private static string GetFileExtension(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            try
            {
                var uri = new Uri(url);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                string? filename = query["filename"];

                if (!string.IsNullOrEmpty(filename))
                {
                    return Path.GetExtension(filename);
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void AddMedia(ScrapedGameData data, ScrapedGameData.MediaResult? media)
        {
            if (media != null)
            {
                data.Media.Add(media);
            }
        }

        private static ScrapedGameData.MediaResult? GetMediaResult(string url, string mediaType)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            return new ScrapedGameData.MediaResult
            {
                Url = url,
                FileExtension = GetFileExtension(url),
                Region = string.Empty,
                MediaType = mediaType
            };
        }

        public async Task<ScrapedGameData> ScrapeEmuMoviesAsync(
            ScraperParameters parameters,
            Dictionary<string, List<string>> mediaLists)
        {
            var result = new ScrapedGameData();

            if (parameters.ElementsToScrape == null || parameters.ElementsToScrape.Count == 0)
            {
                result.WasSuccessful = false;
                result.Messages.Add("No elements to scrape");
                return result;
            }

            foreach (string element in parameters.ElementsToScrape)
            {
                string? url = null;

                switch (element)
                {
                    case "fanart":
                        url = await GetMediaUrl("Background", parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url!, "fanart"));
                        break;

                    case "boxback":
                        url = await GetMediaUrl("BoxBack", parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url!, "boxback"));
                        break;

                    case "boxart":
                        url = await GetMediaUrl(parameters.BoxArtSource, parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url!, "boxart"));
                        break;

                    case "wheel":
                        url = await GetMediaUrl(parameters.WheelSource, parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url!, "wheel"));
                        break;

                    case "manual":
                        url = await GetMediaUrl("Manual", parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url!, "manual"));
                        break;

                    case "music":
                        url = await GetMediaUrl("Music", parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url!, "music"));
                        break;

                    case "image":
                        url = await GetMediaUrl(parameters.ImageSource, parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url!, "image"));
                        break;

                    case "titleshot":
                        url = await GetMediaUrl("Title", parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url!, "titleshot"));
                        break;

                    case "thumbnail":
                        url = await GetMediaUrl(parameters.ThumbnailSource, parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url!, "thumbnail"));
                        break;

                    case "marquee":
                        url = await GetMediaUrl(parameters.MarqueeSource, parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url!, "marquee"));
                        break;

                    case "cartridge":
                        url = await GetMediaUrl(parameters.CartridgeSource, parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url!, "cartridge"));
                        break;

                    case "video":
                        url = await GetMediaUrl(parameters.VideoSource, parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url!, "video"));
                        break;
                }
            }

            result.WasSuccessful = true;
            return result;
        }

        private async Task<string?> GetMediaUrl(
            string? remoteMediaType,
            ScraperParameters parameters,
            Dictionary<string, List<string>> mediaLists)
        {
            if (string.IsNullOrEmpty(remoteMediaType))
                return null;

            if (string.IsNullOrEmpty(parameters.RomName) || string.IsNullOrEmpty(parameters.RomFileName))
                return null;

            if (!mediaLists.TryGetValue(remoteMediaType, out List<string>? mediaList) || mediaList == null)
                return null;

            string romFileNameWithoutExtension = Path.GetFileNameWithoutExtension(parameters.RomFileName);
            List<string> searchItems = new List<string> { parameters.RomName, romFileNameWithoutExtension };

            string remoteFileName = FindString(searchItems, mediaList);

            if (string.IsNullOrEmpty(remoteFileName))
                return null;

            string fileFormat = Path.GetExtension(remoteFileName);
            if (string.IsNullOrEmpty(fileFormat))
                return null;

            string downloadURL = $"{ApiUrl}/Media/Download?accessToken={parameters.UserAccessToken}&systemName={parameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={UrlHelper.UrlEncodeFileName(remoteFileName)}";

            return downloadURL;
        }

        private static string FindString(List<string> searchText, List<string> mediaList)
        {
            foreach (string item in searchText)
            {
                string result = TextSearchHelper.FindTextMatch(item, mediaList);
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }

            return string.Empty;
        }

        public async Task<(string AccessToken, string ErrorMessage)> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return (string.Empty, "Username and password are required");

            var credentials = new
            {
                username,
                password
            };

            string url = $"{ApiUrl}/User/authenticate";

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            try
            {
                var jsonContent = JsonSerializer.Serialize(credentials, options);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);

                using var httpResponse = await _httpClient.SendAsync(request);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    return (string.Empty, $"Authentication failed: {httpResponse.StatusCode}");
                }

                string responseBody = await httpResponse.Content.ReadAsStringAsync();

                using JsonDocument doc = JsonDocument.Parse(responseBody);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("data", out JsonElement data) &&
                    data.TryGetProperty("acessToken", out JsonElement accessTokenElement))
                {
                    string? token = accessTokenElement.GetString();
                    if (!string.IsNullOrEmpty(token))
                        return (token, string.Empty);
                }

                return (string.Empty, "Access token not found in response");
            }
            catch (HttpRequestException ex)
            {
                return (string.Empty, $"HTTP request failed: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return (string.Empty, $"Failed to parse JSON response: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (string.Empty, $"Authentication error: {ex.Message}");
            }
        }

        public async Task<(List<string>? MediaTypes, string ErrorMessage)> GetMediaTypesAsync(string system)
        {
            if (string.IsNullOrEmpty(system))
                return (null, "System name is required");

            string url = $"{ApiUrl}/Media/MediaTypes?systemName={system}";

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
                var httpResponse = await _httpClient.GetAsync(url).ConfigureAwait(false);
                httpResponse.EnsureSuccessStatusCode();

                string jsonString = await httpResponse.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(jsonString))
                    return (null, "Empty response from API");

                var mediaTypes = DeserializeJsonToList(jsonString);
                if (mediaTypes == null)
                    return (null, "Failed to deserialize media types");

                return (mediaTypes, string.Empty);
            }
            catch (HttpRequestException ex)
            {
                return (null, $"HTTP request failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (null, $"Error getting media types: {ex.Message}");
            }
        }

        public async Task<(List<string>? MediaList, string ErrorMessage)> GetMediaListAsync(string system, string mediaTitle)
        {
            if (string.IsNullOrEmpty(system) || string.IsNullOrEmpty(mediaTitle))
                return (null, "System name and media title are required");

            string url = $"{ApiUrl}/Media/MediaList?systemName={system}&mediaType={mediaTitle}&mediaSet=default";

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
                var httpResponse = await _httpClient.GetAsync(url).ConfigureAwait(false);
                httpResponse.EnsureSuccessStatusCode();

                string jsonString = await httpResponse.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(jsonString))
                    return (null, "Empty response from API");

                var mediaList = DeserializeJsonToList(jsonString);
                if (mediaList == null)
                    return (null, "Failed to deserialize media list");

                return (mediaList, string.Empty);
            }
            catch (HttpRequestException ex)
            {
                return (null, $"HTTP request failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (null, $"Error getting media list: {ex.Message}");
            }
        }

        private static List<string>? DeserializeJsonToList(string jsonString)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(jsonString);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("data", out JsonElement dataElement))
                    return null;

                List<string> dataList = new List<string>();
                foreach (JsonElement element in dataElement.EnumerateArray())
                {
                    string? value = element.GetString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        dataList.Add(value);
                    }
                }
                return dataList;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}