using GamelistManager.classes.core;
using GamelistManager.classes.helpers;
using Microsoft.Win32;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;

namespace GamelistManager.pages
{
    public partial class DatToolPage : Page
    {
        private readonly List<GameReportItem> _gamelistSummary = [];
        private readonly List<GameReportItem> _datSummary = [];
        private MainWindow _mainWindow;
        private bool _allowStreamingFromMame = false;
        private DatHeader? _datHeader = null; // Store DAT header information
        private Process? _mameProcess = null; // Track MAME process for proper disposal

        public DatToolPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
        }

        public void ResetDatToolPage()
        {
            textBlock_DatFileName.Text = "";
            text_DatTotal.Text = "—";
            text_DatParents.Text = "—";
            text_DatClones.Text = "—";
            text_DatCHD.Text = "—";
            text_DatNonPlayable.Text = "—";
            text_DatPlayable.Text = "—";
            text_GamelistTotal.Text = "—";
            text_GamelistParents.Text = "—";
            text_GamelistClones.Text = "—";
            text_GamelistCHD.Text = "—";
            text_GamelistNonPlayable.Text = "—";
            text_GamelistMissingParents.Text = "—";
            text_GamelistMissingClones.Text = "—";
            text_GamelistNotInDAT.Text = "—";
            text_DatInfoName.Text = "—";
            text_DatInfoVersion.Text = "—";
            text_DatInfoAuthor.Text = "—";
            text_DatInfoDate.Text = "—";
            text_DatInfoDescription.Text = "—";
            checkBox_IncludeHidden.IsEnabled = false;
            checkBox_IncludeHidden.IsChecked = false;
            _datSummary.Clear();
            _gamelistSummary.Clear();
            _datHeader = null;
    
            if (SharedData.CurrentSystem == "mame" &&
              !string.IsNullOrWhiteSpace(Properties.Settings.Default.MamePath) &&
              File.Exists(Properties.Settings.Default.MamePath))
            {
                button_StreamFromMame.IsEnabled = true;
                _allowStreamingFromMame = true;
            }
            else
            {
                button_StreamFromMame.IsEnabled = false;
                _allowStreamingFromMame = false;
            }

        }

        private async void button_StreamFromMame_Click(object sender, RoutedEventArgs e)
        {
            ResetDatToolPage();
            comboBox_ReportView.IsEnabled = false;
           
            Mouse.OverrideCursor = Cursors.Wait;
            button_StreamFromMame.IsEnabled = false;
            button_OpenDatFile.IsEnabled = false;
            button_CloseDATPage.IsEnabled = false;

            string exePath = Properties.Settings.Default.MamePath;
            string arguments = "-listxml";

            try
            {
                (Stream? xmlStream, Process? mameProcess) = await GetMameListXmlStreamAsync(exePath, arguments);

                if (xmlStream == null)
                {
                    Mouse.OverrideCursor = null;
                    MessageBox.Show("Failed to get XML stream from MAME.", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);

                    if (_allowStreamingFromMame)
                    {
                        button_StreamFromMame.IsEnabled = true;
                    }
                    button_OpenDatFile.IsEnabled = true;
                    Mouse.OverrideCursor = null;
                    return;
                }

                _mameProcess = mameProcess;
                await ProcessDatStreamAsync(xmlStream, "MAME -listxml output", _mameProcess);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error streaming from MAME: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button_CloseDATPage.IsEnabled = true;
                button_StreamFromMame.IsEnabled = true;
                button_OpenDatFile.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        private async void button_OpenDatFile_Click(object sender, RoutedEventArgs e)
        {
            // Open file dialog
            var dlg = new OpenFileDialog
            {
                Title = "Select a DAT File",
                Filter = "DAT files (*.dat)|*.dat|XML files (*.xml)|*.xml|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dlg.ShowDialog() != true)
            {
                button_OpenDatFile.IsEnabled = true;
                button_CloseDATPage.IsEnabled = true;
                if (_allowStreamingFromMame)
                {
                    button_StreamFromMame.IsEnabled = true;
                }
                return;
            }

            ResetDatToolPage();
            comboBox_ReportView.IsEnabled = false;
            button_OpenDatFile.IsEnabled = false;
            button_StreamFromMame.IsEnabled = false;
            button_CloseDATPage.IsEnabled = false;

            try
            {
                string datFileName = Path.GetFileName(dlg.FileName);
                using Stream xmlStream = File.OpenRead(dlg.FileName);
                await ProcessDatStreamAsync(xmlStream, datFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening DAT file: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button_OpenDatFile.IsEnabled = true;
                button_CloseDATPage.IsEnabled = true;
                Mouse.OverrideCursor = null;
                if (_allowStreamingFromMame)
                {
                    button_StreamFromMame.IsEnabled = true;
                }
            }
        }

        private async Task ProcessDatStreamAsync(Stream xmlStream, string datFileName, Process? mameProcess = null)
        {

            textBlock_DatFileName.Text = datFileName;

            try
            {
                // --- Parse XML in background ---
                var (datEntries, datHeader) = await CreateDatSummaryAsync(xmlStream);

                xmlStream.Dispose();

                if (datEntries == null || datEntries.Count == 0)
                {
                    MessageBox.Show("No entries were found in the DAT file.", "No Data",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _datSummary.Clear();
                _datSummary.AddRange(datEntries);
                _datHeader = datHeader;

                comboBox_ReportView.IsEnabled = false;

                try
                {
                    UpdateDatSummaryCounts();
                    UpdateDatHeaderInfo();

                    _gamelistSummary.Clear();
                    _gamelistSummary.AddRange(await CreateGamelistSummaryAsync(_datSummary));

                    bool includeHidden = checkBox_IncludeHidden.IsChecked == true;
                    UpdateGamelistSummaryCounts(includeHidden);

                }
                finally
                {
                    comboBox_ReportView.IsEnabled = true;
                }

                checkBox_IncludeHidden.IsEnabled = true;
            }
            finally
            {
                // Dispose MAME process if provided
                mameProcess?.Dispose();
                if (mameProcess == _mameProcess)
                    _mameProcess = null;
            }
        }


        private async Task<List<GameReportItem>> CreateGamelistSummaryAsync(List<GameReportItem> datSummary)
        {
            return await Task.Run(() =>
            {
                var table = SharedData.DataSet.Tables[0];

                // Dictionary for fast DAT lookups by name
                var datLookup = datSummary.ToDictionary(d => d.Name, d => d, StringComparer.OrdinalIgnoreCase);

                // Get the ROM path once
                string mameRomPath = Path.GetDirectoryName(SharedData.XMLFilename) ?? "";

                // Build full gamelist summary
                var gamelistSummary = table.AsEnumerable()
                    .Select(row =>
                    {
                        var romName = NameHelper.NormalizeRomName(row["Rom Path"].ToString()!);

                        if (datLookup.TryGetValue(romName, out var datItem))
                        {
                            // Match found in DAT
                            string chdStatus = "";

                            // If DAT says CHD is required, check if it exists
                            if (!string.IsNullOrEmpty(datItem.CHDRequired))
                            {
                                string chdPath = Path.Combine(mameRomPath, romName);

                                // Check for any .chd files in the game's directory
                                bool chdExists = Directory.Exists(chdPath) &&
                                               Directory.GetFiles(chdPath, "*.chd").Length > 0;

                                chdStatus = chdExists ? $"{datItem.CHDRequired} (OK)" : $"{datItem.CHDRequired} (Missing)";
                            }

                            return new GameReportItem
                            {
                                Name = romName,
                                CloneOf = datItem.CloneOf,
                                Description = datItem.Description,
                                NonPlayable = datItem.NonPlayable,
                                CHDRequired = chdStatus,
                                Missing = "",
                                NotInDat = ""
                            };
                        }
                        else
                        {
                            // No matching DAT entry
                            return new GameReportItem
                            {
                                Name = romName,
                                CloneOf = "",
                                Description = "",
                                NonPlayable = "",
                                CHDRequired = "",
                                Missing = "",
                                NotInDat = "Not In DAT"
                            };
                        }
                    })
                    .ToList();

                return gamelistSummary;
            });
        }


        private async Task<(List<GameReportItem>, DatHeader?)> CreateDatSummaryAsync(Stream xmlStream)
        {
            return await Task.Run(() =>
            {
                var list = new List<GameReportItem>();
                DatHeader? header = null;
                bool headerParsed = false;

                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Ignore,
                    XmlResolver = null,
                    IgnoreWhitespace = true,
                    IgnoreComments = true
                };

                using var reader = XmlReader.Create(xmlStream, settings);

                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element)
                        continue;

                    string elementName = reader.Name.ToLowerInvariant();

                    // Check for MAME root element with attributes (MAME format)
                    if (elementName == "mame" && !headerParsed)
                    {
                        header = new DatHeader
                        {
                            Name = "MAME",
                            Version = reader.GetAttribute("build") ?? "",
                            Description = $"MAME {reader.GetAttribute("build") ?? ""}",
                            Author = "MAME Team"
                        };
                        headerParsed = true;
                        continue;
                    }

                    // Parse header element (FBNeo/standard DAT format)
                    if (elementName == "header" && !headerParsed)
                    {
                        header = ParseDatHeaderInline(reader);
                        headerParsed = true;
                        continue;
                    }

                    // Detect any entry element that defines a game/machine
                    if (elementName is not ("machine" or "game"))
                        continue;

                    string? nameAttr = reader.GetAttribute("name");
                    if (string.IsNullOrEmpty(nameAttr))
                        continue;

                    using var subtree = reader.ReadSubtree();
                    subtree.Read(); // Move to <machine> or <game> start

                    var item = ParseSingleMachine(subtree, elementName);
                    if (item != null)
                        list.Add(item);
                }

                return (list, header);
            });
        }

        private GameReportItem? ParseSingleMachine(XmlReader reader, string elementName)
        {
            string name = NameHelper.NormalizeRomName(reader.GetAttribute("name") ?? "");
            string cloneOf = NameHelper.NormalizeRomName(reader.GetAttribute("cloneof") ?? "");
            string runnable = reader.GetAttribute("runnable") ?? "yes";
            string isBios = reader.GetAttribute("isbios") ?? "no";
            string isDevice = reader.GetAttribute("isdevice") ?? "no";
            string isMechanical = reader.GetAttribute("ismechanical") ?? "no";
            string romOf = reader.GetAttribute("romof") ?? "";

            var nonPlayable = new List<string>();
            var diskStatus = new List<string>();
            string description = "";
            bool needsCHD = false;

            if (runnable.Equals("no", StringComparison.OrdinalIgnoreCase))
                nonPlayable.Add("Not runnable");
            if (isBios.Equals("yes", StringComparison.OrdinalIgnoreCase))
                nonPlayable.Add("BIOS");
            if (isDevice.Equals("yes", StringComparison.OrdinalIgnoreCase))
                nonPlayable.Add("Device");
            if (isMechanical.Equals("yes", StringComparison.OrdinalIgnoreCase))
                nonPlayable.Add("Mechanical");

            string mameRomPath = Path.GetDirectoryName(SharedData.XMLFilename) ?? "";

            // Parse within this one game/machine subtree
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                    continue;

                switch (reader.Name.ToLowerInvariant())
                {
                    case "disk":
                        {
                            var (diskStatusText, nonPlayableReason, requiresCHD) =
                                ResolveDiskStatus(reader, name, mameRomPath);

                            diskStatus.Add(diskStatusText);
                            if (requiresCHD)
                                needsCHD = true;

                            if (!string.IsNullOrEmpty(nonPlayableReason))
                                nonPlayable.Add(nonPlayableReason);

                            break;
                        }

                    case "driver":
                        string driverStatus = reader.GetAttribute("status") ?? "";
                        if (driverStatus.Equals("preliminary", StringComparison.OrdinalIgnoreCase))
                            nonPlayable.Add("Preliminary driver");
                        break;

                    case "description":
                        description = reader.ReadElementContentAsString().Trim();
                        break;
                }
            }

            return new GameReportItem
            {
                Name = name,
                CloneOf = cloneOf,
                Description = description,
                CHDRequired = needsCHD ? string.Join(Environment.NewLine, diskStatus) : "",
                NonPlayable = nonPlayable.Count > 0 ? string.Join(Environment.NewLine, nonPlayable) : "",
            };
        }

        private (string DiskStatus, string? NonPlayableReason, bool RequiresCHD) ResolveDiskStatus(
            XmlReader reader,
            string gameName,
            string romPath)
        {
            string diskName = reader.GetAttribute("name") ?? "";
            string statusAttr = reader.GetAttribute("status") ?? "";
            string optionalAttr = reader.GetAttribute("optional") ?? "";

            bool required = string.IsNullOrEmpty(optionalAttr) ||
                            !optionalAttr.Equals("yes", StringComparison.OrdinalIgnoreCase);

            bool requiresCHD = required;
            string chdFile = $"{diskName}.chd";
            string chdPath = Path.Combine(romPath, gameName, chdFile);

            string diskStatus = string.IsNullOrEmpty(statusAttr) ? chdFile : $"{chdFile} ({statusAttr})";
            string? nonPlayableReason = null;

            if (statusAttr.Equals("nodump", StringComparison.OrdinalIgnoreCase))
            {
                nonPlayableReason = $"Disk {diskName} nodump";
            }

            return (diskStatus, nonPlayableReason, requiresCHD);
        }

        private DatHeader? ParseDatHeaderInline(XmlReader reader)
        {
            var header = new DatHeader();
            int depth = reader.Depth;

            while (reader.Read())
            {
                // Stop when we exit the header element
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
                    break;

                if (reader.NodeType != XmlNodeType.Element)
                    continue;

                switch (reader.Name.ToLowerInvariant())
                {
                    case "name":
                        header.Name = reader.ReadElementContentAsString().Trim();
                        break;
                    case "version":
                        header.Version = reader.ReadElementContentAsString().Trim();
                        break;
                    case "author":
                        header.Author = reader.ReadElementContentAsString().Trim();
                        break;
                    case "date":
                        header.Date = reader.ReadElementContentAsString().Trim();
                        break;
                    case "description":
                        header.Description = reader.ReadElementContentAsString().Trim();
                        break;
                }
            }

            return header;
        }

        private void UpdateDatHeaderInfo()
        {
            if (_datHeader != null)
            {
                text_DatInfoName.Text = !string.IsNullOrEmpty(_datHeader.Name) ? _datHeader.Name : "—";
                text_DatInfoVersion.Text = !string.IsNullOrEmpty(_datHeader.Version) ? _datHeader.Version : "—";
                text_DatInfoAuthor.Text = !string.IsNullOrEmpty(_datHeader.Author) ? _datHeader.Author : "—";
                text_DatInfoDate.Text = !string.IsNullOrEmpty(_datHeader.Date) ? _datHeader.Date : "—";
                text_DatInfoDescription.Text = !string.IsNullOrEmpty(_datHeader.Description) ? _datHeader.Description : "—";
            }
            else
            {
                text_DatInfoName.Text = "—";
                text_DatInfoVersion.Text = "—";
                text_DatInfoAuthor.Text = "—";
                text_DatInfoDate.Text = "—";
                text_DatInfoDescription.Text = "—";
            }
        }



        private void UpdateGamelistSummaryCounts(bool includeHidden)
        {
            if (_gamelistSummary == null || _gamelistSummary.Count == 0)
            {
                text_GamelistTotal.Text = "—";
                text_GamelistParents.Text = "—";
                text_GamelistClones.Text = "—";
                text_GamelistCHD.Text = "—";
                text_GamelistNonPlayable.Text = "—";
                text_GamelistMissingParents.Text = "—";
                text_GamelistMissingClones.Text = "—";
                text_GamelistNotInDAT.Text = "—";
                return;
            }

            var table = SharedData.DataSet.Tables[0];
            HashSet<string>? hiddenSet = null;
            if (!includeHidden)
            {
                hiddenSet = new HashSet<string>(
                    table.AsEnumerable()
                         .Where(row => row.Field<bool>("Hidden"))
                         .Select(row => NameHelper.NormalizeRomName(row["Rom Path"].ToString()!)),
                    StringComparer.OrdinalIgnoreCase
                );
            }

            var filteredList = _gamelistSummary
                .Where(g => includeHidden || (hiddenSet != null && !hiddenSet.Contains(g.Name)))
                .ToList();

            int total = filteredList.Count;
            int chdRequired = filteredList.Count(g => !string.IsNullOrEmpty(g.CHDRequired));
            int nonPlayable = filteredList.Count(g => !string.IsNullOrEmpty(g.NonPlayable));

            // Build DAT lookup to use DAT's parent/clone classification
            var datLookup = _datSummary.ToDictionary(d => d.Name, d => d, StringComparer.OrdinalIgnoreCase);

            // Build lookup of what's in the filtered gamelist
            var gamelistLookup = new HashSet<string>(
                filteredList.Select(g => g.Name),
                StringComparer.OrdinalIgnoreCase
            );

            // Count parents/clones based on DAT classification
            // Only count games that actually exist in the DAT
            int parents = filteredList.Count(g =>
                datLookup.TryGetValue(g.Name, out var datItem) &&
                string.IsNullOrEmpty(datItem.CloneOf));

            int clones = filteredList.Count(g =>
                datLookup.TryGetValue(g.Name, out var datItem) &&
                !string.IsNullOrEmpty(datItem.CloneOf));

            // Games not in DAT at all
            int notInDat = filteredList.Count(g => !datLookup.ContainsKey(g.Name));

            // Count DAT items and how many are present in gamelist
            int datParentsTotal = 0;
            int datParentsPresentInGamelist = 0;
            int datClonesTotal = 0;
            int datClonesPresentInGamelist = 0;

            foreach (var datItem in _datSummary)
            {
                bool isParent = string.IsNullOrEmpty(datItem.CloneOf);
                bool isPresentInGamelist = gamelistLookup.Contains(datItem.Name);

                if (isParent)
                {
                    datParentsTotal++;
                    if (isPresentInGamelist)
                        datParentsPresentInGamelist++;
                }
                else
                {
                    datClonesTotal++;
                    if (isPresentInGamelist)
                        datClonesPresentInGamelist++;
                }
            }

            // missing = dat total - present in gamelist (never negative)
            int missingParents = Math.Max(0, datParentsTotal - datParentsPresentInGamelist);
            int missingClones = Math.Max(0, datClonesTotal - datClonesPresentInGamelist);

            text_GamelistTotal.Text = total.ToString();
            text_GamelistParents.Text = parents.ToString();
            text_GamelistClones.Text = clones.ToString();
            text_GamelistCHD.Text = chdRequired.ToString();
            text_GamelistNonPlayable.Text = nonPlayable.ToString();
            text_GamelistMissingParents.Text = missingParents.ToString();
            text_GamelistMissingClones.Text = missingClones.ToString();
            text_GamelistNotInDAT.Text = notInDat.ToString();
        }


        private void UpdateDatSummaryCounts()
        {
            // Always write values so UI doesn't show stale values
            if (_datSummary == null || _datSummary.Count == 0)
            {
                text_DatTotal.Text = "0";
                text_DatParents.Text = "0";
                text_DatClones.Text = "0";
                text_DatCHD.Text = "0";
                text_DatNonPlayable.Text = "0";
                return;
            }

            int total = _datSummary.Count;
            int parents = _datSummary.Count(g => string.IsNullOrEmpty(g.CloneOf));
            int clones = _datSummary.Count(g => !string.IsNullOrEmpty(g.CloneOf));
            int needsChd = _datSummary.Count(g => !string.IsNullOrEmpty(g.CHDRequired));
            int nonPlayable = _datSummary.Count(g => !string.IsNullOrEmpty(g.NonPlayable));
            int playable = total - nonPlayable;

            text_DatTotal.Text = total.ToString();
            text_DatParents.Text = parents.ToString();
            text_DatClones.Text = clones.ToString();
            text_DatCHD.Text = needsChd.ToString();
            text_DatNonPlayable.Text = nonPlayable.ToString();
            text_DatPlayable.Text = playable.ToString();

        }


        private async void ComboBox_ReportView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_ReportView.SelectedItem is not ComboBoxItem selectedItem || comboBox_ReportView.SelectedIndex <= 0)
            {
                return;
            }

            string selectedText = selectedItem.Content.ToString()!;

            if (_mainWindow.MainDataGrid.ItemsSource is not DataView dataView)
            {
                return;
            }

            // Show cursor and disable during processing
            Mouse.OverrideCursor = Cursors.Wait;
            this.IsEnabled = false;

            SharedData.ChangeTracker?.StopTracking();

            // Move the heavy work to background thread
            await Task.Run(() =>
            {
                var table = dataView.Table;

                // Build a lookup dictionary for fast access
                var reportLookup = _gamelistSummary
                    .ToDictionary(g => g.Name, StringComparer.OrdinalIgnoreCase);

                table.BeginLoadData();

                try
                {
                    // Iterate through ALL rows in the table
                    foreach (DataRow row in table.Rows)
                    {
                        string romName = NameHelper.NormalizeRomName(row["Rom Path"].ToString()!);

                        string newStatus = "";

                        if (reportLookup.TryGetValue(romName, out var reportItem))
                        {
                            newStatus = selectedText switch
                            {
                                "Clones" => !string.IsNullOrEmpty(reportItem.CloneOf) ? $"Clone of {reportItem.CloneOf}" : "",
                                "Non Playable" => !string.IsNullOrEmpty(reportItem.NonPlayable) ? reportItem.NonPlayable : "",
                                "CHD Required" => !string.IsNullOrEmpty(reportItem.CHDRequired) ? reportItem.CHDRequired : "",
                                "Parents" => string.IsNullOrEmpty(reportItem.CloneOf) ? "Parent" : "",
                                "Not In DAT" => !string.IsNullOrEmpty(reportItem.NotInDat) ? reportItem.NotInDat : "",
                                _ => ""
                            };
                        }

                        row["Status"] = string.IsNullOrEmpty(newStatus) ? DBNull.Value : newStatus;
                    }
                }
                finally
                {
                    table.EndLoadData();
                }
            });

            // Back on UI thread - update UI elements
            var statusColumn = _mainWindow.MainDataGrid.Columns
                .FirstOrDefault(c => c.Header?.ToString() == "Status");

            if (statusColumn != null)
            {
                statusColumn.Visibility = Visibility.Visible;
                statusColumn.Width = DataGridLength.Auto;
            }

            dataView.Sort = "Status DESC";

            if (_mainWindow.MainDataGrid != null && _mainWindow.MainDataGrid.Items.Count > 0)
            {
                _mainWindow.MainDataGrid.ScrollIntoView(_mainWindow.MainDataGrid.Items[0]);
            }

            SharedData.ChangeTracker?.ResumeTracking();

            // Force ComboBox to refresh its visual display
            comboBox_ReportView.Items.Refresh();
            comboBox_ReportView.UpdateLayout();

            // Re-enable and restore cursor
            Mouse.OverrideCursor = null;
            this.IsEnabled = true;

            _mainWindow.MainDataGrid.Items.Refresh();
            _mainWindow.MainDataGrid.UpdateLayout();


        }


        private static Task<(Stream?, Process?)> GetMameListXmlStreamAsync(string mamePath, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = mamePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            var process = new Process { StartInfo = startInfo };
            process.Start();

            return Task.FromResult<(Stream?, Process?)>((process.StandardOutput.BaseStream, process));
        }

        private void button_DatPageClose_Click(object sender, RoutedEventArgs e)
        {
            CloseDatToolPage();
        }

        public void CloseDatToolPage()
        {
            // Cleanup MAME process if still running
            if (_mameProcess != null && !_mameProcess.HasExited)
            {
                try
                {
                    _mameProcess.Kill();
                }
                catch { } // Process may have already exited

                _mameProcess.Dispose();
                _mameProcess = null;
            }

            // Unsubscribe from event to prevent memory leaks
            comboBox_ReportView.SelectionChanged -= ComboBox_ReportView_SelectionChanged;

            _mainWindow.MainGrid.RowDefinitions[3].Height = new GridLength(0);
            _mainWindow.gridSplitter_Horizontal.Visibility = Visibility.Collapsed;
        }

        public class GameReportItem
        {
            public string Name { get; set; } = "";
            public string CloneOf { get; set; } = "";
            public string Description { get; set; } = "";
            public string NonPlayable { get; set; } = "";
            public string CHDRequired { get; set; } = "";
            public string Missing { get; set; } = "";
            public string NotInDat { get; set; } = "";
        }

        public class DatHeader
        {
            public string Name { get; set; } = "";
            public string Version { get; set; } = "";
            public string Author { get; set; } = "";
            public string Date { get; set; } = "";
            public string Description { get; set; } = "";
        }

        private void checkBox_IncludeHidden_Checked(object sender, RoutedEventArgs e)
        {
            bool includeHidden = checkBox_IncludeHidden.IsChecked == true;
            UpdateGamelistSummaryCounts(includeHidden);
        }
    }
}