using GamelistManager.control;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace GamelistManager
{
    public partial class ScraperForm : Form
    {
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken cancellationToken;

        private static Stopwatch globalStopwatch = new Stopwatch();
        public string XMLFilename { get; set; }
        public DataSet dataSet { get; set; }
        public DataGridView dataGridView { get; set; }

        public ScraperForm()
        {
            InitializeComponent();
            cancellationToken = cancellationTokenSource.Token;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_Scrapers.Enabled = RadioButton_ScrapeSelected.Checked;
        }


        public async void Button_Start_Click(object sender, EventArgs e)
        {
            // Make a list of elements to scrape
            List<string> elementsToScrape = new List<string>();
            foreach (Control control in groupBox_checkboxes.Controls)
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

            // Make a list of roms to scrape
            List<string> romPaths = null;
            if (RadioButton_ScrapeAll.Checked)
            {
                romPaths = dataGridView.Rows
                    .Cast<DataGridViewRow>()
                    .Select(row => row.Cells["path"].Value as string)
                    .ToList(); // Convert to List<string>
            }
            else
            {
                romPaths = dataGridView.SelectedRows
                    .Cast<DataGridViewRow>()
                    .Select(row => row.Cells["path"].Value as string)
                    .ToList(); // Convert to List<string>
            }

            // Get overwrite value
            bool overWriteData = checkBox_OverwriteExisting.Checked;

            // Clear Log
            if (listBoxLog.Items.Count > 0)
            {
                listBoxLog.Items.Clear();
            }

            button_StartStop.Enabled = false;
            button_Cancel.Enabled = true;
            globalStopwatch.Reset();
            globalStopwatch.Start();

            if (comboBox_Scrapers.SelectedIndex == 1)
            {
                // Get the system Id
                string parentFolderName = Path.GetFileName(Path.GetDirectoryName(XMLFilename));
                SystemIdResolver resolver = new SystemIdResolver();
                int systemId = resolver.ResolveSystemId(parentFolderName);
                if (systemId == 0)
                {
                    MessageBox.Show("The system could not be found!", "Missing System", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Get ScreenSraper creds
                (string userId, string userPassword) = CredentialManager.GetCredentials("ScreenScraper");
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userPassword))
                {
                    MessageBox.Show("ScreenScraper credentials are not set!", "Missing Credentials", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string parentFolderPath = Path.GetDirectoryName(XMLFilename);
                bool scraperFinished = false;

                try
                {
                    scraperFinished = await ScrapeByScreenScraperAsync(parentFolderPath, systemId.ToString(), userId, userPassword, elementsToScrape, romPaths, overWriteData);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error has occured!\n{ex.Message}", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (scraperFinished)
                {
                    AddToLog("Scraping completed!");
                }
                else
                {
                    AddToLog("Scraping cancelled!");
                    return;
                }

            }

            string elapsedTime = $"{globalStopwatch.Elapsed.TotalMinutes:F0} minutes and {globalStopwatch.Elapsed.Seconds} seconds";
            MessageBox.Show($"Finished scraping {romPaths.Count} items in {elapsedTime}");

            // Cleanup after scraping is complete or canceled
            button_StartStop.Enabled = true;
            label_progress.Text = "0%";
            button_Cancel.Enabled = false;
            globalStopwatch.Stop();

            //SaveReminder(cancellationTokenSource.Token.IsCancellationRequested);
        }

        public async Task<bool> ScrapeByScreenScraperAsync(string folderPath, string systemId, string userId, string userPassword, List<string> elementList, List<string> romList, bool overwrite)
        {
            GamelistManagerForm gamelistManagerForm = new GamelistManagerForm();

            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            progressBar_ScrapeProgress.Value = 0;
            progressBar_ScrapeProgress.Minimum = 0;
            progressBar_ScrapeProgress.Maximum = romList.Count();
            progressBar_ScrapeProgress.Step = 1;
            int count = 1;
            int total = romList.Count;

            string devId = "";
            string devPassword = "";
            string region = "us";
            string language = "en";

            // Set the maximum number of concurrent tasks
            int maxConcurrency = 1; // Adjust the number as needed
            SemaphoreSlim semaphoreSlim = new SemaphoreSlim(maxConcurrency);

            // Create a list to store the tasks
            List<Task> tasks = new List<Task>();

            object dataSetLock = new object();

            try
            {
                foreach (string rom in romList)
                {
                    // Check cancelation token
                    cancellationToken.ThrowIfCancellationRequested();

                    await semaphoreSlim.WaitAsync(); // Wait until there's room to proceed

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            // Remove ./ characters from rom name
                            string romName = gamelistManagerForm.ExtractFileNameWithExtension(rom);

                            ScrapeScreenScraper scraper = new ScrapeScreenScraper();
                            Dictionary<string, string> result = await scraper.ScrapeScreenScraperAsync(userId, userPassword, devId, devPassword, region, language, romName, systemId, folderPath, elementList);

                            if (result == null)
                            {
                                // Handle error, if needed
                                AddToLog($"Error scraping '{romName}'");
                                return;
                            }

                            AddToLog($"Scraped rom '{romName}'");

                            // Update progress
                            Interlocked.Increment(ref count);
                            UpdateProgressBar();
                            UpdateLabel(count, total);

                            // Lock the dataset so we can update it safely
                            lock (dataSetLock)
                            {
                                // Process scraped items
                                foreach (var scrapedItem in result)
                                {
                                    string elementName = scrapedItem.Key;
                                    string elementValue = scrapedItem.Value;

                                    if (string.IsNullOrEmpty(elementValue))
                                    {
                                        continue;
                                    }

                                    // Find the table row we need using rom as the key
                                    DataRow tableRow = dataSet.Tables[0].AsEnumerable()
                                        .FirstOrDefault(row => row.Field<string>("path") == rom);

                                    string cellvalue = tableRow[elementName].ToString();

                                    if ((!string.IsNullOrEmpty(cellvalue) && overwrite) || string.IsNullOrEmpty(cellvalue))
                                    {
                                        tableRow[elementName] = elementValue;
                                    }
                                }
                            }
                        }
                        finally
                        {
                            semaphoreSlim.Release(); // Release the semaphore after the task is complete
                        }
                    }
                    ));
                }

                // Wait for all tasks to complete
                await Task.WhenAll(tasks);
                return true; // Scraper finished successfully
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
                return false; // Scraper was cancelled
            }
        }


        private void Button_SelectAll_Click(object sender, EventArgs e)
        {
            foreach (Control control in groupBox_checkboxes.Controls)
            {
                // Check if the control is a checkbox
                if (control is System.Windows.Forms.CheckBox checkBox && checkBox.Enabled == true)
                {
                    // Set checked
                    checkBox.Checked = true;
                }
            }
        }

        private void Button_SelectNone_Click(object sender, EventArgs e)
        {
            foreach (Control control in groupBox_checkboxes.Controls)
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
            comboBox_Scrapers.SelectedIndex = 1;
        }

        public void AddToLog(string logMessage)
        {
            Action updateListBox = () =>
            {
                listBoxLog.Items.Add($"{logMessage}");
                listBoxLog.TopIndex = listBoxLog.Items.Count - 1;
            };

            if (listBoxLog.InvokeRequired)
            {
                listBoxLog.Invoke(updateListBox);
            }
            else
            {
                updateListBox();
            }
        }

        public void UpdateLabel(int current, int total)
        {
            if (label_progress == null) return; // Add null check

            if (label_progress.InvokeRequired)
            {
                label_progress.Invoke(new Action(() => UpdateLabel(current, total)));
            }
            else
            {
                double progress = (double)current / total * 100;

                TimeSpan elapsed = globalStopwatch.Elapsed;

                // Calculate remaining time based on the moving average of the progress and elapsed time
                double averageProgressRate = elapsed.TotalMilliseconds / progress;
                double remainingMilliseconds = (total - current) * averageProgressRate;
                TimeSpan remainingTime = TimeSpan.FromMilliseconds(remainingMilliseconds);

                // Display only non-zero components
                string remainingTimeString = string.Empty;

                if (remainingTime.Hours > 0)
                {
                    remainingTimeString += $"{remainingTime.Hours:D2}h ";
                }

                if (remainingTime.Minutes > 0)
                {
                    remainingTimeString += $"{remainingTime.Minutes:D2}m ";
                }

                remainingTimeString += $"{remainingTime.Seconds:D2}s";

                label_progress.Text = $"{progress:F0}% | Remaining Time: {remainingTimeString}";
            }
        }

        public void UpdateProgressBar()
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


        private void ComboBox_SelectScraper_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<string> availableScraperElements = new List<string>();
            if (comboBox_Scrapers.SelectedIndex == 0)
            {
                // ArcadeDB
                button_Setup.Enabled = false;
                availableScraperElements = new List<string>{
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
            }

            if (comboBox_Scrapers.SelectedIndex == 1)
            {
                // ScreenScraper
                button_Setup.Enabled = true;
                availableScraperElements = new List<string>{
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
                    "video",
                    "developer",
                    "map",
                    "region",
                    "manual"
                };
            }

            foreach (Control control in groupBox_checkboxes.Controls)
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

        private void Button_Stop_Click(object sender, EventArgs e)
        {
            cancellationTokenSource.Cancel();
            button_Cancel.Enabled = false;
            AddToLog("Cancelling.....");
            globalStopwatch.Stop();
            label_progress.Text = "0%";
        }

        private void button_Setup_Click(object sender, EventArgs e)
        {

            button_StartStop.Enabled = false;
            comboBox_Scrapers.Enabled = false;

            groupBox_checkboxes.Visible = false;

            ScreenScraperSetup userControl = new ScreenScraperSetup();
            panel_small.Controls.Add(userControl);

            userControl.Disposed += ScreenScraperSetup_Disposed;

        }

        private void ScreenScraperSetup_Disposed(object sender, EventArgs e)
        {
            ScreenScraperSetup userControl = new ScreenScraperSetup();

            button_StartStop.Enabled = true;
            comboBox_Scrapers.Enabled = true;

            groupBox_checkboxes.Visible = true;

            userControl.Disposed -= ScreenScraperSetup_Disposed;

        }

    }

}


