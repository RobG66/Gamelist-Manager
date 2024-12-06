using LibVLCSharp.Shared;
using System.Data;
using System.IO;
using System.Xml;

namespace GamelistManager.classes
{
    internal class API_ScreenScraper
    {
        private readonly string apiURL = "https://api.screenscraper.fr/api2";
        private readonly string devId = "";
        private readonly string devPassword = "";
        private readonly string software = "GamelistManager";

        public async Task<int> GetMaxThreadsAsync(string userID, string userPassword)
        {
            string url = $"{apiURL}/ssuserInfos.php?devid={devId}&devpassword={devPassword}&softname={software}&output=xml&ssid={userID}&sspassword={userPassword}";
            string xmlResponse = await GetXMLResponse.GetXMLResponseAsync(string.Empty, url);

            if (!string.IsNullOrEmpty(xmlResponse))
            {
                try
                {
                    XmlDocument xmlUserData = new XmlDocument();
                    xmlUserData.LoadXml(xmlResponse);
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
            string xmlResponse = await GetXMLResponse.GetXMLResponseAsync(string.Empty, url);
            if (string.IsNullOrEmpty(xmlResponse))
            {
                return false;
            }

            try
            {
                XmlDocument xmlUserData = new XmlDocument();
                xmlUserData.LoadXml(xmlResponse);
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
            string romName = scraperParameters.RomFileNameWithoutExtension!;
            string cacheFolder = scraperParameters.CacheFolder!;
            bool scrapeByCache = scraperParameters.ScrapeByCache;
            bool skipNonCached = scraperParameters.SkipNonCached;
            bool cachedInfo = false;
            string scrapInfo = (!string.IsNullOrEmpty(scraperParameters.GameID)) ? $"&gameid={scraperParameters.GameID}" : $"&romtype=rom&romnom={romName}";
            string url = $"{apiURL}/jeuInfos.php?devid={devId}&devpassword={devPassword}&softname=GamelistManager&output=xml&ssid={scraperParameters.UserID}&sspassword={scraperParameters.UserPassword}&systemeid={scraperParameters.SystemID}{scrapInfo}";

            string xmlResponse = string.Empty;

            // Scrape by cache if selected
            if (scrapeByCache)
            {
                xmlResponse = ReadXmlFromCache(romName, cacheFolder);
                if (string.IsNullOrEmpty(xmlResponse) && skipNonCached)
                {
                    return null!;
                }
                cachedInfo = true;
            }

            if (string.IsNullOrEmpty(xmlResponse))
            {
                cachedInfo = false;
                xmlResponse = await GetXMLResponse.GetXMLResponseAsync(string.Empty, url);


                if (string.IsNullOrEmpty(xmlResponse))
                {
                    // Try one more time with the name of the rom file
                    string metaName = scraperParameters.Name!;
                    scrapInfo = $"&romtype=rom&romnom={metaName}";
                    url = $"{apiURL}/jeuInfos.php?devid={devId}&devpassword={devPassword}&softname=GamelistManager&output=xml&ssid={scraperParameters.UserID}&sspassword={scraperParameters.UserPassword}&systemeid={scraperParameters.SystemID}{scrapInfo}";
                    xmlResponse = await GetXMLResponse.GetXMLResponseAsync(string.Empty, url);
                }

                // If it's still null, failed.
                if (string.IsNullOrEmpty(xmlResponse))
                {
                    return (null!);
                }

                SaveXmlToCacheFile(xmlResponse, cacheFolder, romName);
            }

            XmlDocument xmlData = new XmlDocument();
            xmlData.LoadXml(xmlResponse);
                     
            int scrapeTotal = 0;
            int scrapeMax = 0;

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

            var elementsToScrape = scraperParameters.ElementsToScrape!;
            bool overwriteMetaData = scraperParameters.OverwriteMetadata;
            XmlNode? mediasNode = xmlData.SelectSingleNode("/Data/jeu/medias");
            bool downloadSuccessful = false;

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
                        string? language = xmlData.SelectSingleNode("/Data/jeu/rom/romlangues")?.InnerText; ;
                        UpdateMetadata(rowView, "Language", language!, overwriteMetaData);
                        break;

                    case "id":
                        string? id = (xmlData.SelectSingleNode("/Data/jeu") as XmlElement)?.GetAttribute("id");
                        UpdateMetadata(rowView, "Game Id", id!, overwriteMetaData);
                        break;

                    case "arcadesystemname":
                        string? arcadeSystemName = xmlData.SelectSingleNode("/Data/jeu/systeme")?.InnerText; ;
                        UpdateMetadata(rowView, "Arcade System Name", arcadeSystemName!, overwriteMetaData);
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
                        string? name = ParseNames(xmlData.SelectSingleNode("/Data/jeu/noms"), scraperParameters.Region!);
                        UpdateMetadata(rowView, "Name", name!, overwriteMetaData);
                        break;

                    case "genre":
                        var genresNode = xmlData.SelectSingleNode("/Data/jeu/genres");
                        if (genresNode != null)
                        {
                            var (_, genreName) = ParseGenres(genresNode, scraperParameters.Language!);
                            UpdateMetadata(rowView, "Genre", genreName!, overwriteMetaData);
                        }
                        break;

                    case "family":
                        string? family = xmlData.SelectSingleNode("/Data/jeu/familles/famille")?.InnerText;
                        UpdateMetadata(rowView, "Family", family!, overwriteMetaData);
                        break;

                    case "region":
                        string? region = xmlData.SelectSingleNode("/Data/jeu/rom/romregions")?.InnerText; ;
                        UpdateMetadata(rowView, "Region", region!, overwriteMetaData);
                        break;

                    case "releasedate":
                        string? releaseDate = ParseReleaseDate(xmlData.SelectSingleNode("/Data/jeu/dates"), scraperParameters.Region!);
                        UpdateMetadata(rowView, "Release Date", releaseDate!, overwriteMetaData);
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
                        await DownloadFile(rowView, "thumbnail", "Box", scraperParameters.BoxSource!, scraperParameters, mediasNode!);
                        break;

                    case "marquee":
                        string logosource = scraperParameters.LogoSource!;
                        downloadSuccessful = await DownloadFile(rowView, "marquee", "Logo", scraperParameters.LogoSource!, scraperParameters, mediasNode!);
                        if (logosource == "wheel-hd" && downloadSuccessful == false)
                        {
                            await DownloadFile(rowView, "marquee", "Logo", "wheel", scraperParameters, mediasNode!);
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

        private async Task<bool> DownloadFile(DataRowView rowView, string mediaName, string mediaType, string remoteMediaType, ScraperParameters scraperParameters, XmlNode mediasNode)
        {            
            (string downloadURL, string fileFormat) = ParseMedia(remoteMediaType, mediasNode, scraperParameters.Region!);
                        
            if (string.IsNullOrEmpty(downloadURL) || string.IsNullOrEmpty(fileFormat))
            {
                return false;
            }

            bool overwriteMedia = scraperParameters.OverwriteMedia;

            var currentValue = rowView[mediaType];
            if (currentValue != DBNull.Value && !string.IsNullOrEmpty(currentValue.ToString()) && !overwriteMedia)
            {
                return false;
            }

            string name = scraperParameters.Name!;
            string romFileNameWithoutExtension = scraperParameters.RomFileNameWithoutExtension!;
            var mediaPaths = scraperParameters.MediaPaths!;
            string destinationFolder = mediaPaths[mediaName];
            string parentFolderPath = scraperParameters.ParentFolderPath!;
            bool verify = scraperParameters.Verify;
            bool overwrite = scraperParameters.OverwriteMedia;

            if (mediaName == "thumbnail")
            {
                mediaName = "thumb";
            }

            string fileName = $"{romFileNameWithoutExtension}-{mediaName}.{fileFormat}";
            string downloadPath = $"{parentFolderPath}\\{destinationFolder}";
            string fileToDownload = $"{downloadPath}\\{fileName}";

            bool downloadSuccessful = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, downloadURL);
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
                    break; // Stop once the first matching media is found
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

