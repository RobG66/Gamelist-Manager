using System;
using System.Collections.Concurrent;
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

    public partial class MediaCheckForm : Form
    {
        string parentFolderPath;
        List<MediaListObject> mediaList;
        ConcurrentBag<MediaListObject> badMediaList;
        private static Stopwatch globalStopwatch = new Stopwatch();
        private int missingVideoCount;
        private int missingImageCount;
        private int corruptImageCount;
        private int singleColorImageCount;
        private int totalMediaCount;
        private int currentCount;
        private int unusedMediaCount;
        private CancellationTokenSource cancellationTokenSource;
        public CancellationToken cancellationToken => cancellationTokenSource.Token;

        public MediaCheckForm()
        {
            InitializeComponent();
            parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename);
            mediaList = new List<MediaListObject>();
            badMediaList = new ConcurrentBag<MediaListObject>();
            singleColorImageCount = 0;
            corruptImageCount = 0;
            missingImageCount = 0;
            missingVideoCount = 0;
            totalMediaCount = 0;
            currentCount = 0;
            unusedMediaCount = 0;
            cancellationTokenSource = new CancellationTokenSource();
        }

        private List<MediaListObject> GetMediaList()
        {
            mediaList = new List<MediaListObject>();

            foreach (DataRow row in SharedData.DataSet.Tables["game"].Rows)
            {
                int rowIndex = row.Table.Rows.IndexOf(row);

                for (int i = 0; i < SharedData.DataSet.Tables["game"].Columns.Count; i++)
                {
                    string columnName = SharedData.DataSet.Tables["game"].Columns[i].ColumnName;
                    int columnIndex = SharedData.DataSet.Tables["game"].Columns.IndexOf(columnName);
                    if (Array.IndexOf(SharedData.MediaTypes, columnName) != -1)
                    {
                        string cellPathValue = row[i].ToString();
                        if (!string.IsNullOrEmpty(cellPathValue))
                        {
                            // Build file list
                            string fullPath = Path.Combine(parentFolderPath, cellPathValue.Replace("./", "").Replace("/", Path.DirectorySeparatorChar.ToString()));
                            string extension = Path.GetExtension(fullPath);

                            // Skip PDF Media
                            // Will revisit this later
                            if (extension != null && extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            // Validate the path is valid before adding to list
                            char[] invalidChars = Path.GetInvalidFileNameChars();
                            bool badPath = fullPath.IndexOfAny(invalidChars) == -1;

                            if (badPath)
                            {
                                continue;
                            }

                            mediaList.Add(new MediaListObject
                            {
                                FullPath = fullPath,
                                Type = (columnName == "video") ? "video" : "image",
                                RowIndex = rowIndex,
                                ColumnIndex = columnIndex,
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
            List<MediaListObject> badMediaListAsList = badMediaList.ToList();

            this.Cursor = Cursors.WaitCursor;

            if (radioButtonExportCSV.Checked)
            {
                string csvFileName = Directory.GetCurrentDirectory() + "\\" + "bad_media.csv";
                if (ExportToCSV(badMediaListAsList, csvFileName))
                {
                    MessageBox.Show($"The file '{csvFileName}' was successfully saved", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Cursor = Cursors.Default;
                    return;
                }
                else
                {
                    MessageBox.Show($"There was an error saving file '{csvFileName}'", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Cursor = Cursors.Default;
                    return;
                }
            }

            string system = Path.GetFileName(parentFolderPath);
            string currentDir = Directory.GetCurrentDirectory();
            string imageBackupDir = Path.Combine(currentDir, "backup", system, "images");
            string videobackupDir = Path.Combine(currentDir, "backup", system, "videos");
            if (!Directory.Exists(imageBackupDir))
            {
                Directory.CreateDirectory(imageBackupDir);
                Directory.CreateDirectory(videobackupDir);
            }

            foreach (var mediaObject in badMediaList)
            {
                string fileName = mediaObject.FullPath;
                string mediaType = mediaObject.Type;
                int rowIndex = mediaObject.RowIndex;
                int columnIndex = mediaObject.ColumnIndex;
                if (radioButtonDelete.Checked)
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch
                    {
                        //nothing?
                    }
                }
                if (radioButtonMove.Checked)
                {
                    string destinationDirectory = imageBackupDir;
                    if (mediaType == "video")
                    {
                        destinationDirectory = videobackupDir;
                    }
                    string shortname = Path.GetFileName(fileName);
                    string newFilePath = Path.Combine(destinationDirectory, shortname);
                    try
                    {
                        File.Move(fileName, newFilePath);
                    }
                    catch
                    {
                        // Catch exception - if we care?
                    }

                }
                if (rowIndex == -1 || columnIndex == -1)
                {
                    continue;
                }

                SharedData.DataSet.Tables["game"].Rows[rowIndex][columnIndex] = DBNull.Value;

            }
            GetMediaCount();
            panelManageMedia.Enabled = false;
            SharedData.IsDataChanged = true;

            this.Cursor = Cursors.Default;

            MessageBox.Show("Cleanup Completed! Don't forget to save these changes.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;

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
            panelCheckMedia.Enabled = false;
            Cursor.Current = Cursors.WaitCursor;
            GetMediaCount();
            Cursor.Current = Cursors.Default;
            panelCheckMedia.Enabled = true;
        }


        private async void button_Start_Click(object sender, EventArgs e)
        {
            if (listBoxLog.Items.Count > 0)
            {
                listBoxLog.Items.Clear();
            }

            singleColorImageCount = 0;
            corruptImageCount = 0;
            missingImageCount = 0;
            missingVideoCount = 0;
            totalMediaCount = 0;
            currentCount = 0;
            unusedMediaCount = 0;

            buttonStart.Enabled = false;
            buttonStop.Enabled = true;
            globalStopwatch.Reset();
            globalStopwatch.Start();
            labelMissingImageCount.Text = "0";
            labelCorruptImageCount.Text = "0";
            labelSingleColorImageCount.Text = "0";
            labelMissingVideosCount.Text = "0";

            panelManageMedia.Enabled = false;

            cancellationTokenSource = new CancellationTokenSource();
            this.Cursor = Cursors.WaitCursor;
            try
            {
                // Asynchronously check media with cancellation support
                await CheckMedia();
            }
            catch (OperationCanceledException)
            {
                // The operation was canceled
                AddToLog("Media check was cancelled!");
                MessageBox.Show("Media checking operation was canceled.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Cursor = Cursors.Default;
                return;
            }
            finally
            {
                // Enable UI elements and reset the stopwatch
                buttonStop.Enabled = false;
                globalStopwatch.Stop();
                globalStopwatch.Reset();
                buttonStart.Enabled = true;
            }

            this.Cursor = Cursors.Default;

            if (badMediaList == null || badMediaList.Count == 0)
            {
                MessageBox.Show("No issues were found.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            panelManageMedia.Enabled = true;
        }

        private void StopCheckingMedia()
        {
            // Cancel the operation if it's still running
            cancellationTokenSource?.Cancel();
        }

        private async Task CheckMedia()
        {

            badMediaList = new ConcurrentBag<MediaListObject>();

            totalMediaCount = mediaList.Count;
            currentCount = 0;

            progressBar1.Minimum = 0;
            progressBar1.Maximum = totalMediaCount;
            progressBar1.Step = 1;
            progressBar1.Value = 0;
            labelProgress.Text = "0%";
            singleColorImageCount = 0;
            corruptImageCount = 0;
            missingImageCount = 0;
            missingVideoCount = 0;
            unusedMediaCount = 0;
            string[] currentImages = Directory.GetFiles($"{parentFolderPath}\\images");
            string[] currentVideos = Directory.GetFiles($"{parentFolderPath}\\videos");
            string[] allMedia = currentImages.Concat(currentVideos).ToArray();
            string[] usedMedia = mediaList.Select(media => media.FullPath).ToArray();

            foreach (string mediaItem in allMedia)
            {
                if (!usedMedia.Contains(mediaItem))
                {
                    unusedMediaCount++;
                    labelUnusedMediaCount.Text = unusedMediaCount.ToString();
                    string shortName = Path.GetFileName(mediaItem);
                    AddToLog($"Unused: {shortName}");
                    MediaListObject unusedItem = new MediaListObject();
                    unusedItem.FullPath = mediaItem;
                    unusedItem.Status = "unused";
                    unusedItem.RowIndex = -1;
                    unusedItem.ColumnIndex = -1;
                    badMediaList.Add(unusedItem);
                }
            }

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
                    string mediaType = item.Type;
                    string shortFileName = Path.GetFileName(fileName);

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
                            if (mediaType == "image")
                            {
                                Interlocked.Increment(ref missingImageCount);
                            }
                            else
                            {
                                Interlocked.Increment(ref missingVideoCount);
                            }
                            item.Status = "missing";
                            badMediaList.Add(item);
                            AddToLog($"Missing {mediaType}: {shortFileName}");
                            break;
                        case "ok":
                            break;
                        case "singlecolor":
                            Interlocked.Increment(ref singleColorImageCount);
                            item.Status = "singlecolor";
                            badMediaList.Add(item);
                            AddToLog($"Single Color Image: {shortFileName}");
                            break;
                        case "corrupt":
                            Interlocked.Increment(ref corruptImageCount);
                            item.Status = "corrupt";
                            badMediaList.Add(item);
                            AddToLog($"Corrupt Image: {shortFileName}");
                            break;
                    }

                    Interlocked.Increment(ref currentCount);
                    UpdateLabels();
                    UpdateProgressBar();
                });
            });
            return;
        }

        public void UpdateLabels()
        {
            if (labelProgress.InvokeRequired)
            {
                labelProgress.Invoke(new Action(() => UpdateLabels()));
            }
            else
            {
                double progress = (double)currentCount / totalMediaCount * 100;

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

                labelProgress.Text = $"{progress:F0}%"; // | Remaining Time: {remainingTimeString}";

                labelMissingImageCount.Text = missingImageCount.ToString();
                labelCorruptImageCount.Text = corruptImageCount.ToString();
                labelSingleColorImageCount.Text = singleColorImageCount.ToString();
                labelMissingVideosCount.Text = missingVideoCount.ToString();
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

        private void GetMediaCount()
        {
            panelManageMedia.Enabled = false;
            labelCorruptImageCount.Text = "0";
            labelMissingImageCount.Text = "0";
            labelSingleColorImageCount.Text = "0";

            mediaList = GetMediaList();

            int image = mediaList.Count(media => media.Type == "image");
            int video = mediaList.Count(media => media.Type == "video");

            labelTotalImagesCount.Text = image.ToString();
            labelTotalVideosCount.Text = video.ToString();
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

    }
}
