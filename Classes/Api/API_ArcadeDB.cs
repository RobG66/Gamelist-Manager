using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.Classes.Api
{
    internal class API_ArcadeDB
    {
        private const string ApiUrl = "http://adb.arcadeitalia.net/service_scraper.php";
        private readonly HttpClient _httpClient;

        public API_ArcadeDB(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private async Task<(bool Success, string Json, string ErrorMessage)> FetchFromApi(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return (true, json, string.Empty);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                return (false, string.Empty, $"HTTP request failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Error fetching from API: {ex.Message}");
            }
        }

        private static void SaveToCache(string json, string cacheFolder, string romName)
        {
            if (string.IsNullOrEmpty(cacheFolder) || string.IsNullOrEmpty(json))
                return;

            try
            {
                Directory.CreateDirectory(cacheFolder);
                string filePath = Path.Combine(cacheFolder, $"{romName}.json");
                File.WriteAllText(filePath, json);
            }
            catch
            {
                // Ignore cache write errors
            }
        }

        private static JsonElement? ReadFromCache(string romName, string cacheFolder)
        {
            if (string.IsNullOrEmpty(cacheFolder))
                return null;

            string filePath = Path.Combine(cacheFolder, $"{romName}.json");

            if (!File.Exists(filePath))
                return null;

            try
            {
                string json = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    try { File.Delete(filePath); } catch { }
                    return null;
                }

                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.Clone();
            }
            catch
            {
                try { File.Delete(filePath); } catch { }
                return null;
            }
        }

        private static JsonElement MergeJsonElements(JsonElement? metadata, JsonElement? media)
        {
            var merged = new Dictionary<string, JsonElement>();

            // Add all properties from metadata first
            if (metadata.HasValue)
            {
                foreach (var prop in metadata.Value.EnumerateObject())
                {
                    merged[prop.Name] = prop.Value.Clone();
                }
            }

            // Overlay properties from media (overwrites duplicates)
            if (media.HasValue)
            {
                foreach (var prop in media.Value.EnumerateObject())
                {
                    merged[prop.Name] = prop.Value.Clone();
                }
            }

            // Serialize to JSON and parse back to JsonElement
            string mergedJson = JsonSerializer.Serialize(merged);
            using var doc = JsonDocument.Parse(mergedJson);
            return doc.RootElement.Clone();
        }

        private static string GetFileExtension(string mediaName)
        {
            string extension = "png";
            if (mediaName == "video")
                extension = "mp4";

            if (mediaName == "manual")
                extension = "pdf";

            return extension;
        }

        private static string GetPropertyValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var value) && value.ValueKind != JsonValueKind.Null)
                return value.ToString();

            return string.Empty;
        }

        private static void AddMedia(ScrapedGameData data, JsonElement element, string sourceProperty, string mediaType)
        {
            if (string.IsNullOrEmpty(sourceProperty))
                return;

            string url = GetPropertyValue(element, sourceProperty);
            if (string.IsNullOrEmpty(url))
                return;

            data.Media.Add(new ScrapedGameData.MediaResult
            {
                Url = url,
                FileExtension = GetFileExtension(mediaType),
                Region = string.Empty,
                MediaType = mediaType
            });
        }

        private static ScrapedGameData ParseGameElement(JsonElement element, ScraperParameters parameters)
        {
            var result = new ScrapedGameData();

            if (parameters.ElementsToScrape == null)
                return result;

            foreach (string elementType in parameters.ElementsToScrape)
            {
                switch (elementType)
                {
                    case "publisher":
                        string publisher = GetPropertyValue(element, "manufacturer");
                        if (!string.IsNullOrEmpty(publisher))
                            result.Data["publisher"] = publisher;
                        break;

                    case "players":
                        string players = GetPropertyValue(element, "players");
                        if (!string.IsNullOrEmpty(players))
                            result.Data["players"] = players;
                        break;

                    case "rating":
                        string ratingStr = GetPropertyValue(element, "rate");
                        if (int.TryParse(ratingStr, out int rating))
                        {
                            // Convert 0-100 to 0.0-1.0 format
                            if (rating == 100)
                                result.Data["rating"] = "1";
                            else if (rating > 0)
                                result.Data["rating"] = "." + rating.ToString().TrimStart('0');
                        }
                        break;

                    case "desc":
                        string desc = GetPropertyValue(element, "history");
                        if (!string.IsNullOrEmpty(desc))
                            result.Data["desc"] = desc;
                        break;

                    case "name":
                        string name = GetPropertyValue(element, "title");
                        if (!string.IsNullOrEmpty(name))
                            result.Data["name"] = name;
                        break;

                    case "genre":
                        string genre = GetPropertyValue(element, "genre");
                        if (!string.IsNullOrEmpty(genre))
                            result.Data["genre"] = genre;
                        break;

                    case "releasedate":
                        string year = GetPropertyValue(element, "year");
                        if (!string.IsNullOrEmpty(year))
                            result.Data["releasedate"] = year;
                        break;

                    case "image":
                        AddMedia(result, element, parameters.ImageSource ?? string.Empty, "image");
                        break;

                    case "thumbnail":
                        AddMedia(result, element, parameters.ThumbnailSource ?? string.Empty, "thumbnail");
                        break;

                    case "marquee":
                        AddMedia(result, element, parameters.MarqueeSource ?? string.Empty, "marquee");
                        break;

                    case "wheel":
                        AddMedia(result, element, parameters.WheelSource ?? string.Empty, "wheel");
                        break;

                    case "bezel":
                        AddMedia(result, element, "url_image_bezel", "bezel");
                        break;

                    case "manual":
                        AddMedia(result, element, "url_manual", "manual");
                        break;

                    case "titleshot":
                        AddMedia(result, element, "url_image_title", "titleshot");
                        break;

                    case "video":
                        string videoSource = parameters.VideoSource ?? "url_video_shortplay";
                        AddMedia(result, element, videoSource, "video");
                        break;
                }
            }

            return result;
        }

        public async Task<(bool Success, ScrapedGameData Data, List<string> Messages)> ScrapeArcadeDBAsync(ScraperParameters parameters)
        {
            var messages = new List<string>();

            if (parameters.ElementsToScrape == null || parameters.ElementsToScrape.Count == 0)
            {
                messages.Add("No elements to scrape");
                return (false, new ScrapedGameData(), messages);
            }

            if (string.IsNullOrEmpty(parameters.RomFileName))
            {
                messages.Add("ROM filename is required");
                return (false, new ScrapedGameData(), messages);
            }

            string romName = Path.GetFileNameWithoutExtension(parameters.RomFileName);

            // Try cache first
            if (parameters.ScrapeByCache && !string.IsNullOrEmpty(parameters.CacheFolder))
            {
                var cached = ReadFromCache(romName, parameters.CacheFolder);

                if (cached.HasValue)
                {
                    return (true, ParseGameElement(cached.Value, parameters), messages);
                }

                if (parameters.SkipNonCached)
                {
                    messages.Add($"{romName} not found in cache and skip non-cached enabled");
                    return (false, new ScrapedGameData(), messages);
                }
            }

            JsonElement? metadataElement = null;
            JsonElement? mediaElement = null;

            // Fetch from API - always fetch both metadata and media
            try
            {
                // Fetch metadata
                string metadataUrl = $"{ApiUrl}?ajax=query_mame&game_name={romName}";
                var (metadataFetched, metadataJson, metadataError) = await FetchFromApi(metadataUrl);

                if (!metadataFetched)
                {
                    messages.Add(string.IsNullOrEmpty(metadataError)
                        ? $"Failed to retrieve metadata from ArcadeDB for {romName}"
                        : metadataError);
                }
                else
                {
                    using var doc = JsonDocument.Parse(metadataJson);

                    if (doc.RootElement.TryGetProperty("result", out var resultArray) &&
                        resultArray.ValueKind == JsonValueKind.Array)
                    {
                        var gameElement = resultArray.EnumerateArray().FirstOrDefault();

                        if (gameElement.ValueKind != JsonValueKind.Undefined)
                        {
                            metadataElement = gameElement.Clone();
                        }
                    }
                }

                // Fetch media
                string mediaUrl = $"{ApiUrl}?ajax=query_mame_media&game_name={romName}";
                var (mediaFetched, mediaJson, mediaError) = await FetchFromApi(mediaUrl);

                if (!mediaFetched)
                {
                    messages.Add(string.IsNullOrEmpty(mediaError)
                        ? $"Failed to retrieve media from ArcadeDB for {romName}"
                        : mediaError);
                }
                else
                {
                    using var doc = JsonDocument.Parse(mediaJson);

                    if (doc.RootElement.TryGetProperty("result", out var resultArray) &&
                        resultArray.ValueKind == JsonValueKind.Array)
                    {
                        var gameElement = resultArray.EnumerateArray().FirstOrDefault();

                        if (gameElement.ValueKind != JsonValueKind.Undefined)
                        {
                            mediaElement = gameElement.Clone();
                        }
                    }
                }

                // Check if we got at least something
                if (!metadataElement.HasValue && !mediaElement.HasValue)
                {
                    messages.Add($"Game {romName} not found in ArcadeDB");
                    return (false, new ScrapedGameData(), messages);
                }

                // Merge both responses into one
                var mergedElement = MergeJsonElements(metadataElement, mediaElement);

                // Save merged result to cache
                if (!string.IsNullOrEmpty(parameters.CacheFolder))
                {
                    string mergedJson = JsonSerializer.Serialize(mergedElement);
                    SaveToCache(mergedJson, parameters.CacheFolder, romName);
                }

                // Parse the merged element
                return (true, ParseGameElement(mergedElement, parameters), messages);
            }
            catch (JsonException ex)
            {
                messages.Add($"Failed to parse JSON for {romName}: {ex.Message}");
                return (false, new ScrapedGameData(), messages);
            }
            catch (Exception ex)
            {
                messages.Add($"Error scraping {romName}: {ex.Message}");
                return (false, new ScrapedGameData(), messages);
            }
        }

        public async Task<Dictionary<string, ScrapedGameData>> ScrapeArcadeDBBatchAsync(
            List<string> romFileNames,
            ScraperParameters parameters,
            CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, ScrapedGameData>();

            if (parameters.ElementsToScrape == null || parameters.ElementsToScrape.Count == 0)
                return results;

            var romNames = romFileNames
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => name!)
                .ToList();

            if (romNames.Count == 0)
                return results;

            // Storage for fetched data
            var metadataElements = new Dictionary<string, JsonElement>();
            var mediaElements = new Dictionary<string, JsonElement>();

            try
            {
                string gameNameQuery = string.Join(";", romNames);

                // Fetch metadata
                string metadataUrl = $"{ApiUrl}?ajax=query_mame&game_name={gameNameQuery}";
                var (metadataFetched, metadataJson, metadataError) = await FetchFromApi(metadataUrl, cancellationToken);

                if (metadataFetched)
                {
                    using var doc = JsonDocument.Parse(metadataJson);

                    if (doc.RootElement.TryGetProperty("result", out var resultArray) &&
                        resultArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var gameElement in resultArray.EnumerateArray())
                        {
                            string gameName = GetPropertyValue(gameElement, "game_name");

                            if (!string.IsNullOrEmpty(gameName))
                            {
                                metadataElements[gameName] = gameElement.Clone();
                            }
                        }
                    }
                }

                // Fetch media
                string mediaUrl = $"{ApiUrl}?ajax=query_mame_media&game_name={gameNameQuery}";
                var (mediaFetched, mediaJson, mediaError) = await FetchFromApi(mediaUrl, cancellationToken);

                if (mediaFetched)
                {
                    using var doc = JsonDocument.Parse(mediaJson);

                    if (doc.RootElement.TryGetProperty("result", out var resultArray) &&
                        resultArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var gameElement in resultArray.EnumerateArray())
                        {
                            string gameName = GetPropertyValue(gameElement, "game_name");

                            if (!string.IsNullOrEmpty(gameName))
                            {
                                mediaElements[gameName] = gameElement.Clone();
                            }
                        }
                    }
                }

                // Merge and process results for each ROM
                var allGameNames = new HashSet<string>(metadataElements.Keys);
                allGameNames.UnionWith(mediaElements.Keys);

                foreach (string gameName in allGameNames)
                {
                    JsonElement? metaElem = metadataElements.ContainsKey(gameName)
                        ? metadataElements[gameName]
                        : null;

                    JsonElement? mediaElem = mediaElements.ContainsKey(gameName)
                        ? mediaElements[gameName]
                        : null;

                    // Merge the two responses
                    var mergedElement = MergeJsonElements(metaElem, mediaElem);

                    // Save merged result to cache
                    if (!string.IsNullOrEmpty(parameters.CacheFolder))
                    {
                        string mergedJson = JsonSerializer.Serialize(mergedElement);
                        SaveToCache(mergedJson, parameters.CacheFolder, gameName);
                    }

                    var gameData = ParseGameElement(mergedElement, parameters);
                    results[gameName] = gameData;
                }

                // Mark games that weren't found
                var foundNames = results.Keys.ToHashSet();
                foreach (string name in romNames.Where(n => !foundNames.Contains(n)))
                {
                    var notFound = new ScrapedGameData();
                    notFound.Messages.Add($"Game {name} not found in ArcadeDB");
                    results[name] = notFound;
                }

                return results;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (JsonException ex)
            {
                foreach (string name in romNames)
                {
                    var failResult = new ScrapedGameData();
                    failResult.Messages.Add($"Failed to parse JSON for {name}: {ex.Message}");
                    results[name] = failResult;
                }
                return results;
            }
            catch (Exception ex)
            {
                foreach (string name in romNames)
                {
                    var failResult = new ScrapedGameData();
                    failResult.Messages.Add($"Error scraping {name}: {ex.Message}");
                    results[name] = failResult;
                }
                return results;
            }
        }
    }
}