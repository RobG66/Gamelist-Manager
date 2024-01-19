using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GamelistManager.form
{
    public partial class Scraper : Form
    {
        private CancellationTokenSource cancellationTokenSource;
        private bool isScraping = false;

        public Scraper()
        {
            InitializeComponent();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            comboBox1.Enabled = radioScrapeSelected.Checked;
        }


        private async void button1_Click(object sender, EventArgs e)
        {
            if (!isScraping)
            {
                DataGridView dgv = ((GamelistManager)this.Owner).MainDataGridView;
                List<string> romPaths = null;

                if (radioScrapeAll.Checked)
                {
                    romPaths = dgv.Rows
                        .Cast<DataGridViewRow>()
                        .Select(row => row.Cells["path"].Value as string)
                        .ToList(); // Convert to List<string>
                }
                else
                {
                    romPaths = dgv.SelectedRows
                        .Cast<DataGridViewRow>()
                        .Select(row => row.Cells["path"].Value as string)
                        .ToList(); // Convert to List<string>
                }


                List<string> elementsToScrape = new List<string>();
                foreach (Control control in panel2.Controls)
                {
                    if (control is CheckBox checkBox && checkBox.Checked)
                    {
                        string elementName = checkBox.Name.Replace("checkbox_", "").ToLower();
                        elementsToScrape.Add(elementName);
                    }
                }

                bool overWriteData = checkBox_OverwriteExisting.Checked;

                progressBar1.Value = 0;
                progressBar1.Minimum = 0;
                progressBar1.Maximum = romPaths.Count();
                progressBar1.Step = 1;

                if (listBoxLog.Items.Count > 0)
                {
                    listBoxLog.Items.Clear();
                }

                // Starting the scraping process
                isScraping = true;
                buttonStart.Text = "Stop";

                // Reset the cancellation token source
                cancellationTokenSource = new CancellationTokenSource();

                // Call the scraper method asynchronously
                //await Task.Run(() => ScrapeArcadeDBAsync(cancellationTokenSource.Token));
                await ScrapeArcadeDBAsync(overWriteData, elementsToScrape, romPaths, cancellationTokenSource.Token);

                // Cleanup after scraping is complete or canceled
                isScraping = false;
                buttonStart.Text = "Start";
            }
            else
            {
                // Stopping the scraping process
                isScraping = false;
                buttonStart.Text = "Start";

                // Cancel the ongoing operation
                cancellationTokenSource?.Cancel();
            }
        }

        private async
        Task
        ScrapeArcadeDBAsync(bool overWriteData, List<string> elementsToScrape, List<string> romPaths, CancellationToken cancellationToken)
        {
            GamelistManager gamelistManager = (GamelistManager)this.Owner;
            DataSet dataSet = gamelistManager.DataSet;

            // Batocera and Scraper element names don't always align
            // Therefore a dictionary is made to cross reference
            Dictionary<string, string> Metadata = new Dictionary<string, string>();
            Metadata.Add("name", "title");
            Metadata.Add("desc", "history");
            Metadata.Add("genre", "genre");
            Metadata.Add("players", "players");
            Metadata.Add("rating", "rate");
            Metadata.Add("lang", "languages");
            Metadata.Add("publisher", "manufacturer");
            Metadata.Add("marquee", "url_image_marquee");
            Metadata.Add("image", "url_image_ingame");
            Metadata.Add("video", "url_video_shortplay_hd");

            int count = 0;
            int batchSize = 50;

            string scraperBaseURL = "http://adb.arcadeitalia.net/service_scraper.php?ajax=query_mame&game_name=";
            string parentFolderPath = Path.GetDirectoryName(((GamelistManager)this.Owner).XMLFilename);

            // Start to process selected datagridview rows
            for (int i = 0; i < romPaths.Count; i += batchSize)
            {
                Thread.Sleep(1000);

                // Take the next batch of roms
                string[] batchArray = romPaths.Skip(i).Take(batchSize).ToArray();

                // Construct a semicolon-separated string of ROM names for the current batch
                string joinedRomNames = string.Join(";", batchArray.Select(path => gamelistManager.ExtractPath(path)));

                // Construct the scraper URL with the batch of ROM names
                string scraperRequestURL = $"{scraperBaseURL}{(joinedRomNames)}";

                // Declare response string
                string jsonResponse = null;

                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        // Make the HTTP request to get the response as a byte array
                        byte[] responseBytes = await client.GetByteArrayAsync(scraperRequestURL);

                        // Convert the byte array to a string using a specific encoding (UTF-8 in this case)
                        jsonResponse = Encoding.UTF8.GetString(responseBytes);
                    }
                }
                catch (Exception ex)
                {
                    // Log failure
                    AddToLog($"Exception: {ex.Message}");
                }

                // Deserialize the JSON to names and values
                GameListResponse scraperResponse = JsonConvert.DeserializeObject<GameListResponse>(jsonResponse);

                // Loop through the returned data and process it
                for (int j = 0; j < batchArray.Length; j++)
                {
                    count++;
                    UpdateProgressBar();

                    if (cancellationToken.IsCancellationRequested)
                    {
                        // Perform cleanup or handle cancellation if needed
                        AddToLog("Scraping canceled by user.");
                        return;
                    }

                    string currentRomPath = batchArray[j];
                    string currentRomName = gamelistManager.ExtractPath(currentRomPath);

                    ScraperArcadeDBGameInfo scraperData = scraperResponse.result[j];

                    UpdateLabel($"Scraping: {currentRomName}");

                    // Loop through the Metadata dictionary
                    foreach (var kvp in Metadata)
                    {
                        string localPropertyName = kvp.Key;
                        string remotePropertyName = kvp.Value;

                        if (!elementsToScrape.Contains(localPropertyName))
                        {
                            continue;
                        }

                        // Get the returned property value from its string name
                        PropertyInfo property = typeof(ScraperArcadeDBGameInfo).GetProperty(remotePropertyName);
                        string scrapedValue = property.GetValue(scraperData)?.ToString();

                        DataRow tableRow = dataSet.Tables[0].AsEnumerable()
                            .FirstOrDefault(row => row.Field<string>("path") == currentRomPath);

                        string columnName = localPropertyName;
                        object cellValueObject = tableRow[columnName];
                        string cellValueString = (cellValueObject != null) ? cellValueObject.ToString() : string.Empty;

                        // Check for empty scrape value
                        if (string.IsNullOrEmpty(scrapedValue))
                        {
                            continue;
                        }

                        // If it's not an image property, we stop here
                        if (!remotePropertyName.Contains("url_"))
                        {
                            // Check if we are allowed to overwrite
                            // Write regardless if the local value is empty
                            if ((!overWriteData && string.IsNullOrEmpty(cellValueString)) || overWriteData)
                            {
                                tableRow[columnName] = scrapedValue;
                            }
                            continue;
                        }

                        // Image handling
                        string remoteDownloadURL = scrapedValue;

                        if (string.IsNullOrEmpty(remoteDownloadURL))
                        {
                            continue;
                        }

                        // What are we downloading, a video or image?                                     
                        string fileName = null;
                        string folderName = null;

                        if (localPropertyName == "video")
                        {
                            folderName = "videos";
                            fileName = $"{currentRomName}-video.mp4";
                        }
                        else
                        {
                            folderName = "images";
                            fileName = $"{currentRomName}-{localPropertyName}.png";
                        }

                        string downloadPath = $"{parentFolderPath}\\{folderName}";
                        string fileToDownload = $"{downloadPath}\\{fileName}";
                        bool result = await DownloadFile(fileToDownload, remoteDownloadURL);
                        // Returns true on success
                        if (result == true)
                        {
                            tableRow[columnName] = $"./{folderName}/{fileName}";
                        }

                    }
                }
            }
        }
        private async Task<bool> DownloadFile(string fileToDownload, string url)
        {

            Thread.Sleep(200);
            
            try
            {
                if (File.Exists(fileToDownload))
                {
                    File.Delete(fileToDownload);
                }

                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync(new Uri(url), fileToDownload);

                    Console.WriteLine($"File downloaded successfully: {fileToDownload}");
                    return true; // Return true if the download was successful
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
                return false; // Return false if an exception occurred during the download
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (Control control in panel2.Controls)
            {
                // Check if the control is a checkbox
                if (control is System.Windows.Forms.CheckBox checkBox && checkBox.Enabled == true)
                {
                    // Set checked
                    checkBox.Checked = true;
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            foreach (Control control in panel2.Controls)
            {
                // Check if the control is a checkbox
                if (control is System.Windows.Forms.CheckBox checkBox && checkBox.Enabled == true)
                {
                    // Set unchecked
                    checkBox.Checked = false;
                }
            }
        }

        private void Scraper_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
        }

        private void AddToLog(string logMessage)
        {
            if (listBoxLog.InvokeRequired)
            {
                listBoxLog.Invoke(new Action(() => listBoxLog.Items.Add($"{DateTime.Now} - {logMessage}")));
            }
            else
            {
                listBoxLog.Items.Add($"{DateTime.Now} - {logMessage}");
            }
        }

        private void UpdateLabel(string text)
        {
            if (label1.InvokeRequired)
            {
                label1.Invoke(new Action(() => label1.Text = text));
            }
            else
            {
                label1.Text = text;
            }
        }

        private void UpdateProgressBar()
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action(() => progressBar1.Value++));
            }
            else
            {
                progressBar1.Value++;
            }
        }
    }


}


