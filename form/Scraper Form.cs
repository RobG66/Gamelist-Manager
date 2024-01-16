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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GamelistManager.form
{
    public partial class Scraper : Form
    {
        public Scraper()
        {
            InitializeComponent();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            comboBox1.Enabled = radioButton1.Checked;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                ScrapeArcadeDB();
            }
        }

        private async void ScrapeArcadeDB()
        {
            listBoxLog.Items.Clear();

            DataGridView dgv = ((GamelistManager)this.Owner).MainDataGridView;
            DataGridViewRow[] datagridviewSelectedRows = dgv.SelectedRows.Cast<DataGridViewRow>().ToArray();

            Dictionary<string, string> Metadata = new Dictionary<string, string>();

            // Batocera and Scraper element names don't always align
            // Therefore a dictionary is made to cross reference
            // We can loop through the controls and elements all at once.
            // Checkbox controls are also named for Batocera element names
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

            progressBar1.Value = 0;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = datagridviewSelectedRows.Length;
            progressBar1.Step = 1;

            int count = 0;
            int batchSize = 50;

            string scraperBaseURL = "http://adb.arcadeitalia.net/service_scraper.php?ajax=query_mame&game_name=";
            string parentFolderPath = Path.GetDirectoryName(((GamelistManager)this.Owner).XMLFilename);


            // Start to process selected datagridview rows
            for (int i = 0; i < datagridviewSelectedRows.Length; i += batchSize)
            {
                // Take the next batch of rows
                DataGridViewRow[] batchRows = datagridviewSelectedRows.Skip(i).Take(batchSize).ToArray();

                // Construct a semicolon-separated string of ROM names for the current batch
                string romNames = string.Join(";", batchRows.Select(row => ((GamelistManager)this.Owner).ExtractPath(row.Cells["path"].Value as string)));

                // Construct the scraper URL with the batch of ROM names
                string scraperRequestURL = $"{scraperBaseURL}{romNames}";

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
                for (int j = 0; j < batchRows.Length; j++)
                {
                    count++;
                    progressBar1.PerformStep();

                    DataGridViewRow row = batchRows[j];
                    ScraperArcadeDBGameInfo scraperData = scraperResponse.result[j];

                    string romName = ((GamelistManager)this.Owner).ExtractPath(row.Cells["path"].Value as string);
                    label1.Text = $"Scraping: {romName}";
                    // Loop through the Metadata dictionary
                    foreach (var kvp in Metadata)
                    {
                        string localPropertyName = kvp.Key;
                        string remotePropertyName = kvp.Value;

                        // Examine the checkbox state
                        Control Control = this.panel2.Controls[$"checkbox_{localPropertyName}"];

                        // If checkbox is not checked, we don't process the data
                        if (Control is System.Windows.Forms.CheckBox checkBox && !checkBox.Checked)
                        {
                            continue;
                        }

                        // Get the returned property value from its string name
                        PropertyInfo property = typeof(ScraperArcadeDBGameInfo).GetProperty(remotePropertyName);
                        string scrapedValue = property.GetValue(scraperData)?.ToString();

                        // Get the local datagridview cell value
                        object cellValueObject = row.Cells[localPropertyName]?.Value;
                        string cellValue = null;
                        if (cellValueObject != null && cellValueObject != DBNull.Value)
                        {
                            cellValue = Convert.ToString(cellValueObject);
                        }


                        // Check for empty scrape value
                        if (string.IsNullOrEmpty(scrapedValue))
                        {
                            continue;
                        }

                        if (!remotePropertyName.Contains("url_")) {
                            // Check if we are allowed to overwrite
                            // Write regardless if the local value is empty
                            if ((!checkBox_OverwriteExisting.Checked && string.IsNullOrEmpty(cellValue)) || checkBox_OverwriteExisting.Checked)
                            {
                                row.Cells[localPropertyName].Value = scrapedValue;
                            }
                            continue;
                        }

                        // Image handling
                        string remoteDownloadURL = scrapedValue;

                        if (string.IsNullOrEmpty(remoteDownloadURL)) {
                            continue;
                        }

                        // What are we downloading, a video or image?                                     
                        string fileName = null;
                        string folderName = null;

                        if (localPropertyName == "video")
                        {
                            folderName = "videos";
                            fileName = $"{romName}-video.mp4";
                        }
                        else
                        {
                            folderName = "images";
                            fileName = $"{romName}-{localPropertyName}.png";
                        }

                        string downloadPath = $"{parentFolderPath}\\{folderName}";
                        string fileToDownload = $"{downloadPath}\\{fileName}";
                        bool result = await DownloadFile(fileToDownload, remoteDownloadURL);
                        // Returns true on success
                        if (result == true)
                        {
                            row.Cells[localPropertyName].Value = $"./{folderName}/{fileName}";
                        }

                    }
                }
            }
            }
                private async Task<bool> DownloadFile(string fileToDownload, string url)
        {
            try
            {
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

    }
}

