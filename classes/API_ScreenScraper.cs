using System.Data;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Xml;

namespace GamelistManager.classes
{
    internal class API_ScreenScraper
    {
        private readonly string apiURL = "https://api.screenscraper.fr/api2";
        private readonly string devId = "";
        private readonly string devPassword = "";
        private readonly string software = "GamelistManager";
        private readonly HttpClient _httpClientService;
        private readonly FileTransfer _fileTransfer;

        public API_ScreenScraper(HttpClient httpClientService)
        {
            _httpClientService = httpClientService;
            _fileTransfer = new FileTransfer(_httpClientService);
        }

        public async Task<string> ConvertHttpResponseMessageToString(HttpResponseMessage httpResponse)
        {
            // Ensure the response is successful
            httpResponse.EnsureSuccessStatusCode();

            // Read the response content as a byte array asynchronously
            var responseBytes = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            // Convert the byte array to a UTF-8 string
            string responseString = Encoding.UTF8.GetString(responseBytes);

            // Return the XML content as a string
            return responseString;
        }


        public async Task<int> GetMaxThreadsAsync(string userID, string userPassword)
        {
            string url = $"{apiURL}/ssuserInfos.php?devid={devId}&devpassword={devPassword}&softname={software}&output=xml&ssid={userID}&sspassword={userPassword}";

            HttpResponseMessage httpResponse = await _httpClientService.GetAsync(url).ConfigureAwait(false);
            string responseString = await ConvertHttpResponseMessageToString(httpResponse);

            if (!string.IsNullOrEmpty(responseString))
            {
                try
                {
                    XmlDocument xmlUserData = new XmlDocument();
                    xmlUserData.LoadXml(responseString);
                    XmlNode? xmlNode = xmlUserData.SelectSingleNode("//ssuser/maxthreads");
                    if (xmlNode == null)
                    {
                        return 1;
                    }

                    if (int.TryParse(xmlNode.InnerText, out int max))
                    {
                        return max;
                    }
                }
                catch
                {
                    return 1;
                }

            }
            return 1;
        }

        public async Task<bool> VerifyScreenScraperCredentialsAsync(string userID, string userPassword)
        {
            // Return boolean true or false if authentication is successful
            string url = $"{apiURL}/ssuserInfos.php?devid={devId}&devpassword={devPassword}&softname={software}&output=xml&ssid={userID}&sspassword={userPassword}";

            HttpResponseMessage httpResponse = await _httpClientService.GetAsync(url).ConfigureAwait(false);
            string responseString = await ConvertHttpResponseMessageToString(httpResponse);

            if (string.IsNullOrEmpty(responseString))
            {
                return false;
            }

            try
            {
                XmlDocument xmlUserData = new XmlDocument();
                xmlUserData.LoadXml(responseString);
                XmlNode? xmlNode = xmlUserData.SelectSingleNode("//ssuser/id");
                if (xmlNode == null)
                {
                    return false;
                }
                if (string.Equals(xmlNode.InnerText, userID, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }


        public void SaveXmlToCacheFile(string xmlString, string cacheFolder, string romName)
        {
            if (!Directory.Exists(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }
            string cacheFilePath = Path.Combine(cacheFolder, $"{romName}.xml");
            File.WriteAllText(cacheFilePath, xmlString);
        }

        private string ReadXmlFromCache(string romName, string cacheFolder)
        {
            if (!Directory.Exists(cacheFolder))
            {
                return string.Empty;
            }

            string cacheFilePath = Path.Combine(cacheFolder, $"{romName}.xml");

            if (!Path.Exists(cacheFilePath))
            {
                return string.Empty;
            }

            string fileContent = File.ReadAllText(cacheFilePath);
            return fileContent;
        }


        public async Task<Tuple<int, int>> ScrapeScreenScraperAsync(DataRowView rowView, ScraperParameters scraperParameters)
        {
            bool scrapeByCache = scraperParameters.ScrapeByCache;
            bool skipNonCached = scraperParameters.SkipNonCached;
            bool cachedInfo = false;
            bool overwriteMetaData = scraperParameters.OverwriteMetadata;
            bool overwriteName = scraperParameters.OverwriteNames;

            string romFileNameWithoutExtension = scraperParameters.RomFileNameWithoutExtension!;
            string cacheFolder = scraperParameters.CacheFolder!;
            string scrapInfo = (!string.IsNullOrEmpty(scraperParameters.GameID)) ? $"&gameid={scraperParameters.GameID}" : $"&romtype=rom&romnom={romFileNameWithoutExtension}";
            string url = $"{apiURL}/jeuInfos.php?devid={devId}&devpassword={devPassword}&softname=GamelistManager&output=xml&ssid={scraperParameters.UserID}&sspassword={scraperParameters.UserPassword}&systemeid={scraperParameters.SystemID}{scrapInfo}";
            string responseString = string.Empty;

            XmlDocument xmlData = new XmlDocument();
            XmlNode? mediasNode = null;

            var elementsToScrape = scraperParameters.ElementsToScrape!;

            int scrapeTotal = 0;
            int scrapeMax = 0;

            bool scrapeRequired =  elementsToScrape.Any(item => item != "region" && item != "lang");

            if (scrapeRequired)
            {
                // Scrape by cache if selected
                if (scrapeByCache)
                {
                    responseString = ReadXmlFromCache(romFileNameWithoutExtension, cacheFolder);
                    if (string.IsNullOrEmpty(responseString) && skipNonCached)
                    {
                        return null!;
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
                        scrapInfo = $"&romtype=rom&romnom={romName}";
                        url = $"{apiURL}/jeuInfos.php?devid={devId}&devpassword={devPassword}&softname=GamelistManager&output=xml&ssid={scraperParameters.UserID}&sspassword={scraperParameters.UserPassword}&systemeid={scraperParameters.SystemID}{scrapInfo}";

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

                    SaveXmlToCacheFile(responseString, cacheFolder, romFileNameWithoutExtension);
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
            }

            if (overwriteMetaData)
            {
                overwriteName = true;
            }

            foreach (string element in elementsToScrape)
            {
                switch (element)
                {
                    case "publisher":
                        string? publisher = xmlData.SelectSingleNode("/Data/jeu/editeur")?.InnerText;
                        UpdateMetadata(rowView, "Publisher", publisher!, overwriteMetaData);
                        break;

                    case "developer":
                        string? developer = xmlData.SelectSingleNode("/Data/jeu/developpeur")?.InnerText;
                        UpdateMetadata(rowView, "Developer", developer!, overwriteMetaData);
                        break;

                    case "players":
                        string? players = xmlData.SelectSingleNode("/Data/jeu/joueurs")?.InnerText;
                        UpdateMetadata(rowView, "Players", players!, overwriteMetaData);
                        break;

                    case "lang":
                        //string? language = xmlData.SelectSingleNode("/Data/jeu/rom/romlangues")?.InnerText; 
                        string languages = RegionLanguageHelper.GetLanguages(scraperParameters.RomFileNameWithExtension!);
                        UpdateMetadata(rowView, "Language", languages, overwriteMetaData);
                        break;

                    case "id":
                        string? id = (xmlData.SelectSingleNode("/Data/jeu") as XmlElement)?.GetAttribute("id");
                        UpdateMetadata(rowView, "Game Id", id!, overwriteMetaData);
                        break;

                    case "arcadesystemname":
                        string? platformId = (xmlData.SelectSingleNode("/Data/jeu/systeme") as XmlElement)?.GetAttribute("id");
                        if (ushort.TryParse(platformId, out ushort idvalue))
                        {
                            string arcadeId = ArcadeSystemID.GetArcadeSystemName(idvalue);
                            if (!string.IsNullOrEmpty(arcadeId))
                            {
                                UpdateMetadata(rowView, "Arcade System Name", arcadeId, overwriteMetaData);
                            }
                        }
                        break;

                    case "rating":
                        string? rating = xmlData.SelectSingleNode("/Data/jeu/note")?.InnerText;
                        string convertedRating = ConvertRating(rating!);
                        UpdateMetadata(rowView, "Rating", convertedRating!, overwriteMetaData);
                        break;

                    case "desc":
                        string? description = xmlData
                        .SelectSingleNode($"/Data/jeu/synopsis/synopsis[@langue='{scraperParameters.Language}']")
                        ?.InnerText
                        ?? xmlData.SelectSingleNode($"/Data/jeu/synopsis/synopsis[@langue='en']")?.InnerText;
                        UpdateMetadata(rowView, "Description", description!, overwriteMetaData);
                        break;

                    case "name":
                        string? name = ParseNames(xmlData.SelectSingleNode("/Data/jeu/noms")!, scraperParameters.Region!);
                        UpdateMetadata(rowView, "Name", name!, overwriteName);
                        break;

                    case "genre":
                        var genresNode = xmlData.SelectSingleNode("/Data/jeu/genres");
                        if (genresNode != null)
                        {
                            string genreLanguage = scraperParameters.Language!;
                            if (scraperParameters.ScrapeEnglishGenreOnly == true)
                            {
                                genreLanguage = "en";
                            }
                            var (_, genreName) = ParseGenres(genresNode, genreLanguage);
                            UpdateMetadata(rowView, "Genre", genreName!, overwriteMetaData);
                        }
                        break;

                    case "family":
                        var familyNode = xmlData.SelectSingleNode("/Data/jeu/familles");
                        if (familyNode != null)
                        {
                            string family = ParseFamily(familyNode, scraperParameters.Language!);
                            if (!string.IsNullOrEmpty(family))
                            {
                                UpdateMetadata(rowView, "Family", family!, overwriteMetaData);
                            }
                        }
                        break;

                    case "region":
                        // string? region = xmlData.SelectSingleNode("/Data/jeu/rom/romregions")?.InnerText; ;
                        string region = RegionLanguageHelper.GetRegion(scraperParameters.RomFileNameWithExtension!);
                        UpdateMetadata(rowView, "Region", region!, overwriteMetaData);
                        break;

                    case "releasedate":
                        string? releaseDate = ParseReleaseDate(xmlData.SelectSingleNode("/Data/jeu/dates")!, scraperParameters.Region!);
                        UpdateMetadata(rowView, "Release Date", releaseDate!, overwriteMetaData);
                        break;

                    case "titleshot":
                        await DownloadFile(rowView, "titleshot", "Titleshot", "sstitle", scraperParameters, mediasNode!);
                        break;

                    case "bezel":
                        await DownloadFile(rowView, "bezel", "Bezel", "bezel-16-9", scraperParameters, mediasNode!);
                        break;

                    case "fanart":
                        await DownloadFile(rowView, "fanart", "Fanart", "fanart", scraperParameters, mediasNode!);
                        break;

                    case "boxback":
                        await DownloadFile(rowView, "boxback", "Boxback", "box-2D-back", scraperParameters, mediasNode!);
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
                        await DownloadFile(rowView, "boxart", "Boxart", scraperParameters.BoxartSource!, scraperParameters, mediasNode!);
                        break;


                    case "marquee":
                        string logosource = scraperParameters.MarqueeSource!;
                        bool downloadSuccessful = await DownloadFile(rowView, "marquee", "Marquee", scraperParameters.MarqueeSource!, scraperParameters, mediasNode!);
                        if (logosource == "wheel-hd" && downloadSuccessful == false)
                        {
                            await DownloadFile(rowView, "marquee", "Marquee", "wheel", scraperParameters, mediasNode!);
                        }
                        break;

                    case "video":
                        await DownloadFile(rowView, "video", "Video", scraperParameters.VideoSource!, scraperParameters, mediasNode!);
                        break;
                }
            }

            return new Tuple<int, int>(scrapeTotal, scrapeMax);

        }

        private void UpdateMetadata(DataRowView rowView, string column, string newValue, bool overwrite)
        {
            if (string.IsNullOrEmpty(newValue))
            {
                return;
            }

            var currentValue = rowView[column];

            // Check if the current value is DBNull or null, and if overwrite is allowed
            if (overwrite || currentValue == DBNull.Value || string.IsNullOrEmpty(currentValue.ToString()))
            {
                rowView[column] = newValue;
            }
        }

        public string ParseFamily(XmlNode familyNode, string language)
        {
            string? family = string.Empty;

            if (familyNode != null)
            {
                // First attempt: find the family in the requested language
                family = familyNode
                            .SelectNodes("famille")
                            .Cast<XmlNode>()
                            .Where(famille =>
                                famille.Attributes["principale"] != null &&
                                famille.Attributes["principale"].Value == "1" &&
                                famille.Attributes["langue"] != null &&
                                famille.Attributes["langue"].Value == language)
                            .Select(famille => famille.InnerText)
                            .FirstOrDefault();

                // Second attempt: if not found, look for English ("en")
                if (string.IsNullOrEmpty(family))
                {
                    family = familyNode
                                .SelectNodes("famille")
                                .Cast<XmlNode>()
                                .Where(famille =>
                                    famille.Attributes["principale"] != null &&
                                    famille.Attributes["principale"].Value == "1" &&
                                    famille.Attributes["langue"] != null &&
                                    famille.Attributes["langue"].Value == "en")
                                .Select(famille => famille.InnerText)
                                .FirstOrDefault();
                }

                // Third attempt: if still not found, get any available family
                if (string.IsNullOrEmpty(family))
                {
                    family = familyNode
                                .SelectNodes("famille")
                                .Cast<XmlNode>()
                                .Select(famille => famille.InnerText)
                                .FirstOrDefault();
                }
            }

            return family!;
        }



        private async Task<bool> DownloadFile(DataRowView rowView, string mediaName, string mediaType, string remoteMediaType, ScraperParameters scraperParameters, XmlNode mediasNode)
        {
            (string downloadURL, string fileFormat) = ParseMedia(remoteMediaType, mediasNode, scraperParameters.Region!);

            if (string.IsNullOrEmpty(downloadURL) || string.IsNullOrEmpty(fileFormat))
            {
                return false;
            }

            bool overwriteMedia = scraperParameters.OverwriteMedia;

            var currentValue = rowView[mediaType];
            if (currentValue != DBNull.Value && currentValue != null && !string.IsNullOrEmpty(currentValue.ToString()) && !overwriteMedia)
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

            string fileName = $"{romFileNameWithoutExtension}-{mediaName}.{fileFormat}";
            string downloadPath = $"{parentFolderPath}\\{destinationFolder}";
            string fileToDownload = $"{downloadPath}\\{fileName}";

            bool downloadSuccessful = await _fileTransfer.DownloadFile(verify, fileToDownload, downloadURL, string.Empty);
            if (downloadSuccessful)
            {
                rowView[mediaType] = $"./{destinationFolder}/{fileName}";
                return true;
            }
            return false;
        }

        private (string Url, string Format) ParseMedia(string mediaType, XmlNode xmlMedias, string region)
        {
            if (xmlMedias == null)
            {
                return (string.Empty, string.Empty);
            }

            // User selected region (first) followed by fallback regions
            string[] regions = { region, "us", "ss", "eu", "uk", "wor", "" };

            XmlNode? media = null;

            foreach (string currentRegion in regions)
            {
                // Find first matching region
                if (!string.IsNullOrEmpty(currentRegion))
                {
                    // Look for region-specific media first
                    media = xmlMedias.SelectSingleNode($"//media[@type='{mediaType}' and @region='{currentRegion}']");
                }
                else
                {
                    // Fallback for no region value at all
                    media = xmlMedias.SelectSingleNode($"//media[@type='{mediaType}']");
                }

                if (media != null)
                {
                    break; // StopPlaying once the first matching media is found
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


        private string ParseReleaseDate(XmlNode xmlNode, string currentRegion)
        {
            if (xmlNode == null)
            {
                return string.Empty;
            }

            string[] regions = { currentRegion, "wor", "us", "ss", "eu", "jp" };

            foreach (string region in regions)
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

        private string ParseNames(XmlNode namesElement, string region)
        {
            if (namesElement == null)
            {
                return string.Empty;
            }

            // User-selected region and backups
            string[] regions = { region, "wor", "us", "ss", "eu", "jp" };

            foreach (string currentRegion in regions)
            {
                XmlNode? xmlNode = namesElement.SelectSingleNode($"nom[@region='{currentRegion}']");
                if (xmlNode != null)
                {
                    string name = xmlNode.InnerText ?? string.Empty;
                    return name;
                }
            }

            return string.Empty;
        }


        private string ConvertRating(string rating)
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

        private (string GenreId, string GenreName) ParseGenres(XmlNode parentNode, string language)
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

