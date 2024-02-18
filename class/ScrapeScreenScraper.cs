using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace GamelistManager
{
    public class ScrapeScreenScraper
    {
        public async Task<Dictionary<string, string>> ScrapeScreenScraperAsync(
                string userId,
                string userPassword,
                string devId,
                string devPassword,
                string region,
                string language,
                string romName,
                string systemID,
                string folderPath,
                bool overwrite,
                List<string> elementsToScrape,
                string boxSource,
                string imageSource,
                string logoSource
            )

        {
            // Build a dictionary from the elements we are going to scrape
            Dictionary<string, string> scraperData = new Dictionary<string, string>();
            foreach (var propertyName in elementsToScrape)
            {
                scraperData.Add(propertyName, null);
            }

            string romNameNoExtension = Path.GetFileNameWithoutExtension(romName);
            string scraperBaseURL = "https://www.screenscraper.fr/api2/";
            string gameinfo = $"jeuInfos.php?devid={devId}&devpassword={devPassword}&softname=GamelistManager&output=xml&ssid={userId}&sspassword={userPassword}&systemeid={systemID}&romtype=rom&romnom=";

            // Build MD5
            string fullRomPath = $"{folderPath}\\{romName}";
            string md5 = ChecksumCreator.CreateMD5(fullRomPath);
            if (!string.IsNullOrEmpty(md5)) {
                scraperData.Add("md5", null);
            }

            // Get the XML response from the website
            string scraperRequestURL = $"{scraperBaseURL}{gameinfo}{romNameNoExtension}";
            XMLResponder xmlResponder = new XMLResponder();
            XmlNode xmlResponse = await xmlResponder.GetXMLResponseAsync(scraperRequestURL);

            if (xmlResponse == null)
            {
                return null;
            }

            string value = null;
            string folderName = null;
            string localType = null;
            string remoteType = null;
            string remoteDownloadURL = null;
            string filenameToDownload = null;
            string downloadPath = null;
            string fileFormat = null;
            string firstRomRegion = null;
            bool downloadSuccess = false;

            // Media node, we only need to select it once
            XmlNode mediasNode = xmlResponse.SelectSingleNode("/Data/jeu/medias");

            foreach (string element in elementsToScrape)
            {
                switch (element)
                {
                    case "publisher":
                        value = xmlResponse.SelectSingleNode("/Data/jeu/editeur")?.InnerText;
                        scraperData["publisher"] = value;
                        break;

                    case "developer":
                        value = xmlResponse.SelectSingleNode("/Data/jeu/developpeur")?.InnerText;
                        scraperData["developer"] = value;
                        break;

                    case "players":
                        value = xmlResponse.SelectSingleNode("/Data/jeu/joueurs")?.InnerText;
                        scraperData["players"] = value;
                        break;

                    case "lang":
                        value = xmlResponse.SelectSingleNode("/Data/jeu/rom/romlangues")?.InnerText;
                        if (string.IsNullOrEmpty(value))
                        {
                            value = "en";
                        }
                        scraperData["lang"] = value;
                        break;

                    case "rating":
                        value = xmlResponse.SelectSingleNode("/Data/jeu/note")?.InnerText;
                        string rating = ProcessRating(value);
                        scraperData["rating"] = rating;
                        break;

                    case "desc":
                        value = xmlResponse.SelectSingleNode($"/Data/jeu/synopsis/synopsis[@langue='{language}']")?.InnerText;
                        if (string.IsNullOrEmpty(value))
                        {
                            // Fallback to english
                            value = xmlResponse.SelectSingleNode($"/Data/jeu/synopsis/synopsis[@langue='en']")?.InnerText;
                        }
                        scraperData["desc"] = value;
                        break;

                    case "name":
                        XmlNode namesNode = xmlResponse.SelectSingleNode("/Data/jeu/noms");
                        value = ParseNames(namesNode, region);
                        scraperData["name"] = value;
                        break;

                    case "genre":
                        XmlNode genresNode = xmlResponse.SelectSingleNode("/Data/jeu/genres");
                        value = ParseGenres(genresNode, language);
                        if (!string.IsNullOrEmpty(value))
                        {
                            value = ParseGenres(genresNode, "en");
                        }
                        scraperData["genre"] = value;
                        break;

                    case "region":
                        string romRegions = xmlResponse.SelectSingleNode("/Data/jeu/rom/romregions")?.InnerText;
                        firstRomRegion = null;
                        if (romRegions != null)
                        {
                            firstRomRegion = romRegions.Split(',')[0].Trim();
                        }
                        scraperData["region"] = firstRomRegion;
                        break;

                    case "releasedate":
                        value = xmlResponse.SelectSingleNode($"/Data/jeu/dates/date[@region='{firstRomRegion}']")?.InnerText;
                        string releasedate = ConvertToISO8601(value);
                        scraperData["releasedate"] = releasedate;
                        break;

                    case "bezel":
                        folderName = "images";
                        localType = "bezel";
                        remoteType = "bezel-16-9";

                        (remoteDownloadURL, fileFormat) = ParseMedia(remoteType, mediasNode, region);
                        if (remoteDownloadURL != null)
                        {
                            filenameToDownload = $"{romNameNoExtension}-{localType}.{fileFormat}";
                            downloadPath = $"{folderPath}\\{folderName}\\{filenameToDownload}";
                            downloadSuccess = await FileTransfer.DownloadFile(overwrite, downloadPath, remoteDownloadURL);
                            if (downloadSuccess == true)
                            {
                                scraperData[localType] = $"./{folderName}/{filenameToDownload}";
                            }
                        }
                        break;

                    case "manual":
                        folderName = "manuals";
                        localType = "manual";
                        remoteType = "manuel";

                        (remoteDownloadURL, fileFormat) = ParseMedia(remoteType, mediasNode, region);
                        if (remoteDownloadURL != null)
                        {
                            filenameToDownload = $"{romNameNoExtension}-{localType}.{fileFormat}";
                            downloadPath = $"{folderPath}\\{folderName}\\{filenameToDownload}";
                            downloadSuccess = await FileTransfer.DownloadFile(overwrite, downloadPath, remoteDownloadURL);
                            if (downloadSuccess == true)
                            {
                                scraperData[localType] = $"./{folderName}/{filenameToDownload}";
                            }
                        }
                        break;

                    case "image":
                        // ss = screenshot
                        remoteType = "ss";
                        if (imageSource.ToLower() == "screenshot title")
                        {
                            remoteType = "sstitle";
                        }
                        folderName = "images";
                        localType = "image";
                        (remoteDownloadURL, fileFormat) = ParseMedia(remoteType, mediasNode, region);
                        if (remoteDownloadURL != null)
                        {
                            filenameToDownload = $"{romNameNoExtension}-{localType}.{fileFormat}";
                            downloadPath = $"{folderPath}\\{folderName}\\{filenameToDownload}";
                            downloadSuccess = await FileTransfer.DownloadFile(overwrite, downloadPath, remoteDownloadURL);
                            if (downloadSuccess == true)
                            {
                                scraperData[localType] = $"./{folderName}/{filenameToDownload}";
                            }
                        }
                        break;

                    case "thumbnail":
                        remoteType = "box-2D";
                        if (boxSource.ToLower() == "box 3d")
                        {
                            remoteType = "box-3D";
                        }
                        folderName = "images";
                        localType = "thumbnail";

                        (remoteDownloadURL, fileFormat) = ParseMedia(remoteType, mediasNode, region);
                        if (remoteDownloadURL != null)
                        {
                            filenameToDownload = $"{romNameNoExtension}-{localType}.{fileFormat}";
                            downloadPath = $"{folderPath}\\{folderName}\\{filenameToDownload}";
                            downloadSuccess = await FileTransfer.DownloadFile(overwrite, downloadPath, remoteDownloadURL);
                            if (downloadSuccess == true)
                            {
                                scraperData[localType] = $"./{folderName}/{filenameToDownload}";
                            }
                        }
                        break;

                    case "marquee":
                        remoteType = "wheel";
                        if (logoSource.ToLower() == "marquee")
                        {
                            remoteType = "screenmarquee";
                        }
                        folderName = "images";
                        localType = "marquee";

                        (remoteDownloadURL, fileFormat) = ParseMedia(remoteType, mediasNode, region);
                        if (remoteDownloadURL != null)
                        {
                            filenameToDownload = $"{romNameNoExtension}-{localType}.{fileFormat}";
                            downloadPath = $"{folderPath}\\{folderName}\\{filenameToDownload}";
                            downloadSuccess = await FileTransfer.DownloadFile(overwrite, downloadPath, remoteDownloadURL);
                            if (downloadSuccess == true)
                            {
                                scraperData[localType] = $"./{folderName}/{filenameToDownload}";
                            }
                        }
                        break;


                    case "video":
                        folderName = "videos";
                        localType = "video";
                        (remoteDownloadURL, fileFormat) = ParseVideo(mediasNode);
                        if (remoteDownloadURL != null)
                        {
                            filenameToDownload = $"{romNameNoExtension}-{localType}.{fileFormat}";
                            downloadPath = $"{folderPath}\\{folderName}\\{filenameToDownload}";
                            downloadSuccess = await FileTransfer.DownloadFile(overwrite, downloadPath, remoteDownloadURL);
                            if (downloadSuccess == true)
                            {
                                scraperData[localType] = $"./{folderName}/{filenameToDownload}";
                            }
                        }
                        break;
                }
            }
            return scraperData;
        }


        private (string Url, string Format) ParseVideo(XmlNode XmlElement)
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


        private (string Url, string Format) ParseMedia(string mediaType, XmlNode xmlMedias, string region)
        {
            if (xmlMedias == null) { return (null, null); }

            // User selected region and backups
            string[] regions = { region, "eu", "us", "ss", "uk", "wor" };

            var media = (XmlNode)null;

            foreach (string currentRegion in regions)
            {
                // Find first matching region
                media = xmlMedias.SelectSingleNode($"//media[@type='{mediaType}' and @region='{currentRegion}']");
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

        private string ParseNames(XmlNode namesElement, string region)
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

        private string ProcessRating(string rating)
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

        private string ParseGenres(XmlNode genresElement, string language)
        {
            if (genresElement == null)
            {
                return null;
            }
            var genreElements = genresElement.SelectNodes("genre")
                .Cast<XmlNode>()
                .Where(e => e.Attributes?["langue"]?.Value == language)
                .ToList();

            if (genreElements.Any())
            {
                // Find the last element with " / " and principale = 0
                var primaryGenreWithSlash = genreElements
                    .LastOrDefault(e => e.InnerText.Contains(" / ") && e.Attributes?["principale"]?.Value == "0");

                if (primaryGenreWithSlash != null)
                {
                    return primaryGenreWithSlash.InnerText;
                }

                // If no element has " / ", take the last element with principale = 0
                var lastGenre = genreElements.LastOrDefault(e => e.Attributes?["principale"]?.Value == "1");

                if (lastGenre != null)
                {
                    return lastGenre.InnerText;
                }
            }

            return null;
        }

        private string ConvertToISO8601(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
            {
                return null;
            }
            try
            {
                DateTime date;
                string format = dateString.Contains("-") ? "yyyy-MM-dd" : "yyyy";

                if (DateTime.TryParseExact(dateString, format, null, System.Globalization.DateTimeStyles.None, out date))
                {
                    return date.ToString("yyyy-MM-ddTHH:mm:ss");
                }
                else
                {
                    return string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}