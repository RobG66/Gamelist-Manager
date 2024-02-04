using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

// this is not done yet!

namespace GamelistManager
{
    public class ScrapeScreenScraper
    {
        ScraperForm scraperForm = new ScraperForm();
        GamelistManagerForm gamelistManagerForm = new GamelistManagerForm();

        public ScrapeScreenScraper(ScraperForm scraperForm)
        {
            this.scraperForm = scraperForm ?? throw new ArgumentNullException(nameof(scraperForm));
            this.gamelistManagerForm = gamelistManagerForm ?? throw new ArgumentNullException(nameof(gamelistManagerForm));
        }

        public async
        Task
        ScrapeScreenScraperAsync(string XMLFilename, DataSet dataSet, bool overWriteData, List<string> elementsToScrape, List<string> romPaths, CancellationToken cancellationToken)
        {
            int total = romPaths.Count;
            int count = 0;
            string preferredLanguage = "en";
            string preferredRegion = "wor";
            string backupLanguage = "en";
            string backupRegion = "us";

            string devId = "";
            string devPassword = "";

            string scraperBaseURL = "https://www.screenscraper.fr/api2/";

            string gameinfo = $"jeuInfos.php?devid={devId}&devpassword={devPassword}&softname=zzz&output=xml&ssid=username&sspassword=userpassword&md5=md5value&systemeid=9999&romtype=rom&romnom=romname";

            string parentFolderPath = Path.GetDirectoryName(XMLFilename);
            string folderName = Path.GetFileName(parentFolderPath);
            int systemID = GetSystemId(folderName);

            for (int i = 0; i < romPaths.Count; i++)
            {
                scraperForm.UpdateProgressBar();
                scraperForm.UpdateLabel(count, total);
                count++;

                if (cancellationToken.IsCancellationRequested)
                {
                    // Perform cleanup or handle cancellation if needed
                    scraperForm.AddToLog("Scraping canceled by user.");
                    return;
                }

                string currentRomPath = romPaths[i];
                string currentRomName = gamelistManagerForm.ExtractFileNameWithExtension(currentRomPath);
                string scraperRequestURL = $"{scraperBaseURL}{(gameinfo)}";
                string fullRomPath = Path.Combine(parentFolderPath, currentRomPath.Replace("./", "").Replace("/", Path.DirectorySeparatorChar.ToString()));

                if (!File.Exists(fullRomPath))
                {
                    scraperForm.AddToLog($"File {fullRomPath} is missing!");
                    continue;
                }

                // Find the datarow we want to scrape data for
                DataRow tableRow = dataSet.Tables[0].AsEnumerable()
                 .FirstOrDefault(row => row.Field<string>("path") == currentRomPath);

                // Generate MD5 if it doesn't exist
                string md5 = GetCellValueAsString(tableRow, "md5");
                if (md5 != null)
                {
                    md5 = ChecksumCreator.CreateMD5(fullRomPath);
                    tableRow["md5"] = md5;
                }

                scraperRequestURL = scraperRequestURL.Replace("9999", systemID.ToString());
                scraperRequestURL = scraperRequestURL.Replace("romname", currentRomName);
                scraperRequestURL = scraperRequestURL.Replace("md5value", md5);
                scraperRequestURL = scraperRequestURL.Replace("username", "robg77");
                scraperRequestURL = scraperRequestURL.Replace("userpassword", "Zippy111");

                // Get the XML response from the website
                XmlDocument xmlResponse = await GetXMLResponseAsync(scraperRequestURL, cancellationToken);

                if (xmlResponse == null)
                {
                    scraperForm.AddToLog($"Error scraping item {currentRomName}");
                    continue;
                }

                scraperForm.AddToLog($"Scraping item {currentRomName}");

                string regionToScrape = preferredRegion;

                // Get the region for the rom
                // Very inconsistent data to work with
                string romRegions = xmlResponse.SelectSingleNode("/Data/jeu/rom/romregions")?.InnerText;
                if (!string.IsNullOrEmpty(romRegions))
                {
                    string[] regions = romRegions.Split(',');
                    
                    int regionCount = regions.Length;
                    if (!regions.Contains(preferredRegion))
                    {
                        regionToScrape = backupRegion;
                        if (!regions.Contains(backupRegion))
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

                string value = null;

                foreach (string element in elementsToScrape)
                {
                    switch (element)
                    {
                        case "publisher":
                            value = xmlResponse.SelectSingleNode("/Data/jeu/editeur")?.InnerText;
                            if (string.IsNullOrEmpty(tableRow["publisher"]?.ToString()) || overWriteData)
                            {
                                tableRow["publisher"] = value;
                            }
                            break;

                        case "developer":
                            value = xmlResponse.SelectSingleNode("/Data/jeu/developpeur")?.InnerText;
                            if (string.IsNullOrEmpty(tableRow["developer"]?.ToString()) || overWriteData)
                            {
                                tableRow["developer"] = value;
                            }
                            break;

                        case "players":
                            value = xmlResponse.SelectSingleNode("/Data/jeu/joueurs")?.InnerText;
                            if (string.IsNullOrEmpty(tableRow["players"]?.ToString()) || overWriteData)
                            {
                                tableRow["players"] = value;
                            }
                            break;

                        case "region":
                            if (string.IsNullOrEmpty(tableRow["region"]?.ToString()) || overWriteData)
                            {
                                tableRow["region"] = regionToScrape;
                            }
                            break;


                        case "lang":
                            value = xmlResponse.SelectSingleNode("/Data/jeu/rom/romlangues")?.InnerText;
                            if (string.IsNullOrEmpty(tableRow["lang"]?.ToString()) || overWriteData)
                            {
                                if (string.IsNullOrEmpty(value))
                                {
                                    value = preferredLanguage;
                                }
                                tableRow["lang"] = value;
                            }
                            break;

                        case "rating":
                            value = xmlResponse.SelectSingleNode("/Data/jeu/note")?.InnerText;
                            string rating = ProcessRating(value);
                            if (string.IsNullOrEmpty(tableRow["rating"]?.ToString()) || overWriteData)
                            {
                                tableRow["rating"] = rating;
                            }
                            break;

                        case "desc":
                            value = xmlResponse.SelectSingleNode($"/Data/jeu/synopsis/synopsis[@langue='{preferredLanguage}']")?.InnerText;
                            if (string.IsNullOrEmpty(value))
                            {
                                value = xmlResponse.SelectSingleNode($"/Data/jeu/synopsis/synopsis[@langue='{backupLanguage}']")?.InnerText;
                            }

                            if (string.IsNullOrEmpty(tableRow["desc"]?.ToString()) || overWriteData)
                            {
                                tableRow["desc"] = value;
                            }
                            break;

                        case "name":
                            XmlNode namesNode = xmlResponse.SelectSingleNode("/Data/jeu/noms");
                            value = ParseNames(namesNode, regionToScrape);
                            if (string.IsNullOrEmpty(tableRow["name"]?.ToString()) || overWriteData)
                            {
                                tableRow["name"] = value;
                            }
                            break;

                        case "genre":
                            XmlNode genresNode = xmlResponse.SelectSingleNode("/Data/jeu/genres");
                            value = ParseGenres(genresNode, preferredLanguage);

                            if (!string.IsNullOrEmpty(value))
                            {
                                value = ParseGenres(genresNode, backupLanguage);
                            }
                            if (string.IsNullOrEmpty(tableRow["genre"]?.ToString()) || overWriteData)
                            {
                                tableRow["genre"] = value;
                            }
                            break;

                        case "releasedate":
                            value = xmlResponse.SelectSingleNode($"/Data/jeu/dates/date[@region='{regionToScrape}']")?.InnerText;
                            if (string.IsNullOrEmpty(tableRow["releasedate"]?.ToString()) || overWriteData)
                            {
                                string releasedate = ConvertToISO8601(value);
                                tableRow["releasedate"] = releasedate;
                            }
                            break;

                        case "manual":
                            XmlNode xmlNode = xmlResponse.SelectSingleNode("/Data/jeu/medias");
                            value = ParseMedia("manuel",xmlNode, regionToScrape);
                            if (string.IsNullOrEmpty(tableRow["manual"]?.ToString()) || overWriteData)
                            {
                                tableRow["manual"] = value;
                                
                            }

                            break;
                    }
                }
            }

        }

        private string ParseMedia(string mediaType, XmlNode mediaElement, string region)
        {
            if (mediaElement == null) { return null; }
            var media = mediaElement?.SelectNodes($"media[@type='{mediaType}' and @region='{region}']");

            if (media.Count != 1)
            {
                return null;
            }
            return media[0].InnerText;
        }

        private string ParseNames(XmlNode namesElement, string region)
        {
            if (namesElement == null) {  return null; }

            var names = namesElement?.SelectNodes($"nom[@region='{region}']");

            if (names.Count != 1)
            {
                return null;
            }
            return names[0].InnerText;
        }

        static List<Dictionary<string, string>> SearchMedia(XmlDocument xmlDoc, string parent, string region, string type)
        {
            List<Dictionary<string, string>> matchingEntries = new List<Dictionary<string, string>>();

            XmlNodeList mediaNodes = xmlDoc.SelectNodes($"//media[@parent='{parent}' and @region='{region}' and @type='{type}']");

            foreach (XmlNode mediaNode in mediaNodes)
            {
                Dictionary<string, string> entry = new Dictionary<string, string>
            {
                { "type", mediaNode.Attributes["type"].Value },
                { "parent", mediaNode.Attributes["parent"].Value },
                { "region", mediaNode.Attributes["region"].Value },
                { "crc", mediaNode.Attributes["crc"].Value },
                { "md5", mediaNode.Attributes["md5"].Value },
                { "sha1", mediaNode.Attributes["sha1"].Value },
                { "size", mediaNode.Attributes["size"].Value },
                { "format", mediaNode.Attributes["format"].Value },
                { "url", mediaNode.InnerText }
            };

                matchingEntries.Add(entry);
            }

            return matchingEntries;
        }

        private List<string> GetMediaTypes()
        {
              List<string> mediaTypes = new List<string>
        {
            "ss", "fanart", "video", "video-normalized", "themehs", "marquee", "screenmarquee",
            "screenmarqueesmall", "manuel", "flyer", "steamgrid", "wheel", "wheel-carbon", 
            "wheel-steel", "box-2D", "box-2D-side", "box-2D-back", "box-texture", "box-3D",
            "bezel-4-3", "bezel-16-9", "mixrbv1", "mixrbv2", "pictoliste", "pictomonochrome", "pictocouleur"
        };
            return mediaTypes;
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


        private int GetSystemId(string name)
        {
            int systemId;
            if (systemValuesDictionary.TryGetValue(name.ToLower(), out systemId))
            {
                return systemId;
            }
            else
            {
                return 0;
            }
        }

        private async Task<XmlDocument> GetXMLResponseAsync(string url, CancellationToken cancellationToken)
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


        private Dictionary<string, int> systemValuesDictionary = new Dictionary<string, int>
        {
            {"3do",29},
            {"3ds",17},
            {"abuse",0},
            {"adam",89},
            {"advision",78},
            {"amiga1200",64},
            {"amiga500",64},
            {"amigacd32",130},
            {"amigacdtv",129},
            {"amstradcpc",65},
            {"apfm1000",0},
            {"apple2",86},
            {"apple2gs",217},
            {"arcadia",94},
            {"archimedes",84},
            {"arduboy",263},
            {"astrocde",44},
            {"atari2600",26},
            {"atari5200",40},
            {"atari7800",41},
            {"atari800",43},
            {"atarist",42},
            {"atom",36},
            {"atomiswave",53},
            {"bbc",37},
            {"boom3",0},
            {"c128",66},
            {"c20",73},
            {"c64",66},
            {"camplynx",88},
            {"cannonball",0},
            {"cave3rd",0},
            {"cavestory",135},
            {"cdi",133},
            {"cdogs",0},
            {"cgenius",0},
            {"channelf",80},
            {"coco",144},
            {"colecovision",48},
            {"corsixth",0},
            {"cplus4",99},
            {"crvision",241},
            {"daphne",49},
            {"devilutionx",0},
            {"dos",0},
            {"dreamcast",23},
            {"easyrpg",231},
            {"ecwolf",0},
            {"eduke32",0},
            {"electron",85},
            {"fbneo",75},
            {"fds",106},
            {"flash",0},
            {"flatpak",0},
            {"fm7",97},
            {"fmtowns",253},
            {"fpinball",199},
            {"fury",0},
            {"gamate",266},
            {"gameandwatch",52},
            {"gamecom",121},
            {"gamecube",13},
            {"gamegear",21},
            {"gamepock",95},
            {"gb",9},
            {"gb2players",0},
            {"gba",12},
            {"gbc",10},
            {"gbc2players",0},
            {"gmaster",103},
            {"gp32",101},
            {"gx4000",0},
            {"gzdoom",0},
            {"hcl",0},
            {"hurrican",0},
            {"ikemen",0},
            {"intellivision",115},
            {"jaguar",27},
            {"laser310",0},
            {"lcdgames",75},
            {"lowresnx",0},
            {"lutro",206},
            {"lynx",28},
            {"macintosh",146},
            {"mame",75},
            {"mastersystem",2},
            {"megadrive",1},
            {"megaduck",90},
            {"model2",0},
            {"model3",55},
            {"moonlight",138},
            {"mrboom",0},
            {"msu - md",0},
            {"msx1",113},
            {"msx2",116},
            {"msx2 +",117},
            {"msxturbor",118},
            {"mugen",0},
            {"multivision",0},
            {"n64",14},
            {"n64dd",122},
            {"namco2x6",0},
            {"naomi",56},
            {"naomi2",56},
            {"nds",15},
            {"neogeo",142},
            {"neogeocd",70},
            {"nes",3},
            {"ngp",25},
            {"ngpc",82},
            {"o2em",0},
            {"odcommander",0},
            {"openbor",214},
            {"openlara",0},
            {"pc88",221},
            {"pc98",208},
            {"pcengine",31},
            {"pcenginecd",114},
            {"pcfx",72},
            {"pdp1",0},
            {"pet",240},
            {"pico",250},
            {"pico8",234},
            {"plugnplay",0},
            {"pokemini",211},
            {"ports",0},
            {"prboom",0},
            {"ps2",58},
            {"ps3",59},
            {"psp",61},
            {"psvita",62},
            {"psx",57},
            {"pv1000",74},
            {"pygame",0},
            {"pyxel",0},
            {"quake3",0},
            {"raze",0},
            {"reminiscence",0},
            {"samcoupe",213},
            {"satellaview",107},
            {"saturn",22},
            {"scummvm",123},
            {"scv",67},
            {"sdlpop",0},
            {"sega32x",19},
            {"segacd",20},
            {"sg1000",109},
            {"sgb",127},
            {"snes",4},
            {"snes - msu1",210},
            {"socrates",0},
            {"solarus",223},
            {"sonicretro",0},
            {"spectravideo",218},
            {"steam",0},
            {"sufami",108},
            {"superbroswar",0},
            {"supergrafx",105},
            {"supervision",207},
            {"supracan",100},
            {"thextech",0},
            {"thomson",141},
            {"ti99",205},
            {"tic80",222},
            {"triforce",0},
            {"tutor",0},
            {"tyrian",0},
            {"tyrquake",0},
            {"uzebox",216},
            {"vc4000",0},
            {"vectrex",102},
            {"vgmplay",0},
            {"videopacplus",0},
            {"virtualboy",11},
            {"vis",144},
            {"vitaquake2",0},
            {"vpinball",198},
            {"vsmile",120},
            {"wasm4",262},
            {"wii",16},
            {"wiiu",18},
            {"windows",138},
            {"windows_installers",138},
            {"wswan",45},
            {"wswanc",46},
            {"x1",220},
            {"x68000",79},
            {"xash3d_fwgs",0},
            {"xbox",32},
            {"xbox360",33},
            {"xegs",0},
            {"xrick",0},
            {"zc210",0},
            {"zx81",77},
            {"zxspectrum",76}
        };

        private string GetCellValueAsString(DataRow row, string columnName)
        {
            if (row != null && row.Table.Columns.Contains(columnName))
            {
                object cellValueObject = row[columnName];
                return (cellValueObject != null) ? cellValueObject.ToString() : string.Empty;
            }
            else
            {
                return string.Empty;
            }
        }


        private string ConvertToISO8601(string dateString)
        {
            if(string.IsNullOrEmpty(dateString))
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