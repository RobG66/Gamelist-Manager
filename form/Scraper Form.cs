using GamelistManager.control;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;


namespace GamelistManager
{
    public partial class Scraper : Form
    {
        private CancellationTokenSource cancellationTokenSource;
        private static Stopwatch globalStopwatch = new Stopwatch();
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

        private void SaveReminder(bool canceled)
        {
            string finish = "Scraping Completed!";
            MessageBoxIcon icon = MessageBoxIcon.Information;

            if (canceled)
            {
                finish = "Scraping Was Cancelled!";
                icon = MessageBoxIcon.Error;
            }

            if (!checkBox_Save.Checked)
            {
                MessageBox.Show($"{finish}", "Notice:", MessageBoxButtons.OK, icon);
                return;
            }

            gamelistManager.SaveFile();
        }

        private async void Button_Start_Click(object sender, EventArgs e)
        {
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


            DataGridView dgv = gamelistManager.MainDataGridView;
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
            button_Cancel.Enabled = true;
            globalStopwatch.Reset();
            globalStopwatch.Start();

            // Reset the cancellation token source
            cancellationTokenSource = new CancellationTokenSource();

            // Call the scraper method asynchronously
            if (comboBox_Scrapers.SelectedIndex == 0)
            {
                ScrapeArcadeDB scraper = new ScrapeArcadeDB(gamelistManager, this);
                await scraper.ScrapeArcadeDBAsync(overWriteData, elementsToScrape, romPaths, cancellationTokenSource.Token);
            }
            if (comboBox_Scrapers.SelectedIndex == 1)
            {
                ScrapeScreenScraper scraper = new ScrapeScreenScraper(gamelistManager, this);
                await scraper.ScrapeScreenScraperAsync(overWriteData, elementsToScrape, romPaths, cancellationTokenSource.Token);

            }

            string elapsedTime = $"{globalStopwatch.Elapsed.TotalMinutes:F0} minutes and { globalStopwatch.Elapsed.Seconds } seconds";
            MessageBox.Show($"Finished scraping {romPaths.Count} items in {elapsedTime}");

            // Cleanup after scraping is complete or canceled
            button_StartStop.Enabled = true;
            label_progress.Text = "0%";
            button_Cancel.Enabled = false;
            globalStopwatch.Stop();

            SaveReminder(cancellationTokenSource.Token.IsCancellationRequested);

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
            comboBox_Scrapers.SelectedIndex = 0;
        }

        public void AddToLog(string logMessage)
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

        public void UpdateLabel(int current, int total)
        {
            if (label_progress.InvokeRequired)
            {
                label_progress.Invoke(new Action(() => UpdateLabel(current, total)));
            }
            else
            {
                double progress = (double)current / total * 100;

                // Assuming you have a global Stopwatch declared outside of this method
                TimeSpan elapsed = globalStopwatch.Elapsed;

                // Calculate remaining time based on the percentage completed
                TimeSpan remainingTime = TimeSpan.FromTicks((long)(elapsed.Ticks / (progress / 100)));

                label_progress.Text = $"{progress:F2}% | Remaining Time: {remainingTime.ToString(@"hh\:mm\:ss")}";
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
                button_Setup.Enabled= true;
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
                    "developer"
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
            cancellationTokenSource?.Cancel();
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


