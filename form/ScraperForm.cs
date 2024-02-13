using GamelistManager.control;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GamelistManager
{
    public partial class ScraperForm : Form
    {
        private GamelistManagerForm gamelistManagerForm;
        private ScreenScraperSetup screenScraperSetup;
        private ScraperPreview scraperPreview;
        private CancellationTokenSource cancellationTokenSource;
        public CancellationToken cancellationToken => cancellationTokenSource.Token;
        private static Stopwatch globalStopwatch = new Stopwatch();
        private int scraperCount;
        private bool scraperFinished;
        private int totalCount;
        private int scrapeErrors;

        public ScraperForm(GamelistManagerForm form)
        {
            InitializeComponent();
            screenScraperSetup = new ScreenScraperSetup();
            scraperPreview = new ScraperPreview();
            cancellationTokenSource = new CancellationTokenSource();
            scraperCount = 0;
            totalCount = 0;
            scrapeErrors = 0;
            scraperFinished = false;
            gamelistManagerForm = form;
        }

        private List<string> GetElementsToScrape()
        {
            List<string> elements = new List<string>();
            foreach (Control control in panel_Checkboxes.Controls)
            {
                if (control is CheckBox checkBox && checkBox.Checked && checkBox.Enabled)
                {
                    string elementName = checkBox.Name.Replace("checkbox_", "").ToLower();
                    elements.Add(elementName);
                }
            }
            return elements;
        }

        private List<string> GetRomsToScrape()
        {
            // Make a list of romPaths to scrape
            List<string> romPaths = null;
            if (RadioButton_ScrapeAll.Checked)
            {
                romPaths = gamelistManagerForm.dataGridView.Rows
                    .Cast<DataGridViewRow>()
                    .Select(row => row.Cells["path"].Value as string)
                    .ToList(); // Convert to List<string>
            }
            else
            {
                romPaths = gamelistManagerForm.dataGridView.SelectedRows
                    .Cast<DataGridViewRow>()
                    .Select(row => row.Cells["path"].Value as string)
                    .ToList(); // Convert to List<string>
            }
            return romPaths;
        }

        private (List<string> elements, List<string> romPaths) PreScrapeSetup()
        {
            // Clear Log
            if (listBoxLog.Items.Count > 0)
            {
                listBoxLog.Items.Clear();
            }

            // Form settings
            panel_ScraperOptions.Enabled = false;
            button_StartStop.Enabled = false;
            button_Cancel.Enabled = true;
            button_Setup.Enabled = false;
            globalStopwatch.Reset();
            globalStopwatch.Start();
            panel_Checkboxes.Visible = false;
            panel_small.Controls.Add(scraperPreview);

            // List creation
            List<string> elements = GetElementsToScrape();
            List<string> romPaths = GetRomsToScrape();

            // Global scrape counter variables
            scraperCount = 0;
            totalCount = romPaths.Count;
            scrapeErrors = 0;

            // Reset progressbar
            progressBar_ScrapeProgress.Value = 0;
            progressBar_ScrapeProgress.Minimum = 0;
            progressBar_ScrapeProgress.Step = 1;
            progressBar_ScrapeProgress.Maximum = totalCount;

            // Reset cancellation tokensource
            cancellationTokenSource = new CancellationTokenSource();

            // Return lists
            return (elements, romPaths);
        }


        private void PostScrapeCleanup()
        {
            // Cleanup
            panel_ScraperOptions.Enabled = true;
            button_StartStop.Enabled = true;
            button_Cancel.Enabled = false;
            button_Setup.Enabled = true;
            globalStopwatch.Stop();
            label_progress.Text = "0%";
            panel_Checkboxes.Visible = true;
            panel_small.Controls.Remove(scraperPreview);
        }

        private async void StartScraper()
        {
            (List<string> elements, List<string> romPaths) = PreScrapeSetup();

            if (elements.Count == 0)
            {
                MessageBox.Show("No metadata selection was made", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                PostScrapeCleanup();
                return;
            }

            // Set some variables
            bool overWriteData = checkBox_OverwriteExisting.Checked;
            string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename);
            string parentFolderName = Path.GetFileName(parentFolderPath);
            bool scraperFinished = false;

            // ScreenScraper Specific
            if (comboBox_Scrapers.SelectedIndex == 1)
            {
                // Get the system Id
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

                try
                {
                    scraperFinished = await ScrapeByScreenScraperAsync(parentFolderPath, systemId.ToString(), userId, userPassword, elements, romPaths, overWriteData);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error has occured!\n{ex.Message}", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }

            // ArcadeDB Specific
            if (comboBox_Scrapers.SelectedIndex == 0)
            {
                if (parentFolderName != "mame" && parentFolderName != "fbneo")
                {
                    MessageBox.Show("This doesn't appear to be a gamelist for Mame or FBNeo!\n" +
                   "You cannot scrape this gamelist with ArcadeDB.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    scraperFinished = await ScrapeByArcadeDBAsync(parentFolderPath, elements, romPaths, overWriteData);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error has occured!\n{ex.Message}", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (scraperFinished)
            {
                AddToLog("Scraping completed!");
            }
            else
            {
                AddToLog("Scraping cancelled!");
            }

            SharedData.DataSet.AcceptChanges();

            string elapsedTime = $"{globalStopwatch.Elapsed.TotalMinutes:F0} minutes and {globalStopwatch.Elapsed.Seconds} seconds";
            MessageBox.Show($"Finished scraping {scraperCount} items in {elapsedTime}");

            if (checkBox_Save.Checked)
            {
                SaveReminder();
            }

            PostScrapeCleanup();
        }

        private void SaveReminder()
        {
            gamelistManagerForm.SaveFile(SharedData.XMLFilename);
        }

        public async Task<bool> ScrapeByArcadeDBAsync(
        string folderPath,
        List<string> elementList,
        List<string> romList,
        bool overwrite
        )
        {
            ScrapeArcadeDB scrapeArcadeDB = new ScrapeArcadeDB();

            try
            {
                foreach (string romName in romList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    AddToLog($"Scraping rom '{romName}'");
                    Interlocked.Increment(ref scraperCount);
                    UpdateLabel(scraperCount, totalCount, scrapeErrors);
                    UpdateProgressBar();

                    Dictionary<string, string> result = await scrapeArcadeDB.ScrapeArcadeDBAsync(
                       romName,
                       folderPath,
                       overwrite,
                       elementList
                   );

                    if (result == null)
                    {
                        // Handle error, if needed
                        AddToLog($"Error scraping '{romName}'");
                        Interlocked.Increment(ref scrapeErrors);
                        UpdateProgressBar();
                        UpdateLabel(scraperCount, totalCount, scrapeErrors);
                        continue;
                    }

                    string marquee = result["marquee"];
                    string screenshot = result["image"];
                    if (!string.IsNullOrEmpty(marquee))
                    {
                        string imageFileName1 = Path.GetFileName(marquee);
                        string imageFileName2 = Path.GetFileName(screenshot);
                        string imagePath1 = $"{folderPath}\\images\\{imageFileName1}";
                        string imagePath2 = $"{folderPath}\\images\\{imageFileName2}";
                        this.Invoke((MethodInvoker)delegate
                        {
                            // Assuming this control has access to UpdatePictureBox method
                            scraperPreview.UpdatePictureBox(imagePath1, imagePath2);
                        });
                    }

                    lock (SharedData.DatasetLock)
                    {
                        DataRow tableRow = SharedData.DataSet.Tables[0].AsEnumerable()
                         .FirstOrDefault(r => r.Field<string>("path") == romName);
                        // Process scraped items
                        foreach (var scrapedItem in result)
                        {
                            string elementName = scrapedItem.Key;
                            string elementValue = scrapedItem.Value;
                            if (string.IsNullOrEmpty(elementValue))
                            {
                                continue;
                            }

                            string cellvalue = tableRow[elementName].ToString();

                            if ((!string.IsNullOrEmpty(cellvalue) && overwrite) || string.IsNullOrEmpty(cellvalue))
                            {
                                tableRow[elementName] = elementValue;
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
                return false; // Scraper was cancelled
            }
            return true;
        }
    

        public async Task<bool> ScrapeByScreenScraperAsync(
        string folderPath,
        string systemId,
        string userId,
        string userPassword,
        List<string> elementList,
        List<string> romList,
        bool overwrite
        )
        {
            string boxSource = RegistryManager.ReadRegistryValue("BoxSource");
            string imageSource = RegistryManager.ReadRegistryValue("ImageSource");
            string logoSource = RegistryManager.ReadRegistryValue("LogoSource");
            string region = RegistryManager.ReadRegistryValue("Region");
            string language = RegistryManager.ReadRegistryValue("language");

            string devId = "";
            string devPassword = "";

            // Set the maximum number of concurrent tasks
            string maxThreadsValue = RegistryManager.ReadRegistryValue("MaxThreads");
            int maxC = 1;
            if (!string.IsNullOrEmpty(maxThreadsValue))
            {
                maxC = int.Parse(maxThreadsValue);
            }

            int maxConcurrency = maxC; // Adjust the number as needed
            SemaphoreSlim semaphoreSlim = new SemaphoreSlim(maxConcurrency);

            // Create a list to store the tasks
            List<Task> tasks = new List<Task>();

            try
            {
                foreach (string romName in romList)
                {
                    // Check cancelation token
                    await semaphoreSlim.WaitAsync(); // Wait until there's room to proceed
                    cancellationToken.ThrowIfCancellationRequested();

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            AddToLog($"Scraping rom '{romName}'");
                            Interlocked.Increment(ref scraperCount);
                            UpdateLabel(scraperCount, totalCount, scrapeErrors);
                            UpdateProgressBar();

                            ScrapeScreenScraper scraper = new ScrapeScreenScraper();
                            Dictionary<string, string> result = await scraper.ScrapeScreenScraperAsync(
                               userId,
                               userPassword,
                               devId,
                               devPassword,
                               region,
                               language,
                               romName,
                               systemId,
                               folderPath,
                               overwrite,
                               elementList,
                               boxSource,
                               imageSource,
                               logoSource
                           );

                            if (result == null)
                            {
                                // Handle error, if needed
                                AddToLog($"Error scraping '{romName}'");
                                Interlocked.Increment(ref scrapeErrors);
                                UpdateProgressBar();
                                UpdateLabel(scraperCount, totalCount, scrapeErrors);
                                return;
                            }

                            string marquee = result["marquee"];
                            string screenshot = result["image"];
                            if (!string.IsNullOrEmpty(marquee))
                            {
                                string imageFileName1 = Path.GetFileName(marquee);
                                string imageFileName2 = Path.GetFileName(screenshot);
                                string imagePath1 = $"{folderPath}\\images\\{imageFileName1}";
                                string imagePath2 = $"{folderPath}\\images\\{imageFileName2}";
                                this.Invoke((MethodInvoker)delegate
                                {
                                    // Assuming this control has access to UpdatePictureBox method
                                    scraperPreview.UpdatePictureBox(imagePath1, imagePath2);
                                });
                            }

                            lock (SharedData.DatasetLock)
                            {
                                DataRow tableRow = SharedData.DataSet.Tables[0].AsEnumerable()
                                 .FirstOrDefault(r => r.Field<string>("path") == romName);
                                // Process scraped items
                                foreach (var scrapedItem in result)
                                {
                                    string elementName = scrapedItem.Key;
                                    string elementValue = scrapedItem.Value;
                                    if (string.IsNullOrEmpty(elementValue))
                                    {
                                        continue;
                                    }

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
            foreach (Control control in panel_Checkboxes.Controls)
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
            foreach (Control control in panel_Checkboxes.Controls)
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

            (string userName, string userPassword) = CredentialManager.GetCredentials("ScreenScraper");
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPassword))
            {
                button_StartStop.Enabled = false;
            }

            string romPath = Path.GetFileName(Path.GetDirectoryName(SharedData.XMLFilename));
            System.Drawing.Image image = (Bitmap)Properties.Resources.ResourceManager.GetObject(romPath);
            //Image image = LoadImageFromResource(romPath);

            if (image is System.Drawing.Image)
            {
                pictureBox1.Image = image;
            }

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

        private DateTime startTime = DateTime.Now;

        public void UpdateLabel(int current, int total, int errors)
        {
            if (label_progress == null || label_Counts == null) return;

            if (label_progress.InvokeRequired || label_Counts.InvokeRequired)
            {
                label_progress.Invoke(new Action(() => UpdateLabel(current, total, errors)));
            }
            else
            {
                double progress = (double)current / total * 100;

                TimeSpan elapsed = DateTime.Now - startTime;

                double remainingMilliseconds = (elapsed.TotalMilliseconds / progress) * (100 - progress);
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

                // Update the label_Counts with the current count and totalCount count
                label_Counts.Text = $"Count:{current}/{total} | Errors:{errors}";
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
                    "manual",
                    "thumbnail"
                };
            }

            foreach (Control control in panel_Checkboxes.Controls)
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
            AddToLog("Cancelling scraper task......");
            globalStopwatch.Stop();
            label_progress.Text = "0%";
        }

        private void button_Setup_Click(object sender, EventArgs e)
        {
            button_StartStop.Enabled = false;
            button_Setup.Enabled = false;
            panel_ScraperOptions.Enabled = false;

            panel_Checkboxes.Visible = false;

            ScreenScraperSetup userControl = new ScreenScraperSetup();
            panel_small.Controls.Add(userControl);


            userControl.Disposed += ScreenScraperSetup_Disposed;
        }

        private void ScreenScraperSetup_Disposed(object sender, EventArgs e)
        {
            ScreenScraperSetup userControl = new ScreenScraperSetup();

            panel_ScraperOptions.Enabled = true;
            button_StartStop.Enabled = true;
            button_Setup.Enabled = true;
            panel_Checkboxes.Visible = true;

            userControl.Disposed -= ScreenScraperSetup_Disposed;

        }

        private void button_StartStop_Click(object sender, EventArgs e)
        {
            StartScraper();
        }
    }

}


