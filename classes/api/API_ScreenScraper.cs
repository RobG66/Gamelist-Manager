using GamelistManager.classes.helpers;
using GamelistManager.classes.io;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Xml;

namespace GamelistManager.classes.api
{
    public class ScreenScraperResult
    {
        public ScrapedGameData GameData { get; set; } = new();
        public int ScrapeLimitProgress { get; set; }
        public int ScrapeLimitMax { get; set; }
    }

    internal class API_ScreenScraper
    {
        private const string ApiUrl = "https://api.screenscraper.fr/api2";
        private readonly string _devId;
        private readonly string _devPassword;
        private const string Software = "GamelistManager";
        private readonly HttpClient _httpClient;
        private static readonly List<string> DefaultRegions = new List<string> { "wor", "us", "ss", "eu", "jp" };

        public API_ScreenScraper(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _devId = ConfigurationManager.AppSettings["ScreenScraper_DevId"]
                ?? throw new InvalidOperationException("ScreenScraper_DevId not found in App.config");
            _devPassword = ConfigurationManager.AppSettings["ScreenScraper_DevPassword"]
                ?? throw new InvalidOperationException("ScreenScraper_DevPassword not found in App.config");
        }

        private async Task<(string Xml, string ErrorMessage)> FetchFromApi(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                string xml = Encoding.UTF8.GetString(bytes);
                return (xml, string.Empty);
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

        private void SaveToCache(string xml, string cacheFolder, string romName, ScraperParameters parameters)
        {
            if (string.IsNullOrEmpty(cacheFolder) || string.IsNullOrEmpty(xml))
                return;

            try
            {
                Directory.CreateDirectory(cacheFolder);

                var redactions = new[]
                {
                    (parameters.UserPassword, "{{REDACTED_USERPASS}}"),
                    (parameters.UserID, "{{REDACTED_USERID}}"),
                    (_devPassword, "{{REDACTED_PASSWORD}}"),
                    (_devId, "{{REDACTED_DEVID}}")
                };

                foreach (var (sensitive, placeholder) in redactions)
                {
                    if (!string.IsNullOrEmpty(sensitive))
                    {
                        xml = xml.Replace(sensitive, placeholder);
                    }
                }

                string filePath = Path.Combine(cacheFolder, $"{romName}.xml");
                File.WriteAllText(filePath, xml);
            }
            catch
            {
                // Ignore cache write errors
            }
        }

        private string ReadFromCache(string romName, string cacheFolder, ScraperParameters parameters)
        {
            if (string.IsNullOrEmpty(cacheFolder))
                return string.Empty;

            string filePath = Path.Combine(cacheFolder, $"{romName}.xml");

            if (!File.Exists(filePath))
                return string.Empty;

            try
            {
                string xml = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(xml))
                    return string.Empty;

                // Restore redacted values
                var restorations = new[]
                {
                    ("{{REDACTED_USERPASS}}", parameters.UserPassword),
                    ("{{REDACTED_USERID}}", parameters.UserID),
                    ("{{REDACTED_PASSWORD}}", _devPassword),
                    ("{{REDACTED_DEVID}}", _devId)
                };

                foreach (var (placeholder, actual) in restorations)
                {
                    if (!string.IsNullOrEmpty(actual))
                    {
                        xml = xml.Replace(placeholder, actual);
                    }
                }

                return xml;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<(int MaxThreads, string ErrorMessage)> AuthenticateAsync(string userID, string userPassword)
        {
            if (string.IsNullOrEmpty(userID) || string.IsNullOrEmpty(userPassword))
                return (-1, "User ID and password are required");

            string url = $"{ApiUrl}/ssuserInfos.php?devid={_devId}&devpassword={_devPassword}&softname={Software}&output=xml&ssid={userID}&sspassword={userPassword}";

            var (xml, fetchError) = await FetchFromApi(url);

            if (string.IsNullOrEmpty(xml))
            {
                string errorMsg = string.IsNullOrEmpty(fetchError)
                    ? "Failed to retrieve authentication data"
                    : fetchError;
                return (-1, errorMsg);
            }

            try
            {
                XmlDocument xmlDoc = new();
                xmlDoc.LoadXml(xml);

                XmlNode? idNode = xmlDoc.SelectSingleNode("//ssuser/id");
                if (idNode == null)
                {
                    return (-1, "User ID not found in authentication response");
                }

                if (!string.Equals(idNode.InnerText, userID, StringComparison.OrdinalIgnoreCase))
                {
                    return (-1, "User ID mismatch in authentication response");
                }

                XmlNode? maxThreadsNode = xmlDoc.SelectSingleNode("//ssuser/maxthreads");
                if (maxThreadsNode == null)
                {
                    return (1, string.Empty);
                }

                int maxThreads = int.TryParse(maxThreadsNode.InnerText, out int mt) ? mt : 1;
                return (maxThreads, string.Empty);
            }
            catch (XmlException ex)
            {
                return (-1, $"Failed to parse authentication XML: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (-1, $"Error during authentication: {ex.Message}");
            }
        }

        private string BuildScrapeUrl(ScraperParameters parameters, string romName)
        {
            string baseUrl = $"{ApiUrl}/jeuInfos.php?devid={_devId}&devpassword={_devPassword}&softname={Software}&output=xml&ssid={parameters.UserID}&sspassword={parameters.UserPassword}&systemeid={parameters.SystemID}";

            if (!string.IsNullOrEmpty(parameters.GameID))
            {
                return $"{baseUrl}&gameid={parameters.GameID}";
            }

            string scrapInfo = $"&romtype=rom&romnom={Uri.EscapeDataString(romName)}";

            if (string.IsNullOrEmpty(parameters.ParentFolderPath) || string.IsNullOrEmpty(parameters.RomFileName))
                return baseUrl + scrapInfo;

            string romFullPath = Path.Combine(parameters.ParentFolderPath, parameters.RomFileName);

            if (!File.Exists(romFullPath))
                return baseUrl + scrapInfo;

            long fileSize = new FileInfo(romFullPath).Length;

            // Use MD5 for files <= 128MB
            if (fileSize > 0 && fileSize <= 131072 * 1024)
            {
                string md5 = ChecksumHelper.CalculateMD5(romFullPath);
                if (!string.IsNullOrEmpty(md5))
                {
                    scrapInfo += $"&md5={md5}";

                    string crc32 = ChecksumHelper.CalculateCRC32(romFullPath);
                    if (!string.IsNullOrEmpty(crc32))
                    {
                        scrapInfo += $"&crc={crc32}";
                    }
                }
            }

            scrapInfo += $"&romtaille={fileSize}";

            return baseUrl + scrapInfo;
        }

        private string BuildScrapeUrlByName(ScraperParameters parameters)
        {
            if (string.IsNullOrEmpty(parameters.RomName))
                return string.Empty;

            string scrapInfo = $"&romtype=rom&romnom={Uri.EscapeDataString(parameters.RomName)}";
            return $"{ApiUrl}/jeuInfos.php?devid={_devId}&devpassword={_devPassword}&softname={Software}&output=xml&ssid={parameters.UserID}&sspassword={parameters.UserPassword}&systemeid={parameters.SystemID}{scrapInfo}";
        }

        private static void AddMedia(ScrapedGameData data, ScrapedGameData.MediaResult? media)
        {
            if (media != null)
            {
                data.Media.Add(media);
            }
        }

        private static ScrapedGameData.MediaResult? GetMediaResult(string remoteMediaType, string mediaType, ScraperParameters parameters, XmlNode mediasNode)
        {
            if (string.IsNullOrEmpty(remoteMediaType) || mediasNode == null)
                return null;

            (string url, string format, string region) = (string.Empty, string.Empty, string.Empty);

            // Try with user's preferred regions first
            if (parameters.SSRegions != null && parameters.SSRegions.Count > 0)
            {
                (url, format, region) = ParseMedia(remoteMediaType, mediasNode, parameters.SSRegions);
            }

            // If nothing found and scrape any media is enabled, try all regions
            if (string.IsNullOrEmpty(url) && parameters.ScrapeAnyMedia)
            {
                var allRegions = mediasNode.SelectNodes($"//media[@type='{remoteMediaType}']")
                    ?.Cast<XmlNode>()
                    .Select(n => n.Attributes?["region"]?.Value ?? string.Empty)
                    .Distinct()
                    .ToList();

                if (allRegions != null && allRegions.Count > 0)
                {
                    (url, format, region) = ParseMedia(remoteMediaType, mediasNode, allRegions);
                }
            }

            // Fallback for videos which often have no region
            if (string.IsNullOrEmpty(url) && remoteMediaType.Contains("video"))
            {
                (url, format, region) = ParseMedia(remoteMediaType, mediasNode, new List<string> { string.Empty });
            }

            if (string.IsNullOrEmpty(url))
                return null;

            return new ScrapedGameData.MediaResult
            {
                Url = url,
                FileExtension = format.StartsWith(".") ? format : $".{format}",
                Region = region ?? string.Empty,
                MediaType = mediaType
            };
        }

        private static void ParseElements(XmlDocument xmlData, XmlNode mediasNode, ScraperParameters parameters, ScrapedGameData gameData)
        {
            if (parameters.ElementsToScrape == null)
                return;

            foreach (string element in parameters.ElementsToScrape)
            {
                switch (element)
                {
                    case "publisher":
                        string? publisher = xmlData.SelectSingleNode("/Data/jeu/editeur")?.InnerText;
                        if (!string.IsNullOrEmpty(publisher))
                            gameData.Data["publisher"] = publisher;
                        break;

                    case "developer":
                        string? developer = xmlData.SelectSingleNode("/Data/jeu/developpeur")?.InnerText;
                        if (!string.IsNullOrEmpty(developer))
                            gameData.Data["developer"] = developer;
                        break;

                    case "players":
                        string? players = xmlData.SelectSingleNode("/Data/jeu/joueurs")?.InnerText;
                        if (!string.IsNullOrEmpty(players))
                            gameData.Data["players"] = players;
                        break;

                    case "id":
                        string? id = (xmlData.SelectSingleNode("/Data/jeu") as XmlElement)?.GetAttribute("id");
                        if (!string.IsNullOrEmpty(id))
                            gameData.Data["id"] = id;
                        break;

                    case "arcadesystemname":
                        string? platformId = (xmlData.SelectSingleNode("/Data/jeu/systeme") as XmlElement)?.GetAttribute("id");
                        if (ushort.TryParse(platformId, out ushort idValue))
                        {
                            string arcadeId = ArcadeSystemID.GetArcadeSystemNameByID(idValue);
                            if (!string.IsNullOrEmpty(arcadeId))
                                gameData.Data["arcadesystemname"] = arcadeId;
                        }
                        break;

                    case "rating":
                        string? rating = xmlData.SelectSingleNode("/Data/jeu/note")?.InnerText;
                        string convertedRating = ConvertRating(rating);
                        if (!string.IsNullOrEmpty(convertedRating))
                            gameData.Data["rating"] = convertedRating;
                        break;

                    case "desc":
                        string descLanguage = !string.IsNullOrEmpty(parameters.SSLanguage) ? parameters.SSLanguage : "en";
                        string? desc = xmlData
                            .SelectSingleNode($"/Data/jeu/synopsis/synopsis[@langue='{descLanguage}']")
                            ?.InnerText
                            ?? xmlData.SelectSingleNode("/Data/jeu/synopsis/synopsis[@langue='en']")?.InnerText;
                        if (!string.IsNullOrEmpty(desc))
                            gameData.Data["desc"] = desc;
                        break;

                    case "name":
                        {
                            var namesNode = xmlData.SelectSingleNode("/Data/jeu/noms");
                            if (namesNode != null)
                            {
                                string nameLanguage = !string.IsNullOrEmpty(parameters.SSLanguage) ? parameters.SSLanguage : "en";
                                var regions = parameters.SSRegions != null && parameters.SSRegions.Count > 0
                                    ? parameters.SSRegions
                                    : DefaultRegions;
                                string name = ParseNames(namesNode, parameters.RomFileName!, nameLanguage, regions);
                                if (!string.IsNullOrEmpty(name))
                                    gameData.Data["name"] = name;
                            }
                        }
                        break;

                    case "genre":
                        var genresNode = xmlData.SelectSingleNode("/Data/jeu/genres");
                        if (genresNode != null)
                        {
                            string genreLanguage = parameters.ScrapeEnglishGenreOnly == true ? "en"
                                : (!string.IsNullOrEmpty(parameters.SSLanguage) ? parameters.SSLanguage : "en");
                            var (_, genreName) = ParseGenres(genresNode, genreLanguage);
                            if (!string.IsNullOrEmpty(genreName))
                                gameData.Data["genre"] = genreName;
                        }
                        break;

                    case "family":
                        var familyNode = xmlData.SelectSingleNode("/Data/jeu/familles");
                        if (familyNode != null)
                        {
                            string familyLanguage = !string.IsNullOrEmpty(parameters.SSLanguage) ? parameters.SSLanguage : "en";
                            string family = ParseFamily(familyNode, familyLanguage);
                            if (!string.IsNullOrEmpty(family))
                                gameData.Data["family"] = family;
                        }
                        break;

                    case "releasedate":
                        {
                            var datesNode = xmlData.SelectSingleNode("/Data/jeu/dates");
                            if (datesNode != null)
                            {
                                var regions = parameters.SSRegions != null && parameters.SSRegions.Count > 0
                                    ? parameters.SSRegions
                                    : DefaultRegions;
                                string releaseDate = ParseReleaseDate(datesNode, regions);
                                if (!string.IsNullOrEmpty(releaseDate))
                                    gameData.Data["releasedate"] = releaseDate;
                            }
                        }
                        break;

                    case "titleshot":
                        AddMedia(gameData, GetMediaResult("sstitle", "titleshot", parameters, mediasNode));
                        break;

                    case "bezel":
                        AddMedia(gameData, GetMediaResult("bezel-16-9", "bezel", parameters, mediasNode));
                        break;

                    case "fanart":
                        AddMedia(gameData, GetMediaResult("fanart", "fanart", parameters, mediasNode));
                        break;

                    case "boxback":
                        AddMedia(gameData, GetMediaResult("box-2D-back", "boxback", parameters, mediasNode));
                        break;

                    case "manual":
                        AddMedia(gameData, GetMediaResult("manuel", "manual", parameters, mediasNode));
                        break;

                    case "image":
                        AddMedia(gameData, GetMediaResult(parameters.ImageSource!, "image", parameters, mediasNode));
                        break;

                    case "cartridge":
                        AddMedia(gameData, GetMediaResult(parameters.CartridgeSource!, "cartridge", parameters, mediasNode));
                        break;


                    case "thumbnail":
                        AddMedia(gameData, GetMediaResult(parameters.ThumbnailSource!, "thumbnail", parameters, mediasNode));
                        break;

                    case "boxart":
                        AddMedia(gameData, GetMediaResult(parameters.BoxArtSource!, "boxart", parameters, mediasNode));
                        break;

                    case "marquee":
                        {
                            var marqueeMedia = GetMediaResult(parameters.MarqueeSource!, "marquee", parameters, mediasNode);
                            if (marqueeMedia == null && parameters.MarqueeSource == "wheel-hd")
                            {
                                marqueeMedia = GetMediaResult("wheel", "marquee", parameters, mediasNode);
                            }
                            AddMedia(gameData, marqueeMedia);
                        }
                        break;

                    case "wheel":
                        {
                            var wheelMedia = GetMediaResult(parameters.WheelSource!, "wheel", parameters, mediasNode);
                            if (wheelMedia == null && parameters.WheelSource == "wheel-hd")
                            {
                                wheelMedia = GetMediaResult("wheel", "wheel", parameters, mediasNode);
                            }
                            AddMedia(gameData, wheelMedia);
                        }
                        break;

                    case "video":
                        AddMedia(gameData, GetMediaResult(parameters.VideoSource!, "video", parameters, mediasNode));
                        break;

                    case "map":
                        AddMedia(gameData, GetMediaResult("phy", "map", parameters, mediasNode));
                        break;

                    case "magazine":
                        AddMedia(gameData, GetMediaResult("presse", "magazine", parameters, mediasNode));
                        break;

                    case "mix":
                        var mixMedia = GetMediaResult("mixrbv2", "mix", parameters, mediasNode);
                        if (mixMedia == null)
                        {
                            mixMedia = GetMediaResult("mixrbv1", "mix", parameters, mediasNode);
                        }
                        AddMedia(gameData, mixMedia);
                        break;
                }
            }
        }

        public async Task<ScreenScraperResult> ScrapeScreenScraperAsync(ScraperParameters parameters)
        {
            var result = new ScreenScraperResult { GameData = new ScrapedGameData() };

            if (parameters.ElementsToScrape == null || parameters.ElementsToScrape.Count == 0)
            {
                result.GameData.WasSuccessful = false;
                result.GameData.Messages.Add("No elements to scrape");
                return result;
            }

            if (string.IsNullOrEmpty(parameters.RomFileName))
            {
                result.GameData.WasSuccessful = false;
                result.GameData.Messages.Add("ROM filename is required");
                return result;
            }

            string romName = Path.GetFileNameWithoutExtension(parameters.RomFileName);
            string xml = string.Empty;
            bool fromCache = false;

            // Try cache first
            if (parameters.ScrapeByCache && !string.IsNullOrEmpty(parameters.CacheFolder))
            {
                xml = ReadFromCache(romName, parameters.CacheFolder, parameters);

                if (!string.IsNullOrEmpty(xml))
                {
                    fromCache = true;
                }
                else if (parameters.SkipNonCached)
                {
                    result.GameData.WasSuccessful = false;
                    result.GameData.Messages.Add($"{romName} not found in cache and skip non-cached enabled");
                    return result;
                }
            }

            // Fetch from API if not in cache
            if (string.IsNullOrEmpty(xml))
            {
                string url = BuildScrapeUrl(parameters, romName);
                var (apiXml, fetchError) = await FetchFromApi(url);
                xml = apiXml;

                // Retry with ROM name if first attempt failed
                if (string.IsNullOrEmpty(xml) && !string.IsNullOrEmpty(parameters.RomName))
                {
                    string retryUrl = BuildScrapeUrlByName(parameters);
                    if (!string.IsNullOrEmpty(retryUrl))
                    {
                        (apiXml, fetchError) = await FetchFromApi(retryUrl);
                        xml = apiXml;
                    }
                }

                if (string.IsNullOrEmpty(xml))
                {
                    result.GameData.WasSuccessful = false;
                    result.GameData.Messages.Add(string.IsNullOrEmpty(fetchError)
                        ? $"Failed to retrieve data from ScreenScraper for {romName}"
                        : fetchError);
                    return result;
                }

                if (!string.IsNullOrEmpty(parameters.CacheFolder))
                {
                    SaveToCache(xml, parameters.CacheFolder, romName, parameters);
                }
            }

            // Parse XML
            XmlDocument xmlDoc = new();
            try
            {
                xmlDoc.LoadXml(xml);
            }
            catch (XmlException ex)
            {
                result.GameData.WasSuccessful = false;
                result.GameData.Messages.Add($"Failed to parse XML for {romName}: {ex.Message}");
                return result;
            }

            // Get scrape limits (only if not from cache)
            if (!fromCache)
            {
                var totalRequestsNode = xmlDoc.SelectSingleNode("/Data/ssuser/requeststoday");
                if (totalRequestsNode != null && int.TryParse(totalRequestsNode.InnerText, out int total))
                {
                    result.ScrapeLimitProgress = total;
                }

                var allowedRequestsNode = xmlDoc.SelectSingleNode("/Data/ssuser/maxrequestsperday");
                if (allowedRequestsNode != null && int.TryParse(allowedRequestsNode.InnerText, out int max))
                {
                    result.ScrapeLimitMax = max;
                }
            }

            var mediasNode = xmlDoc.SelectSingleNode("/Data/jeu/medias");

            if (mediasNode == null)
            {
                result.GameData.WasSuccessful = false;
                result.GameData.Messages.Add($"No media data found for {romName}");
                return result;
            }

            // Parse elements
            ParseElements(xmlDoc, mediasNode, parameters, result.GameData);

            result.GameData.WasSuccessful = true;
            return result;
        }

        private static string ParseFamily(XmlNode familyNode, string language)
        {
            if (familyNode == null)
                return string.Empty;

            // Try user's language with principale=1
            string family = familyNode
                .SelectNodes("famille")?
                .Cast<XmlNode>()
                .Where(f => f.Attributes?["principale"]?.Value == "1" && f.Attributes?["langue"]?.Value == language)
                .Select(f => f.InnerText)
                .FirstOrDefault() ?? string.Empty;

            // Fallback to English with principale=1
            if (string.IsNullOrEmpty(family))
            {
                family = familyNode
                    .SelectNodes("famille")?
                    .Cast<XmlNode>()
                    .Where(f => f.Attributes?["principale"]?.Value == "1" && f.Attributes?["langue"]?.Value == "en")
                    .Select(f => f.InnerText)
                    .FirstOrDefault() ?? string.Empty;
            }

            // Fallback to any famille
            if (string.IsNullOrEmpty(family))
            {
                family = familyNode
                    .SelectNodes("famille")?
                    .Cast<XmlNode>()
                    .Select(f => f.InnerText)
                    .FirstOrDefault() ?? string.Empty;
            }

            return family;
        }

        private static (string Url, string Format, string Region) ParseMedia(string mediaType, XmlNode xmlMedias, List<string> regionsList)
        {
            if (xmlMedias == null)
                return (string.Empty, string.Empty, string.Empty);

            foreach (string currentRegion in regionsList)
            {
                XmlNode? media = !string.IsNullOrEmpty(currentRegion)
                    ? xmlMedias.SelectSingleNode($"//media[@type='{mediaType}' and @region='{currentRegion}']")
                    : xmlMedias.SelectSingleNode($"//media[@type='{mediaType}']");

                if (media != null)
                {
                    string url = media.InnerText;
                    string format = media.Attributes?["format"]?.Value ?? string.Empty;
                    return (url, format, currentRegion);
                }
            }

            return (string.Empty, string.Empty, string.Empty);
        }

        private static string ParseReleaseDate(XmlNode xmlNode, List<string> regionsList)
        {
            if (xmlNode == null || regionsList == null || regionsList.Count == 0)
                return string.Empty;

            foreach (string region in regionsList)
            {
                XmlNode? dateNode = xmlNode.SelectSingleNode($"date[@region='{region}']");
                if (dateNode != null)
                {
                    string text = dateNode.InnerText?.Trim() ?? string.Empty;
                    if (!string.IsNullOrEmpty(text))
                        return text;
                }
            }

            return string.Empty;
        }

        private static string ParseNames(XmlNode namesElement, string romFileName, string userLanguage, List<string> userRegions)
        {
            if (namesElement == null)
                return string.Empty;

            string romLanguage = RegionLanguageHelper.GetLanguage(romFileName);
            string romRegion = RegionLanguageHelper.GetRegion(romFileName);

            // Determine romlang
            string romlang;
            if (romLanguage.Contains(userLanguage))
            {
                romlang = userLanguage;
            }
            else if (romLanguage.Split(',').Length == 1)
            {
                romlang = romLanguage;
            }
            else
            {
                romlang = romRegion;
            }

            // Build search order: start with detected language/region, then user's preferred regions
            List<string> searchOrder = new List<string>();

            // Special handling for Japanese region when user language is not Japanese
            if (romRegion == "jp" && userLanguage != "jp")
            {
                searchOrder.Add(romRegion);
            }
            else
            {
                searchOrder.Add(romlang);
                searchOrder.Add(romRegion);
            }

            // Add user's preferred regions
            if (userRegions != null && userRegions.Count > 0)
            {
                searchOrder.AddRange(userRegions);
            }

            // Remove duplicates while preserving order
            var uniqueOrder = searchOrder.Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();

            // Try each region in order
            foreach (string searchRegion in uniqueOrder)
            {
                XmlNode? xmlNode = namesElement.SelectSingleNode($"nom[@region='{searchRegion}']");
                if (xmlNode != null)
                {
                    string name = xmlNode.InnerText ?? string.Empty;
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }

            return string.Empty;
        }

        private static string ConvertRating(string? rating)
        {
            if (string.IsNullOrEmpty(rating) || !int.TryParse(rating, out int ratingValue) || ratingValue == 0)
                return string.Empty;

            // Convert 0-20 rating to 0.0-1.0 format
            float ratingValFloat = ratingValue / 20.0f;
            return ratingValFloat.ToString();
        }

        private static (string GenreId, string GenreName) ParseGenres(XmlNode parentNode, string language)
        {
            if (parentNode == null)
                return (string.Empty, string.Empty);

            var genreNodes = parentNode.SelectNodes("genre");
            if (genreNodes == null)
                return (string.Empty, string.Empty);

            string genre = string.Empty;
            string subgenre = string.Empty;
            string genreId = string.Empty;

            // First pass: Get genres in user's language
            foreach (XmlNode node in genreNodes)
            {
                string nodeLang = node.Attributes?["langue"]?.Value ?? string.Empty;
                string principale = node.Attributes?["principale"]?.Value ?? string.Empty;

                if (nodeLang == language)
                {
                    if (principale == "1")
                    {
                        genre = node.InnerText;
                        genreId = node.Attributes?["id"]?.Value ?? string.Empty;
                    }
                    else if (principale == "0")
                    {
                        subgenre = node.InnerText;
                    }
                }
            }

            // Second pass: Fallback to English if needed
            if (language != "en")
            {
                foreach (XmlNode node in genreNodes)
                {
                    string nodeLang = node.Attributes?["langue"]?.Value ?? string.Empty;
                    string principale = node.Attributes?["principale"]?.Value ?? string.Empty;

                    if (nodeLang == "en")
                    {
                        if (string.IsNullOrEmpty(genre) && principale == "1")
                        {
                            genre = node.InnerText;
                            genreId = node.Attributes?["id"]?.Value ?? string.Empty;
                        }

                        if (string.IsNullOrEmpty(subgenre) && principale == "0")
                        {
                            subgenre = node.InnerText;
                        }
                    }
                }
            }

            // Post-processing
            int sep = genre.IndexOf('/');
            if (sep != -1)
                genre = genre.Substring(0, sep).Trim();

            sep = subgenre.IndexOf('/');
            if (string.IsNullOrEmpty(genre) || sep != -1)
                genre = subgenre;
            else if (!string.IsNullOrEmpty(genre) && !string.IsNullOrEmpty(subgenre))
                genre = genre + " / " + subgenre;

            return (genreId, genre);
        }
    }
}