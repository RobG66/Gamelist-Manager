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
using System.Threading.Tasks;

namespace Gamelist_Manager.Classes.Api
{
    internal class API_EmuMovies
    {
        private const string ApiUrl = "https://api3.emumovies.com/api";

        private string _bearerToken;
        private readonly HttpClient _httpClient;

        public API_EmuMovies(HttpClient httpClient)
        {
            _httpClient = httpClient;
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

                return !string.IsNullOrEmpty(filename) ? Path.GetExtension(filename) : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void AddMedia(ScrapedGameData data, ScrapedGameData.MediaResult? media)
        {
            if (media != null)
                data.Media.Add(media);
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

        public (bool Success, ScrapedGameData Data, List<string> Messages) ScrapeEmuMoviesAsync(
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

            // Pre-normalize search terms once for the entire scrape operation
            // rather than re-normalizing on every element lookup
            string romFileNameWithoutExtension = Path.GetFileNameWithoutExtension(parameters.RomFileName ?? string.Empty);
            var normalizedSearchTerms = new NormalizedSearchTerms(romFileNameWithoutExtension, parameters.RomName ?? string.Empty);

            foreach (string element in parameters.ElementsToScrape)
            {
                string url = string.Empty;

                switch (element)
                {
                    case "fanart":
                        url = GetMediaUrl("Background", parameters, mediaLists, normalizedSearchTerms);
                        AddMedia(result, GetMediaResult(url, "fanart"));
                        break;

                    case "boxback":
                        url = GetMediaUrl("BoxBack", parameters, mediaLists, normalizedSearchTerms);
                        AddMedia(result, GetMediaResult(url, "boxback"));
                        break;

                    case "boxart":
                        url = GetMediaUrl(parameters.BoxArtSource, parameters, mediaLists, normalizedSearchTerms);
                        AddMedia(result, GetMediaResult(url, "boxart"));
                        break;

                    case "mix":
                        url = GetMediaUrl(parameters.MixSource, parameters, mediaLists, normalizedSearchTerms);
                        AddMedia(result, GetMediaResult(url, "mix"));
                        break;

                    case "wheel":
                        url = GetMediaUrl(parameters.WheelSource, parameters, mediaLists, normalizedSearchTerms);
                        AddMedia(result, GetMediaResult(url, "wheel"));
                        break;

                    case "manual":
                        url = GetMediaUrl("Manual", parameters, mediaLists, normalizedSearchTerms);
                        AddMedia(result, GetMediaResult(url, "manual"));
                        break;

                    case "music":
                        url = GetMediaUrl("Music", parameters, mediaLists, normalizedSearchTerms);
                        AddMedia(result, GetMediaResult(url, "music"));
                        break;

                    case "image":
                        url = GetMediaUrl(parameters.ImageSource, parameters, mediaLists, normalizedSearchTerms);
                        AddMedia(result, GetMediaResult(url, "image"));
                        break;

                    case "titleshot":
                        url = GetMediaUrl("Title", parameters, mediaLists, normalizedSearchTerms);
                        AddMedia(result, GetMediaResult(url, "titleshot"));
                        break;

                    case "thumbnail":
                        url = GetMediaUrl(parameters.ThumbnailSource, parameters, mediaLists, normalizedSearchTerms);
                        AddMedia(result, GetMediaResult(url, "thumbnail"));
                        break;

                    case "marquee":
                        url = GetMediaUrl(parameters.MarqueeSource, parameters, mediaLists, normalizedSearchTerms);
                        AddMedia(result, GetMediaResult(url, "marquee"));
                        break;

                    case "cartridge":
                        url = GetMediaUrl(parameters.CartridgeSource, parameters, mediaLists, normalizedSearchTerms);
                        AddMedia(result, GetMediaResult(url, "cartridge"));
                        break;

                    case "video":
                        url = GetMediaUrl(parameters.VideoSource, parameters, mediaLists, normalizedSearchTerms);
                        AddMedia(result, GetMediaResult(url, "video"));
                        break;
                }
            }

            return (true, result, messages);
        }

        private string GetMediaUrl(
            string? remoteMediaType,
            ScraperParameters parameters,
            Dictionary<string, List<string>> mediaLists,
            NormalizedSearchTerms normalizedSearchTerms)
        {
            if (string.IsNullOrEmpty(remoteMediaType))
                return string.Empty;

            if (string.IsNullOrEmpty(parameters.RomName) || string.IsNullOrEmpty(parameters.RomFileName))
                return string.Empty;

            if (!mediaLists.TryGetValue(remoteMediaType, out List<string>? mediaList) || mediaList == null)
                return string.Empty;

            string remoteFileName = FindString(normalizedSearchTerms, mediaList);

            if (string.IsNullOrEmpty(remoteFileName))
                return string.Empty;

            string fileFormat = Path.GetExtension(remoteFileName);
            if (string.IsNullOrEmpty(fileFormat))
                return string.Empty;

            string encodedUrl = Uri.EscapeDataString(remoteFileName);
            return $"{ApiUrl}/Media/Download?accessToken={parameters.UserAccessToken}&systemName={parameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={encodedUrl}";
        }

        private static string FindString(NormalizedSearchTerms terms, List<string> mediaList)
        {
            // Ensure cache built once for this media list
            EmuMoviesTextSearchHelper.EnsureCached(mediaList);

            // First pass: exact filename match (case-insensitive, extension stripped)
            string? match = EmuMoviesTextSearchHelper.FindExactMatch(terms.RomFileName, mediaList);
            if (!string.IsNullOrEmpty(match)) return match;

            match = EmuMoviesTextSearchHelper.FindExactMatch(terms.RomName, mediaList);
            if (!string.IsNullOrEmpty(match)) return match;

            // Second pass: normalized fuzzy match — use pre-normalized terms, no NormalizeText call here
            match = EmuMoviesTextSearchHelper.FindTextMatchNormalized(terms.NormalizedRomFileName, mediaList);
            if (!string.IsNullOrEmpty(match)) return match;

            match = EmuMoviesTextSearchHelper.FindTextMatchNormalized(terms.NormalizedRomName, mediaList);
            if (!string.IsNullOrEmpty(match)) return match;

            return string.Empty;
        }

        public async Task<(bool Success, string AccessToken, string ErrorMessage)> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return (false, string.Empty, "Username and password are required");

            var credentials = new { username, password };
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
                    return (false, string.Empty, $"Authentication failed: {httpResponse.StatusCode}");

                string responseBody = await httpResponse.Content.ReadAsStringAsync();

                using JsonDocument doc = JsonDocument.Parse(responseBody);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("data", out JsonElement data) &&
                    data.TryGetProperty("acessToken", out JsonElement accessTokenElement))
                {
                    string? token = accessTokenElement.GetString();
                    if (!string.IsNullOrEmpty(token))
                        return (true, token, string.Empty);
                }

                return (false, string.Empty, "Access token not found in response");
            }
            catch (HttpRequestException ex)
            {
                int lastColon = ex.Message.LastIndexOf(':');
                string reason = lastColon >= 0 ? ex.Message[(lastColon + 1)..].Trim() : ex.Message;
                return (false, string.Empty, reason);
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
    }

    internal readonly struct NormalizedSearchTerms
    {
        public readonly string RomFileName;
        public readonly string RomName;
        public readonly string NormalizedRomFileName;
        public readonly string NormalizedRomName;

        public NormalizedSearchTerms(string romFileName, string romName)
        {
            RomFileName = romFileName;
            RomName = romName;
            NormalizedRomFileName = EmuMoviesTextSearchHelper.GetNormalizedCached(romFileName);
            NormalizedRomName = EmuMoviesTextSearchHelper.GetNormalizedCached(romName);
        }
    }
}
