using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

// this is not done yet!

namespace GamelistManager
{
    public class ScrapeScreenScraper
    {
        public async Task<Dictionary<string, string>> ScrapeScreenScraperAsync(string userId, string userPassword, string devId, string devPassword, string region, string language, string romName, string systemID, string folderPath, List<string> elementsToScrape)
        {
            // Build an empty dictionary to start
            Dictionary<string, string> scraperData = new Dictionary<string, string>();
            foreach (var propertyName in elementsToScrape)
            {
                scraperData[propertyName] = null;
            }

            string scraperBaseURL = "https://www.screenscraper.fr/api2/";
            string gameinfo = $"jeuInfos.php?devid={devId}&devpassword={devPassword}&softname=GamelistManager&output=xml&ssid={userId}&sspassword={userPassword}&systemeid={systemID}&romtype=rom&romnom=";

            string fullRomPath = $"{folderPath}\\{romName}";
            string md5 = ChecksumCreator.CreateMD5(fullRomPath);
            scraperData["md5"] = md5;

            string scraperRequestURL = $"{scraperBaseURL}{(gameinfo)}{romName}";

            // Get the XML response from the website
            XmlDocument xmlResponse = await GetXMLResponseAsync(scraperRequestURL);

            if (xmlResponse == null)
            {
                return null;
            }

            // Regions are so messy to deal with
            string romRegions = xmlResponse.SelectSingleNode("/Data/jeu/rom/romregions")?.InnerText;
            string regionToScrape = ParseRegions(region, romRegions);

            string value = null;
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

                    case "region":
                        scraperData["region"] = regionToScrape;
                        break;


                    case "lang":
                        value = xmlResponse.SelectSingleNode("/Data/jeu/rom/romlangues")?.InnerText;
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
                            value = xmlResponse.SelectSingleNode($"/Data/jeu/synopsis/synopsis[@langue='en']")?.InnerText;
                        }
                        scraperData["desc"] = value;
                        break;

                    case "name":
                        XmlNode namesNode = xmlResponse.SelectSingleNode("/Data/jeu/noms");
                        value = ParseNames(namesNode, regionToScrape);
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

                    case "releasedate":
                        value = xmlResponse.SelectSingleNode($"/Data/jeu/dates/date[@region='{regionToScrape}']")?.InnerText;
                        string releasedate = ConvertToISO8601(value);
                        scraperData["releasedate"] = releasedate;
                        break;
                }
            }
            return scraperData;
        }

        private (string Url, string Format) ParseMedia(string mediaType, XmlNode xmlMedias, string region)
        {
            if (xmlMedias == null) { return (null, null); }

            var media = xmlMedias.SelectSingleNode($"//media[@type='{mediaType}' and @region='{region}']");

            if (media == null)
            {
                return (null, null);
            }

            string url = media.InnerText;
            string format = media.Attributes["format"]?.Value;

            return (url, format);
        }

        private string ParseRegions(string region, string romRegions)
        {
            string regionToScrape = null;
            if (!string.IsNullOrEmpty(romRegions))
            {
                string[] regions = romRegions.Split(',');
                if (!regions.Contains(region))
                {
                    region = "wor";
                    if (!regions.Contains(region))
                    {
                        // take the last one, usually the right choice?
                        regionToScrape = regions[(regions.Length - 1)];
                    }
                }
            }
            else
            {
                // I don't know.....
                regionToScrape = "us";
            }
            return regionToScrape;
        }


        private string ParseNames(XmlNode namesElement, string region)
        {
            if (namesElement == null) { return null; }

            var names = namesElement?.SelectNodes($"nom[@region='{region}']");

            if (names.Count != 1)
            {
                return null;
            }
            return names[0].InnerText;
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

        private async Task<XmlDocument> GetXMLResponseAsync(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] responseBytes = await client.GetByteArrayAsync(url);
                    string responseString = Encoding.UTF8.GetString(responseBytes);

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(responseString);

                    return xmlDoc;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string ParseGenres(XmlNode genresElement, string language)
        {
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

            return string.Empty;
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