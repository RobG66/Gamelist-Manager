using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace GamelistManager
{

    public partial class MediaCheckForm : Form
    {
        GamelistManagerForm gamelistManagerForm;
        string parentFolderPath;
        DataGridView dataGridView1;
        List<MediaListObject> mediaList;
        private static Stopwatch globalStopwatch = new Stopwatch();
        int missingCount;
        int corruptCount;
        int singleColorCount;

        public MediaCheckForm(string path, DataGridView dgv)
        {
            InitializeComponent();
            dataGridView1 = dgv;
            parentFolderPath = path;
            mediaList = new List<MediaListObject>();
            singleColorCount = 0;
            corruptCount = 0;
            missingCount = 0;
        }

        private List<MediaListObject> GetMediaList(string mediaType)
        {
            mediaList = new List<MediaListObject>();
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                // Check if the column tag is set to "image"
                if (column.Tag != null && column.Tag.ToString() == mediaType)
                {
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        // Access the cell in the specified column
                        DataGridViewCell cell = row.Cells[column.Index];

                        string cellPathValue = cell.Value?.ToString();
                        if (!string.IsNullOrEmpty(cellPathValue))
                        {
                            // Build file list
                            string fullPath = Path.Combine(parentFolderPath, cellPathValue.Replace("./", "").Replace("/", Path.DirectorySeparatorChar.ToString()));

                            mediaList.Add(new MediaListObject
                            {
                                FullPath = fullPath,
                                RowIndex = row.Index,
                                ColumnIndex = column.Index,
                                Status = string.Empty
                            });
                        }
                    }
                }
            }
            return mediaList;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MediaCheckForm_Load(object sender, EventArgs e)
        {
            radioButton_Images.Checked = true;
        }

        private CancellationTokenSource cancellationTokenSource;

        private async void button_Start_Click(object sender, EventArgs e)
        {
            // Start button clicked
            button_Start.Enabled = false;
            button_Stop.Enabled = true;
            globalStopwatch.Reset();
            globalStopwatch.Start();
            label_CorruptCount.Text = "0";
            label_MissingCount.Text = "0";
            label_SingleColorCount.Text = "0";
            panel2.Enabled = false;

            cancellationTokenSource = new CancellationTokenSource();
          
            await CheckMedia(cancellationTokenSource.Token);

            button_Stop.Enabled = false;
            globalStopwatch.Stop();
            globalStopwatch.Reset();
            button_Start.Enabled = true;

            if (cancellationTokenSource.IsCancellationRequested)
            {
                MessageBox.Show("Image checking operation was canceled.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            panel2.Enabled = true;
            
        }

        private void StopCheckingMedia()
        {
            if (cancellationTokenSource != null)
            {
                // Cancel the operation if it's still running
                cancellationTokenSource.Cancel();
            }
        }

        private async Task CheckMedia(CancellationToken cancellationToken)
        {
            int totalFiles = mediaList.Count;
            int count = 0;

            progressBar1.Minimum = 0;
            progressBar1.Maximum = totalFiles;
            progressBar1.Step = 1;
            progressBar1.Value = 0;

            singleColorCount = 0;
            corruptCount = 0;
            missingCount = 0;

            await Task.Run(() =>
            {
                Parallel.ForEach(mediaList, (item, parallelLoopState) =>
                {
                    // Check if cancellation is requested inside the loop
                    if (cancellationToken.IsCancellationRequested)
                    {
                        parallelLoopState.Break();
                        return;
                    }

                    string fileName = item.FullPath;
                    int rowIndex = item.RowIndex;
                    int columnIndex = item.ColumnIndex;

                    string result = ImageChecker.CheckImage(fileName);
                    //string result = ImageChecker.CheckImage(fileName);

                    switch (result)
                    {
                        case "missing":
                            Interlocked.Increment(ref missingCount);
                            item.Status = "missing";
                            break;
                        case "ok":
                            break;
                        case "singlecolor":
                            Interlocked.Increment(ref singleColorCount);
                            item.Status = "singlecolor";
                            break;
                        case "corrupt":
                            Interlocked.Increment(ref corruptCount);
                            item.Status = "corrupt";
                            break;
                    }
                    Interlocked.Increment(ref count);
                    UpdateLabels(count, totalFiles, missingCount, corruptCount, singleColorCount);
                    UpdateProgressBar();
                });
            });

            return;
        }

        public void UpdateLabels(int current, int total, int missing, int corrupt, int singleColor)
        {
            if (label_progress.InvokeRequired)
            {
                label_progress.Invoke(new Action(() => UpdateLabels(current, total, missing, corrupt, singleColor)));
            }
            else
            {
                double progress = (double)current / total * 100;

                // Assuming you have a global Stopwatch declared outside of this method
                TimeSpan elapsed = globalStopwatch.Elapsed;

                // Calculate remaining time based on the progress percentage
                TimeSpan remainingTime = TimeSpan.FromTicks((long)(elapsed.Ticks * (1 - (progress / 100)) / (progress / 100)));

                string remainingTimeString = "";

                if (remainingTime.Hours > 0)
                {
                    remainingTimeString += $"{remainingTime.Hours}h ";
                }

                if (remainingTime.Minutes > 0 || remainingTime.Hours > 0)
                {
                    remainingTimeString += $"{remainingTime.Minutes}m ";
                }

                remainingTimeString += $"{remainingTime.Seconds}s";

                label_progress.Text = $"{progress:F0}%"; // | Remaining Time: {remainingTimeString}";

                label_MissingCount.Text = missing.ToString();
                label_CorruptCount.Text = corrupt.ToString();
                label_SingleColorCount.Text = singleColor.ToString();
            }
        }

        private void UpdateProgressBar()
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new MethodInvoker(delegate { UpdateProgressBar(); }));
            }
            else
            {
                progressBar1.PerformStep();
            }
        }
        private void radioButton_Images_CheckedChanged(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;

            string mediaType = "image";
            if (radioButton_Videos.Checked)
            {
                mediaType = "video";
            }

            mediaList = GetMediaList(mediaType);
            int totalitems = mediaList.Count;
            label2.Text = $"There are {totalitems} {mediaType}s in this gamelist";

            Cursor.Current = Cursors.Default;
        }

        private void MediaCheckForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopCheckingMedia();
        }

        private void MediaCheckForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.FormClosing -= MediaCheckForm_FormClosing;
        }

        private void button_Stop_Click(object sender, EventArgs e)
        {
            StopCheckingMedia();
        }
    }
}
