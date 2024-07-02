using System.Collections.Generic;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Text.RegularExpressions;

namespace GamelistManager
{
    internal static class API_EmuMovies
    {
        private static readonly Dictionary<(string, string), int> memo = new Dictionary<(string, string), int>();
        private static readonly string apiURL = "http://api3.emumovies.com/api";
        private static readonly string apiKey = "";


        public static async Task<ScraperData> ScrapeEmuMoviesAsync(ScraperParameters scraperParameters, ListBox listBox, Dictionary<string, List<string>> emumoviesMediaLists)
        {
            string folderName = null;
            string remoteElementName = null;
            string remoteDownloadURL = null;
            string destinationFolder = null;
            string fileName = null;
            string downloadPath = null;
            string fileToDownload = null;
            bool downloadResult = false;
            string result = null;
            List<string> mediaList = null;
    
            ScraperData scraperData = new ScraperData();
                       
            foreach (string element in scraperParameters.ElementsToScrape)
            {
                Console.WriteLine($"{element}");
                switch (element) {
                   case "fanart":
                        folderName = "images";
                        mediaList = emumoviesMediaLists["Background"];
                        if (mediaList != null)
                        {
                            result = FuzzySearch(scraperParameters.Name, mediaList, 5);
                            if (result == null)
                            {
                                result = FuzzySearch(scraperParameters.RomFileNameWithoutExtension, mediaList, 5);
                            }
                            Console.WriteLine(scraperParameters.Name);
                            Console.WriteLine(result);
                        }
                        break;

                  
                    case "boxback":
                        folderName = "images";
                        mediaList = emumoviesMediaLists["BoxBack"];
                        if (mediaList != null)
                        {
                            result = FuzzySearch(scraperParameters.Name, mediaList, 5);
                            if (result == null)
                            {
                                result = FuzzySearch(scraperParameters.RomFileNameWithoutExtension, mediaList, 5);
                            }
                            Console.WriteLine(scraperParameters.Name);
                            Console.WriteLine(result);
                        }
                        break;

                    case "manual":
                        folderName = "manuals";
                        mediaList = emumoviesMediaLists["Manual"];
                        if (mediaList != null)
                        {
                            result = FuzzySearch(scraperParameters.Name, mediaList, 5);
                            if (result == null)
                            {
                                result = FuzzySearch(scraperParameters.RomFileNameWithoutExtension, mediaList, 5);
                            }
                            Console.WriteLine(scraperParameters.Name);
                            Console.WriteLine(result);
                        }
                        break;

                    case "image":
                        folderName = "images";
                        remoteElementName = scraperParameters.ImageSource;
                        mediaList = emumoviesMediaLists[remoteElementName];
                        if (mediaList != null) { 
                            result = FuzzySearch(scraperParameters.Name, mediaList, 5);
                            if (result == null)
                            {
                                result = FuzzySearch(scraperParameters.RomFileNameWithoutExtension, mediaList, 5);
                            }

                            Console.WriteLine(scraperParameters.Name);
                            Console.WriteLine(result);
                        }
                        break;

                    case "thumbnail":
                        folderName = "images";
                        mediaList = emumoviesMediaLists[scraperParameters.BoxSource];
                        if (mediaList != null)
                        {
                            result = FuzzySearch(scraperParameters.Name, mediaList, 5);
                            if (result == null)
                            {
                                result = FuzzySearch(scraperParameters.RomFileNameWithoutExtension, mediaList, 5);
                            }
                            Console.WriteLine(scraperParameters.Name);
                            Console.WriteLine(result);
                        }
                        break;

                    case "marquee":
                        mediaList = emumoviesMediaLists[scraperParameters.LogoSource];
                        if (mediaList != null)
                        {
                            result = FuzzySearch(scraperParameters.Name, mediaList, 5);
                            if (result == null)
                            {
                                result = FuzzySearch(scraperParameters.RomFileNameWithoutExtension, mediaList, 5);
                            }
                            Console.WriteLine(scraperParameters.Name);
                            Console.WriteLine(result);
                        }

                        break;


                    case "video":
                        folderName = "videos";
                        mediaList = emumoviesMediaLists["Video_MP4"];
                        if (mediaList != null)
                        {
                            result = FuzzySearch(scraperParameters.Name, mediaList, 5);
                            if (result == null)
                            {
                                result = FuzzySearch(scraperParameters.RomFileNameWithoutExtension, mediaList, 5);
                            }
                            Console.WriteLine(scraperParameters.Name);
                            Console.WriteLine(result);
                        }
                        break;
                }
            }
            return scraperData;
        }

        public static int LevenshteinDistance(string s, string t, int threshold)
        {
            if (memo.TryGetValue((s, t), out int cachedValue)) return cachedValue;

            int n = s.Length;
            int m = t.Length;

            if (Math.Abs(n - m) > threshold) return int.MaxValue;

            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                int minDist = int.MaxValue;
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);

                    if (d[i, j] < minDist) minDist = d[i, j];
                }

                if (minDist > threshold) return int.MaxValue;
            }

            int result = d[n, m];
            memo[(s, t)] = result;
            return result;
        }

        public static string RemoveBracketedContent(string input)
        {
            return Regex.Replace(input, @"\s*\([^)]*\)", string.Empty).Trim();
        }

        public static string FuzzySearch(string searchName, List<string> names, int threshold)
        {
            string normalizedSearchName = RemoveBracketedContent(searchName);

            var closestMatch = names
                .Select(name => new { OriginalName = name, NormalizedName = RemoveBracketedContent(name) })
                .Select(item => new { item.OriginalName, Distance = LevenshteinDistance(normalizedSearchName, item.NormalizedName, threshold) })
                .Where(x => x.Distance <= threshold)
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            return closestMatch?.OriginalName;
        }

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            return client;
        }

        private static void ShowDownload(ListBox listBox, string message)
        {
            if (listBox.InvokeRequired)
            {
                listBox.Invoke((MethodInvoker)delegate
                {
                    listBox.Items.Add(message);
                    listBox.TopIndex = listBox.Items.Count - 1;
                });
            }
            else
            {
                listBox.Items.Add(message);
                listBox.TopIndex = listBox.Items.Count - 1;
            }
        }


        public static async Task<string> AuthenticateEmuMoviesAsync(string username, string password)
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

                using (var client = CreateHttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(responseBody))
                        {
                            JsonElement root = doc.RootElement;
                            JsonElement data = root.GetProperty("data");
                            JsonElement acessTokenElement = data.GetProperty("acessToken");
                            Console.WriteLine(acessTokenElement.ToString());
                            return acessTokenElement.GetString();
                        }
                    }
                    else
                    {
                        return null;
                    }
                }


            }

            public static async Task<List<string>> GetMediaTypes(string system)
            {
                string url = $"{apiURL}/Media/MediaTypes?systemName={system}";
                List<string> mediaTypes = await FetchAndParseJsonResponse(url);
                return mediaTypes;
            }

            public static async Task<List<string>> GetMediaList(string system, string mediaTitle)
            {
                string url = $"{apiURL}/Media/MediaList?systemName={system}&mediaType={mediaTitle}&mediaSet=default";
                List<string> mediaList = await FetchAndParseJsonResponse(url);
                return mediaList;
            }

            static async Task<List<string>> FetchAndParseJsonResponse(string url)
            {

                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response;
                    try
                    {
                        response = await client.GetAsync(url);
                        response.EnsureSuccessStatusCode();
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"HTTP request failed: {ex.Message}");
                        return null;
                    }

                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    try
                    {
                        JsonDocument doc = JsonDocument.Parse(jsonResponse);
                        JsonElement root = doc.RootElement;
                        JsonElement dataElement = root.GetProperty("data");

                        List<string> dataList = new List<string>();
                        foreach (JsonElement element in dataElement.EnumerateArray())
                        {
                            dataList.Add(element.GetString());
                        }
                        return dataList;
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"JSON parsing failed: {ex.Message}");
                        return null;
                    }
                }
            }
        }
}