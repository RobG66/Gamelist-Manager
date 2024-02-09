using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace GamelistManager
{

    public partial class MediaCheckForm : Form
    {
        string parentFolderPath;
        DataGridView dataGridView1;
        List<MediaListObject> mediaList;
        ConcurrentBag<MediaListObject> badMediaList;
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
            badMediaList = new ConcurrentBag<MediaListObject>();
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

                            string extension = Path.GetExtension(fullPath);

                            // Skip PDF Media
                            if (extension != null && extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            fullPath = CleanFileName(fullPath);

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

        static string CleanFileName(string fileName)
        {
            // Define a regular expression to match illegal characters in filenames
            char[] invalidChars = Path.GetInvalidFileNameChars().Except(new char[] { ':', '\\', '/' }).ToArray();
            string illegalCharactersPattern = "[" + Regex.Escape(new string(invalidChars)) + "]";

            // Replace illegal characters with an empty string
            string cleanedFileName = Regex.Replace(fileName, illegalCharactersPattern, "");

            return cleanedFileName;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            List<MediaListObject> badMediaListAsList = badMediaList.ToList();

            if (radioButton_ExportCSV.Checked)
            {
                string csvFileName = Directory.GetCurrentDirectory() + "\\" + "bad_media.csv";
                if (ExportToCSV(badMediaListAsList, csvFileName))
                {
                    MessageBox.Show($"The file '{csvFileName}' was successfully saved", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                else
                {
                    MessageBox.Show($"There was an error saving file '{csvFileName}'", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            foreach (var mediaObject in badMediaListAsList)
            {
                string fileName = mediaObject.FullPath;
                int rowIndex = mediaObject.RowIndex;
                int columnIndex = mediaObject.ColumnIndex;
                if (radioButton_Delete.Checked)
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch
                    {
                        // Just continue for now
                        continue;
                    }

                if (radioButton_Rename.Checked)
                {
                    string oldFilePath = fileName;
                    string newFileNamePrefix = "bad-";
                    string directory = Path.GetDirectoryName(oldFilePath);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(oldFilePath);
                    string newFilePath = Path.Combine(directory, $"{newFileNamePrefix}{fileNameWithoutExtension}");
                    try
                    {
                        File.Move(oldFilePath, newFilePath);
                    }
                    catch
                    {
                        // Catch exception - if we care?
                    }

                }
                dataGridView1.Rows[rowIndex].Cells[columnIndex].Value = DBNull.Value;
            }
            panel2.Enabled = false;
        }

        static bool ExportToCSV(List<MediaListObject> mediaObjects, string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // Write header
                    writer.WriteLine("FullPath,Status");

                    // Write records
                    foreach (var mediaObject in mediaObjects)
                    {
                        writer.WriteLine($"{mediaObject.FullPath},{mediaObject.Status}");
                    }
                }
                // Return success!
                return true;
            }

            catch
            {
                // Return failure
                return false;
            }

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
            label_MissingCount.Text = "0";
            if (radioButton_Images.Checked)
            {
                label_CorruptCount.Text = "0";
                label_SingleColorCount.Text = "0";
            }
            panel2.Enabled = false;

            cancellationTokenSource = new CancellationTokenSource();

            string mediaType = "image";

            if (radioButton_Videos.Checked)
            {
                mediaType = "video";
            }

            await CheckMedia(mediaType, cancellationTokenSource.Token);

            button_Stop.Enabled = false;
            globalStopwatch.Stop();
            globalStopwatch.Reset();
            button_Start.Enabled = true;

            if (cancellationTokenSource.IsCancellationRequested)
            {
                MessageBox.Show("Media checking operation was canceled.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (badMediaList == null || badMediaList.Count == 0)
            {
                MessageBox.Show("No bad media was found.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private async Task CheckMedia(string mediaType, CancellationToken cancellationToken)
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

            badMediaList = new ConcurrentBag<MediaListObject>();

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

                    string result = null;

                    if (mediaType == "image")
                    {
                        result = ImageChecker.CheckImage(fileName);
                    }
                    else if (mediaType == "video")
                    {
                        result = File.Exists(fileName) ? "ok" : "missing";
                    }

                    switch (result)
                    {
                        case "missing":
                            Interlocked.Increment(ref missingCount);
                            item.Status = "missing";
                            badMediaList.Add(item);
                            break;
                        case "ok":
                            break;
                        case "singlecolor":
                            Interlocked.Increment(ref singleColorCount);
                            item.Status = "singlecolor";
                            badMediaList.Add(item);
                            break;
                        case "corrupt":
                            Interlocked.Increment(ref corruptCount);
                            item.Status = "corrupt";
                            badMediaList.Add(item);
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
                if (radioButton_Images.Checked)
                {
                    label_CorruptCount.Text = corrupt.ToString();
                    label_SingleColorCount.Text = singleColor.ToString();
                }
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
            panel2.Enabled = false;
            label_CorruptCount.Text = "0";
            label_MissingCount.Text = "0";
            label_SingleColorCount.Text = "0";

            Cursor.Current = Cursors.WaitCursor;

            string mediaType = "image";
            if (radioButton_Videos.Checked)
            {
                mediaType = "video";
                label_CorruptCount.Text = "N/A";
                label_SingleColorCount.Text = "N/A";
            }

            mediaList = GetMediaList(mediaType);
            int totalitems = mediaList.Count;
            label2.Text = $"There are {totalitems} {mediaType}s";

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
