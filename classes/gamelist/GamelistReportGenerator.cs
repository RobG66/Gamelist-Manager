using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GamelistManager.classes.gamelist
{
    public class GamelistStatistics
    {
        public string SystemName { get; set; } = string.Empty;
        public string GamelistPath { get; set; } = string.Empty;
        public int TotalGames { get; set; }
        public int HiddenGames { get; set; }
        public int VisibleGames { get; set; }

        // Media counts and sizes
        public int ImageCount { get; set; }
        public long ImageSize { get; set; }
        public int VideoCount { get; set; }
        public long VideoSize { get; set; }
        public int ManualCount { get; set; }
        public long ManualSize { get; set; }
        public int MarqueeCount { get; set; }
        public long MarqueeSize { get; set; }
        public int ThumbnailCount { get; set; }
        public long ThumbnailSize { get; set; }
        public int WheelCount { get; set; }
        public long WheelSize { get; set; }
        public int BoxBackCount { get; set; }
        public long BoxBackSize { get; set; }
        public int BoxArtCount { get; set; }
        public long BoxArtSize { get; set; }
        public int FanArtCount { get; set; }
        public long FanArtSize { get; set; }
        public int MapCount { get; set; }
        public long MapSize { get; set; }
        public int BezelCount { get; set; }
        public long BezelSize { get; set; }
        public int CartridgeCount { get; set; }
        public long CartridgeSize { get; set; }
        public int TitleshotCount { get; set; }
        public long TitleshotSize { get; set; }
        public int MusicCount { get; set; }
        public long MusicSize { get; set; }
        public int MagazineCount { get; set; }
        public long MagazineSize { get; set; }
        public int MixCount { get; set; }
        public long MixSize { get; set; }

        public long RomFolderSize { get; set; }
        public long TotalMediaSize { get; set; }
        public long ActualRomSize { get; set; }
        public bool LoadError { get; set; }
    }

    public static partial class GamelistReportGenerator
    {
        // Main entry point - call this method with a list of gamelist file paths
        public static async Task GenerateReportAsync(List<string> gamelistPaths, bool includeStorage = true, bool ignoreDuplicates = true)
        {
            // Check if list is empty or null
            if (gamelistPaths == null || gamelistPaths.Count == 0)
            {
                MessageBox.Show(
                    "No gamelist paths found. Please ensure systems are loaded with valid gamelist paths.",
                    "No Data",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            var cts = new CancellationTokenSource();
            var progress = new Progress<int>();
            var progressWindow = ShowProgressWindow(gamelistPaths.Count, progress, cts);

            try
            {
                var allStats = await Task.Run(() =>
                {
                    int completed = 0;
                    var results = new System.Collections.Concurrent.ConcurrentBag<GamelistStatistics>();

                    try
                    {
                        // Process all gamelists with better cancellation support
                        Parallel.ForEach(gamelistPaths,
                            new ParallelOptions
                            {
                                MaxDegreeOfParallelism = Environment.ProcessorCount,
                                CancellationToken = cts.Token
                            },
                            gamelistPath =>
                            {
                                cts.Token.ThrowIfCancellationRequested();

                                GamelistStatistics? stat = null;

                                if (!string.IsNullOrWhiteSpace(gamelistPath))
                                {
                                    if (!File.Exists(gamelistPath))
                                    {
                                        string systemName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(gamelistPath) ?? "Unknown");
                                        stat = new GamelistStatistics
                                        {
                                            SystemName = systemName,
                                            GamelistPath = gamelistPath,
                                            LoadError = true
                                        };
                                    }
                                    else
                                    {
                                        string systemName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(gamelistPath) ?? "Unknown");
                                        stat = AnalyzeGamelist(gamelistPath, systemName, includeStorage, ignoreDuplicates);
                                    }

                                    if (stat != null)
                                        results.Add(stat);
                                }

                                // Update progress
                                int current = Interlocked.Increment(ref completed);
                                ((IProgress<int>)progress).Report(current);
                            });

                        return results.OrderBy(s => s.SystemName, StringComparer.OrdinalIgnoreCase).ToList();
                    }
                    catch (OperationCanceledException)
                    {
                        return new List<GamelistStatistics>();
                    }
                }, cts.Token);

                // Close progress window
                progressWindow.Close();

                // Check if cancelled
                if (cts.Token.IsCancellationRequested || allStats.Count == 0)
                {
                    MessageBox.Show(
                        "Report generation cancelled.",
                        "Cancelled",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    return;
                }

                // Generate report text
                string reportText = BuildReportText(allStats, includeStorage);

                // Show report in window with option to save or open in notepad
                ShowReportWindow(reportText, includeStorage);
            }
            catch (OperationCanceledException)
            {
                progressWindow.Close();
                MessageBox.Show(
                    "Report generation cancelled.",
                    "Cancelled",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                MessageBox.Show(
                    $"Error generating report: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private static Window ShowProgressWindow(int totalItems, IProgress<int> progress, CancellationTokenSource cts)
        {
            var window = new Window
            {
                Title = "Generating Report",
                Width = 350,
                Height = 170,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var textBlock = new TextBlock
            {
                Text = "Analyzing gamelists, please wait...",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };
            Grid.SetRow(textBlock, 0);
            grid.Children.Add(textBlock);

            var progressBar = new ProgressBar
            {
                Height = 25,
                Minimum = 0,
                Maximum = totalItems,
                Value = 0,
                Margin = new Thickness(10, 5, 10, 5)
            };
            Grid.SetRow(progressBar, 1);
            grid.Children.Add(progressBar);

            var statusText = new TextBlock
            {
                Text = $"0 / {totalItems}",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 5)
            };
            Grid.SetRow(statusText, 2);
            grid.Children.Add(statusText);

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 20,
                Margin = new Thickness(10, 10, 10, 15),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            try
            {
                cancelButton.Style = (Style)Application.Current.FindResource("GreyRoundedButton");
            }
            catch { }

            Grid.SetRow(cancelButton, 3);
            cancelButton.Click += (s, e) =>
            {
                cts.Cancel();
                cancelButton.IsEnabled = false;
                textBlock.Text = "Cancelling...";
            };
            grid.Children.Add(cancelButton);

            // Update progress bar when progress is reported
            ((Progress<int>)progress).ProgressChanged += (s, value) =>
            {
                window.Dispatcher.Invoke(() =>
                {
                    progressBar.Value = value;
                    statusText.Text = $"{value} / {totalItems}";
                });
            };

            window.Content = grid;
            window.Show();

            return window;
        }

        private static GamelistStatistics AnalyzeGamelist(string gamelistPath, string systemName, bool includeStorage, bool ignoreDuplicates)
        {
            var stats = new GamelistStatistics
            {
                SystemName = systemName,
                GamelistPath = gamelistPath
            };

            try
            {
                DataSet? dataSet = GamelistLoader.LoadGamelist(gamelistPath, ignoreDuplicates);

                if (dataSet?.Tables["game"] is not DataTable gameTable)
                {
                    stats.LoadError = true;
                    return stats;
                }

                stats.TotalGames = gameTable.Rows.Count;


                string baseDir = System.IO.Path.GetDirectoryName(gamelistPath) ?? string.Empty;

                var allMediaTypes = GamelistMetaData.GetMetaDataDictionary().Values
                    .Where(m => m.DataType == MetaDataType.Image ||
                               m.DataType == MetaDataType.Video ||
                               m.DataType == MetaDataType.Music ||
                               m.DataType == MetaDataType.Document)
                    .ToList();

                string hiddenColName = GamelistMetaData.GetMetadataNameByType("hidden");
                bool hasHiddenCol = !string.IsNullOrEmpty(hiddenColName) && gameTable.Columns.Contains(hiddenColName);

                var allMediaPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (DataRow row in gameTable.Rows)
                {
                    if (hasHiddenCol && row[hiddenColName] != DBNull.Value && Convert.ToBoolean(row[hiddenColName]))
                    {
                        stats.HiddenGames++;
                    }

                    foreach (var mediaType in allMediaTypes)
                    {
                        string colName = mediaType.Name;
                        if (!gameTable.Columns.Contains(colName) || !IsMediaPresent(row[colName]))
                            continue;

                        long size = 0;

                        if (includeStorage)
                        {
                            string mediaPath = ResolveMediaPath(row[colName].ToString()!, baseDir);

                            if (!string.IsNullOrEmpty(mediaPath) && File.Exists(mediaPath))
                            {
                                try
                                {
                                    size = new FileInfo(mediaPath).Length;
                                    allMediaPaths.Add(mediaPath);
                                }
                                catch { }
                            }
                        }

                        switch (mediaType.Key)
                        {
                            case MetaDataKeys.image:
                                stats.ImageCount++;
                                stats.ImageSize += size;
                                break;
                            case MetaDataKeys.video:
                                stats.VideoCount++;
                                stats.VideoSize += size;
                                break;
                            case MetaDataKeys.manual:
                                stats.ManualCount++;
                                stats.ManualSize += size;
                                break;
                            case MetaDataKeys.marquee:
                                stats.MarqueeCount++;
                                stats.MarqueeSize += size;
                                break;
                            case MetaDataKeys.thumbnail:
                                stats.ThumbnailCount++;
                                stats.ThumbnailSize += size;
                                break;
                            case MetaDataKeys.wheel:
                                stats.WheelCount++;
                                stats.WheelSize += size;
                                break;
                            case MetaDataKeys.boxback:
                                stats.BoxBackCount++;
                                stats.BoxBackSize += size;
                                break;
                            case MetaDataKeys.boxart:
                                stats.BoxArtCount++;
                                stats.BoxArtSize += size;
                                break;
                            case MetaDataKeys.fanart:
                                stats.FanArtCount++;
                                stats.FanArtSize += size;
                                break;
                            case MetaDataKeys.map:
                                stats.MapCount++;
                                stats.MapSize += size;
                                break;
                            case MetaDataKeys.bezel:
                                stats.BezelCount++;
                                stats.BezelSize += size;
                                break;
                            case MetaDataKeys.cartridge:
                                stats.CartridgeCount++;
                                stats.CartridgeSize += size;
                                break;
                            case MetaDataKeys.titleshot:
                                stats.TitleshotCount++;
                                stats.TitleshotSize += size;
                                break;
                            case MetaDataKeys.music:
                                stats.MusicCount++;
                                stats.MusicSize += size;
                                break;
                            case MetaDataKeys.magazine:
                                stats.MagazineCount++;
                                stats.MagazineSize += size;
                                break;
                            case MetaDataKeys.mix:
                                stats.MixCount++;
                                stats.MixSize += size;
                                break;
                        }
                    }
                }

                stats.VisibleGames = stats.TotalGames - stats.HiddenGames;

                if (includeStorage)
                {
                    foreach (var mediaPath in allMediaPaths)
                    {
                        try
                        {
                            stats.TotalMediaSize += new FileInfo(mediaPath).Length;
                        }
                        catch { }
                    }

                    if (!string.IsNullOrEmpty(baseDir) && Directory.Exists(baseDir))
                    {
                        try
                        {
                            string romDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(baseDir) ?? baseDir, "roms", systemName);

                            if (!Directory.Exists(romDir))
                                romDir = baseDir;

                            if (Directory.Exists(romDir))
                            {
                                stats.RomFolderSize = GetDirectorySize(romDir);
                                stats.ActualRomSize = stats.RomFolderSize - stats.TotalMediaSize;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch
            {
                stats.LoadError = true;
            }

            return stats;
        }

        private static bool IsMediaPresent(object value)
        {
            return value != DBNull.Value && value != null && !string.IsNullOrWhiteSpace(value.ToString());
        }

        private static string ResolveMediaPath(string mediaPath, string baseDir)
        {
            if (string.IsNullOrWhiteSpace(mediaPath))
                return string.Empty;

            if (mediaPath.StartsWith("./"))
                mediaPath = mediaPath.Substring(2);

            if (System.IO.Path.IsPathRooted(mediaPath))
                return mediaPath;

            return System.IO.Path.Combine(baseDir, mediaPath);
        }

        private static long GetDirectorySize(string directory)
        {
            long size = 0;
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(directory);

                foreach (FileInfo file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        size += file.Length;
                    }
                    catch { }
                }
            }
            catch { }
            return size;
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private static string BuildReportText(List<GamelistStatistics> allStats, bool includeStorage)
        {
            var report = new StringBuilder(8192);

            report.AppendLine("═══════════════════════════════════════════════════════════════");
            report.AppendLine("                    GAMELIST STATISTICS REPORT");
            report.AppendLine($"                    Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("═══════════════════════════════════════════════════════════════");
            report.AppendLine();

            int totalGames = 0, totalHidden = 0, totalVisible = 0;
            int totalImages = 0, totalVideos = 0, totalManuals = 0;
            long totalImageSize = 0, totalVideoSize = 0, totalManualSize = 0;
            long totalAllMediaSize = 0, totalRomSize = 0;
            int systemsProcessed = 0;

            foreach (var stats in allStats)
            {
                report.AppendLine($"┌─ SYSTEM: {stats.SystemName}");
                report.AppendLine($"│  Path: {stats.GamelistPath}");

                if (stats.LoadError)
                {
                    report.AppendLine("│  ERROR: Could not load gamelist");
                }
                else
                {
                    report.AppendLine($"│");
                    report.AppendLine($"│  Games:");
                    report.AppendLine($"│    Total:   {stats.TotalGames,6}");
                    report.AppendLine($"│    Hidden:  {stats.HiddenGames,6}");
                    report.AppendLine($"│    Visible: {stats.VisibleGames,6}");

                    if (includeStorage)
                    {
                        report.AppendLine($"│");
                        report.AppendLine($"│  ROM Folder: {FormatBytes(stats.RomFolderSize),12}");
                        report.AppendLine($"│  Actual ROMs: {FormatBytes(stats.ActualRomSize),12}");
                    }

                    report.AppendLine($"│");
                    report.AppendLine($"│  Media:");

                    if (stats.ImageCount > 0)
                        report.AppendLine($"│    Images:     {stats.ImageCount,6}" + (includeStorage ? $"  ({FormatBytes(stats.ImageSize),12})" : ""));
                    if (stats.VideoCount > 0)
                        report.AppendLine($"│    Videos:     {stats.VideoCount,6}" + (includeStorage ? $"  ({FormatBytes(stats.VideoSize),12})" : ""));
                    if (stats.ManualCount > 0)
                        report.AppendLine($"│    Manuals:    {stats.ManualCount,6}" + (includeStorage ? $"  ({FormatBytes(stats.ManualSize),12})" : ""));
                    if (stats.MarqueeCount > 0)
                        report.AppendLine($"│    Marquees:   {stats.MarqueeCount,6}" + (includeStorage ? $"  ({FormatBytes(stats.MarqueeSize),12})" : ""));
                    if (stats.ThumbnailCount > 0)
                        report.AppendLine($"│    Thumbnails: {stats.ThumbnailCount,6}" + (includeStorage ? $"  ({FormatBytes(stats.ThumbnailSize),12})" : ""));
                    if (stats.WheelCount > 0)
                        report.AppendLine($"│    Wheels:     {stats.WheelCount,6}" + (includeStorage ? $"  ({FormatBytes(stats.WheelSize),12})" : ""));
                    if (stats.BoxBackCount > 0)
                        report.AppendLine($"│    Box Backs:  {stats.BoxBackCount,6}" + (includeStorage ? $"  ({FormatBytes(stats.BoxBackSize),12})" : ""));
                    if (stats.BoxArtCount > 0)
                        report.AppendLine($"│    Box Art:    {stats.BoxArtCount,6}" + (includeStorage ? $"  ({FormatBytes(stats.BoxArtSize),12})" : ""));
                    if (stats.FanArtCount > 0)
                        report.AppendLine($"│    Fan Art:    {stats.FanArtCount,6}" + (includeStorage ? $"  ({FormatBytes(stats.FanArtSize),12})" : ""));
                    if (stats.MapCount > 0)
                        report.AppendLine($"│    Maps:       {stats.MapCount,6}" + (includeStorage ? $"  ({FormatBytes(stats.MapSize),12})" : ""));
                    if (stats.BezelCount > 0)
                        report.AppendLine($"│    Bezels:     {stats.BezelCount,6}" + (includeStorage ? $"  ({FormatBytes(stats.BezelSize),12})" : ""));
                    if (stats.CartridgeCount > 0)
                        report.AppendLine($"│    Cartridges: {stats.CartridgeCount,6}" + (includeStorage ? $"  ({FormatBytes(stats.CartridgeSize),12})" : ""));
                    if (stats.TitleshotCount > 0)
                        report.AppendLine($"│    Titleshots: {stats.TitleshotCount,6}" + (includeStorage ? $"  ({FormatBytes(stats.TitleshotSize),12})" : ""));
                    if (stats.MusicCount > 0)
                        report.AppendLine($"│    Music:      {stats.MusicCount,6}" + (includeStorage ? $"  ({FormatBytes(stats.MusicSize),12})" : ""));
                    if (stats.MagazineCount > 0)
                        report.AppendLine($"│    Magazines:  {stats.MagazineCount,6}" + (includeStorage ? $"  ({FormatBytes(stats.MagazineSize),12})" : ""));
                    if (stats.MixCount > 0)
                        report.AppendLine($"│    Mix:        {stats.MixCount,6}" + (includeStorage ? $"  ({FormatBytes(stats.MixSize),12})" : ""));

                    totalGames += stats.TotalGames;
                    totalHidden += stats.HiddenGames;
                    totalVisible += stats.VisibleGames;
                    totalImages += stats.ImageCount;
                    totalVideos += stats.VideoCount;
                    totalManuals += stats.ManualCount;
                    totalImageSize += stats.ImageSize;
                    totalVideoSize += stats.VideoSize;
                    totalManualSize += stats.ManualSize;
                    totalAllMediaSize += stats.TotalMediaSize;
                    totalRomSize += stats.ActualRomSize;
                    systemsProcessed++;
                }

                report.AppendLine("└" + new string('─', 63));
                report.AppendLine();
            }

            report.AppendLine("═══════════════════════════════════════════════════════════════");
            report.AppendLine("                           SUMMARY");
            report.AppendLine("═══════════════════════════════════════════════════════════════");
            report.AppendLine();
            report.AppendLine($"Systems Processed:    {systemsProcessed,6}");
            report.AppendLine($"Total Games:          {totalGames,6}");
            report.AppendLine($"Total Hidden:         {totalHidden,6}");
            report.AppendLine($"Total Visible:        {totalVisible,6}");
            report.AppendLine();

            if (includeStorage)
            {
                report.AppendLine($"Total ROM Size:       {FormatBytes(totalRomSize),6}");
                report.AppendLine();
                report.AppendLine($"Total Images:         {totalImages,6}  ({FormatBytes(totalImageSize),12})");
                report.AppendLine($"Total Videos:         {totalVideos,6}  ({FormatBytes(totalVideoSize),12})");
                report.AppendLine($"Total Manuals:        {totalManuals,6}  ({FormatBytes(totalManualSize),12})");
                report.AppendLine();
                report.AppendLine($"Total Media Size:     {FormatBytes(totalAllMediaSize),6}");
                report.AppendLine($"Total Collection:     {FormatBytes(totalRomSize + totalAllMediaSize),6}");
                report.AppendLine();
            }
            else
            {
                report.AppendLine($"Total Images:         {totalImages,6}");
                report.AppendLine($"Total Videos:         {totalVideos,6}");
                report.AppendLine($"Total Manuals:        {totalManuals,6}");
                report.AppendLine();
            }

            if (totalGames > 0)
            {
                report.AppendLine("Coverage Percentages:");
                report.AppendLine($"  Images:  {(totalImages * 100.0 / totalGames):F1}%");
                report.AppendLine($"  Videos:  {(totalVideos * 100.0 / totalGames):F1}%");
                report.AppendLine($"  Manuals: {(totalManuals * 100.0 / totalGames):F1}%");
            }

            report.AppendLine("═══════════════════════════════════════════════════════════════");

            return report.ToString();
        }

        private static void ShowReportWindow(string reportText, bool includeStorage)
        {
            var window = new Window
            {
                Title = "Gamelist Statistics Report",
                Width = includeStorage ? 1000 : 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var mainGrid = new Grid();

            if (includeStorage)
            {
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            else
            {
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Text report
            var textBox = new TextBox
            {
                Text = reportText,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas, Courier New"),
                Padding = new Thickness(10)
            };
            Grid.SetRow(textBox, 0);
            Grid.SetColumn(textBox, 0);
            mainGrid.Children.Add(textBox);

            // Charts (only if storage is included)
            if (includeStorage)
            {
                var chartStack = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var coverageChart = CreateMediaCoverageChart(reportText);
                coverageChart.Height = 240;
                chartStack.Children.Add(coverageChart);

                var spacer = new Border { Height = 15 };
                chartStack.Children.Add(spacer);

                var diskChart = CreateDiskUsageChart(reportText);
                diskChart.Height = 250;
                chartStack.Children.Add(diskChart);

                Grid.SetRow(chartStack, 0);
                Grid.SetColumn(chartStack, 1);
                mainGrid.Children.Add(chartStack);
            }

            // Button panel at bottom
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            var saveButton = new Button
            {
                Content = "Save to File",
                Width = 100,
                Height = 20,
                Margin = new Thickness(5)
            };
            try { saveButton.Style = (Style)Application.Current.FindResource("GreyRoundedButton"); } catch { }
            saveButton.Click += (s, e) => SaveReportToFile(reportText);

            var notepadButton = new Button
            {
                Content = "Open in Notepad",
                Width = 120,
                Height = 20,
                Margin = new Thickness(5)
            };
            try { notepadButton.Style = (Style)Application.Current.FindResource("GreyRoundedButton"); } catch { }
            notepadButton.Click += (s, e) => OpenInNotepad(reportText);

            var closeButton = new Button
            {
                Content = "Close",
                Width = 100,
                Height = 20,
                Margin = new Thickness(5)
            };
            try { closeButton.Style = (Style)Application.Current.FindResource("GreyRoundedButton"); } catch { }
            closeButton.Click += (s, e) => window.Close();

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(notepadButton);
            buttonPanel.Children.Add(closeButton);

            Grid.SetRow(buttonPanel, 1);
            Grid.SetColumnSpan(buttonPanel, includeStorage ? 2 : 1);
            mainGrid.Children.Add(buttonPanel);

            window.Content = mainGrid;
            window.ShowDialog();
        }

        private static Canvas CreateMediaCoverageChart(string reportText)
        {
            var canvas = new Canvas
            {
                Background = Brushes.White,
                Margin = new Thickness(10,0,10,0)
            };

            var lines = reportText.Split('\n');
            int totalGames = 0;
            int totalImages = 0;
            int totalVideos = 0;
            int totalManuals = 0;

            foreach (var line in lines)
            {
                if (line.Contains("Total Games:"))
                {
                    string digits = new string(line.Where(char.IsDigit).ToArray());
                    if (!int.TryParse(digits, out totalGames))
                    {
                        totalGames = 0; // fallback if parsing fails
                    }
                }
                else if (line.Contains("Total Images:"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"Total Images:\s+(\d+)");
                    if (match.Success)
                    {
                        if (!int.TryParse(match.Groups[1].Value, out totalImages))
                            totalImages = 0;
                    }
                }
                else if (line.Contains("Total Videos:"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"Total Videos:\s+(\d+)");
                    if (match.Success)
                    {
                        if (!int.TryParse(match.Groups[1].Value, out totalVideos))
                            totalVideos = 0;
                    }
                }
                else if (line.Contains("Total Manuals:"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"Total Manuals:\s+(\d+)");
                    if (match.Success)
                    {
                        if (!int.TryParse(match.Groups[1].Value, out totalManuals))
                            totalManuals = 0;
                    }
                }
            }


            if (totalGames == 0)
            {
                var noDataText = new TextBlock
                {
                    Text = "No data available",
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Canvas.SetLeft(noDataText, 50);
                Canvas.SetTop(noDataText, 150);
                canvas.Children.Add(noDataText);
                return canvas;
            }

            double imagePercent = (totalImages * 100.0) / totalGames;
            double videoPercent = (totalVideos * 100.0) / totalGames;
            double manualPercent = (totalManuals * 100.0) / totalGames;

            var title = new TextBlock
            {
                Text = "Media Coverage",
                FontSize = 14,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(title, 50);
            Canvas.SetTop(title, 10);
            canvas.Children.Add(title);

            double centerX = 120;
            double centerY = 100;
            double radius = 55;
            double startAngle = 0;

            var colors = new[]
            {
                Brushes.DodgerBlue,
                Brushes.OrangeRed,
                Brushes.MediumSeaGreen
            };

            var dataPoints = new[] { imagePercent, videoPercent, manualPercent };
            var labels = new[] { "Images", "Videos", "Manuals" };

            for (int i = 0; i < dataPoints.Length; i++)
            {
                double sweepAngle = (dataPoints[i] / 100.0) * 360;

                if (sweepAngle > 0.1)
                {
                    var segment = CreatePieSegment(centerX, centerY, radius, startAngle, sweepAngle, colors[i]);
                    canvas.Children.Add(segment);
                    startAngle += sweepAngle;
                }
            }

            int legendY = 170;
            for (int i = 0; i < dataPoints.Length; i++)
            {
                var legendRect = new Rectangle
                {
                    Width = 12,
                    Height = 12,
                    Fill = colors[i]
                };
                Canvas.SetLeft(legendRect, 30);
                Canvas.SetTop(legendRect, legendY);
                canvas.Children.Add(legendRect);

                var legendText = new TextBlock
                {
                    Text = $"{labels[i]}: {dataPoints[i]:F1}%",
                    FontSize = 11
                };
                Canvas.SetLeft(legendText, 47);
                Canvas.SetTop(legendText, legendY - 1);
                canvas.Children.Add(legendText);

                legendY += 20;
            }

            return canvas;
        }

        private static Canvas CreateDiskUsageChart(string reportText)
        {
            var canvas = new Canvas
            {
                Background = Brushes.White,
                Margin = new Thickness(10,0,10,0)
            };

            var lines = reportText.Split('\n');
            long totalRomSize = 0;
            long totalMediaSize = 0;
            long totalCollectionSize = 0;

            foreach (var line in lines)
            {
                if (line.Contains("Total ROM Size:"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"Total ROM Size:\s+(.+)");
                    if (match.Success)
                        totalRomSize = ParseSizeString(match.Groups[1].Value.Trim());
                }
                else if (line.Contains("Total Media Size:"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"Total Media Size:\s+(.+)");
                    if (match.Success)
                        totalMediaSize = ParseSizeString(match.Groups[1].Value.Trim());
                }
                else if (line.Contains("Total Collection:"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"Total Collection:\s+(.+)");
                    if (match.Success)
                        totalCollectionSize = ParseSizeString(match.Groups[1].Value.Trim());
                }
            }

            long diskTotalSize = 0;
            long diskFreeSize = 0;

            try
            {
                var pathMatch = System.Text.RegularExpressions.Regex.Match(reportText, @"Path: (.+)");
                if (pathMatch.Success)
                {
                    string gamelistPath = pathMatch.Groups[1].Value.Trim();
                    string driveLetter = System.IO.Path.GetPathRoot(gamelistPath) ?? "C:\\";

                    DriveInfo drive = new DriveInfo(driveLetter);
                    if (drive.IsReady)
                    {
                        diskTotalSize = drive.TotalSize;
                        diskFreeSize = drive.AvailableFreeSpace;
                    }
                }
            }
            catch { }

            if (diskTotalSize == 0 || totalCollectionSize == 0)
            {
                var noDataText = new TextBlock
                {
                    Text = "Disk info unavailable",
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Canvas.SetLeft(noDataText, 50);
                Canvas.SetTop(noDataText, 150);
                canvas.Children.Add(noDataText);
                return canvas;
            }

            long diskUsedSize = diskTotalSize - diskFreeSize;
            long otherUsedSize = diskUsedSize - totalCollectionSize;

            double romPercent = (totalRomSize * 100.0) / diskTotalSize;
            double mediaPercent = (totalMediaSize * 100.0) / diskTotalSize;
            double otherPercent = (otherUsedSize * 100.0) / diskTotalSize;
            double freePercent = (diskFreeSize * 100.0) / diskTotalSize;

            var title = new TextBlock
            {
                Text = "Disk Usage",
                FontSize = 14,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(title, 65);
            Canvas.SetTop(title, 10);
            canvas.Children.Add(title);

            double centerX = 120;
            double centerY = 100;
            double radius = 55;
            double startAngle = 0;

            var colors = new[]
            {
                Brushes.CornflowerBlue,
                Brushes.Coral,
                Brushes.LightGray,
                Brushes.Green
            };

            var dataPoints = new[] { romPercent, mediaPercent, otherPercent, freePercent };
            var labels = new[]
            {
                $"ROMs: {FormatBytes(totalRomSize)}",
                $"Media: {FormatBytes(totalMediaSize)}",
                $"Other: {FormatBytes(otherUsedSize)}",
                $"Free: {FormatBytes(diskFreeSize)}"
            };

            for (int i = 0; i < dataPoints.Length; i++)
            {
                double sweepAngle = (dataPoints[i] / 100.0) * 360;

                if (sweepAngle > 0.1)
                {
                    var segment = CreatePieSegment(centerX, centerY, radius, startAngle, sweepAngle, colors[i]);
                    canvas.Children.Add(segment);
                    startAngle += sweepAngle;
                }
            }

            int legendY = 170;
            for (int i = 0; i < dataPoints.Length; i++)
            {
                if (dataPoints[i] > 0.1)
                {
                    var legendRect = new Rectangle
                    {
                        Width = 12,
                        Height = 12,
                        Fill = colors[i],
                        Stroke = Brushes.Gray,
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(legendRect, 15);
                    Canvas.SetTop(legendRect, legendY);
                    canvas.Children.Add(legendRect);

                    var legendText = new TextBlock
                    {
                        Text = labels[i],
                        FontSize = 11
                    };
                    Canvas.SetLeft(legendText, 32);
                    Canvas.SetTop(legendText, legendY - 1);
                    canvas.Children.Add(legendText);

                    legendY += 18;
                }
            }

            return canvas;
        }

        private static System.Windows.Shapes.Path CreatePieSegment(
    double centerX,
    double centerY,
    double radius,
    double startAngle,
    double sweepAngle,
    Brush fill)
        {
            const double strokeThickness = 2;
            const double innerRadius = strokeThickness / 2;

            if (sweepAngle >= 359.99)
            {
                return new System.Windows.Shapes.Path
                {
                    Data = new EllipseGeometry(
                        new Point(centerX, centerY),
                        radius,
                        radius),
                    Fill = fill,
                    Stroke = Brushes.White,
                    StrokeThickness = strokeThickness
                };
            }

            if (sweepAngle < 0.1)
                sweepAngle = 0.1;

            double startRad = (startAngle - 90) * Math.PI / 180.0;
            double endRad = (startAngle + sweepAngle - 90) * Math.PI / 180.0;

            // Outer arc points
            double x1 = centerX + radius * Math.Cos(startRad);
            double y1 = centerY + radius * Math.Sin(startRad);
            double x2 = centerX + radius * Math.Cos(endRad);
            double y2 = centerY + radius * Math.Sin(endRad);

            // Inner start/end points (slightly off-center)
            double ix1 = centerX + innerRadius * Math.Cos(startRad);
            double iy1 = centerY + innerRadius * Math.Sin(startRad);
            double ix2 = centerX + innerRadius * Math.Cos(endRad);
            double iy2 = centerY + innerRadius * Math.Sin(endRad);

            bool isLargeArc = sweepAngle > 180;

            var figure = new PathFigure
            {
                StartPoint = new Point(ix1, iy1),
                IsClosed = true
            };

            figure.Segments.Add(new LineSegment(new Point(x1, y1), true));
            figure.Segments.Add(new ArcSegment(
                new Point(x2, y2),
                new Size(radius, radius),
                0,
                isLargeArc,
                SweepDirection.Clockwise,
                true));
            figure.Segments.Add(new LineSegment(new Point(ix2, iy2), true));

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            return new System.Windows.Shapes.Path
            {
                Data = geometry,
                Fill = fill,
                Stroke = Brushes.White,
                StrokeThickness = strokeThickness,
                StrokeLineJoin = PenLineJoin.Round
            };
        }


        private static long ParseSizeString(string sizeStr)
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(sizeStr, @"([\d.]+)\s*([A-Z]+)");
                if (match.Success)
                {
                    double value = double.Parse(match.Groups[1].Value);
                    string unit = match.Groups[2].Value.ToUpper();

                    return unit switch
                    {
                        "B" => (long)value,
                        "KB" => (long)(value * 1024),
                        "MB" => (long)(value * 1024 * 1024),
                        "GB" => (long)(value * 1024 * 1024 * 1024),
                        "TB" => (long)(value * 1024L * 1024 * 1024 * 1024),
                        _ => 0
                    };
                }
            }
            catch { }
            return 0;
        }

        private static void SaveReportToFile(string reportText)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    FileName = $"GamelistReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    DefaultExt = ".txt"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveDialog.FileName, reportText);
                    MessageBox.Show(
                        $"Report saved successfully to:\n{saveDialog.FileName}",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error saving report: {ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private static void OpenInNotepad(string reportText)
        {
            try
            {
                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"GamelistReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(tempPath, reportText);

                Process.Start("notepad.exe", tempPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error opening in Notepad: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}