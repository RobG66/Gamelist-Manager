using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.Classes.Api
{
    internal class API_EmuMovies
    {
        private const string ApiUrl = "https://api3.emumovies.com/api";

        // Developer bearer token for EmuMovies API
        private string _bearerToken;

        private readonly HttpClient _httpClient;

        public API_EmuMovies(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Get bearer token from configuration service (with environment variable fallback)
            _bearerToken = Gamelist_Manager.Services.Secrets.EmuMoviesBearerToken;
        }

        private static string GetFileExtension(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            try
            {
                var uri = new Uri(url);
                string rawQuery = uri.Query.TrimStart('?');
                string? filename = null;

                foreach (var part in rawQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    int eq = part.IndexOf('=');
                    if (eq > 0 && part[..eq].Trim().Equals("filename", StringComparison.OrdinalIgnoreCase))
                    {
                        filename = Uri.UnescapeDataString(part[(eq + 1)..]);
                        break;
                    }
                }

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

        public async Task<(bool Success, ScrapedGameData Data, List<string> Messages)> ScrapeEmuMoviesAsync(
            ScraperParameters parameters,
            Dictionary<string, List<string>> mediaLists)
        {
            var result = new ScrapedGameData();
            var messages = new List<string>();

            if (parameters.ElementsToScrape == null || parameters.ElementsToScrape.Count == 0)
            {
                messages.Add("No elements to scrape");
                return (false, result, messages);
            }

            foreach (string element in parameters.ElementsToScrape)
            {
                string url = string.Empty;

                switch (element)
                {
                    case "fanart":
                        url = await GetMediaUrl("Background", parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url, "fanart"));
                        break;

                    case "boxback":
                        url = await GetMediaUrl("BoxBack", parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url, "boxback"));
                        break;

                    case "boxart":
                        url = await GetMediaUrl(parameters.BoxArtSource, parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url, "boxart"));
                        break;

                    case "mix":
                        url = await GetMediaUrl(parameters.MixSource, parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url, "mix"));
                        break;

                    case "wheel":
                        url = await GetMediaUrl(parameters.WheelSource, parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url, "wheel"));
                        break;

                    case "manual":
                        url = await GetMediaUrl("Manual", parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url, "manual"));
                        break;

                    case "music":
                        url = await GetMediaUrl("Music", parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url, "music"));
                        break;

                    case "image":
                        url = await GetMediaUrl(parameters.ImageSource, parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url, "image"));
                        break;

                    case "titleshot":
                        url = await GetMediaUrl("Title", parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url, "titleshot"));
                        break;

                    case "thumbnail":
                        url = await GetMediaUrl(parameters.ThumbnailSource, parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url, "thumbnail"));
                        break;

                    case "marquee":
                        url = await GetMediaUrl(parameters.MarqueeSource, parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url, "marquee"));
                        break;

                    case "cartridge":
                        url = await GetMediaUrl(parameters.CartridgeSource, parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url, "cartridge"));
                        break;

                    case "video":
                        url = await GetMediaUrl(parameters.VideoSource, parameters, mediaLists);
                        AddMedia(result, GetMediaResult(url, "video"));
                        break;
                }
            }

            return (true, result, messages);
        }

        private async Task<string> GetMediaUrl(
            string? remoteMediaType,
            ScraperParameters parameters,
            Dictionary<string, List<string>> mediaLists)
        {
            if (string.IsNullOrEmpty(remoteMediaType))
                return string.Empty;

            if (string.IsNullOrEmpty(parameters.RomName) || string.IsNullOrEmpty(parameters.RomFileName))
                return string.Empty;

            if (!mediaLists.TryGetValue(remoteMediaType, out List<string>? mediaList) || mediaList == null)
                return string.Empty;

            string romFileNameWithoutExtension = Path.GetFileNameWithoutExtension(parameters.RomFileName);
            List<string> searchItems = new List<string> { romFileNameWithoutExtension, parameters.RomName, };

            string remoteFileName = FindString(searchItems, mediaList);

            if (string.IsNullOrEmpty(remoteFileName))
                return string.Empty;

            string fileFormat = Path.GetExtension(remoteFileName);
            if (string.IsNullOrEmpty(fileFormat))
                return string.Empty;

            string encodedURL = WebUtility.UrlEncode(remoteFileName);
            string downloadURL = $"{ApiUrl}/Media/Download?accessToken={parameters.UserAccessToken}&systemName={parameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={encodedURL}";

            return downloadURL;
        }

        private static string FindString(List<string> searchText, List<string> mediaList)
        {
            // First pass: exact filename match (case-insensitive, extension stripped)
            foreach (string item in searchText)
            {
                string? exactMatch = TextSearchHelper.FindExactMatch(item, mediaList);
                if (!string.IsNullOrEmpty(exactMatch))
                    return exactMatch;
            }

            // Second pass: normalized fuzzy match
            foreach (string item in searchText)
            {
                string? result = TextSearchHelper.FindTextMatch(item, mediaList);
                if (!string.IsNullOrEmpty(result))
                    return result;
            }

            return string.Empty;
        }

        public async Task<(bool Success, string AccessToken, string ErrorMessage)> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return (false, string.Empty, "Username and password are required");
            }

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
                    return (false, string.Empty, $"Authentication failed: {httpResponse.StatusCode}");
                }

                string responseBody = await httpResponse.Content.ReadAsStringAsync();

                using JsonDocument doc = JsonDocument.Parse(responseBody);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("data", out JsonElement data) &&
                    data.TryGetProperty("acessToken", out JsonElement accessTokenElement))
                {
                    string? token = accessTokenElement.GetString();
                    if (!string.IsNullOrEmpty(token))
                    {
                        return (true, token, string.Empty);
                    }
                }

                return (false, string.Empty, "Access token not found in response");
            }
            catch (HttpRequestException ex)
            {
                return (false, string.Empty, $"HTTP request failed: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return (false, string.Empty, $"Failed to parse JSON response: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Authentication error: {ex.Message}");
            }
        }

        public async Task<(bool Success, List<string> MediaTypes, string ErrorMessage)> GetMediaTypesAsync(string system, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(system))
            {
                return (false, new List<string>(), "System name is required");
            }

            string url = $"{ApiUrl}/Media/MediaTypes?systemName={system}";

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
                var httpResponse = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                httpResponse.EnsureSuccessStatusCode();

                string jsonString = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

                if (string.IsNullOrEmpty(jsonString))
                {
                    return (false, new List<string>(), "Empty response from API");
                }

                var types = DeserializeJsonToList(jsonString);
                if (types == null)
                {
                    return (false, new List<string>(), "Failed to deserialize media types");
                }

                return (true, types, string.Empty);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                return (false, new List<string>(), $"HTTP request failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, new List<string>(), $"Error getting media types: {ex.Message}");
            }
        }

        public async Task<(bool Success, List<string> MediaList, string ErrorMessage)> GetMediaListAsync(string system, string mediaTitle, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(system) || string.IsNullOrEmpty(mediaTitle))
            {
                return (false, new List<string>(), "System name and media title are required");
            }

            string url = $"{ApiUrl}/Media/MediaList?systemName={system}&mediaType={mediaTitle}&mediaSet=default";

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
                var httpResponse = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                httpResponse.EnsureSuccessStatusCode();

                string jsonString = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

                if (string.IsNullOrEmpty(jsonString))
                {
                    return (false, new List<string>(), "Empty response from API");
                }

                var list = DeserializeJsonToList(jsonString);
                if (list == null)
                {
                    return (false, new List<string>(), "Failed to deserialize media list");
                }

                return (true, list, string.Empty);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                return (false, new List<string>(), $"HTTP request failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, new List<string>(), $"Error getting media list: {ex.Message}");
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