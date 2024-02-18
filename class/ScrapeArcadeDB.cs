using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
            string jsonResponse = await GetJsonResponseAsync(scraperURL);

            if (jsonResponse == null)
            {
                return null;
            }

            ScrapeResult scrapeResult = JsonConvert.DeserializeObject<ScrapeResult>(jsonResponse);

            if (scrapeResult.Result.Count == 0)
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

            GameInfo ScrapedGameInfo = scrapeResult.Result[0];
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
                        scraperData["publisher"] = ScrapedGameInfo.manufacturer;
                        break;

                    case "players":
                        scraperData["players"] = ScrapedGameInfo.players.ToString();
                        break;

                    case "lang":
                        scraperData["lang"] = ScrapedGameInfo.languages;
                        break;

                    case "rating":
                        int rating = ScrapedGameInfo.rate;
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
                        scraperData["desc"] = ScrapedGameInfo.history;
                        break;

                    case "name":
                        scraperData["name"] = ScrapedGameInfo.title;
                        break;

                    case "genre":
                        scraperData["genre"] = ScrapedGameInfo.genre;
                        break;

                    case "releasedate":
                        scraperData["releasedate"] = ConvertToISO8601(ScrapedGameInfo.year);
                        break;

                    case "image":
                        remoteDownloadURL = ScrapedGameInfo.url_image_ingame;
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
                        remoteDownloadURL = ScrapedGameInfo.url_image_marquee;
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
                        remoteDownloadURL = ScrapedGameInfo.url_video_shortplay_hd;
                        if (remoteDownloadURL == null)
                        {
                            remoteDownloadURL = ScrapedGameInfo.url_video_shortplay;
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

        private async Task<string> GetJsonResponseAsync(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] responseBytes = await client.GetByteArrayAsync(url);
                    return Encoding.UTF8.GetString(responseBytes);
                }
            }
            catch
            {
                return null;
            }
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
