using GamelistManager.classes.io;
using System.Configuration;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Xml;

namespace GamelistManager.classes.api
{
    internal class API_ScreenScraper
    {
        private readonly string _apiURL = "https://api.screenscraper.fr/api2";
        private readonly string _devId;
        private readonly string _devPassword;
        private readonly string _software = "GamelistManager";
        private readonly HttpClient _httpClientService;
        private readonly FileTransfer _fileTransfer;

        public API_ScreenScraper(HttpClient httpClientService)
        {
            _httpClientService = httpClientService;
            _fileTransfer = new FileTransfer(_httpClientService);

            _devId = ConfigurationManager.AppSettings["ScreenScraper_DevId"]
                ?? throw new InvalidOperationException("ScreenScraper_DevId not found in App.config");
            _devPassword = ConfigurationManager.AppSettings["ScreenScraper_DevPassword"]
                ?? throw new InvalidOperationException("ScreenScraper_DevPassword not found in App.config");
        }

        public static async Task<string> ConvertHttpResponseMessageToString(HttpResponseMessage httpResponse)
        {
            // Ensure the response is successful
            try
            {
                httpResponse.EnsureSuccessStatusCode();
            }
            catch
            {
                return string.Empty;
            }
            // Read the response content as a byte array asynchronously
            var responseBytes = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            // Convert the byte array to a UTF-8 string
            string responseString = Encoding.UTF8.GetString(responseBytes);

            // Return the XML content as a string
            return responseString;
        }


        public async Task<int> AuthenticateAsync(string userID, string userPassword)
        {
            string url = $"{_apiURL}/ssuserInfos.php?devid={_devId}&devpassword={_devPassword}&softname={_software}&output=xml&ssid={userID}&sspassword={userPassword}";

            HttpResponseMessage httpResponse = await _httpClientService.GetAsync(url).ConfigureAwait(false);
            string responseString = await ConvertHttpResponseMessageToString(httpResponse);

            if (string.IsNullOrEmpty(responseString))
            {
                return -1; // failed authentication (no response)
            }

            try
            {
                XmlDocument xmlUserData = new();
                xmlUserData.LoadXml(responseString);

                // First verify authentication
                XmlNode? idNode = xmlUserData.SelectSingleNode("//ssuser/id");
                if (idNode == null || !string.Equals(idNode.InnerText, userID, StringComparison.OrdinalIgnoreCase))
                {
                    return -1; // authentication failed
                }

                // Then get maxthreads
                XmlNode? maxThreadsNode = xmlUserData.SelectSingleNode("//ssuser/maxthreads");
                if (maxThreadsNode == null)
                {
                    return 1; // authenticated but no maxthreads found
                }

                return int.TryParse(maxThreadsNode.InnerText, out int maxThreads)
                    ? maxThreads
                    : 1; // fallback if parse fails
            }
            catch
            {
                return -1; // failed to parse or network issue
            }
        }

        public void SaveXmlToCacheFile(ScraperParameters scraperParameters, string xmlString, string cacheFolder, string romName)
        {
            if (!Directory.Exists(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }

            string cacheFilePath = Path.Combine(cacheFolder, $"{romName}.xml");

            // Redact in a specific order to avoid partial replacements
            var redactions = new[]
            {
                (scraperParameters.UserPassword, "{{REDACTED_USERPASS}}"),
                (scraperParameters.UserID, "{{REDACTED_USERID}}"),
                (_devPassword, "{{REDACTED_PASSWORD}}"),
                (_devId, "{{REDACTED_DEVID}}")
            };

            foreach (var (sensitive, placeholder) in redactions)
            {
                if (!string.IsNullOrEmpty(sensitive))
                {
                    xmlString = xmlString.Replace(sensitive, placeholder);
                }
            }

            File.WriteAllText(cacheFilePath, xmlString);
        }

        private string ReadXmlFromCache(string romName, string cacheFolder)
        {
            if (!Directory.Exists(cacheFolder))
            {
                return string.Empty;
            }

            string cacheFilePath = Path.Combine(cacheFolder, $"{romName}.xml");

            if (!File.Exists(cacheFilePath))
            {
                return string.Empty;
            }

            string fileContent = File.ReadAllText(cacheFilePath);
                      
            return fileContent;
        }


        public async Task<Tuple<int, int>> ScrapeScreenScraperAsync(DataRowView rowView, ScraperParameters scraperParameters)
        {

            if (scraperParameters.ElementsToScrape == null || scraperParameters.ElementsToScrape.Count == 0)
            {
                return (null!);
            }

            bool cachedInfo = false;

            //string scrapInfo = (!string.IsNullOrEmpty(scraperParameters.GameID)) ? $"&gameid={scraperParameters.GameID}" : $"&romtype=rom&romnom={romFileNameWithoutExtension}";
            string scrapInfo = (!string.IsNullOrEmpty(scraperParameters.GameID))
                ? $"&gameid={scraperParameters.GameID}"
                : $"&romtype=rom&romnom={Uri.EscapeDataString(scraperParameters.RomFileNameWithoutExtension!)}"; // Now encoded

            string url = $"{_apiURL}/jeuInfos.php?devid={_devId}&devpassword={_devPassword}&softname=GamelistManager&output=xml&ssid={scraperParameters.UserID}&sspassword={scraperParameters.UserPassword}&systemeid={scraperParameters.SystemID}{scrapInfo}";
            string responseString = string.Empty;
          
            XmlDocument xmlData = new();
            XmlNode? mediasNode = null;

            int scrapeTotal = 0;
            int scrapeMax = 0;

            // Scrape by cache if selected
            if (scraperParameters.ScrapeByCache)
            {
                responseString = ReadXmlFromCache(scraperParameters.RomFileNameWithoutExtension!, scraperParameters.CacheFolder!);
                if (string.IsNullOrEmpty(responseString) && scraperParameters.SkipNonCached)
                {
                    return null!;
                }
                else if (!string.IsNullOrEmpty(responseString))
                {
                    // Restore redacted values from cache
                    var restorations = new[]
                    {
                        ("{{REDACTED_USERPASS}}", scraperParameters.UserPassword),
                        ("{{REDACTED_USERID}}", scraperParameters.UserID),
                        ("{{REDACTED_PASSWORD}}", _devPassword),
                        ("{{REDACTED_DEVID}}", _devId)
                    };

                    foreach (var (placeholder, actual) in restorations)
                    {
                        if (!string.IsNullOrEmpty(actual))
                        {
                            responseString = responseString.Replace(placeholder, actual);
                        }
                    }
                }
                cachedInfo = true;
            }

            if (string.IsNullOrEmpty(responseString))
            {
                cachedInfo = false;

                try
                {
                    HttpResponseMessage httpResponse = await _httpClientService.GetAsync(url).ConfigureAwait(false);
                    responseString = await ConvertHttpResponseMessageToString(httpResponse);
                }
                catch
                {
                    responseString = string.Empty;
                }

                if (string.IsNullOrEmpty(responseString))
                {
                    // Try one more time with the name of the rom file
                    string romName = scraperParameters.Name!;
                    scrapInfo = $"&romtype=rom&romnom={Uri.EscapeDataString(romName)}";  // Now encoded
                    url = $"{_apiURL}/jeuInfos.php?devid={_devId}&devpassword={_devPassword}&softname=GamelistManager&output=xml&ssid={scraperParameters.UserID}&sspassword={scraperParameters.UserPassword}&systemeid={scraperParameters.SystemID}{scrapInfo}";

                    try
                    {
                        HttpResponseMessage httpResponse2 = await _httpClientService.GetAsync(url).ConfigureAwait(false);
                        responseString = await ConvertHttpResponseMessageToString(httpResponse2);
                    }
                    catch
                    {
                        responseString = string.Empty;
                    }
                }

                // If it's still null, failed.
                if (string.IsNullOrEmpty(responseString))
                {
                    return (null!);
                }

                SaveXmlToCacheFile(scraperParameters, responseString, scraperParameters.CacheFolder!, scraperParameters.RomFileNameWithoutExtension!);
            }

            xmlData.LoadXml(responseString);

            if (!cachedInfo)
            {
                // Retrieve total requests for today
                var totalRequestsNode = xmlData.SelectSingleNode("/Data/ssuser/requeststoday");
                if (totalRequestsNode != null && int.TryParse(totalRequestsNode.InnerText, out int total))
                {
                    scrapeTotal = total;
                }

                // Retrieve maximum allowed requests per day
                var allowedRequestsNode = xmlData.SelectSingleNode("/Data/ssuser/maxrequestsperday");
                if (allowedRequestsNode != null && int.TryParse(allowedRequestsNode.InnerText, out int max))
                {
                    scrapeMax = max;
                }
            }

            mediasNode = xmlData.SelectSingleNode("/Data/jeu/medias");

            foreach (string element in scraperParameters.ElementsToScrape)
            {
                switch (element)
                {
                    case "publisher":
                        string? publisher = xmlData.SelectSingleNode("/Data/jeu/editeur")?.InnerText;
                        UpdateMetadata(rowView, "Publisher", publisher!, scraperParameters.OverwriteMetadata);
                        break;

                    case "developer":
                        string? developer = xmlData.SelectSingleNode("/Data/jeu/developpeur")?.InnerText;
                        UpdateMetadata(rowView, "Developer", developer!, scraperParameters.OverwriteMetadata);
                        break;

                    case "players":
                        string? players = xmlData.SelectSingleNode("/Data/jeu/joueurs")?.InnerText;
                        UpdateMetadata(rowView, "Players", players!, scraperParameters.OverwriteMetadata);
                        break;


                    case "id":
                        string? id = (xmlData.SelectSingleNode("/Data/jeu") as XmlElement)?.GetAttribute("id");
                        UpdateMetadata(rowView, "Game Id", id!, scraperParameters.OverwriteMetadata);
                        break;

                    case "arcadesystemname":
                        string? platformId = (xmlData.SelectSingleNode("/Data/jeu/systeme") as XmlElement)?.GetAttribute("id");
                        if (ushort.TryParse(platformId, out ushort idvalue))
                        {
                            string arcadeId = ArcadeSystemID.GetArcadeSystemNameByID(idvalue);
                            if (!string.IsNullOrEmpty(arcadeId))
                            {
                                UpdateMetadata(rowView, "Arcade System Name", arcadeId, scraperParameters.OverwriteMetadata);
                            }
                        }
                        break;

                    case "rating":
                        string? rating = xmlData.SelectSingleNode("/Data/jeu/note")?.InnerText;
                        string convertedRating = ConvertRating(rating!);
                        UpdateMetadata(rowView, "Rating", convertedRating!, scraperParameters.OverwriteMetadata);
                        break;

                    case "desc":
                        string? description = xmlData
                        .SelectSingleNode($"/Data/jeu/synopsis/synopsis[@langue='{scraperParameters.SSLanguage}']")
                        ?.InnerText
                        ?? xmlData.SelectSingleNode($"/Data/jeu/synopsis/synopsis[@langue='en']")?.InnerText;
                        UpdateMetadata(rowView, "Description", description!, scraperParameters.OverwriteMetadata);
                        break;

                    case "name":
                        string? name = ParseNames(xmlData.SelectSingleNode("/Data/jeu/noms")!, scraperParameters.SSRegions!);
                        UpdateMetadata(rowView, "Name", name, scraperParameters.OverwriteName);
                        break;

                    case "genre":
                        var genresNode = xmlData.SelectSingleNode("/Data/jeu/genres");
                        if (genresNode != null)
                        {
                            string genreLanguage = scraperParameters.SSLanguage!;
                            if (scraperParameters.ScrapeEnglishGenreOnly == true)
                            {
                                genreLanguage = "en";
                            }
                            var (_, genreName) = ParseGenres(genresNode, genreLanguage);
                            UpdateMetadata(rowView, "Genre", genreName!, scraperParameters.OverwriteMetadata);
                        }
                        break;

                    case "family":
                        var familyNode = xmlData.SelectSingleNode("/Data/jeu/familles");
                        if (familyNode != null)
                        {
                            string family = ParseFamily(familyNode, scraperParameters.SSLanguage!);
                            if (!string.IsNullOrEmpty(family))
                            {
                                UpdateMetadata(rowView, "Family", family!, scraperParameters.OverwriteMetadata);
                            }
                        }
                        break;

                    case "releasedate":
                        string? releaseDate = ParseReleaseDate(xmlData.SelectSingleNode("/Data/jeu/dates")!, scraperParameters.SSRegions!);
                        UpdateMetadata(rowView, "Release Date", releaseDate!, scraperParameters.OverwriteMetadata);
                        break;

                    case "titleshot":
                        await DownloadFile(rowView, "titleshot", "Titleshot", "sstitle", scraperParameters, mediasNode!);
                        break;

                    case "bezel":
                        await DownloadFile(rowView, "bezel", "Bezel", "bezel-16-9", scraperParameters, mediasNode!);
                        break;

                    case "fanart":
                        await DownloadFile(rowView, "fanart", "Fan Art", "fanart", scraperParameters, mediasNode!);
                        break;

                    case "boxback":
                        await DownloadFile(rowView, "boxback", "Box Back", "box-2D-back", scraperParameters, mediasNode!);
                        break;

                    case "manual":
                        await DownloadFile(rowView, "manual", "Manual", "manuel", scraperParameters, mediasNode!);
                        break;

                    case "image":
                        await DownloadFile(rowView, "image", "Image", scraperParameters.ImageSource!, scraperParameters, mediasNode!);
                        break;

                    case "thumbnail":
                        await DownloadFile(rowView, "thumbnail", "Thumbnail", scraperParameters.ThumbnailSource!, scraperParameters, mediasNode!);
                        break;

                    case "boxart":
                        await DownloadFile(rowView, "boxart", "Box Art", scraperParameters.BoxArtSource!, scraperParameters, mediasNode!);
                        break;


                    case "marquee":
                        string marqueeSource = scraperParameters.MarqueeSource!;
                        bool downloadSuccessful = await DownloadFile(rowView, "marquee", "Marquee", scraperParameters.MarqueeSource!, scraperParameters, mediasNode!);
                        if (marqueeSource == "wheel-hd" && downloadSuccessful == false)
                        {
                            await DownloadFile(rowView, "marquee", "Marquee", "wheel", scraperParameters, mediasNode!);
                        }
                        break;

                    case "wheel":
                        string wheelSource = scraperParameters.WheelSource!;
                        bool downloadSuccessful2 = await DownloadFile(rowView, "wheel", "Wheel", scraperParameters.WheelSource!, scraperParameters, mediasNode!);
                        if (wheelSource == "wheel-hd" && downloadSuccessful2 == false)
                        {
                            await DownloadFile(rowView, "wheel", "Wheel", "wheel", scraperParameters, mediasNode!);
                        }
                        break;

                    case "video":
                        await DownloadFile(rowView, "video", "Video", scraperParameters.VideoSource!, scraperParameters, mediasNode!);
                        break;
                }
            }

            return new Tuple<int, int>(scrapeTotal, scrapeMax);

        }

        private static void UpdateMetadata(DataRowView rowView, string column, string newValue, bool overwrite)
        {
            if (string.IsNullOrEmpty(newValue))
            {
                return;
            }

            var currentValue = rowView[column];

            // Check if the current Value is DBNull or null, and if overwrite is allowed
            if (overwrite || currentValue == DBNull.Value || string.IsNullOrEmpty(currentValue.ToString()))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    rowView[column] = newValue;
                });
            }
        }

        public static string ParseFamily(XmlNode familyNode, string language)
        {
            string family = string.Empty;
            if (familyNode != null)
            {
                // First attempt: find the family in the requested language
                family = familyNode
                            .SelectNodes("famille")?
                            .Cast<XmlNode>()
                            .Where(famille =>
                                famille.Attributes?["principale"]?.Value == "1" &&
                                famille.Attributes?["langue"]?.Value == language)
                            .Select(famille => famille.InnerText)
                            .FirstOrDefault() ?? string.Empty;

                // Second attempt: if not found, look for English ("en")
                if (string.IsNullOrEmpty(family))
                {
                    family = familyNode
                                .SelectNodes("famille")?
                                .Cast<XmlNode>()
                                .Where(famille =>
                                    famille.Attributes?["principale"]?.Value == "1" &&
                                    famille.Attributes?["langue"]?.Value == "en")
                                .Select(famille => famille.InnerText)
                                .FirstOrDefault() ?? string.Empty;
                }

                // Third attempt: if still not found, get any available family
                if (string.IsNullOrEmpty(family))
                {
                    family = familyNode
                                .SelectNodes("famille")?
                                .Cast<XmlNode>()
                                .Select(famille => famille.InnerText)
                                .FirstOrDefault() ?? string.Empty;
                }
            }
            return family;
        }
        private async Task<bool> DownloadFile(DataRowView rowView, string mediaName, string mediaType, string remoteMediaType, ScraperParameters scraperParameters, XmlNode mediasNode)
        {
            if (mediasNode == null)
            {
                return false;
            }

            (string downloadURL, string fileFormat) = (string.Empty, string.Empty);

            // Try with user's preferred regions first (if any)
            if (scraperParameters.SSRegions != null && scraperParameters.SSRegions.Count > 0)
            {
                (downloadURL, fileFormat) = ParseMedia(remoteMediaType, mediasNode, scraperParameters.SSRegions);
            }

            if (string.IsNullOrEmpty(downloadURL))
            {
                if (scraperParameters.ScrapeAnyMedia)
                {
                    // Try all available regions from the XML (including empty region via ??)
                    var allRegions = mediasNode.SelectNodes($"//media[@type='{remoteMediaType}']")
                        ?.Cast<XmlNode>()
                        .Select(n => n.Attributes?["region"]?.Value ?? string.Empty)
                        .Distinct()
                        .ToList();

                    if (allRegions != null && allRegions.Count > 0)
                    {
                        (downloadURL, fileFormat) = ParseMedia(remoteMediaType, mediasNode, allRegions);
                    }
                }
                else if (mediaName == "video")
                {
                    // Fallback for videos which often have no region
                    (downloadURL, fileFormat) = ParseMedia(remoteMediaType, mediasNode, new List<string> { string.Empty });
                }
            }


            if (string.IsNullOrEmpty(downloadURL) || string.IsNullOrEmpty(fileFormat))
            {
                return false;
            }

            var currentValue = rowView[mediaType];
            if (currentValue != DBNull.Value && currentValue != null && !string.IsNullOrEmpty(currentValue.ToString()) && !scraperParameters.OverwriteMedia)
            {
                return false;
            }

            string romFileNameWithoutExtension = scraperParameters.RomFileNameWithoutExtension!;
            var mediaPaths = scraperParameters.MediaPaths!;
            string destinationFolder = mediaPaths[mediaName];
            string parentFolderPath = scraperParameters.ParentFolderPath!;
            bool verify = scraperParameters.Verify;

            if (mediaName == "thumbnail")
            {
                mediaName = "thumb";
            }

            string destinationFolderWindows = destinationFolder.Replace('/', '\\');
            string fileName = $"{romFileNameWithoutExtension}-{mediaName}.{fileFormat}";
            string downloadPath = Path.Combine(parentFolderPath, destinationFolderWindows);
            string fileToDownload = Path.Combine(downloadPath, fileName);

            bool downloadSuccessful = await _fileTransfer.DownloadFile(verify, fileToDownload, downloadURL, string.Empty);
            if (downloadSuccessful)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    rowView[mediaType] = $"./{destinationFolder}/{fileName}";
                });
                return true;
            }
            return false;
        }


        private static (string, string) ParseMedia(string mediaType, XmlNode xmlMedias, List<string> regionsList)
        {
            if (xmlMedias == null)
            {
                return (string.Empty, string.Empty);
            }

            XmlNode? media = null;

            foreach (string currentRegion in regionsList)
            {
                // Find first matching Region
                if (!string.IsNullOrEmpty(currentRegion))
                {
                    // Look for Region-specific media first
                    media = xmlMedias.SelectSingleNode($"//media[@type='{mediaType}' and @region='{currentRegion}']");
                }
                else
                {
                    // Fallback for no Region Value at all
                    media = xmlMedias.SelectSingleNode($"//media[@type='{mediaType}']");
                }

                if (media != null)
                {
                    break;
                }
            }

            // Check if media was found
            if (media == null)
            {
                return (string.Empty, string.Empty); // Return empty if no media was found
            }

            // Retrieve URL and format
            string url = media.InnerText;
            string format = media.Attributes?["format"]?.Value ?? string.Empty;

            return (url, format);
        }


        private static string ParseReleaseDate(XmlNode xmlNode, List<string> regionsList)
        {
            if (xmlNode == null)
            {
                return string.Empty;
            }

            foreach (string region in regionsList)
            {
                XmlNode? xmlNode2 = xmlNode.SelectSingleNode($"date[@region='{region}']");
                if (xmlNode2 != null)
                {
                    // Get the InnerText of the found node
                    string releaseDate = xmlNode2.InnerText ?? string.Empty;

                    // Return the releaseDate if it's not empty
                    if (!string.IsNullOrEmpty(releaseDate))
                    {
                        return releaseDate;
                    }
                }
            }
            return string.Empty;
        }

        private static string ParseNames(XmlNode namesElement, List<string> regionsList)
        {
            if (namesElement == null)
            {
                return string.Empty;
            }

            foreach (string region in regionsList)
            {
                XmlNode? xmlNode = namesElement.SelectSingleNode($"nom[@region='{region}']");
                if (xmlNode != null)
                {
                    string name = xmlNode.InnerText ?? string.Empty;
                    return name;
                }
            }

            return string.Empty;
        }


        private static string ConvertRating(string rating)
        {
            if (string.IsNullOrEmpty(rating))
            {
                return string.Empty;
            }

            int ratingvalue = int.Parse(rating);
            if (ratingvalue == 0)
            {
                return string.Empty;
            }
            float ratingValFloat = ratingvalue / 20.0f;
            return ratingValFloat.ToString();
        }

        private static (string GenreId, string GenreName) ParseGenres(XmlNode parentNode, string language)
        {
            if (parentNode == null)
            {
                return (string.Empty, string.Empty);
            }

            var genreNode = parentNode.SelectNodes("genre")?
                .Cast<XmlNode>()
                .Where(e => e.Attributes?["langue"]?.Value == language)
                .ToList();

            // If no genres found in the specified language, check in "en"
            if ((genreNode == null || genreNode.Count == 0) && language != "en")
            {
                genreNode = parentNode.SelectNodes("genre")?
                    .Cast<XmlNode>()
                    .Where(e => e.Attributes?["langue"]?.Value == "en")
                    .ToList();
            }

            if (genreNode != null && genreNode.Count > 0)
            {
                // Find the last genre with " / " and principale = 0
                var primaryGenreWithSlash = genreNode?
                    .LastOrDefault(e => e.InnerText.Contains(" / ") && e.Attributes?["principale"]?.Value == "0");

                if (primaryGenreWithSlash != null)
                {
                    return (primaryGenreWithSlash.Attributes?["id"]?.Value!, primaryGenreWithSlash.InnerText);
                }

                // If no genre has " / ", take the last genre with principale = 1
                var lastGenre = genreNode?.LastOrDefault(e => e.Attributes?["principale"]?.Value == "1");

                if (lastGenre != null)
                {
                    return (lastGenre.Attributes?["id"]?.Value!, lastGenre.InnerText);
                }
            }

            return (string.Empty, string.Empty);
        }
    }
}

