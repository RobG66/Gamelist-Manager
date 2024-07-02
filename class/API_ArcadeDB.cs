using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GamelistManager
{
    internal static class API_ArcadeDB
    {
        private static readonly string apiURL = "http://adb.arcadeitalia.net/service_scraper.php";
        private static readonly HttpClient client = new HttpClient();

        public static async Task<ArcadeDBMetaData> ScrapeGame(string romName)
        {
            string url = $"{apiURL}?ajax=query_mame&game_name={romName}";
            try
            {
                // Fetch the response bytes from the URL
                byte[] responseBytes = await client.GetByteArrayAsync(url);

                // Convert the byte array to a UTF-8 string
                string jsonString = Encoding.UTF8.GetString(responseBytes);

                // Deserialize the JSON string into a GameInfoWrapper object
                GameInfoWrapper wrapper = JsonSerializer.Deserialize<GameInfoWrapper>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // Allows case-insensitive property matching
                });

                // Return the first item in the "Result" list, or null if the list is empty
                return wrapper?.Result?.FirstOrDefault();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected error: {e.Message}");
                return null;
            }
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

        public static async Task<ScraperData> ScrapeArcadeDBAsync(ScraperParameters scraperParameters,ListBox ListBoxControl
            )
        {
            var gameInfo = await ScrapeGame(scraperParameters.RomFileNameWithExtension);

            if (gameInfo == null)
            {
                return null;
            }

            string remoteDownloadURL = null;
            string destinationFolder = null;
            string fileName = null;
            string downloadPath = null;
            string fileToDownload = null;
            bool downloadResult = false;
       
            ScraperData scraperData = new ScraperData();

            foreach (string element in scraperParameters.ElementsToScrape)
            {
                switch (element)
                {
                    case "publisher":
                        scraperData.publisher = gameInfo.manufacturer;
                        break;

                    case "players":
                        scraperData.players = gameInfo.players.ToString();
                        break;

                    case "lang":
                        scraperData.lang = gameInfo.languages;
                        break;

                    case "rating":
                        int rating = gameInfo.rate;
                        string convertedRating = null;
                        if (rating == 100)
                        {
                            convertedRating = "1";
                        }
                        else if (rating > 0 && rating < 100)
                        {
                            convertedRating = "." + rating.ToString().TrimStart('0');
                        }
                        scraperData.rating = convertedRating;
                        break;

                    case "desc":
                        scraperData.desc = gameInfo.history;
                        break;

                    case "name":
                        scraperData.name = gameInfo.title;
                        break;

                    case "genre":
                        scraperData.genre = gameInfo.genre;
                        break;

                    case "releasedate":
                        scraperData.releasedate = ISO8601Converter.ConvertToISO8601(gameInfo.year);
                        break;

                    case "image":
                        remoteDownloadURL = gameInfo.url_image_ingame; 
                        if (!string.IsNullOrEmpty(remoteDownloadURL))
                        {
                            destinationFolder = "images";
                            fileName = $"{scraperParameters.RomFileNameWithoutExtension}-image.png";
                            downloadPath = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}";
                            fileToDownload = $"{downloadPath}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(scraperParameters.Overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult)
                            {
                                scraperData.image = $"./{destinationFolder}/{fileName}";
                                ShowDownload(ListBoxControl, $"Downloaded: {fileName}");
                            }
                        }
                        break;

                    case "marquee":
                        remoteDownloadURL = gameInfo.url_image_marquee;
                        if (!string.IsNullOrEmpty(remoteDownloadURL))
                        {
                            destinationFolder = "images";
                            fileName = $"{scraperParameters.RomFileNameWithoutExtension}-marquee.png";
                            downloadPath = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}";
                            fileToDownload = $"{downloadPath}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(scraperParameters.Overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult)
                            {
                                scraperData.marquee = $"./{destinationFolder}/{fileName}";
                                ShowDownload(ListBoxControl, $"Downloaded: {fileName}");
                            }
                        }
                        break;

                    case "video":
                        remoteDownloadURL = gameInfo.url_video_shortplay_hd;
                        if (string.IsNullOrEmpty(remoteDownloadURL))
                        {
                            remoteDownloadURL = gameInfo.url_video_shortplay;
                        }
                        if (!string.IsNullOrEmpty(remoteDownloadURL))
                        {
                            destinationFolder = "videos";
                            fileName = $"{scraperParameters.RomFileNameWithoutExtension}-video.mp4";
                            downloadPath = $"{scraperParameters.ParentFolderPath}\\{destinationFolder}";
                            fileToDownload = $"{downloadPath}\\{fileName}";
                            downloadResult = await FileTransfer.DownloadFile(scraperParameters.Overwrite, fileToDownload, remoteDownloadURL);
                            if (downloadResult)
                            {
                                scraperData.video = $"./{destinationFolder}/{fileName}";
                                ShowDownload(ListBoxControl, $"Downloaded: {fileName}");
                            }
                        }
                        break;
                }
            }

            return scraperData;
        }

        // Wrapper class to match the JSON structure
        public class GameInfoWrapper
        {
            public List<ArcadeDBMetaData> Result { get; set; }
        }

        public class ArcadeDBMetaData
        {
            public int index { get; set; }  // Progressive number, starts from 1
            public string url { get; set; }  // Url for detailed game informations
            public string game_name { get; set; }  // Game romset. Ex. mslug, atetris
            public string title { get; set; }  // Game title. Ex. Metal Slug, Tetris (set 1)
            public string cloneof { get; set; }  // Romset of the parent game, if any
            public string manufacturer { get; set; }  // Manufacturer
            public string url_image_ingame { get; set; }  // Url of in-game snapshot image file (png format), empty if not present
            public string url_image_title { get; set; }  // Url of title snapshot image file (png format), empty if not present
            public string url_image_marquee { get; set; }  // Url of marquee snapshot image file (png format), empty if not present
            public string url_image_cabinet { get; set; }  // Url of cabinet snapshot image file (png format), empty if not present
            public string url_image_flyer { get; set; }  // Url of flyer snapshot image file (png format), empty if not present
            public string url_icon { get; set; }  // Url of icon image file (ico format), empty if not present
            public string genre { get; set; }  // Genre of the game, from category files. Ex. Fight, Drive, etc.
            public int players { get; set; }  // Number of supported players
            public string year { get; set; }  // Year of release of the game. It may contain symbols to indicate release date not accurate. Ex. 19??
            public string status { get; set; }  // Emulation driver status. Values: GOOD, IMPERFECT, PRELIMINARY, TEST, UNKNOWN
            public string history { get; set; }  // Game history
            public string history_copyright_short { get; set; }  // Short copyright history
            public string history_copyright_long { get; set; }  // Long copyright history
            public string youtube_video_id { get; set; }  // Youtube video ID
            public string url_video_shortplay { get; set; }  // Url of shortplay video file (mp4 format), empty if not present
            public string url_video_shortplay_hd { get; set; }  // Url of shortplay high-definition video file (mp4 format), empty if not present
            public int emulator_id { get; set; }  // Internal emulator id, useful to check romset update
            public string emulator_name { get; set; }  // Name and release of the emulator. Ex. "Mame 0.189 (30-aug-2017)"
            public string languages { get; set; }  // Supported languages, comma separated values
            public int rate { get; set; }  // Medium game rating. Values starting from 0 (no rating) up to 100 (maximum rating) with steps of 1
            public string short_title { get; set; }  // Simplified name of the game
            public string nplayers { get; set; }  // Players and player modes, from nplayers.ini (Nomax)
            public string input_controls { get; set; }  // Input controls types, from gamelist.xml (Mame)
            public int input_buttons { get; set; }  // Input buttons, from gamelist.xml (Mame)
            public string buttons_colors { get; set; }  // Input buttons colors and labels, from colors.ini (headkaze) and controls.xml (SirPoonga)
            public string serie { get; set; }  // Serie name, from series.ini (ProgettoSnaps)
            public string screen_orientation { get; set; }  // Screen orientation. Values: Horizontal, Vertical
            public string screen_resolution { get; set; }  // Screen resolution, frequency and rotation
        }
    }
}
