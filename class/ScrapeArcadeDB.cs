//using Newtonsoft.Json;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Forms;
using System;

namespace GamelistManager
{

    public class ScrapeArcadeDB
    {
        public async Task<Dictionary<string, string>> ScrapeArcadeDBAsync(
        string romName,
        string folderPath,
        bool overwrite,
        List<string> elementsToScrape
        )
        {
            // Build a dictionary from the elements we are going to scrape
            Dictionary<string, string> scraperData = new Dictionary<string, string>();
            foreach (var propertyName in elementsToScrape)
            {
                scraperData.Add(propertyName, null);
            }

            string romNameNoExtension = Path.GetFileNameWithoutExtension(romName);
            string scraperURL = $"http://adb.arcadeitalia.net/service_scraper.php?ajax=query_mame&game_name={romNameNoExtension}";
            GameInfo scrapedGameInfo = await GetGameInfoAsync(scraperURL);
            
            if (scrapedGameInfo == null)
            {
                return null;
            }

           if (scrapedGameInfo.game_name == null)
            {
                return null;
            }

            // Build MD5
            string fullRomPath = $"{folderPath}\\{romName}";
            string md5 = ChecksumCreator.CreateMD5(fullRomPath);
            if (!string.IsNullOrEmpty(md5))
            {
                scraperData.Add("md5", null);
            }

            string remoteDownloadURL = null;
            string folderName = null;
            string fileName = null;
            string downloadPath = null;
            string fileToDownload = null;
            bool result = false;
            foreach (string element in elementsToScrape)
            {
                switch (element)
                {
                    case "publisher":
                        scraperData["publisher"] = scrapedGameInfo.manufacturer;
                        break;

                    case "players":
                        scraperData["players"] = scrapedGameInfo.players.ToString();
                        break;

                    case "lang":
                        scraperData["lang"] = scrapedGameInfo.languages;
                        break;

                    case "rating":
                        int rating = scrapedGameInfo.rate;
                        string convertedRating = null;
                        if (rating == 100)
                        {
                            convertedRating = "1";
                        }
                        if (rating > 0 && rating < 100)
                        {
                            convertedRating = "." + rating.ToString().TrimStart('0');
                        }
                        scraperData["rating"] = convertedRating;
                        break;

                    case "desc":
                        scraperData["desc"] = scrapedGameInfo.history;
                        break;

                    case "name":
                        scraperData["name"] = scrapedGameInfo.title;
                        break;

                    case "genre":
                        scraperData["genre"] = scrapedGameInfo.genre;
                        break;

                    case "releasedate":
                        scraperData["releasedate"] = ISO8601Converter.ConvertToISO8601(scrapedGameInfo.year);
                        break;

                    case "image":
                        remoteDownloadURL = scrapedGameInfo.url_image_ingame;
                        if (remoteDownloadURL == null)
                        {
                            continue;
                        }
                        folderName = "images";
                        fileName = $"{romNameNoExtension}-image.png";
                        downloadPath = $"{folderPath}\\{folderName}";
                        fileToDownload = $"{downloadPath}\\{fileName}";
                        result = await FileTransfer.DownloadFile(overwrite, fileToDownload, remoteDownloadURL);
                        if (result)
                        {
                            scraperData["image"] = $"./{folderName}/{fileName}";
                        }
                        break;

                    case "marquee":
                        remoteDownloadURL = scrapedGameInfo.url_image_marquee;
                        if (remoteDownloadURL == null)
                        {
                            continue;
                        }
                        folderName = "images";
                        fileName = $"{romNameNoExtension}-marquee.png";
                        downloadPath = $"{folderPath}\\{folderName}";
                        fileToDownload = $"{downloadPath}\\{fileName}";
                        result = await FileTransfer.DownloadFile(overwrite, fileToDownload, remoteDownloadURL);
                        if (result)
                        {
                           scraperData["marquee"] = $"./{folderName}/{fileName}";
                        }
                        break;

                    case "video":
                        remoteDownloadURL = scrapedGameInfo.url_video_shortplay_hd;
                        if (remoteDownloadURL == null)
                        {
                            remoteDownloadURL = scrapedGameInfo.url_video_shortplay;
                        }
                        if (remoteDownloadURL == null)
                        {
                            continue;
                        }
                        folderName = "videos";
                        fileName = $"{romNameNoExtension}-video.mp4";
                        downloadPath = $"{folderPath}\\{folderName}";
                        fileToDownload = $"{downloadPath}\\{fileName}";
                        result = await FileTransfer.DownloadFile(overwrite, fileToDownload, remoteDownloadURL);
                        if (result)
                        {
                            scraperData["video"] = $"./{folderName}/{fileName}";
                        }
                        break;
                }
            }
            return scraperData;
        }

        public class GameInfoWrapper
        {
            public List<GameInfo> Result { get; set; }
        }

        private async Task<GameInfo> GetGameInfoAsync(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] responseBytes = await client.GetByteArrayAsync(url);
                    string jsonString = Encoding.UTF8.GetString(responseBytes);

                    // Deserialize the entire object into GameInfoWrapper
                    GameInfoWrapper wrapper = JsonSerializer.Deserialize<GameInfoWrapper>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // This allows case-insensitive property matching
                    });

                    // Return the first item in the "result" array
                    return wrapper?.Result?.FirstOrDefault();
                }
            }
            catch
            {
                return null;
            }
        }


        public class GameInfo
        {
            public int index { get; set; }
            public string url { get; set; }
            public string game_name { get; set; }
            public string title { get; set; }
            public string cloneof { get; set; }
            public string manufacturer { get; set; }
            public string url_image_ingame { get; set; }
            public string url_image_title { get; set; }
            public string url_image_marquee { get; set; }
            public string url_image_cabinet { get; set; }
            public string url_image_flyer { get; set; }
            public string url_icon { get; set; }
            public string genre { get; set; }
            public int players { get; set; }
            public string year { get; set; }
            public string status { get; set; }
            public string history { get; set; }
            public string history_copyright_short { get; set; }
            public string history_copyright_long { get; set; }
            public string youtube_video_id { get; set; }
            public string url_video_shortplay { get; set; }
            public string url_video_shortplay_hd { get; set; }
            public int emulator_id { get; set; }
            public string emulator_name { get; set; }
            public string languages { get; set; }
            public int rate { get; set; }
            public string short_title { get; set; }
            public string nplayers { get; set; }
            public string input_controls { get; set; }
            public int input_buttons { get; set; }
            public string buttons_colors { get; set; }
            public string serie { get; set; }
            public string screen_orientation { get; set; }
            public string screen_resolution { get; set; }
        }

        public class ScrapeResult
        {
            public int Release { get; set; }
            public List<GameInfo> Result { get; set; }
        }
    }

}
