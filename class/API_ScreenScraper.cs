using System.Net.Http;
using System.Text.Json;
using System.Text;
using System;
using System.Threading.Tasks;
using System.Xml;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GamelistManager
{
    internal static class API_ScreenScraper
    {
        private static readonly string apiURL = "https://api.screenscraper.fr/api2";
        private static readonly string devId = "";
        private static readonly string devPassword = "";
        private static readonly string software = "GamelistManager";
        private static readonly HttpClient client = new HttpClient();

        public static async Task<int> GetMaxScrap(string userID, string userPassword)
        {
            XmlNode xmlData = await AuthenticateAsync(userID, userPassword);
            if (xmlData != null)
            {
                XmlNode maxThreadsNode = xmlData.SelectSingleNode("//ssuser/maxthreads");
                if (maxThreadsNode != null && int.TryParse(maxThreadsNode.InnerText, out int max))
                {
                    return max;
                }
            }
            return 1;
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

        public static async Task<XmlNode> AuthenticateAsync(string username, string password)
        {
            string url = $"{apiURL}/ssuserInfos.php?devid={devId}&devpassword={devPassword}&softname={software}&output=xml&ssid={username}&sspassword={password}";
            XMLResponder responder = new XMLResponder();
            XmlNode xmlResponse = await responder.GetXMLResponseAsync(url);
            return xmlResponse;
        }

     
        public static async Task<ScraperData> ScrapeScreenScraperAsync(ScreenScrapeParameters screenScrapeParameters,ListBox ListBoxControl)
        {

            string scrapeName = screenScrapeParameters.Name;
            string scrapInfo = (!string.IsNullOrEmpty(screenScrapeParameters.GameID)) ? $"&gameid={screenScrapeParameters.GameID}" : $"&romtype=rom&romnom={scrapeName}";
            string url = $"{apiURL}/jeuInfos.php?devid={devId}&devpassword={devPassword}&softname=GamelistManager&output=xml&ssid={screenScrapeParameters.UserID}&sspassword={screenScrapeParameters.UserPassword}&systemeid={screenScrapeParameters.SystemID}{scrapInfo}";

            XMLResponder xmlResponder = new XMLResponder();
            XmlNode xmlResponse = await xmlResponder.GetXMLResponseAsync(url);

            if (xmlResponse == null)
            {
                return null;
            }
            
            string remoteDownloadURL = null;
            string fileFormat = null;
            string fileName = null;
            string fileToDownload = null;
            bool downloadResult = false;
            string folderName = null;
            string remoteElementName = null;
            string value = null;
            
            List<string> elementsToScrape = screenScrapeParameters.ElementsToScrape;

            ScraperData scraperData = new ScraperData();
            // medias xmlnode contains media download URL     
            XmlNode mediasNode = xmlResponse.SelectSingleNode("/Data/jeu/medias");

            foreach (string element in elementsToScrape)
            {
                switch (element)
                {
                    case "publisher":
                        value = xmlResponse.SelectSingleNode("/Data/jeu/editeur")?.InnerText;
                        scraperData.publisher = value;
                        break;

                    case "developer":
                        value = xmlResponse.SelectSingleNode("/Data/jeu/developpeur")?.InnerText;
                        scraperData.developer = value;
                        break;

                    case "players":
                        value = xmlResponse.SelectSingleNode("/Data/jeu/joueurs")?.InnerText;
                        scraperData.players = value;
                        break;

                    case "lang":
                        value = xmlResponse.SelectSingleNode("/Data/jeu/rom/romlangues")?.InnerText;
                        if (string.IsNullOrEmpty(value))
                        {
                            value = "en";
                        }
                        scraperData.lang = value;
                        break;

                    case "id":
                        XmlElement game = xmlResponse.SelectSingleNode("/Data/jeu") as XmlElement;
                        string idValue = game.GetAttribute("id");
                        if (!string.IsNullOrEmpty(idValue))
                        {
                            scraperData.id = idValue;
                        }
                        break;

                    case "arcadesystemname":
                        value = xmlResponse.SelectSingleNode("/Data/jeu/systeme")?.InnerText;
                        scraperData.arcadesystemname = value;
                        break;

                    case "rating":
                        value = xmlResponse.SelectSingleNode("/Data/jeu/note")?.InnerText;
                        string rating = ProcessRating(value);
                        scraperData.rating = rating;
                        break;

                    case "desc":
                        value = xmlResponse.SelectSingleNode($"/Data/jeu/synopsis/synopsis[@langue='{screenScrapeParameters.Language}']")?.InnerText;
                        if (string.IsNullOrEmpty(value))
                        {
                            // Fallback to english
                            value = xmlResponse.SelectSingleNode($"/Data/jeu/synopsis/synopsis[@langue='en']")?.InnerText;
                        }
                        scraperData.desc = value;
                        break;

                    case "name":
                        XmlNode namesNode = xmlResponse.SelectSingleNode("/Data/jeu/noms");
                        value = ParseNames(namesNode, screenScrapeParameters.Region);
                        scraperData.name = value;
                        break;

                    case "genre":
                        XmlNode genresNode = xmlResponse.SelectSingleNode("/Data/jeu/genres");
                        (string genreID, string genreName) = ParseGenres(genresNode, screenScrapeParameters.Language);
                        scraperData.genre = genreName;
                        scraperData.genreid = genreID;
                        break;

                    case "region":
                        string romRegions = xmlResponse.SelectSingleNode("/Data/jeu/rom/romregions")?.InnerText;
                        scraperData.region = romRegions;
                        break;

                    case "releasedate":
                        XmlNode releaseDateNode = xmlResponse.SelectSingleNode("/Data/jeu/dates");
                        value = ParseReleaseDate(releaseDateNode);
                        string releasedate = ISO8601Converter.ConvertToISO8601(value);
                        scraperData.releasedate = releasedate;
                        break;

                    case "bezel":
                        folderName = "images";
                        remoteElementName = "bezel-16-9";
                        (remoteDownloadURL, fileFormat) = ParseMedia(remoteElementName, mediasNode, screenScrapeParameters.Region);
                                                
                        if (remoteDownloadURL != null)
                        {
                            if (!Directory.Exists($"{screenScrapeParameters.ParentFolderPath}\\{folderName}"))
                            {
                                Directory.CreateDirectory($"{screenScrapeParameters.ParentFolderPath}\\{folderName}");
                            }
                            fileName = $"{screenScrapeParameters.RomFileNameWithoutExtension}-{element}.{fileFormat}";
                            fileToDownload = $"{screenScrapeParameters.ParentFolderPath}\\{folderName}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(screenScrapeParameters.Overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult == true)
                            {
                                scraperData.bezel = $"./{folderName}/{fileName}";
                                ShowDownload(ListBoxControl, $"Downloaded: {fileName}");
                            }
                        }
                        break;

                    case "fanart":
                        folderName = "images";
                        remoteElementName = "fanart";

                        (remoteDownloadURL, fileFormat) = ParseMedia(remoteElementName, mediasNode, screenScrapeParameters.Region);
                        if (remoteDownloadURL != null)
                        {
                            if (!Directory.Exists($"{screenScrapeParameters.ParentFolderPath}\\{folderName}"))
                            {
                                Directory.CreateDirectory($"{screenScrapeParameters.ParentFolderPath}\\{folderName}");
                            }
                            fileName = $"{screenScrapeParameters.RomFileNameWithoutExtension}-{element}.{fileFormat}";
                            fileToDownload = $"{screenScrapeParameters.ParentFolderPath}\\{folderName}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(screenScrapeParameters.Overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult == true)
                            {
                                scraperData.fanart = $"./{folderName}/{fileName}";
                                ShowDownload(ListBoxControl, $"Downloaded: {fileName}");
                            }
                        }
                        break;

                    case "boxback":
                        folderName = "images";
                        remoteElementName = "box-2D-back";

                        (remoteDownloadURL, fileFormat) = ParseMedia(remoteElementName, mediasNode, screenScrapeParameters.Region);
                        if (remoteDownloadURL != null)
                        {
                            if (!Directory.Exists($"{screenScrapeParameters.ParentFolderPath}\\{folderName}"))
                            {
                                Directory.CreateDirectory($"{screenScrapeParameters.ParentFolderPath}\\{folderName}");
                            }
                            fileName = $"{screenScrapeParameters.RomFileNameWithoutExtension}-{element}.{fileFormat}";
                            fileToDownload = $"{screenScrapeParameters.ParentFolderPath}\\{folderName}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(screenScrapeParameters.Overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult == true)
                            {
                                scraperData.boxback = $"./{folderName}/{fileName}";
                                ShowDownload(ListBoxControl, $"Downloaded: {fileName}");
                            }
                        }
                        break;

                    case "manual":
                        folderName = "manuals";
                        remoteElementName = "manuel";

                        (remoteDownloadURL, fileFormat) = ParseMedia(remoteElementName, mediasNode, screenScrapeParameters.Region);
                        if (remoteDownloadURL != null)
                        {
                            if (!Directory.Exists($"{screenScrapeParameters.ParentFolderPath}\\{folderName}"))
                            {
                                Directory.CreateDirectory($"{screenScrapeParameters.ParentFolderPath}\\{folderName}");
                            }
                            fileName = $"{screenScrapeParameters.RomFileNameWithoutExtension}-{element}.{fileFormat}";
                            fileToDownload = $"{screenScrapeParameters.ParentFolderPath}\\{folderName}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(screenScrapeParameters.Overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult == true)
                            {
                                scraperData.manual = $"./{folderName}/{fileName}";
                                ShowDownload(ListBoxControl, $"Downloaded: {fileName}");
                            }
                        }
                        break;

                    case "image":
                        // ss = screenshot
                        remoteElementName = "ss";
                        if (screenScrapeParameters.ImageSource.ToLower() == "screenshot title")
                        {
                            remoteElementName = "sstitle";
                        }
                        folderName = "images";
                        (remoteDownloadURL, fileFormat) = ParseMedia(remoteElementName, mediasNode, screenScrapeParameters.Region);
                        if (remoteDownloadURL != null)
                        {
                            if (!Directory.Exists($"{screenScrapeParameters.ParentFolderPath}\\{folderName}"))
                            {
                                Directory.CreateDirectory($"{screenScrapeParameters.ParentFolderPath}\\{folderName}");
                            }
                            fileName = $"{screenScrapeParameters.RomFileNameWithoutExtension}-{element}.{fileFormat}";
                            fileToDownload = $"{screenScrapeParameters.ParentFolderPath}\\{folderName}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(screenScrapeParameters.Overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult == true)
                            {
                                scraperData.image = $"./{folderName}/{fileName}";
                                ShowDownload(ListBoxControl, $"Downloaded: {fileName}");
                            }
                        }
                        break;

                    case "thumbnail":
                        remoteElementName = "box-2D";
                        if (screenScrapeParameters.BoxSource.ToLower() == "box 3d")
                        {
                            remoteElementName = "box-3D";
                        }
                        folderName = "images";
                        (remoteDownloadURL, fileFormat) = ParseMedia(remoteElementName, mediasNode, screenScrapeParameters.Region);
                        if (remoteDownloadURL != null)
                        {
                            if (!Directory.Exists($"{screenScrapeParameters.ParentFolderPath}\\{folderName}"))
                            {
                                Directory.CreateDirectory($"{screenScrapeParameters.ParentFolderPath}\\{folderName}");
                            }
                            fileName = $"{screenScrapeParameters.RomFileNameWithoutExtension}-{element}.{fileFormat}";
                            fileToDownload = $"{screenScrapeParameters.ParentFolderPath}\\{folderName}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(screenScrapeParameters.Overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult == true)
                            {
                                scraperData.thumbnail = $"./{folderName}/{fileName}";
                                ShowDownload(ListBoxControl, $"Downloaded: {fileName}");
                            }
                        }
                        break;

                    case "marquee":
                        remoteElementName = "wheel";
                        if (screenScrapeParameters.LogoSource.ToLower() == "marquee")
                        {
                            remoteElementName = "screenmarquee";
                        }
                        folderName = "images";

                        (remoteDownloadURL, fileFormat) = ParseMedia(remoteElementName, mediasNode, screenScrapeParameters.Region);

                        if (remoteDownloadURL != null)
                        {
                            if (!Directory.Exists($"{screenScrapeParameters.ParentFolderPath}\\{folderName}"))
                            {
                                Directory.CreateDirectory($"{screenScrapeParameters.ParentFolderPath}\\{folderName}");
                            }
                            fileName = $"{screenScrapeParameters.RomFileNameWithoutExtension}-{element}.{fileFormat}";
                            fileToDownload = $"{screenScrapeParameters.ParentFolderPath}\\{folderName}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(screenScrapeParameters.Overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult == true)
                            {
                                scraperData.marquee = $"./{folderName}/{fileName}";
                                ShowDownload(ListBoxControl, $"Downloaded: {fileName}");
                            }
                        }
                        break;


                    case "video":
                        folderName = "videos";

                        (remoteDownloadURL, fileFormat) = ParseVideo(mediasNode);
                        if (remoteDownloadURL != null)
                        {
                            if (!Directory.Exists($"{screenScrapeParameters.ParentFolderPath}\\{folderName}"))
                            {
                                Directory.CreateDirectory($"{screenScrapeParameters.ParentFolderPath}\\{folderName}");
                            }
                            fileName = $"{screenScrapeParameters.RomFileNameWithoutExtension}-{element}.{fileFormat}";
                            fileToDownload = $"{screenScrapeParameters.ParentFolderPath}\\{folderName}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(screenScrapeParameters.Overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult == true)
                            {
                                scraperData.video = $"./{folderName}/{fileName}";
                                ShowDownload(ListBoxControl, $"Downloaded: {fileName}");
                            }
                        }
                        break;
                }
            }

            return scraperData;

        }

        private static (string Url, string Format) ParseVideo(XmlNode XmlElement)
        {
            if (XmlElement == null) { return (null, null); }

            var media = XmlElement.SelectSingleNode($"//media[@type='video-normalized']");
            if (media == null)
            {
                media = XmlElement.SelectSingleNode($"//media[@type='video']");
            }

            if (media != null)
            {
                string url = media.InnerText;
                string format = media.Attributes["format"]?.Value;
                return (url, format);
            }

            return (null, null);
        }

        private static (string Url, string Format) ParseMedia(string mediaType, XmlNode xmlMedias, string region)
        {
            if (xmlMedias == null) { return (null, null); }

            // User selected region (first) followed by fallback regions
            string[] regions = { region, "us", "ss", "eu", "uk", "wor", "" };

            XmlNode media = null;

            foreach (string currentRegion in regions)
            {
                // Find first matching region
                if (!string.IsNullOrEmpty(currentRegion))
                {
                    media = xmlMedias.SelectSingleNode($"//media[@type='{mediaType}' and @region='{currentRegion}']");
                }
                else
                {
                    media = xmlMedias.SelectSingleNode($"//media[@type='{mediaType}']");
                }
                if (media != null)
                {
                    break;
                }
            }

            if (media == null)
            {
                return (null, null);
            }

            string url = media.InnerText;
            string format = media.Attributes["format"]?.Value;
            return (url, format);
        }

        private static string ParseReleaseDate(XmlNode namesElement)
        {
            if (namesElement == null) { return null; }

            string[] regions = { "us", "wor", "ss", "eu", "uk" };

            foreach (string currentRegion in regions)
            {
                XmlNode releaseDate = namesElement.SelectSingleNode($"date[@region='{currentRegion}']");
                if (releaseDate != null)
                {
                    return releaseDate.InnerText;
                }
            }

            // Return null if no matching "date" node is found
            return null;
        }

        private static string ParseNames(XmlNode namesElement, string region)
        {
            if (namesElement == null) { return null; }

            // User selected region and backups
            string[] regions = { region, "eu", "us", "ss", "uk", "wor" };

            var name = (XmlNode)null;

            foreach (string currentRegion in regions)
            {
                name = namesElement.SelectSingleNode($"nom[@region='{currentRegion}']");
                if (name != null)
                {
                   break;
                }
                
            }

            if (name == null)
            {
                return null;
            }
            return name.InnerText;
        }

        private static string ProcessRating(string rating)
        {
            if (rating == null) { return null; }

            int ratingvalue = int.Parse(rating);
            if (ratingvalue == 0)
            {
                return null;
            }
            float ratingValFloat = ratingvalue / 20.0f;
            return ratingValFloat.ToString();
        }

        private static (string GenreId, string GenreName) ParseGenres(XmlNode genresElement, string language)
        {
            if (genresElement == null)
            {
                return (null, null);
            }

            var genreElements = genresElement.SelectNodes("genre")
                .Cast<XmlNode>()
                .Where(e => e.Attributes?["langue"]?.Value == language)
                .ToList();

            // If the specified language is not found, try to find in "en" language
            if (!genreElements.Any() && language != "en")
            {
                genreElements = genresElement.SelectNodes("genre")
                    .Cast<XmlNode>()
                    .Where(e => e.Attributes?["langue"]?.Value == "en")
                    .ToList();
            }

            if (genreElements.Any())
            {
                // Find the last element with " / " and principale = 0
                var primaryGenreWithSlash = genreElements
                    .LastOrDefault(e => e.InnerText.Contains(" / ") && e.Attributes?["principale"]?.Value == "0");

                if (primaryGenreWithSlash != null)
                {
                    return (primaryGenreWithSlash.Attributes?["id"]?.Value, primaryGenreWithSlash.InnerText);
                }

                // If no element has " / ", take the last element with principale = 0
                var lastGenre = genreElements.LastOrDefault(e => e.Attributes?["principale"]?.Value == "1");

                if (lastGenre != null)
                {
                    return (lastGenre.Attributes?["id"]?.Value, lastGenre.InnerText);
                }
            }
            return (null, null);
        }

        public class ScreenScrapeParameters
        {
            public string RomFileNameWithExtension { get; set; }
            public string RomFileNameWithoutExtension { get; set; }
            public string Name { get; set; }
            public string GameID { get; set; }
            public string SystemID { get; set; }
            public string UserID { get; set; }
            public string UserPassword { get; set; }
            public string ParentFolderPath { get; set; }
            public string Language { get; set; }
            public string Region { get; set; }
            public string ImageSource { get; set; }
            public string BoxSource { get; set; }
            public string Marquee { get; set; }
            public string LogoSource { get; set; }
            public bool Overwrite { get; set; }
            public List<string> ElementsToScrape { get; set; }
        }

    }
}

       