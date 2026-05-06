using Gamelist_Manager.Classes.Api;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.Classes.Helpers
{
    internal class EmuMoviesMediaCacheHelper
    {
        private const string ApiUrl = "https://api3.emumovies.com/api";

        private readonly HttpClient _httpClient;
        private readonly string _bearerToken;

        private static List<string> _mediaTypes = new();
        private static readonly ConcurrentDictionary<(string SystemId, string MediaType), List<string>> _mediaListCache = new();
        private static readonly SemaphoreSlim _fetchLock = new(1, 1);

        public EmuMoviesMediaCacheHelper(HttpClient httpClient, string bearerToken)
        {
            _httpClient = httpClient;
            _bearerToken = bearerToken;
        }

        public async Task PopulateMediaListsAsync(string systemId, ScraperProperties scraperProperties, Action<string>? log = null, CancellationToken cancellationToken = default)
        {
            bool hasListsForSystem = _mediaTypes.Count > 0 &&
                _mediaTypes.TrueForAll(mt => _mediaListCache.ContainsKey((systemId, mt)));

            if (hasListsForSystem)
            {
                foreach (string mediaType in _mediaTypes)
                {
                    if (_mediaListCache.TryGetValue((systemId, mediaType), out var cachedList))
                        scraperProperties.EmuMoviesMediaLists[mediaType] = cachedList;
                }
                return;
            }

            // If WaitAsync is cancelled before acquiring, the semaphore is not held and
            // the finally block is not reached — so Release() is only called when acquired.
            await _fetchLock.WaitAsync(cancellationToken);
            bool downloading = false;
            try
            {
                // Re-check inside lock in case another thread populated while we waited
                bool stillNeeded = _mediaTypes.Count == 0 ||
                    !_mediaTypes.TrueForAll(mt => _mediaListCache.ContainsKey((systemId, mt)));

                if (!stillNeeded)
                    return;

                log?.Invoke("Downloading EmuMovies media lists...");
                downloading = true;

                var (success, mediaTypes, error) = await GetMediaTypesAsync(systemId, cancellationToken);
                if (!success)
                    throw new InvalidOperationException(error);

                _mediaTypes = mediaTypes;

                foreach (string mediaType in _mediaTypes)
                {
                    var key = (systemId, mediaType);
                    if (_mediaListCache.ContainsKey(key))
                    {
                        scraperProperties.EmuMoviesMediaLists[mediaType] = _mediaListCache[key];
                        continue;
                    }

                    var (listSuccess, mediaList, _) = await GetMediaListAsync(systemId, mediaType, cancellationToken);
                    if (!listSuccess)
                        mediaList = new List<string>();

                    _mediaListCache[key] = mediaList;
                    scraperProperties.EmuMoviesMediaLists[mediaType] = mediaList;
                }
            }
            catch (OperationCanceledException) when (downloading)
            {
                // Discard any partially-built static state so the next attempt starts clean.
                Clear();
                throw;
            }
            finally
            {
                _fetchLock.Release();
            }
        }

        private async Task<(bool Success, List<string> MediaTypes, string ErrorMessage)> GetMediaTypesAsync(string system, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(system))
                return (false, new List<string>(), "System name is required");

            string url = $"{ApiUrl}/Media/MediaTypes?systemName={system}";

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
                var httpResponse = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                httpResponse.EnsureSuccessStatusCode();

                string jsonString = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

                if (string.IsNullOrEmpty(jsonString))
                    return (false, new List<string>(), "Empty response from API");

                var types = DeserializeJsonToList(jsonString);
                if (types == null)
                    return (false, new List<string>(), "Failed to deserialize media types");

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

        private async Task<(bool Success, List<string> MediaList, string ErrorMessage)> GetMediaListAsync(string system, string mediaTitle, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(system) || string.IsNullOrEmpty(mediaTitle))
                return (false, new List<string>(), "System name and media title are required");

            string url = $"{ApiUrl}/Media/MediaList?systemName={system}&mediaType={mediaTitle}&mediaSet=default";

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
                var httpResponse = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                httpResponse.EnsureSuccessStatusCode();

                string jsonString = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

                if (string.IsNullOrEmpty(jsonString))
                    return (false, new List<string>(), "Empty response from API");

                var list = DeserializeJsonToList(jsonString);
                if (list == null)
                    return (false, new List<string>(), "Failed to deserialize media list");

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
                        dataList.Add(value);
                }
                return dataList;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public void Clear()
        {
            _mediaListCache.Clear();
            _mediaTypes = new();
        }
    }
}