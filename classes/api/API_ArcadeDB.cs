using GamelistManager.pages;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace GamelistManager.classes.api
{
    internal class API_ArcadeDB
    {
        private const string ApiUrl = "http://adb.arcadeitalia.net/service_scraper.php";
        private readonly HttpClient _httpClient;

        public API_ArcadeDB(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private async Task<(string Json, string ErrorMessage)> FetchFromApi(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return (json, string.Empty);
            }
            catch (HttpRequestException ex)
            {
                return (string.Empty, $"HTTP request failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (string.Empty, $"Error fetching from API: {ex.Message}");
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
            var result = new ScrapedGameData { WasSuccessful = true };

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

        public async Task<ScrapedGameData> ScrapeArcadeDBAsync(ScraperParameters parameters)
        {
            var result = new ScrapedGameData();

            if (parameters.ElementsToScrape == null || parameters.ElementsToScrape.Count == 0)
            {
                result.WasSuccessful = false;
                result.Messages.Add("No elements to scrape");
                return result;
            }

            if (string.IsNullOrEmpty(parameters.RomFileName))
            {
                result.WasSuccessful = false;
                result.Messages.Add("ROM filename is required");
                return result;
            }

            string romName = Path.GetFileNameWithoutExtension(parameters.RomFileName);

            // Try cache first
            if (parameters.ScrapeByCache && !string.IsNullOrEmpty(parameters.CacheFolder))
            {
                var cached = ReadFromCache(romName, parameters.CacheFolder);

                if (cached.HasValue)
                {
                    return ParseGameElement(cached.Value, parameters);
                }

                if (parameters.SkipNonCached)
                {
                    result.WasSuccessful = false;
                    result.Messages.Add($"{romName} not found in cache and skip non-cached enabled");
                    return result;
                }
            }

            // Fetch from API
            string url = $"{ApiUrl}?ajax=query_mame&game_name={romName}";
            var (json, fetchError) = await FetchFromApi(url);

            if (string.IsNullOrEmpty(json))
            {
                result.WasSuccessful = false;
                result.Messages.Add(string.IsNullOrEmpty(fetchError)
                    ? $"Failed to retrieve data from ArcadeDB for {romName}"
                    : fetchError);
                return result;
            }

            // Parse response
            try
            {
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("result", out var resultArray) ||
                    resultArray.ValueKind != JsonValueKind.Array)
                {
                    result.WasSuccessful = false;
                    result.Messages.Add($"Invalid response format from ArcadeDB for {romName}");
                    return result;
                }

                var gameElement = resultArray.EnumerateArray().FirstOrDefault();

                if (gameElement.ValueKind == JsonValueKind.Undefined)
                {
                    result.WasSuccessful = false;
                    result.Messages.Add($"Game {romName} not found in ArcadeDB");
                    return result;
                }

                // Save to cache
                if (!string.IsNullOrEmpty(parameters.CacheFolder))
                {
                    SaveToCache(gameElement.GetRawText(), parameters.CacheFolder, romName);
                }

                result = ParseGameElement(gameElement, parameters);
                return result;
            }
            catch (JsonException ex)
            {
                result.WasSuccessful = false;
                result.Messages.Add($"Failed to parse JSON for {romName}: {ex.Message}");
                return result;
            }
            catch (Exception ex)
            {
                result.WasSuccessful = false;
                result.Messages.Add($"Error scraping {romName}: {ex.Message}");
                return result;
            }
        }

        public async Task<Dictionary<string, ScrapedGameData>> ScrapeArcadeDBBatchAsync(
            List<string> romFileNames,
            ScraperParameters parameters)
        {
            var results = new Dictionary<string, ScrapedGameData>();

            if (parameters.ElementsToScrape == null || parameters.ElementsToScrape.Count == 0)
                return results;

            var romNames = romFileNames
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();

            if (romNames.Count == 0)
                return results;

            // Build API URL
            string gameNameQuery = string.Join(";", romNames);
            string url = $"{ApiUrl}?ajax=query_mame&game_name={gameNameQuery}";

            var (json, fetchError) = await FetchFromApi(url);

            if (string.IsNullOrEmpty(json))
            {
                string errorMsg = string.IsNullOrEmpty(fetchError)
                    ? "Failed to retrieve data from ArcadeDB"
                    : fetchError;

                foreach (string name in romNames)
                {
                    var failResult = new ScrapedGameData { WasSuccessful = false };
                    failResult.Messages.Add($"{errorMsg} for {name}");
                    results[name] = failResult;
                }
                return results;
            }

            // Parse response
            try
            {
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("result", out var resultArray) ||
                    resultArray.ValueKind != JsonValueKind.Array)
                {
                    foreach (string name in romNames)
                    {
                        var failResult = new ScrapedGameData { WasSuccessful = false };
                        failResult.Messages.Add($"Invalid response format from ArcadeDB for {name}");
                        results[name] = failResult;
                    }
                    return results;
                }

                // Process each game in response
                foreach (var gameElement in resultArray.EnumerateArray())
                {
                    string gameName = GetPropertyValue(gameElement, "game_name");

                    if (string.IsNullOrEmpty(gameName))
                        continue;

                    // Save to cache
                    if (!string.IsNullOrEmpty(parameters.CacheFolder))
                    {
                        SaveToCache(gameElement.GetRawText(), parameters.CacheFolder, gameName);
                    }

                    var gameData = ParseGameElement(gameElement, parameters);
                    results[gameName] = gameData;
                }

                // Mark games that weren't found
                var foundNames = results.Keys.ToHashSet();
                foreach (string name in romNames.Where(n => !foundNames.Contains(n)))
                {
                    var notFound = new ScrapedGameData { WasSuccessful = false };
                    notFound.Messages.Add($"Game {name} not found in ArcadeDB");
                    results[name] = notFound;
                }

                return results;
            }
            catch (JsonException ex)
            {
                foreach (string name in romNames)
                {
                    var failResult = new ScrapedGameData { WasSuccessful = false };
                    failResult.Messages.Add($"Failed to parse JSON for {name}: {ex.Message}");
                    results[name] = failResult;
                }
                return results;
            }
            catch (Exception ex)
            {
                foreach (string name in romNames)
                {
                    var failResult = new ScrapedGameData { WasSuccessful = false };
                    failResult.Messages.Add($"Error scraping {name}: {ex.Message}");
                    results[name] = failResult;
                }
                return results;
            }
        }
    }
}