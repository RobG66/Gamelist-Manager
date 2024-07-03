using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using FuzzySharp;

namespace GamelistManager
{
    internal class API_EmuMovies
    {
        private readonly ConcurrentDictionary<(string, string), int> memo = new ConcurrentDictionary<(string, string), int>();
        private readonly string apiURL = "http://api3.emumovies.com/api";
        private readonly string apiKey = "";

        public async Task<ScraperData> ScrapeEmuMoviesAsync(ScraperParameters scraperParameters, ListBox ListBoxControl, Dictionary<string, List<string>> emumoviesMediaLists)
        {
            string folderName = null;
            string remoteElementName = null;
            string remoteDownloadURL = null;
            string destinationFolder = null;
            string remoteFileName = null;
            string downloadPath = null;
            string newFileName = null;
            string fileToDownload = null;
            bool downloadResult = false;
            string remoteMediaType = null;
            List<string> mediaList = null;
            string fileFormat = null;
            ScraperData scraperData = new ScraperData();

            Console.WriteLine($"scraping {scraperParameters.Name}");

            foreach (string element in scraperParameters.ElementsToScrape)
            {
                switch (element)
                {
                    case "fanart":
                        folderName = "images";
                        remoteMediaType = "Background";
                        mediaList = emumoviesMediaLists[remoteMediaType];
                        if (mediaList == null)
                        {
                            // No media
                            continue;
                        }
                        remoteFileName = FuzzySearch(scraperParameters.Name, mediaList);
                        if (string.IsNullOrEmpty(remoteFileName))
                        {
                            remoteFileName = FuzzySearch(scraperParameters.RomFileNameWithoutExtension, mediaList);
                            if (string.IsNullOrEmpty(remoteFileName))
                            {
                                continue;
                            }
                        }
                        fileFormat = null;
                        fileFormat = Path.GetExtension(remoteFileName);
                        if (string.IsNullOrEmpty(fileFormat))
                        {
                            continue;
                        }
                        newFileName = $"{scraperParameters.RomFileNameWithoutExtension}-{element}{fileFormat}";
                        fileToDownload = $"{scraperParameters.ParentFolderPath}\\{folderName}\\{newFileName}";
                        remoteDownloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
                        downloadResult = await FileTransfer.DownloadFile(scraperParameters.Overwrite, fileToDownload, remoteDownloadURL);
                        if (downloadResult == true)
                        {
                            scraperData.fanart = $"./{folderName}/{newFileName}";
                            ShowDownload(ListBoxControl, $"Downloaded: {newFileName}");
                        }
                        break;

                    case "boxback":
                        folderName = "images";
                        remoteMediaType = "BoxBack";
                        mediaList = emumoviesMediaLists[remoteMediaType];
                        if (mediaList == null)
                        {
                            // No media
                            continue;
                        }
                        remoteFileName = FuzzySearch(scraperParameters.Name, mediaList);
                        if (string.IsNullOrEmpty(remoteFileName))
                        {
                            remoteFileName = FuzzySearch(scraperParameters.RomFileNameWithoutExtension, mediaList);
                            if (string.IsNullOrEmpty(remoteFileName))
                            {
                                continue;
                            }
                        }
                        fileFormat = null;
                        fileFormat = Path.GetExtension(remoteFileName);
                        if (string.IsNullOrEmpty(fileFormat))
                        {
                            continue;
                        }
                        newFileName = $"{scraperParameters.RomFileNameWithoutExtension}-{element}{fileFormat}";
                        fileToDownload = $"{scraperParameters.ParentFolderPath}\\{folderName}\\{newFileName}";
                        remoteDownloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
                        downloadResult = await FileTransfer.DownloadFile(scraperParameters.Overwrite, fileToDownload, remoteDownloadURL);
                        if (downloadResult == true)
                        {
                            scraperData.boxback = $"./{folderName}/{newFileName}";
                            ShowDownload(ListBoxControl, $"Downloaded: {newFileName}");
                        }

                        break;

                    case "image":
                        folderName = "images";
                        remoteMediaType = scraperParameters.ImageSource;
                        mediaList = emumoviesMediaLists[remoteMediaType];
                        if (mediaList == null)
                        {
                            // No media
                            continue;
                        }
                        remoteFileName = FuzzySearch(scraperParameters.Name, mediaList);
                        if (string.IsNullOrEmpty(remoteFileName))
                        {
                            remoteFileName = FuzzySearch(scraperParameters.RomFileNameWithoutExtension, mediaList);
                            if (string.IsNullOrEmpty(remoteFileName))
                            {
                                continue;
                            }
                        }
                        fileFormat = null;
                        fileFormat = Path.GetExtension(remoteFileName);
                        if (string.IsNullOrEmpty(fileFormat))
                        {
                            continue;
                        }
                        newFileName = $"{scraperParameters.RomFileNameWithoutExtension}-{element}{fileFormat}";
                        fileToDownload = $"{scraperParameters.ParentFolderPath}\\{folderName}\\{newFileName}";
                        remoteDownloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
                        downloadResult = await FileTransfer.DownloadFile(scraperParameters.Overwrite, fileToDownload, remoteDownloadURL);
                        if (downloadResult == true)
                        {
                            scraperData.image = $"./{folderName}/{newFileName}";
                            ShowDownload(ListBoxControl, $"Downloaded: {newFileName}");
                        }
                        break;

                    case "thumbnail":
                        folderName = "images";
                        remoteMediaType = scraperParameters.BoxSource;
                        mediaList = emumoviesMediaLists[remoteMediaType];
                        if (mediaList == null)
                        {
                            // No media
                            continue;
                        }
                        remoteFileName = FuzzySearch(scraperParameters.Name, mediaList);
                        if (string.IsNullOrEmpty(remoteFileName))
                        {
                            remoteFileName = FuzzySearch(scraperParameters.RomFileNameWithoutExtension, mediaList);
                            if (string.IsNullOrEmpty(remoteFileName))
                            {
                                continue;
                            }
                        }
                        fileFormat = null;
                        fileFormat = Path.GetExtension(remoteFileName);
                        if (string.IsNullOrEmpty(fileFormat))
                        {
                            continue;
                        }
                        newFileName = $"{scraperParameters.RomFileNameWithoutExtension}-{element}{fileFormat}";
                        fileToDownload = $"{scraperParameters.ParentFolderPath}\\{folderName}\\{newFileName}";
                        remoteDownloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
                        downloadResult = await FileTransfer.DownloadFile(scraperParameters.Overwrite, fileToDownload, remoteDownloadURL);
                        if (downloadResult == true)
                        {
                            scraperData.thumbnail = $"./{folderName}/{newFileName}";
                            ShowDownload(ListBoxControl, $"Downloaded: {newFileName}");
                        }
                        break;

                    case "marquee":
                        remoteMediaType = scraperParameters.LogoSource;
                        mediaList = emumoviesMediaLists[remoteMediaType];
                        folderName = "images";
                        if (mediaList == null)
                        {
                            // No media
                            continue;
                        }
                        remoteFileName = FuzzySearch(scraperParameters.Name, mediaList);
                        if (string.IsNullOrEmpty(remoteFileName))
                        {
                            remoteFileName = FuzzySearch(scraperParameters.RomFileNameWithoutExtension, mediaList);
                            if (string.IsNullOrEmpty(remoteFileName))
                            {
                                continue;
                            }
                        }
                        fileFormat = null;
                        fileFormat = Path.GetExtension(remoteFileName);
                        if (string.IsNullOrEmpty(fileFormat))
                        {
                            continue;
                        }
                        newFileName = $"{scraperParameters.RomFileNameWithoutExtension}-{element}{fileFormat}";
                        fileToDownload = $"{scraperParameters.ParentFolderPath}\\{folderName}\\{newFileName}";
                        remoteDownloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
                        downloadResult = await FileTransfer.DownloadFile(scraperParameters.Overwrite, fileToDownload, remoteDownloadURL);
                        if (downloadResult == true)
                        {
                            scraperData.marquee = $"./{folderName}/{newFileName}";
                            ShowDownload(ListBoxControl, $"Downloaded: {newFileName}");
                        }
                        break;


                    case "video":
                        folderName = "videos";
                        remoteMediaType = "Video_MP4";
                        mediaList = emumoviesMediaLists[remoteMediaType];
                        if (mediaList == null)
                        {
                            // No media
                            continue;
                        }
                        remoteFileName = FuzzySearch(scraperParameters.Name, mediaList);
                        if (string.IsNullOrEmpty(remoteFileName))
                        {
                            remoteFileName = FuzzySearch(scraperParameters.RomFileNameWithoutExtension, mediaList);
                            if (string.IsNullOrEmpty(remoteFileName))
                            {
                                continue;
                            }
                        }
                        fileFormat = null;
                        fileFormat = Path.GetExtension(remoteFileName);
                        if (string.IsNullOrEmpty(fileFormat))
                        {
                            continue;
                        }
                        newFileName = $"{scraperParameters.RomFileNameWithoutExtension}-{element}{fileFormat}";
                        fileToDownload = $"{scraperParameters.ParentFolderPath}\\{folderName}\\{newFileName}";
                        remoteDownloadURL = $"{apiURL}/Media/Download?accessToken={scraperParameters.UserToken}&systemName={scraperParameters.SystemID}&mediaType={remoteMediaType}&mediaSet=default&filename={remoteFileName}";
                        downloadResult = await FileTransfer.DownloadFile(scraperParameters.Overwrite, fileToDownload, remoteDownloadURL);
                        if (downloadResult == true)
                        {
                            scraperData.video = $"./{folderName}/{newFileName}";
                            ShowDownload(ListBoxControl, $"Downloaded: {newFileName}");
                        }
                        break;
                }
            }
            return scraperData;
        }

        private string FuzzySearch(string searchName, List<string> names)
        {
            var results = Process.ExtractTop(searchName, names, cutoff: 80, limit: 1);
            var bestMatch = results.FirstOrDefault();

            if (bestMatch != null)
            {
                return bestMatch.Value; // Return the best matching string
            }
            else
            {
                return null; // Return null if no match is found
            }
        }

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

        private void ShowDownload(ListBox listBox, string message)
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
                        //Console.WriteLine(acessTokenElement.ToString());
                        return acessTokenElement.GetString();
                    }
                }
                else
                {
                    return null;
                }
            }


        }

        public async Task<List<string>> GetMediaTypes(string system)
        {
            string url = $"{apiURL}/Media/MediaTypes?systemName={system}";
            List<string> mediaTypes = await FetchAndParseJsonResponse(url);
            return mediaTypes;
        }

        public async Task<List<string>> GetMediaList(string system, string mediaTitle)
        {
            string url = $"{apiURL}/Media/MediaList?systemName={system}&mediaType={mediaTitle}&mediaSet=default";
            List<string> mediaList = await FetchAndParseJsonResponse(url);
            return mediaList;
        }

        async Task<List<string>> FetchAndParseJsonResponse(string url)
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