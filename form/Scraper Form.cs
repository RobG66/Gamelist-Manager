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

        public Scraper()
        {
            InitializeComponent();
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

            GamelistManager gamelistManager = new GamelistManager();
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
            button_Cancel.Enabled = true;
            globalStopwatch.Start();

            // Reset the cancellation token source
            cancellationTokenSource = new CancellationTokenSource();

            // Call the scraper method asynchronously
            //await Task.Run(() => ScrapeArcadeDBAsync(cancellationTokenSource.Token));
            ScrapeArcadeDB scraper = new ScrapeArcadeDB();
            await scraper.ScrapeArcadeDBAsync(overWriteData, elementsToScrape, romPaths, cancellationTokenSource.Token);

            // Cleanup after scraping is complete or canceled
            button_StartStop.Enabled = true;
            label_progress.Text = "0%";
            button_Cancel.Enabled = false;
            globalStopwatch.Stop();

            saveReminder(cancellationTokenSource.Token.IsCancellationRequested);

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
            globalStopwatch.Stop();
            label_progress.Text = "0%";
        }
    }

}


