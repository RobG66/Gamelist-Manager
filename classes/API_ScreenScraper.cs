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
            string? xmlData = await GetXMLResponse(userID, userPassword);
            if (!string.IsNullOrEmpty(xmlData))
            {
                try
                {
                    XmlDocument xmlUserData = new XmlDocument();
                    xmlUserData.LoadXml(xmlData);
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

        public async Task<bool> VerifyScreenScraperCredentialsAsync(string userId, string userPassword)
        {
            // Return boolean true or false if authentication is successful
            string xmlResponse = await GetXMLResponse(userId, userPassword);
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
                if (string.Equals(xmlNode.InnerText, userId, StringComparison.OrdinalIgnoreCase))
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

        public async Task<string> GetXMLResponse(string username, string password)
        {
            string url = $"{apiURL}/ssuserInfos.php?devid={devId}&devpassword={devPassword}&softname={software}&output=xml&ssid={username}&sspassword={password}";
            GetXMLResponse responder = new GetXMLResponse();
            string xmlResponse = await responder.GetXMLResponseAsync(url);
            return xmlResponse;
        }

        public void SaveXmlNodeToCacheFile(string xmlString, string cacheFolder, string romName)
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


        public async Task<Tuple<int, int, MetaDataList>> ScrapeScreenScraperAsync(ScraperParameters scraperParameters)
        {

            string romName = scraperParameters.RomFileNameWithoutExtension!;
            string cacheFolder = scraperParameters.CacheFolder!;
            bool scrapeByCache = scraperParameters.ScrapeByCache;
            string scrapInfo = (!string.IsNullOrEmpty(scraperParameters.GameID)) ? $"&gameid={scraperParameters.GameID}" : $"&romtype=rom&romnom={romName}";
            string url = $"{apiURL}/jeuInfos.php?devid={devId}&devpassword={devPassword}&softname=GamelistManager&output=xml&ssid={scraperParameters.UserID}&sspassword={scraperParameters.UserPassword}&systemeid={scraperParameters.SystemID}{scrapInfo}";

            string xmlResponse = string.Empty;

            // Scrape by cache if selected
            if (scrapeByCache == true)
            {
                xmlResponse = ReadXmlFromCache(romName, cacheFolder);
            }

            if (string.IsNullOrEmpty(xmlResponse))
            {
                GetXMLResponse xmlResponder = new GetXMLResponse();
                xmlResponse = await xmlResponder.GetXMLResponseAsync(url);

                if (string.IsNullOrEmpty(xmlResponse))
                {
                    // Try one more time with the name of the rom file
                    string metaName = scraperParameters.Name!;
                    scrapInfo = $"&romtype=rom&romnom={metaName}";
                    url = $"{apiURL}/jeuInfos.php?devid={devId}&devpassword={devPassword}&softname=GamelistManager&output=xml&ssid={scraperParameters.UserID}&sspassword={scraperParameters.UserPassword}&systemeid={scraperParameters.SystemID}{scrapInfo}";
                    xmlResponse = await xmlResponder.GetXMLResponseAsync(url);
                }

                // If it's still null, failed.
                if (string.IsNullOrEmpty(xmlResponse))
                {
                    return (null!);
                }

                SaveXmlNodeToCacheFile(xmlResponse, cacheFolder, romName);
            }

            XmlDocument xmlData = new XmlDocument();
            xmlData.LoadXml(xmlResponse);

            MetaDataList metaDataList = new MetaDataList();

            int scrapeTotal = 0;
            int scrapeMax = 0;

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

            string remoteDownloadURL = string.Empty;
            string fileFormat = string.Empty;
            string fileName = string.Empty;
            string fileToDownload = string.Empty;
            string destinationFolder = string.Empty;
            string remoteElementName = string.Empty;
            bool downloadResult = false;
            bool overwrite = scraperParameters.Overwrite;
            bool verify = scraperParameters.Verify;
            var mediaPaths = scraperParameters.MediaPaths!;
            var elementsToScrape = scraperParameters.ElementsToScrape!;

            // medias xmlnode contains media download URL     
            XmlNode? mediasNode = xmlData.SelectSingleNode("/Data/jeu/medias");

            foreach (string element in elementsToScrape)
            {
                switch (element)
                {
                    case "publisher":
                        XmlNode? publisherNode = xmlData.SelectSingleNode("/Data/jeu/editeur");
                        if (publisherNode != null)
                        {
                            string publisher = publisherNode.InnerText;
                            if (!string.IsNullOrEmpty(publisher))
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.publisher, publisher);
                            }
                        }
                        break;

                    case "developer":
                        XmlNode? developerNode = xmlData.SelectSingleNode("/Data/jeu/developpeur");
                        if (developerNode != null)
                        {
                            string developer = developerNode.InnerText;
                            if (!string.IsNullOrEmpty(developer))
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.developer, developer);
                            }
                        }
                        break;

                    case "players":
                        XmlNode? playersNode = xmlData.SelectSingleNode("/Data/jeu/joueurs");
                        if (playersNode != null)
                        {
                            string players = playersNode.InnerText;
                            if (!string.IsNullOrEmpty(players))
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.players, players);
                            }
                        }
                        break;

                    case "lang":
                        XmlNode? languageNode = xmlData.SelectSingleNode("/Data/jeu/rom/romlangues");
                        if (languageNode != null)
                        {
                            string? language = languageNode.InnerText;
                            if (!string.IsNullOrEmpty(language))
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.lang, language);
                            }
                        }
                        break;

                    case "id":
                        XmlNode? gameNode = xmlData.SelectSingleNode("/Data/jeu");
                        if (gameNode != null && gameNode is XmlElement xmlElement)
                        {
                            string? id = xmlElement.GetAttribute("id");
                            if (!string.IsNullOrEmpty(id))
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.id, id);
                            }
                        }
                        break;

                    case "arcadesystemname":
                        XmlNode? asnNode = xmlData.SelectSingleNode("/Data/jeu/systeme");
                        if (asnNode != null)
                        {
                            string arcadesystemname = asnNode.InnerText;
                            if (!string.IsNullOrEmpty(arcadesystemname))
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.arcadesystemname, arcadesystemname);
                            }
                        }
                        break;

                    case "rating":
                        XmlNode? ratingNode = xmlData.SelectSingleNode("/Data/jeu/note");
                        if (ratingNode != null)
                        {
                            string rating = ratingNode.InnerText;
                            if (!string.IsNullOrEmpty(rating))
                            {
                                string convertedRating = ConvertRating(rating);
                                metaDataList.SetMetadataValue(MetaDataKeys.rating, convertedRating);
                            }
                        }
                        break;

                    case "desc":
                        XmlNode? descriptionNode = xmlData.SelectSingleNode($"/Data/jeu/synopsis/synopsis[@langue='{scraperParameters.Language}']");
                        if (descriptionNode == null)
                        {
                            descriptionNode = xmlData.SelectSingleNode($"/Data/jeu/synopsis/synopsis[@langue='en']");
                        }
                        if (descriptionNode != null)
                        {
                            string description = descriptionNode.InnerText;
                            if (string.IsNullOrEmpty(description))
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.desc, description);
                            }
                        }
                        break;

                    case "name":
                        XmlNode? namesNode = xmlData.SelectSingleNode("/Data/jeu/noms");
                        if (namesNode != null)
                        {
                            string name = ParseNames(namesNode, scraperParameters.Region!);
                            if (!string.IsNullOrEmpty(name))
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.name, name);
                            }
                        }
                        break;

                    case "genre":
                        XmlNode? genresNode = xmlData.SelectSingleNode("/Data/jeu/genres");
                        if (genresNode != null)
                        {
                            (string genreID, string genreName) = ParseGenres(genresNode, scraperParameters.Language!);
                            if (!string.IsNullOrEmpty(genreName))
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.genre, genreName);
                                //metaDataList.SetMetadataValue(MetaDataKeys.genreIds, genreID);
                            }
                        }
                        break;

                    case "family":
                        XmlNode? familyNode = xmlData.SelectSingleNode("/Data/jeu/familles/famille");
                        if (familyNode != null)
                        {
                            string family = familyNode.InnerText;
                            if (!string.IsNullOrEmpty(family))
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.family, family);
                            }
                        }
                        break;

                    case "region":
                        XmlNode? regionNode = xmlData.SelectSingleNode("/Data/jeu/rom/romregions");
                        if (regionNode != null)
                        {
                            string region = regionNode.InnerText;
                            if (!string.IsNullOrEmpty(region))
                            {
                                metaDataList.SetMetadataValue(MetaDataKeys.region, region);
                            }
                        }
                        break;

                    case "releasedate":
                        XmlNode? releaseDateNode = xmlData.SelectSingleNode("/Data/jeu/dates");
                        if (releaseDateNode != null)
                        {
                            string releaseDate = ParseReleaseDate(releaseDateNode, scraperParameters.Region!);
                            if (!string.IsNullOrEmpty(releaseDate))
                            {
                                string convertedReleaseDate = ISO8601Converter.ConvertToISO8601(releaseDate);
                                metaDataList.SetMetadataValue(MetaDataKeys.releasedate, convertedReleaseDate);
                            }
                        }
                        break;

                    case "bezel":
                        destinationFolder = mediaPaths["bezel"];
                        remoteElementName = "bezel-16-9";
                        if (mediasNode != null)
                        {
                            (remoteDownloadURL, fileFormat) = ParseMedia(remoteElementName, mediasNode, scraperParameters.Region!);

                            if (!string.IsNullOrEmpty(remoteDownloadURL))
                            {
                                if (!Directory.Exists($"{scraperParameters.ParentFolderPath}\\{destinationFolder}"))
                                {
                                    Directory.CreateDirectory($"{scraperParameters.ParentFolderPath}\\{destinationFolder}");
                                }
                                fileName = $"{scraperParameters.RomFileNameWithoutExtension}-{element}.{fileFormat}";
                                fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                                downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                                if (downloadResult == true)
                                {
                                    metaDataList.SetMetadataValue(MetaDataKeys.bezel, $"./{destinationFolder}/{fileName}");
                                }
                            }
                        }
                        break;

                    case "fanart":
                        destinationFolder = mediaPaths["fanart"];
                        remoteElementName = "fanart";
                        if (mediasNode != null)
                        {
                            (remoteDownloadURL, fileFormat) = ParseMedia(remoteElementName, mediasNode, scraperParameters.Region!);
                            if (!string.IsNullOrEmpty(remoteDownloadURL))
                            {
                                if (!Directory.Exists($"{scraperParameters.ParentFolderPath}\\{destinationFolder}"))
                                {
                                    Directory.CreateDirectory($"{scraperParameters.ParentFolderPath}\\{destinationFolder}");
                                }
                                fileName = $"{scraperParameters.RomFileNameWithoutExtension}-{element}.{fileFormat}";
                                fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                                downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                                if (downloadResult == true)
                                {
                                    metaDataList.SetMetadataValue(MetaDataKeys.fanart, $"./{destinationFolder}/{fileName}");
                                }
                            }
                        }
                        break;

                    case "boxback":
                        destinationFolder = mediaPaths["boxback"];
                        remoteElementName = "box-2D-back";
                        if (mediasNode != null)
                        {
                            (remoteDownloadURL, fileFormat) = ParseMedia(remoteElementName, mediasNode, scraperParameters.Region!);
                            if (!string.IsNullOrEmpty(remoteDownloadURL))
                            {
                                if (!Directory.Exists($"{scraperParameters.ParentFolderPath}\\{destinationFolder}"))
                                {
                                    Directory.CreateDirectory($"{scraperParameters.ParentFolderPath}\\{destinationFolder}");
                                }
                                fileName = $"{scraperParameters.RomFileNameWithoutExtension}-{element}.{fileFormat}";
                                fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                                downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                                if (downloadResult == true)
                                {
                                    metaDataList.SetMetadataValue(MetaDataKeys.boxback, $"./{destinationFolder}/{fileName}");
                                }
                            }
                        }
                        break;

                    case "manual":
                        destinationFolder = mediaPaths["manual"];
                        remoteElementName = "manuel";
                        if (mediasNode != null)
                        {
                            (remoteDownloadURL, fileFormat) = ParseMedia(remoteElementName, mediasNode, scraperParameters.Region!);
                            if (!string.IsNullOrEmpty(remoteDownloadURL))
                            {
                                if (!Directory.Exists($"{scraperParameters.ParentFolderPath}\\{destinationFolder}"))
                                {
                                    Directory.CreateDirectory($"{scraperParameters.ParentFolderPath}\\{destinationFolder}");
                                }
                                fileName = $"{scraperParameters.RomFileNameWithoutExtension}-{element}.{fileFormat}";
                                fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                                downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                                if (downloadResult == true)
                                {
                                    metaDataList.SetMetadataValue(MetaDataKeys.manual, $"./{destinationFolder}/{fileName}");
                                }
                            }
                        }
                        break;

                    case "image":
                        destinationFolder = mediaPaths["image"];
                        remoteElementName = scraperParameters.ImageSource!;
                        if (mediasNode != null)
                        {
                            (remoteDownloadURL, fileFormat) = ParseMedia(remoteElementName, mediasNode, scraperParameters.Region!);
                            if (!string.IsNullOrEmpty(remoteDownloadURL))
                            {
                                if (!Directory.Exists($"{scraperParameters.ParentFolderPath}\\{destinationFolder}"))
                                {
                                    Directory.CreateDirectory($"{scraperParameters.ParentFolderPath}\\{destinationFolder}");
                                }
                                fileName = $"{scraperParameters.RomFileNameWithoutExtension}-{element}.{fileFormat}";
                                fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                                downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                                if (downloadResult == true)
                                {
                                    metaDataList.SetMetadataValue(MetaDataKeys.image, $"./{destinationFolder}/{fileName}");
                                }
                            }
                        }
                        break;

                    case "thumbnail":
                        destinationFolder = mediaPaths["thumbnail"];
                        remoteElementName = scraperParameters.BoxSource!;
                        if (mediasNode != null)
                        {
                            (remoteDownloadURL, fileFormat) = ParseMedia(remoteElementName, mediasNode, scraperParameters.Region!);
                            if (!string.IsNullOrEmpty(remoteDownloadURL))
                            {
                                if (!Directory.Exists($"{scraperParameters.ParentFolderPath}\\{destinationFolder}"))
                                {
                                    Directory.CreateDirectory($"{scraperParameters.ParentFolderPath}\\{destinationFolder}");
                                }
                                // Use thumb instead of thumbnail, same as batocera scraping
                                fileName = $"{scraperParameters.RomFileNameWithoutExtension}-thumb.{fileFormat}";
                                fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                                downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                                if (downloadResult == true)
                                {
                                    metaDataList.SetMetadataValue(MetaDataKeys.thumbnail, $"./{destinationFolder}/{fileName}");
                                }
                            }
                        }
                        break;

                    case "marquee":
                        destinationFolder = mediaPaths["marquee"];
                        remoteElementName = scraperParameters.LogoSource!;
                        if (mediasNode != null)
                        {
                            (remoteDownloadURL, fileFormat) = ParseMedia(remoteElementName, mediasNode, scraperParameters.Region!);

                            if (!string.IsNullOrEmpty(remoteDownloadURL))
                            {
                                if (!Directory.Exists($"{scraperParameters.ParentFolderPath}\\{destinationFolder}"))
                                {
                                    Directory.CreateDirectory($"{scraperParameters.ParentFolderPath}\\{destinationFolder}");
                                }
                                fileName = $"{scraperParameters.RomFileNameWithoutExtension}-{element}.{fileFormat}";
                                fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                                downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                                if (downloadResult == true)
                                {
                                    metaDataList.SetMetadataValue(MetaDataKeys.marquee, $"./{destinationFolder}/{fileName}");
                                }
                            }
                        }
                        break;


                    case "video":
                        destinationFolder = mediaPaths["video"];
                        remoteElementName = scraperParameters.VideoSource!;
                        if (mediasNode != null)
                        {
                            (remoteDownloadURL, fileFormat) = ParseMedia(remoteElementName, mediasNode, scraperParameters.Region!);
                            if (!string.IsNullOrEmpty(remoteDownloadURL))
                            {
                                if (!Directory.Exists($"{scraperParameters.ParentFolderPath}\\{destinationFolder}"))
                                {
                                    Directory.CreateDirectory($"{scraperParameters.ParentFolderPath}\\{destinationFolder}");
                                }
                                fileName = $"{scraperParameters.RomFileNameWithoutExtension}-{element}.{fileFormat}";
                                fileToDownload = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}\\{fileName}";
                                downloadResult = await FileTransfer.DownloadFile(verify, overwrite, fileToDownload, remoteDownloadURL);
                                if (downloadResult == true)
                                {
                                    metaDataList.SetMetadataValue(MetaDataKeys.video, $"./{destinationFolder}/{fileName}");
                                }
                            }
                        }
                        break;
                }
            }

            return new Tuple<int, int, MetaDataList>(scrapeTotal, scrapeMax, metaDataList);

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


        private string ParseReleaseDate(XmlNode namesElement, string region)
        {
            if (namesElement == null)
            {
                return string.Empty;
            }

            string[] regions = { region, "wor", "us", "ss", "eu", "jp" };

            foreach (string currentRegion in regions)
            {
                XmlNode? xmlNode = namesElement.SelectSingleNode($"date[@region='{region}']");
                if (xmlNode != null)
                {
                    // Get the InnerText of the found node
                    string releaseDate = xmlNode.InnerText ?? string.Empty;

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

