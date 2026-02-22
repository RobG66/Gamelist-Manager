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
            comboBox_ReportView.SelectedIndex = 0;
            comboBox_ReportView.IsEnabled = false;
            button_FindMissing.IsEnabled = false;

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

        private async void Button_StreamFromMame_Click(object sender, RoutedEventArgs e)
        {
            ResetDatToolPage();
            comboBox_ReportView.IsEnabled = false;

            Mouse.OverrideCursor = Cursors.Wait;
            _mainWindow.IsEnabled = false;
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
                    MessageBox.Show(
                        Window.GetWindow(this),
                        "Failed to get XML stream from MAME.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    if (_allowStreamingFromMame)
                    {
                        button_StreamFromMame.IsEnabled = true;
                    }
                    button_OpenDatFile.IsEnabled = true;
                    Mouse.OverrideCursor = null;
                    _mainWindow.IsEnabled = true;
                    return;
                }

                _mameProcess = mameProcess;
                await ProcessDatStreamAsync(xmlStream, "MAME -listxml output", _mameProcess);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Window.GetWindow(this),
                    $"Error streaming from MAME: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _mainWindow.IsEnabled = true;
                button_CloseDATPage.IsEnabled = true;
                button_StreamFromMame.IsEnabled = true;
                button_OpenDatFile.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        private async void Button_OpenDatFile_Click(object sender, RoutedEventArgs e)
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
            Mouse.OverrideCursor = Cursors.Wait;
            _mainWindow.IsEnabled = false;

            try
            {
                string datFileName = Path.GetFileName(dlg.FileName);
                using Stream xmlStream = File.OpenRead(dlg.FileName);
                await ProcessDatStreamAsync(xmlStream, datFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Window.GetWindow(this),
                    $"Error opening DAT file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _mainWindow.IsEnabled = true;
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
                    MessageBox.Show(
                        Window.GetWindow(this),
                        "No entries were found in the DAT file.", "No Data",
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
                checkBox_CsvOutput.IsEnabled = true;
                button_FindMissing.IsEnabled = true;
            }
            finally
            {
                // Dispose MAME process if provided
                mameProcess?.Dispose();
                if (mameProcess == _mameProcess)
                    _mameProcess = null;
            }
        }


        private static async Task<List<GameReportItem>> CreateGamelistSummaryAsync(List<GameReportItem> datSummary)
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
                        var romName = FilePathHelper.NormalizeRomName(row["Rom Path"].ToString()!);

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


        private static async Task<(List<GameReportItem>, DatHeader?)> CreateDatSummaryAsync(Stream xmlStream)
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

        private static GameReportItem? ParseSingleMachine(XmlReader reader, string elementName)
        {
            string name = FilePathHelper.NormalizeRomName(reader.GetAttribute("name") ?? "");
            string cloneOf = FilePathHelper.NormalizeRomName(reader.GetAttribute("cloneof") ?? "");
            string runnable = reader.GetAttribute("runnable") ?? "yes";
            string isBios = reader.GetAttribute("isbios") ?? "no";
            string isDevice = reader.GetAttribute("isdevice") ?? "no";
            string isMechanical = reader.GetAttribute("ismechanical") ?? "no";
            string romOf = reader.GetAttribute("romof") ?? "";

            var nonPlayable = new List<string>();
            var diskStatus = new List<string>();
            string description = "";
            bool needsCHD = false;
            bool hasSoftwareList = false;
            bool hasScreen = false;  

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
                if (reader.NodeType == XmlNodeType.Element)
                {
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

                        case "display":
                            hasScreen = true;
                            break;

                        case "softwarelist":
                            hasSoftwareList = true;
                            break;

                        case "device":
                            {
                                string deviceType = reader.GetAttribute("type") ?? "";
                                if (deviceType.Equals("software_list", StringComparison.OrdinalIgnoreCase))
                                {
                                    hasSoftwareList = true;
                                }
                                break;
                            }
                    }
                }
            }

            if (hasSoftwareList)
            {
                nonPlayable.Add("Software list");
            }

            //  This does not work for FBNEO dat.  What a PITA!!
            // Check if no screen tag found - indicates not a game
            //if (!hasScreen && nonPlayable.Count == 0)
            //{
            //    nonPlayable.Add("Not a game");
            //}

            return new GameReportItem
            {
                Name = name,
                CloneOf = cloneOf,
                Description = description,
                CHDRequired = needsCHD ? string.Join(Environment.NewLine, diskStatus) : "",
                NonPlayable = nonPlayable.Count > 0 ? string.Join(Environment.NewLine, nonPlayable) : "",
            };
        }

        private static (string DiskStatus, string? NonPlayableReason, bool RequiresCHD) ResolveDiskStatus(
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

        private static DatHeader? ParseDatHeaderInline(XmlReader reader)
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
                         .Select(row => FilePathHelper.NormalizeRomName(row["Rom Path"].ToString()!)),
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

            ShowReport(selectedText, dataView);
        }

        private async void ShowReport(string selectedText, DataView dataView)
        {

            // Show cursor and disable during processing
            Mouse.OverrideCursor = Cursors.Wait;
            this.IsEnabled = false;

            SharedData.ChangeTracker?.StopTracking();

            // Move the heavy work to background thread
            int itemsWithStatus = await Task.Run(() =>
            {
                var table = dataView.Table;

                // Build a lookup dictionary for fast access
                var reportLookup = _gamelistSummary
                    .ToDictionary(g => g.Name, StringComparer.OrdinalIgnoreCase);

                table.BeginLoadData();
                int count = 0;

                try
                {
                    // Iterate through ALL rows in the table
                    foreach (DataRow row in table.Rows)
                    {
                        string romName = FilePathHelper.NormalizeRomName(row["Rom Path"].ToString()!);

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
                        if (!string.IsNullOrEmpty(newStatus))
                            count++;
                    }
                }
                finally
                {
                    table.EndLoadData();
                }

                return count;
            });

            // Check if any items have status
            if (itemsWithStatus == 0)
            {
                // Hide status column
                var statusColumn = _mainWindow.MainDataGrid.Columns
                    .FirstOrDefault(c => c.Header?.ToString() == "Status");
                if (statusColumn != null)
                {
                    statusColumn.Visibility = Visibility.Collapsed;
                    if (_mainWindow.comboBox_CustomFilter.Items.Contains("Status"))
                        _mainWindow.comboBox_CustomFilter.Items.Remove("Status");
                }

                // Reset combobox selection
                comboBox_ReportView.SelectedIndex = 0;

                Mouse.OverrideCursor = null;
                this.IsEnabled = true;
                SharedData.ChangeTracker?.ResumeTracking();

                MessageBox.Show(
                    Window.GetWindow(this),
                    $"No items found for '{selectedText}' report.", "No Items",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Back on UI thread - update UI elements
            var statusColumnVisible = _mainWindow.MainDataGrid.Columns
                .FirstOrDefault(c => c.Header?.ToString() == "Status");

            if (statusColumnVisible != null)
            {
                statusColumnVisible.Visibility = Visibility.Visible;
                statusColumnVisible.Width = DataGridLength.Auto;
                if (!_mainWindow.comboBox_CustomFilter.Items.Contains("Status"))
                    _mainWindow.comboBox_CustomFilter.Items.Add("Status");
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

        private void Button_DatPageClose_Click(object sender, RoutedEventArgs e)
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

            ResetDatToolPage();
           
            _mainWindow.MainGrid.RowDefinitions[4].Height = new GridLength(0);
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

        private void CheckBox_IncludeHidden_Checked(object sender, RoutedEventArgs e)
        {
            bool includeHidden = checkBox_IncludeHidden.IsChecked == true;
            UpdateGamelistSummaryCounts(includeHidden);
        }


        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // If value contains comma, quote, or newline, wrap in quotes and escape internal quotes
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        private static string GenerateTextReport(
          List<(string Name, string Description, string NonPlayable, string CHDRequired)> missingParents,
          List<(string Name, string CloneOf, string Description, string NonPlayable, string CHDRequired)> missingClones,
          string datFileName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("===================================================================");
            sb.AppendLine("MISSING GAMES REPORT");
            sb.AppendLine("===================================================================");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"DAT File: {datFileName}");
            sb.AppendLine();
            sb.AppendLine($"Total Missing Parents: {missingParents.Count}");
            sb.AppendLine($"Total Missing Clones: {missingClones.Count}");
            sb.AppendLine($"Total Missing: {missingParents.Count + missingClones.Count}");
            sb.AppendLine("===================================================================");
            sb.AppendLine();

            // Sort and add parents
            if (missingParents.Count > 0)
            {
                sb.AppendLine("MISSING PARENTS (Alphabetical)");
                sb.AppendLine("-------------------------------------------------------------------");

                foreach (var parent in missingParents.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"{parent.Name}");

                    if (!string.IsNullOrEmpty(parent.Description))
                        sb.AppendLine($"  Description: {parent.Description}");

                    // Determine playability
                    bool isPlayable = string.IsNullOrEmpty(parent.NonPlayable);
                    sb.AppendLine($"  Playable: {(isPlayable ? "Yes" : "No")}");

                    // Show status reasons if non-playable
                    if (!string.IsNullOrEmpty(parent.NonPlayable))
                    {
                        // Convert newlines to comma-separated
                        string statusReasons = parent.NonPlayable.Replace(Environment.NewLine, ", ");
                        sb.AppendLine($"  Status: {statusReasons}");
                    }

                    // Show CHD requirements
                    if (!string.IsNullOrEmpty(parent.CHDRequired))
                    {
                        string chdInfo = parent.CHDRequired.Replace(Environment.NewLine, ", ");
                        sb.AppendLine($"  CHD Required: {chdInfo}");
                    }

                    sb.AppendLine();
                }
            }

            // Sort and add clones (by parent, then by clone name)
            if (missingClones.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("MISSING CLONES (Grouped by Parent, Alphabetical)");
                sb.AppendLine("-------------------------------------------------------------------");

                var clonesByParent = missingClones
                    .GroupBy(c => c.CloneOf, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                foreach (var parentGroup in clonesByParent)
                {
                    sb.AppendLine($"Parent: {parentGroup.Key}");

                    foreach (var clone in parentGroup.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        sb.AppendLine($"  {clone.Name}");

                        if (!string.IsNullOrEmpty(clone.Description))
                            sb.AppendLine($"    Description: {clone.Description}");

                        // Determine playability
                        bool isPlayable = string.IsNullOrEmpty(clone.NonPlayable);
                        sb.AppendLine($"    Playable: {(isPlayable ? "Yes" : "No")}");

                        // Show status reasons if non-playable
                        if (!string.IsNullOrEmpty(clone.NonPlayable))
                        {
                            // Convert newlines to comma-separated
                            string statusReasons = clone.NonPlayable.Replace(Environment.NewLine, ", ");
                            sb.AppendLine($"    Status: {statusReasons}");
                        }

                        // Show CHD requirements
                        if (!string.IsNullOrEmpty(clone.CHDRequired))
                        {
                            string chdInfo = clone.CHDRequired.Replace(Environment.NewLine, ", ");
                            sb.AppendLine($"    CHD Required: {chdInfo}");
                        }
                    }
                    sb.AppendLine();
                }
            }

            sb.AppendLine("===================================================================");
            sb.AppendLine("END OF REPORT");
            sb.AppendLine("===================================================================");

            return sb.ToString();
        }

        // Replace the GenerateCsvReport method with this enhanced version:
        private static string GenerateCsvReport(
            List<(string Name, string Description, string NonPlayable, string CHDRequired)> missingParents,
            List<(string Name, string CloneOf, string Description, string NonPlayable, string CHDRequired)> missingClones,
            string datFileName)
        {
            var sb = new StringBuilder();

            // CSV Header - added more columns
            sb.AppendLine("Type,Name,CloneOf,Description,Playable,Status,CHD Required");

            // Add parents (sorted alphabetically)
            foreach (var parent in missingParents.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
            {
                bool isPlayable = string.IsNullOrEmpty(parent.NonPlayable);
                string statusReasons = string.IsNullOrEmpty(parent.NonPlayable)
                    ? ""
                    : parent.NonPlayable.Replace(Environment.NewLine, ", ");
                string chdInfo = string.IsNullOrEmpty(parent.CHDRequired)
                    ? ""
                    : parent.CHDRequired.Replace(Environment.NewLine, ", ");

                sb.AppendLine($"Parent,{EscapeCsv(parent.Name)},,{EscapeCsv(parent.Description)},{(isPlayable ? "Yes" : "No")},{EscapeCsv(statusReasons)},{EscapeCsv(chdInfo)}");
            }

            // Add clones (sorted by parent, then by clone name)
            var clonesByParent = missingClones
                .GroupBy(c => c.CloneOf, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var parentGroup in clonesByParent)
            {
                foreach (var clone in parentGroup.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
                {
                    bool isPlayable = string.IsNullOrEmpty(clone.NonPlayable);
                    string statusReasons = string.IsNullOrEmpty(clone.NonPlayable)
                        ? ""
                        : clone.NonPlayable.Replace(Environment.NewLine, ", ");
                    string chdInfo = string.IsNullOrEmpty(clone.CHDRequired)
                        ? ""
                        : clone.CHDRequired.Replace(Environment.NewLine, ", ");

                    sb.AppendLine($"Clone,{EscapeCsv(clone.Name)},{EscapeCsv(clone.CloneOf)},{EscapeCsv(clone.Description)},{(isPlayable ? "Yes" : "No")},{EscapeCsv(statusReasons)},{EscapeCsv(chdInfo)}");
                }
            }

            return sb.ToString();
        }

        private async void Button_GenerateMissingReport_Click(object sender, RoutedEventArgs e)
        {
            if (_datSummary == null || _datSummary.Count == 0)
            {
                MessageBox.Show(
                    Window.GetWindow(this),
                    "No DAT file loaded.", "No Data",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_gamelistSummary == null || _gamelistSummary.Count == 0)
            {
                MessageBox.Show(
                    Window.GetWindow(this),
                    "No gamelist data available.", "No Data",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            this.IsEnabled = false;

            try
            {
                // Capture UI values on UI thread before going to background
                bool isTextFormat = checkBox_CsvOutput.IsChecked != true; // Unchecked = Text, Checked = CSV
                string datFileName = textBlock_DatFileName.Text;

                string reportContent = await Task.Run(() =>
                {
                    // Get all games in gamelist (no filtering)
                    var gamelistLookup = new HashSet<string>(
                        _gamelistSummary.Select(g => g.Name),
                        StringComparer.OrdinalIgnoreCase
                    );
                                        
                    // Find missing games from DAT
                    var missingParents = new List<(string Name, string Description, string NonPlayable, string CHDRequired)>();
                    var missingClones = new List<(string Name, string CloneOf, string Description, string NonPlayable, string CHDRequired)>();

                    foreach (var datItem in _datSummary)
                    {
                        // Skip if game exists in gamelist
                        if (gamelistLookup.Contains(datItem.Name))
                        {
                            continue;
                        }

                        // Skip devices, BIOS entries, and software list items
                        if (!string.IsNullOrEmpty(datItem.NonPlayable))
                        {
                            string nonPlayable = datItem.NonPlayable.ToLowerInvariant();
                            if (nonPlayable.Contains("device") ||
                                nonPlayable.Contains("bios") ||
                                nonPlayable.Contains("software list"))
                                continue;
                        }

                        if (string.IsNullOrEmpty(datItem.CloneOf))
                        {
                            // Missing parent
                            missingParents.Add((datItem.Name, datItem.Description, datItem.NonPlayable, datItem.CHDRequired));
                        }
                        else
                        {
                            // Missing clone
                            missingClones.Add((datItem.Name, datItem.CloneOf, datItem.Description, datItem.NonPlayable, datItem.CHDRequired));
                        }
                    }

                    // If nothing missing, return empty
                    if (missingParents.Count == 0 && missingClones.Count == 0)
                        return "";

                    // Generate appropriate format
                    return isTextFormat
                        ? GenerateTextReport(missingParents, missingClones, datFileName)
                        : GenerateCsvReport(missingParents, missingClones, datFileName);
                });

                if (string.IsNullOrEmpty(reportContent))
                {
                    MessageBox.Show(
                        Window.GetWindow(this),
                        "All playable games from the DAT are present in your gamelist!", "No Missing Games",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Create temp file with appropriate extension
                string extension = isTextFormat ? "txt" : "csv";
                string tempFilePath = Path.Combine(Path.GetTempPath(), $"MissingGames_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}");
                await File.WriteAllTextAsync(tempFilePath, reportContent);

                // Open file with appropriate application
                Process.Start(new ProcessStartInfo(tempFilePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Window.GetWindow(this),
                    $"Error generating report: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                this.IsEnabled = true;
            }
        }
    }
}