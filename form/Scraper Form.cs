using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace GamelistManager.form
{
    public partial class Scraper : Form
    {
        private CancellationTokenSource cancellationTokenSource;
        private bool isScraping = false;
        private GamelistManager gamelistManager;

        public Scraper(GamelistManager owner)
        {
            InitializeComponent();
            this.gamelistManager = owner;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_Scrapers.Enabled = RadioButton_ScrapeSelected.Checked;
        }

        private void saveReminder(bool canceled)
        {
            string finish = null;
            MessageBoxIcon icon = MessageBoxIcon.Information;

            if (canceled)
            {
                finish = "Scraping Was Cancelled!";
                icon = MessageBoxIcon.Error;
            }
            else
            {
                finish = "Scraping Completed!";
            }

            if (!checkBox_Save.Checked)
            {
                MessageBox.Show($"{finish}", "Notice:", MessageBoxButtons.OK, icon);
                return;
            }

            gamelistManager.SaveFile();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
  
            List<string> elementsToScrape = new List<string>();
            foreach (Control control in panel_CheckboxGroup.Controls)
            {
                if (control is CheckBox checkBox && checkBox.Checked)
                {
                    string elementName = checkBox.Name.Replace("checkbox_", "").ToLower();
                    elementsToScrape.Add(elementName);
                }
            }

            if (elementsToScrape.Count == 0)
            {
                MessageBox.Show("No metadata selection was made", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            DataGridView dgv = ((GamelistManager)this.Owner).MainDataGridView;
            List<string> romPaths = null;

            if (RadioButton_ScrapeAll.Checked)
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

            bool overWriteData = checkBox_OverwriteExisting.Checked;
            progressBar_ScrapeProgress.Value = 0;
            progressBar_ScrapeProgress.Minimum = 0;
            progressBar_ScrapeProgress.Maximum = romPaths.Count();
            progressBar_ScrapeProgress.Step = 1;

            if (listBoxLog.Items.Count > 0)
            {
                listBoxLog.Items.Clear();
            }


            button_StartStop.Enabled = false;
            button_Cancel.Enabled = true    ;

            // Reset the cancellation token source
            cancellationTokenSource = new CancellationTokenSource();

            // Call the scraper method asynchronously
            //await Task.Run(() => ScrapeArcadeDBAsync(cancellationTokenSource.Token));
            await ScrapeArcadeDBAsync(overWriteData, elementsToScrape, romPaths, cancellationTokenSource.Token);

            // Cleanup after scraping is complete or canceled
            button_StartStop.Enabled = true;
            button_Cancel.Enabled = false ; 

            if (cancellationTokenSource.Token.IsCancellationRequested)
            {
             
            }

            saveReminder(cancellationTokenSource.Token.IsCancellationRequested);
       
        }

        private async
        Task
        ScrapeArcadeDBAsync(bool overWriteData, List<string> elementsToScrape, List<string> romPaths, CancellationToken cancellationToken)
        {

            int total = romPaths.Count;
            int count = 0;

            DataSet dataSet = gamelistManager.DataSet;

            // Batocera and ArcadeDB element names don't always align
            // Therefore a dictionary is made to cross reference
            Dictionary<string, string> Metadata = new Dictionary<string, string>();
            Metadata.Add("name", "title");
            Metadata.Add("desc", "history");
            Metadata.Add("genre", "genre");
            Metadata.Add("releasedate", "year");
            Metadata.Add("players", "players");
            Metadata.Add("rating", "rate");
            Metadata.Add("lang", "languages");
            Metadata.Add("publisher", "manufacturer");
            Metadata.Add("marquee", "url_image_marquee");
            Metadata.Add("image", "url_image_ingame");
            Metadata.Add("video", "url_video_shortplay_hd");

            int batchSize = 50;

            string scraperBaseURL = "http://adb.arcadeitalia.net/service_scraper.php?ajax=query_mame&game_name=";
            string parentFolderPath = Path.GetDirectoryName(gamelistManager.XMLFilename);

            // Start to process selected datagridview rows
            for (int i = 0; i < romPaths.Count; i += batchSize)
            {
          
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

                if (jsonResponse == null)
                {
                    AddToLog("Scraper returned no data!");
                    count = count + batchArray.Length;
                    continue;
                }

                // Deserialize the JSON to names and values
                GameListResponse scraperResponse = null;
                try
                {
                    scraperResponse = JsonConvert.DeserializeObject<GameListResponse>(jsonResponse);
                }
                catch (JsonException ex)
                {
                    // Log the exception details
                    AddToLog($"JSON deserialization error: {ex.Message}");
                    count = count + batchArray.Length;
                    continue;
                }

                if (scraperResponse == null)
                {
                    AddToLog("Deserialization failed. The JSON may be invalid or the type is incompatible.");
                    count = count + batchArray.Length;
                    continue;
                }

                // Loop through the returned data and process it
                for (int j = 0; j < batchArray.Length; j++)
                {
                    count++;
                    UpdateProgressBar();
                    UpdateLabel(count, total);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        // Perform cleanup or handle cancellation if needed
                        AddToLog("Scraping canceled by user.");
                        return;
                    }

                    string currentRomPath = batchArray[j];
                    string currentRomName = gamelistManager.ExtractPath(currentRomPath);

                    ScraperArcadeDBGameInfo scraperData = scraperResponse.result[j];

                    AddToLog($"Scraping: {currentRomName}");
                  
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
                        bool result = await DownloadFile(overWriteData, fileToDownload, remoteDownloadURL);
                        // Returns true on success
                        if (result == true)
                        {
                            tableRow[columnName] = $"./{folderName}/{fileName}";
                        }

                    }
                }
            }
        }
        private async Task<bool> DownloadFile(bool overWriteData, string fileToDownload, string url)
        {

            Thread.Sleep(200);

            try
            {
                if (File.Exists(fileToDownload))
                {
                    if (overWriteData)
                    {
                        File.Delete(fileToDownload);
                    }
                    else
                    {
                        return true;
                    }
                }

                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync(new Uri(url), fileToDownload);

                    AddToLog ($"File download OK: {fileToDownload}");
                    return true; // Return true if the download was successful
                }
            }
            catch
            {
                AddToLog($"File download Fail: {fileToDownload}");
                return false; // Return false if an exception occurred during the download
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (Control control in panel_CheckboxGroup.Controls)
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
            foreach (Control control in panel_CheckboxGroup.Controls)
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
            comboBox_Scrapers.SelectedIndex = 0;
        }

        private void AddToLog(string logMessage)
        {
            if (listBoxLog.InvokeRequired)
            {
                listBoxLog.Invoke(new Action(() => listBoxLog.Items.Add($"{DateTime.Now} - {logMessage}")));
                listBoxLog.TopIndex = listBoxLog.Items.Count - 1;
            }
            else
            {
                listBoxLog.Items.Add($"{logMessage}");
                listBoxLog.TopIndex = listBoxLog.Items.Count - 1;
            }
        }

        private void UpdateLabel(int current, int total)
        {
            if (label.InvokeRequired)
            {
                label.Invoke(new Action(() => UpdateLabel(current, total)));
            }
            else
            {
                label.Text = $"{current * 100 / total}%";
            }
        }

        private void UpdateProgressBar()
        {
            if (progressBar_ScrapeProgress.InvokeRequired)
            {
                progressBar_ScrapeProgress.Invoke(new Action(() => progressBar_ScrapeProgress.Value++));
            }
            else
            {
                progressBar_ScrapeProgress.Value++;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_Scrapers.SelectedIndex == 0)
            {
                // ArcadeDB
                List<string> availableScraperElements = new List<string>{
                "name",
                "desc",
                "genre",
                "players",
                "rating",
                "lang",
                "releasedate",
                "publisher",
                "marquee",
                "image",
                "video"
                };

                foreach (Control control in panel_CheckboxGroup.Controls)
                {

                    if (control is System.Windows.Forms.CheckBox checkBox)
                    {
                        string checkboxShortName = control.Name.Replace("checkbox_", "").ToLower();
                        if (availableScraperElements.Contains(checkboxShortName))
                        {
                            checkBox.Enabled = true;
                        }

                        else
                        {
                            checkBox.Enabled = false;
                        }
                    }
                }

            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();
            button_Cancel.Enabled = false;
            AddToLog("Cancelling.....");
        }
    }
}





