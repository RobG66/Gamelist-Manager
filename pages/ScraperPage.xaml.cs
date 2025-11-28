using GamelistManager.classes.api;
using GamelistManager.classes.core;
using GamelistManager.classes.gamelist;
using GamelistManager.classes.helpers;
using GamelistManager.classes.io;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;


namespace GamelistManager.pages
{
    public partial class Scraper : Page
    {
        private string _lastScraper = string.Empty;
        private string _currentScraper = string.Empty;
        private static Stopwatch _globalStopwatch = new();
        private Dictionary<string, List<string>> _emumoviesMediaLists = [];
        private CancellationTokenSource _cancellationTokenSource = new();
        private MainWindow _mainWindow;
        private Dictionary<string, Dictionary<string, string>>? _allIniSections;
        private double _card2OriginalWidth = 0;
        private bool _card2Expanded = true;
        private bool _card2InitialStateExpanded = false;
        private bool _isAuthenticated = false;
        private int _maxConcurrency = 0;
        public bool _isScraping = false;

        // Property for CancellationToken
        private CancellationToken CancellationToken => _cancellationTokenSource.Token;

        // Constructor
        public Scraper(MainWindow window)
        {
            InitializeComponent();
            _currentScraper = comboBox_SelectedScraper.Text; // Ensure comboBox_SelectedScraper is initialized
            _mainWindow = window ?? throw new ArgumentNullException(nameof(window));
            LogHelper.Instance.Initialize(LogListBox); // Ensure LogListBox is initialized before calling Initialize
            border_Card2Content.Loaded += (s, e) =>
            {
                _card2OriginalWidth = border_Card2Content.ActualWidth;
            };
        }

        private void SaveScraperSettings(string scraper, string system)
        {
            // --- Save checkboxes ---
            var allCheckBoxes = VisualTreeHelper.GetAllVisualChildren<CheckBox>(grid_MetaAndMediaControls);

            var checkedTags = allCheckBoxes
                .Where(cb => cb.IsChecked == true && cb.IsEnabled && cb.Tag != null)
                .Select(cb => cb.Tag.ToString()!.Trim())
                .ToList();

            var propertyName = $"{scraper}_Checkboxes";
            var property = typeof(Properties.Settings).GetProperty(propertyName);
            property?.SetValue(Properties.Settings.Default, JsonSerializer.Serialize(checkedTags));

            // --- Save comboboxes ---
            var allComboBoxes = VisualTreeHelper.GetAllVisualChildren<ComboBox>(grid_MediaControls)
                                .Where(cb => cb.IsEnabled && cb.Tag != null);

            var mediaSources = allComboBoxes.ToDictionary(
                cb => cb.Tag.ToString()!.Trim(),
                cb => cb.Text.Trim()
            );

            // --- Load existing media sources ---
            var allMediaSources = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            var existingJson = Properties.Settings.Default.MediaSources;

            try
            {
                allMediaSources = !string.IsNullOrEmpty(existingJson)
                    ? JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(existingJson)
                        ?? new()
                    : new();
            }
            catch
            {
                allMediaSources = new();
            }


            // --- Update nested dictionary ---
            if (!allMediaSources.ContainsKey(scraper))
                allMediaSources[scraper] = [];

            allMediaSources[scraper][system] = mediaSources;

            // --- Save back to settings ---
            Properties.Settings.Default.MediaSources = JsonSerializer.Serialize(allMediaSources);
            Properties.Settings.Default.Save();
        }

        private void LoadScraperSettings(string scraper)
        {
            string currentSystem = SharedData.CurrentSystem;

            button_Setup.IsEnabled = false;

            checkBox_OverwriteMetadata.IsEnabled = true;
            checkBox_ScrapeFromCache.IsEnabled = true;
            checkBox_ScrapeFromCache.IsChecked = false;
            checkBox_OnlyScrapeFromCache.IsEnabled = true;
            checkBox_OnlyScrapeFromCache.IsChecked = false;
            checkBox_OverwriteMetadata.IsChecked = false;
            checkBox_OverwriteMetadata.IsEnabled = true;

            // EmuMovies uses a different caching method
            // Disable cache options
            // Disable overwrite metadata
            if (_currentScraper == "EmuMovies")
            {
                button_Setup.IsEnabled = true;
                checkBox_ScrapeFromCache.IsEnabled = false;
                checkBox_ScrapeFromCache.IsChecked = false;
                checkBox_OnlyScrapeFromCache.IsEnabled = false;
                checkBox_OnlyScrapeFromCache.IsChecked = false;
                checkBox_OverwriteMetadata.IsChecked = false;
                checkBox_OverwriteMetadata.IsEnabled = false;
            }

            if (_currentScraper == "ScreenScraper")
            {
                button_Setup.IsEnabled = true;
            }

            UpdateCacheCount(_currentScraper, currentSystem);

            // --- Load checkboxes ---
            var allCheckBoxes = VisualTreeHelper.GetAllVisualChildren<CheckBox>(grid_MetaAndMediaControls);

            var availableElements = GamelistMetaData.GetScraperElements(scraper);

            var propertyName = $"{scraper}_Checkboxes";
            var property = typeof(Properties.Settings).GetProperty(propertyName);
            var checkboxJson = property?.GetValue(Properties.Settings.Default) as string ?? "[]";
            List<string> checkedTags;
            try
            {
                checkedTags = JsonSerializer.Deserialize<List<string>>(checkboxJson) ?? [];
            }
            catch
            {
                checkedTags = [];
            }

            foreach (var checkBox in allCheckBoxes)
            {
                var tag = checkBox.Tag.ToString();
                if (!string.IsNullOrEmpty(tag) && availableElements.Contains(tag))
                {
                    checkBox.IsEnabled = true;
                    checkBox.IsChecked = checkedTags.Contains(tag);
                }
                else
                {
                    checkBox.IsEnabled = false;
                    checkBox.IsChecked = false;
                }
            }

            // --- Load comboboxes ---
            var allComboBoxes = VisualTreeHelper.GetAllVisualChildren<ComboBox>(grid_MediaControls);

            // Load media sources JSON
            var mediaSourcesJson = Properties.Settings.Default.MediaSources;
            Dictionary<string, Dictionary<string, Dictionary<string, string>>> allMediaSources;
            try
            {
                allMediaSources = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(mediaSourcesJson)
                                  ?? [];
            }
            catch
            {
                allMediaSources = [];
            }

            var mediaSources = allMediaSources.TryGetValue(scraper, out var scraperDict) &&
                               scraperDict.TryGetValue(SharedData.CurrentSystem, out var systemDict)
                               ? systemDict
                               : [];

            // Load INI sections
            string iniFile = $".\\ini\\{scraper.ToLower()}_options.ini";
            _allIniSections = IniFileReader.ReadIniFile(iniFile);

            foreach (var comboBox in allComboBoxes)
            {
                comboBox.Items.Clear();
                string? tagValue = comboBox.Tag.ToString();
                if (!string.IsNullOrEmpty(tagValue) && _allIniSections.TryGetValue(tagValue, out var iniSection))
                {
                    comboBox.IsEnabled = true;
                    foreach (var key in iniSection.Keys)
                    {
                        comboBox.Items.Add(key);
                    }

                    if (mediaSources.TryGetValue(tagValue, out var previousSelection) &&
                        comboBox.Items.Cast<object>().Any(item => item.ToString() == previousSelection))
                    {
                        comboBox.SelectedItem = previousSelection;
                    }
                    else
                    {
                        comboBox.SelectedIndex = 0;
                    }
                }
                else
                {
                    comboBox.IsEnabled = false;
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SharedData.CurrentSystem))
            {
                button_Start.IsEnabled = false;
            }

            string savedScraper = Properties.Settings.Default.LastScraper;

            // Ensure that the ComboBox is populated before setting the selected sourceItem
            if (!string.IsNullOrEmpty(savedScraper))
            {
                var item = comboBox_SelectedScraper.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (i.Content as string) == savedScraper);
                if (item != null)
                {
                    comboBox_SelectedScraper.SelectedItem = item;
                }
            }
            else
            {
                comboBox_SelectedScraper.SelectedIndex = 0;
            }

            int selectedIndex = comboBox_SelectedScraper.SelectedIndex;
            _currentScraper = (comboBox_SelectedScraper.Items[selectedIndex] as ComboBoxItem)?.Content.ToString()!;
            comboBox_SelectedScraper.SelectionChanged += ComboBox_SelectedScraper_SelectionChanged;

            LoadScraperSettings(_currentScraper);


            // Set to false initially to ensure button state is correct
            ShowOrHideCounts(false);

            bool showCounts = Properties.Settings.Default.ShowCounts;
            ShowOrHideCounts(showCounts);

        }

        private void Button_SelectAllMetaData_Click(object sender, RoutedEventArgs e)
        {
            SetMetaCheckboxes(true);
        }

        private void Button_SelectAllMedia_Click(object sender, RoutedEventArgs e)
        {
            SetMediaCheckboxes(true);
        }

        private void Button_SelectNoMedia_Click(object sender, RoutedEventArgs e)
        {
            SetMediaCheckboxes(false);
        }

        private void Button_SelectNoMetaData_Click(object sender, RoutedEventArgs e)
        {
            SetMetaCheckboxes(false);
        }

        private void SetMetaCheckboxes(bool isChecked)
        {
            var checkBoxes = VisualTreeHelper.GetAllVisualChildren<CheckBox>(grid_MetadataControls);
            if (checkBoxes != null)
            {
                foreach (CheckBox checkBox in checkBoxes)
                {
                    if (checkBox.IsEnabled)
                    {
                        checkBox.IsChecked = isChecked;
                    }
                }
            }
        }

        private void SetMediaCheckboxes(bool isChecked)
        {
            var checkBoxes = VisualTreeHelper.GetAllVisualChildren<CheckBox>(grid_MediaControls);

            if (checkBoxes != null)
            {
                foreach (CheckBox checkBox in checkBoxes)
                {
                    if (checkBox.IsEnabled)
                    {
                        checkBox.IsChecked = isChecked;
                    }
                }
            }
        }

        private void ComboBox_SelectedScraper_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox || comboBox.SelectedItem == null)
            {
                // Should never be empty, but always check anyhow
                return;
            }

            ComboBoxItem selectedItem = (ComboBoxItem)comboBox.Items[comboBox.SelectedIndex];

            if (!string.IsNullOrEmpty(_lastScraper))
            {
                SaveScraperSettings(_lastScraper, SharedData.CurrentSystem);
            }

            _lastScraper = _currentScraper;
            _currentScraper = selectedItem.Content.ToString()!;

            LoadScraperSettings(_currentScraper);
            Properties.Settings.Default.LastScraper = _currentScraper;
            Properties.Settings.Default.Save();
            _isAuthenticated = false;
        }

        public void UpdateCacheCount(string scraper, string system)
        {
            button_ClearCache.IsEnabled = false;

            // Program Directory
            string programDirectory = SharedData.ProgramDirectory;

            string cachePath = $@"{programDirectory}\cache\{scraper}\{system}";

            if (!Directory.Exists(cachePath))
            {
                textBlock_cacheNumber.Text = string.Empty;
                textBlock_cacheText.Text = "Cache is empty";
            }
            else
            {
                var files = Directory.GetFiles(cachePath);
                if (files.Length == 0)
                {
                    textBlock_cacheNumber.Text = string.Empty;
                    textBlock_cacheText.Text = "Cache is empty";
                }
                else
                {
                    button_ClearCache.IsEnabled = true;
                    if (scraper != "EmuMovies")
                    {
                        textBlock_cacheNumber.Text = files.Length.ToString();
                        textBlock_cacheText.Text = "items in cache";
                    }
                    else
                    {
                        textBlock_cacheNumber.Text = string.Empty;
                        textBlock_cacheText.Text = $"Medialists cached";
                    }
                }
            }
        }

        private void Button_Start_Click(object sender, RoutedEventArgs e)
        {
            SaveScraperSettings(_currentScraper, SharedData.CurrentSystem);
            StartScraping();
        }

        private async void StopScraping(string message)
        {
            button_Stop.IsEnabled = false;
            TextSearchHelper.ClearCache();

            _globalStopwatch.Stop();
            _mainWindow.menu_Main.IsEnabled = true;
            //_mainWindow.MainDataGrid.IsEnabled = true;
            _mainWindow.MainDataGrid.IsReadOnly = false;
            _mainWindow.MainDataGrid.IsHitTestVisible = true;
            _mainWindow.stackPanel_InfoBar.IsEnabled = true;
            _mainWindow.textBox_Description.IsEnabled = true;

            if (!string.IsNullOrEmpty(message))
            {
                label_CurrentScrape.Content = "Cancelled!";
                await LogHelper.Instance.LogAsync(message, System.Windows.Media.Brushes.Red);
            }
            else
            {
                await LogHelper.Instance.LogAsync("Scraping Completed!", System.Windows.Media.Brushes.Teal);
                label_CurrentScrape.Content = "Completed!";
            }

            comboBox_SelectedScraper.IsEnabled = true;
            stackPanel_ScraperSettings.IsEnabled = true;
            stackPanel_OverwriteOptions.IsEnabled = true;
            stackPanel_ScrapeMode.IsEnabled = true;
            stackPanel_SetupAndClearCacheButtons.IsEnabled = true;

            grid_MetaAndMediaControls.IsEnabled = true;
            UpdateCacheCount(_currentScraper, SharedData.CurrentSystem);

            button_Start.IsEnabled = true;
            stackPanel_ScraperButtons.IsEnabled = true;

            bool autoExpandLogger = Properties.Settings.Default.AutoExpandLogger;
            if (autoExpandLogger && _card2InitialStateExpanded)
            {
                SetCard2Expanded(true);
            }

            _isScraping = false;

        }


        private async void StartScraping()
        {
            if (_allIniSections == null)
            {
                MessageBox.Show("INI sections not loaded correctly. Cannot start scraping.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LogHelper.Instance.ClearLog();

            string verificationStatus = Properties.Settings.Default.VerifyDownloadedImages ? "Image verification is ON" : "Image verification is OFF";
            await LogHelper.Instance.LogAsync(verificationStatus, System.Windows.Media.Brushes.Teal);

            // ParentFolderPath string
            string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename)!;

            // Local Disables
            button_Start.IsEnabled = false;
            button_Stop.IsEnabled = false; // enabled again when the jobs start
            comboBox_SelectedScraper.IsEnabled = false;
            stackPanel_ScraperSettings.IsEnabled = false;
            stackPanel_OverwriteOptions.IsEnabled = false;
            stackPanel_ScrapeMode.IsEnabled = false;
            stackPanel_ScraperButtons.IsEnabled = false;
            stackPanel_SetupAndClearCacheButtons.IsEnabled = false;
            grid_MetaAndMediaControls.IsEnabled = false;

            _card2InitialStateExpanded = _card2Expanded;
            bool autoExpandLogger = Properties.Settings.Default.AutoExpandLogger;
            if (autoExpandLogger)
            {
                SetCard2Expanded(false);
            }

            // Parent Window disables
            _mainWindow.menu_Main.IsEnabled = false;
            _mainWindow.stackPanel_InfoBar.IsEnabled = false;
            _mainWindow.textBox_Description.IsEnabled = false;
            //_mainWindow.MainDataGrid.IsEnabled = false;
            _mainWindow.MainDataGrid.IsReadOnly = true;
            _mainWindow.MainDataGrid.IsHitTestVisible = false;

            // Reset labels and progressbar Value
            label_CurrentScrape.Content = "N/A";
            label_Percentage.Content = "0%";
            progressBar_ProgressBar.Value = 0;
            label_ThreadCount.Content = "1";
            label_ProgressBarCount.Content = "N/A";
            label_ScrapeLimitCount.Content = "N/A";

            // Make a list of elements to be scraped
            // Enumerate checked checkboxes and get tag Value
            // These tags match metadata key names
            var elementsToScrape = VisualTreeHelper
               .GetAllVisualChildren<CheckBox>(grid_MetaAndMediaControls)
               .Where(cb => cb.IsChecked == true && cb.Tag != null)
               .Select(cb => cb.Tag.ToString()!.Trim())
               .Where(tag => !string.IsNullOrEmpty(tag))
               .ToList();

            if (elementsToScrape.Count == 0)
            {
                StopScraping("There were no items selected.");
                return;
            }

            // Create bool values based on gui element state
            bool scrapeAll = radioButton_ScrapeAll.IsChecked == true;
            bool scrapeHidden = checkBox_ScrapeHidden.IsChecked == true;

            // All or selected based on scrapeAll bool
            var datagridRowSelection = scrapeAll
            ? _mainWindow.MainDataGrid.Items.OfType<DataRowView>().ToList()
            : _mainWindow.MainDataGrid.SelectedItems.OfType<DataRowView>().ToList();

            // Filter hidden items if scrapeHidden is false (default)
            if (!scrapeHidden)
            {
                datagridRowSelection = datagridRowSelection
                    .Where(rowView => !Convert.ToBoolean(rowView.Row["Hidden"]))
                    .ToList();
            }

            // Sometimes if you have selected rows, but exclude hidden, there's nothing to scrape
            if (datagridRowSelection.Count == 0)
            {
                StopScraping("There were no items to scrape!");
                return;
            }


            // Is there a systems section?
            // ArcadeDB for example does not use this
            var systemID = string.Empty;
            if (_allIniSections.TryGetValue("Systems", out var section))
            {
                if (!section.TryGetValue(SharedData.CurrentSystem, out systemID))
                {
                    StopScraping($"A system ID is missing for system '{SharedData.CurrentSystem}'");
                    return;
                }
            }

            List<string> screenscraperRegions = [];
            string screenscraperLanguage = string.Empty;
            string userName = string.Empty;
            string userPassword = string.Empty;
            string userAccessToken = string.Empty;
            bool scrapeByCache = checkBox_ScrapeFromCache.IsChecked == true;
            bool skipNonCached = checkBox_OnlyScrapeFromCache.IsChecked == true;
            bool overWriteName = checkBox_OverwriteNames.IsChecked == true;
            bool overWriteMetadata = checkBox_OverwriteMetadata.IsChecked == true;
            bool overWriteMedia = checkBox_OverwriteMedia.IsChecked == true;
            bool englishGenreOnly = Properties.Settings.Default.ScrapeEnglishGenreOnly; // ScreenScraper Specific
            bool scrapeAnyMedia = Properties.Settings.Default.ScrapeAnyMedia; // ScreenScraper Specific
            bool isArcade = ArcadeSystemID.HasArcadeSystemName(SharedData.CurrentSystem);

            if (overWriteMetadata)
            {
                overWriteName = true;
            }

            string cacheFolder = $"{SharedData.ProgramDirectory}\\cache\\{_currentScraper}\\{SharedData.CurrentSystem}";
            if (!Directory.Exists(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }

            switch (_currentScraper)
            {
                case "ArcadeDB":
                    {
                        _maxConcurrency = 1;
                    }
                    break;


                case "EmuMovies":
                    {
                        label_CurrentScrape.Content = "Authenticating...";
                        Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                        (userName, userPassword) = CredentialHelper.GetCredentials("EmuMovies");
                        if (string.IsNullOrEmpty(userName))
                        {
                            StopScraping($"Credentials for {_currentScraper} are not configured.");
                            return;
                        }

                        await LogHelper.Instance.LogAsync($"Verifying {_currentScraper} credentials...", System.Windows.Media.Brushes.Teal);

                        // Get the user access token
                        using var httpClient = new HttpClient();
                        API_EmuMovies aPI_EmuMovies = new API_EmuMovies(httpClient);
                        userAccessToken = await aPI_EmuMovies.AuthenticateEmuMoviesAsync(userName, userPassword); if (string.IsNullOrEmpty(userAccessToken))
                        {
                            StopScraping("Error retrieving user authentication token from EmuMovies.");
                            return;
                        }

                        // Retrieve media types for current system
                        List<string> emumoviesMediaTypes = [];
                        emumoviesMediaTypes = await aPI_EmuMovies.GetMediaTypes(systemID);
                        if (emumoviesMediaTypes == null)
                        {
                            StopScraping($"Error retrieving media lists for {systemID}.");
                            return;
                        }

                        if (!File.Exists($"{cacheFolder}\\cache.json"))
                        {
                            label_CurrentScrape.Content = "Downloading media lists...";
                            await LogHelper.Instance.LogAsync("Downloading media lists...", System.Windows.Media.Brushes.Teal);
                            Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

                            foreach (string mediaType in emumoviesMediaTypes)
                            {
                                List<string> medialist = await aPI_EmuMovies.GetMediaList(systemID, mediaType);
                                _emumoviesMediaLists[mediaType] = medialist;
                            }
                            string json = JsonSerializer.Serialize(_emumoviesMediaLists, new JsonSerializerOptions { WriteIndented = true });
                            File.WriteAllText($"{cacheFolder}\\cache.json", json);
                        }
                        else
                        {
                            string json = File.ReadAllText($"{cacheFolder}\\cache.json");
                            var mediaLists = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);

                            if (mediaLists != null)
                            {
                                foreach (var kvp in mediaLists)
                                {
                                    _emumoviesMediaLists[kvp.Key] = kvp.Value;
                                }
                            }
                        }
                        _maxConcurrency = 2;
                    }
                    break;

                case "ScreenScraper":
                    {
                        label_CurrentScrape.Content = "Authenticating...";

                        (userName, userPassword) = CredentialHelper.GetCredentials("ScreenScraper");
                        if (string.IsNullOrEmpty(userName))
                        {
                            StopScraping($"The credentials for ScreenScraper are not configured.");
                            return;
                        }

                        await LogHelper.Instance.LogAsync($"Verifying {_currentScraper} credentials...", System.Windows.Media.Brushes.Teal);
                        Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

                        if (!_isAuthenticated)
                        {
                            using var httpClient = new HttpClient();
                            var aPI_ScreenScraper = new API_ScreenScraper(httpClient);
                            _maxConcurrency = await aPI_ScreenScraper.AuthenticateAsync(userName, userPassword);

                            if (_maxConcurrency == -1)
                            {
                                StopScraping("The ScreenScraper credentials failed to logon to remote server.");
                                await LogHelper.Instance.LogAsync($"Logon failed, please check the credentials.", System.Windows.Media.Brushes.Red);
                                return;
                            }
                            _isAuthenticated = true;
                        }

                        await LogHelper.Instance.LogAsync($"Max Threads: {_maxConcurrency}", System.Windows.Media.Brushes.Teal);

                        string pattern = @"\((.*?)\)";

                        string primaryRegion = Properties.Settings.Default.Region;
                        string primaryRegionCode = "us";

                        if (!string.IsNullOrEmpty(primaryRegion))
                        {
                            // This regex captures whatever is inside (...)
                            var match = Regex.Match(primaryRegion, @"\((.*?)\)");
                            if (match.Success)
                            {
                                primaryRegionCode = match.Groups[1].Value;
                            }
                        }

                        string jsonFallbackRegions = Properties.Settings.Default.Region_Fallback;
                        var fallbackRegions = JsonSerializer.Deserialize<List<string>>(jsonFallbackRegions) ?? [];

                        // Extract the values inside parentheses
                        foreach (var item in fallbackRegions)
                        {
                            var match = Regex.Match(item, @"\((.*?)\)");
                            if (match.Success)
                            {
                                screenscraperRegions.Add(match.Groups[1].Value);
                            }
                        }

                        await LogHelper.Instance.LogAsync($"Region: {primaryRegionCode}", System.Windows.Media.Brushes.Teal);
                        await LogHelper.Instance.LogAsync(
                            $"Region Fallback: {string.Join(", ", screenscraperRegions)}",
                            System.Windows.Media.Brushes.Teal
                            );

                        // Always add primary Region as first item
                        screenscraperRegions.Insert(0, primaryRegionCode);

                        string language = Properties.Settings.Default.Language;

                        if (!string.IsNullOrEmpty(language))
                        {
                            Match match = Regex.Match(language, pattern);
                            if (match.Success)
                            {
                                screenscraperLanguage = match.Groups[1].Value;
                            }
                            else
                            {
                                screenscraperLanguage = "en";
                            }
                        }
                        else
                        {
                            screenscraperLanguage = "en";
                        }

                        await LogHelper.Instance.LogAsync($"Language : {screenscraperLanguage}", System.Windows.Media.Brushes.Teal);
                    }
                    break;
            }
            ;

            // Retrieve source values for media
            string? imageSource = GetSource("ImageSource", comboBox_ImageSource.Text);
            string? wheelSource = GetSource("WheelSource", comboBox_WheelSource.Text);
            string? thumbnailSource = GetSource("ThumbnailSource", comboBox_ThumbnailSource.Text);
            string? boxartSource = GetSource("BoxArtSource", comboBox_BoxArtSource.Text);
            string? marqueeSource = GetSource("MarqueeSource", comboBox_MarqueeSource.Text);
            string? cartridgeSource = GetSource("CartridgeSource", comboBox_CartridgeSource.Text);
            string? videoSource = GetSource("VideoSource", comboBox_VideoSource.Text);


            // Setup counters 
            int scraperCount = 0;
            int scraperTotal = datagridRowSelection.Count;
            await LogHelper.Instance.LogAsync($"Items To Scrape: {scraperTotal}", System.Windows.Media.Brushes.Teal);
            DateTime startTime = DateTime.Now;

            // Setup progress bar
            progressBar_ProgressBar.Value = 0;
            progressBar_ProgressBar.Minimum = 0;
            progressBar_ProgressBar.Maximum = scraperTotal;

            // Show maximum threads
            label_ThreadCount.Content = _maxConcurrency.ToString();

            // Reset timer
            _globalStopwatch.Reset();
            _globalStopwatch.Start();

            // Reset cancellation tokensource
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            // Get the media paths dictionary
            string jsonString = Properties.Settings.Default.MediaPaths;
            Dictionary<string, string> mediaPaths = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString)!;

            // Declare basic scraper parameters
            // These do not change
            ScraperParameters baseScraperParameters = new ScraperParameters();
            baseScraperParameters.SystemID = systemID;
            baseScraperParameters.UserID = userName;
            baseScraperParameters.UserPassword = userPassword;
            baseScraperParameters.ParentFolderPath = parentFolderPath;
            baseScraperParameters.SSLanguage = screenscraperLanguage;
            baseScraperParameters.ScrapeEnglishGenreOnly = englishGenreOnly;
            baseScraperParameters.ScrapeAnyMedia = scrapeAnyMedia;
            baseScraperParameters.SSRegions = screenscraperRegions;
            baseScraperParameters.ImageSource = imageSource;
            baseScraperParameters.ThumbnailSource = thumbnailSource;
            baseScraperParameters.WheelSource = wheelSource;
            baseScraperParameters.BoxArtSource = boxartSource;
            baseScraperParameters.MarqueeSource = marqueeSource;
            baseScraperParameters.CartridgeSource = cartridgeSource;
            baseScraperParameters.VideoSource = videoSource;
            baseScraperParameters.OverwriteMedia = overWriteMedia;
            baseScraperParameters.OverwriteMetadata = overWriteMetadata;
            baseScraperParameters.OverwriteName = overWriteName;
            baseScraperParameters.UserAccessToken = userAccessToken;
            baseScraperParameters.ScraperPlatform = _currentScraper;
            baseScraperParameters.MediaPaths = mediaPaths;
            baseScraperParameters.ElementsToScrape = elementsToScrape!;
            baseScraperParameters.ScrapeByCache = scrapeByCache;
            baseScraperParameters.SkipNonCached = skipNonCached;
            baseScraperParameters.CacheFolder = cacheFolder;
            baseScraperParameters.ScrapeByGameID = false;
            baseScraperParameters.Verify = Properties.Settings.Default.VerifyDownloadedImages;
            
            // Zero daily scrape counters
            int scrapeLimitMax = 0;
            int scrapeLimitProgress = 0;


            // StopPlaying button is now enabled since scraping will begin
            button_Stop.IsEnabled = true;

            // Arcade Names for Region Scraping
            string filePath = Properties.Settings.Default.MamePath;
            if (MameNames.Names.Count == 0 && isArcade)
            {
                if (File.Exists(filePath))
                {
                    await LogHelper.Instance.LogAsync("Generating arcade names dictionary...", System.Windows.Media.Brushes.Teal);
                    await MameNames.GenerateAsync(filePath);
                }
                else
                {
                    await LogHelper.Instance.LogAsync("MAME executable path is not valid or configured!", System.Windows.Media.Brushes.Orange);
                }
            }

            if (MameNames.Names.Count == 0 && isArcade)
            {
                await LogHelper.Instance.LogAsync("Arcade name dictionary is not created.", System.Windows.Media.Brushes.Orange);
                await LogHelper.Instance.LogAsync("Region and Language scraping will be limited.", System.Windows.Media.Brushes.Orange);
            }

            //
            // Actual scraping starts here
            //

            _isScraping = true;
            SharedData.ChangeTracker!.StartBulkOperation();

            var serviceCollection = new ServiceCollection();
            var startup = new Startup(new ConfigurationBuilder().Build());
            startup.ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            label_CurrentScrape.Content = "Ready!";

            stackPanel_ScraperButtons.IsEnabled = true;

            // await LogHelper.Instance.LogAsync("Starting in 5 seconds!", System.Windows.Media.Brushes.Teal);
            await Task.Delay(2000);

            var tasks = new List<Task>();

            using var semaphore = new SemaphoreSlim(_maxConcurrency);
            try
            {
                foreach (DataRowView rowView in datagridRowSelection)
                {
                    await semaphore.WaitAsync(CancellationToken);

                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            CancellationToken.ThrowIfCancellationRequested();

                            string romPath = rowView["Rom Path"].ToString()!; // Never empty
                            string romFileNameNoExtension = Path.GetFileNameWithoutExtension(romPath);
                            string romFileNameWithExtension = romPath[2..];
                            string gameName = rowView["Name"]?.ToString() ?? romFileNameNoExtension;
                            string gameID = rowView["Game Id"]?.ToString() ?? string.Empty;

                            Interlocked.Increment(ref scraperCount);

                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                progressBar_ProgressBar.Value++;
                            });

                            UpdateProgressLabels(gameName, startTime, scraperCount, scraperTotal, scrapeLimitProgress, scrapeLimitMax);

                            var scraperParameters = baseScraperParameters.Clone();
                            scraperParameters.RomFileNameWithoutExtension = romFileNameNoExtension;
                            scraperParameters.RomFileNameWithExtension = romFileNameWithExtension;
                            scraperParameters.GameID = gameID;
                            scraperParameters.Name = gameName;

                            // Determine MAME arcade name for Region/language scraping
                            string? mameArcadeName = MameNames.Names.TryGetValue(romFileNameNoExtension, out string? arcadeName) ? arcadeName : string.Empty;
                            if (scraperParameters.ElementsToScrape!.Contains("Region"))
                            {
                                string nameValue = !string.IsNullOrEmpty(mameArcadeName) ? mameArcadeName : romFileNameNoExtension;
                                string romRegion = RegionLanguageHelper.GetRegion(nameValue);
                                rowView.Row["Region"] = romRegion;
                            }
                            if (scraperParameters.ElementsToScrape.Contains("Language"))
                            {
                                string nameValue = !string.IsNullOrEmpty(mameArcadeName) ? mameArcadeName : romFileNameNoExtension;
                                string romLanguage = RegionLanguageHelper.GetLanguage(nameValue);
                                rowView.Row["Language"] = romLanguage;
                            }

                            var itemsToScrape = new List<string>(scraperParameters.ElementsToScrape);
                            var itemsToRemove = new List<string>();

                            // Always remove Region and Language from the list, these are handled internally below
                            itemsToScrape.Remove("region");
                            itemsToScrape.Remove("language");

                            // Check if we need to skip items based on current Value and overwrite settings
                            foreach (var item in itemsToScrape!)
                            {
                                string columnName = GamelistMetaData.GetMetadataNameByType(item);
                                var itemValue = rowView.Row[columnName];
                                if (itemValue == null || itemValue == DBNull.Value || string.IsNullOrEmpty(itemValue.ToString()))
                                {
                                    continue;
                                }

                                string metaDataType = GamelistMetaData.GetMetadataDataTypeByType(item);
                                
                                if (item == "name")
                                {
                                    if (!overWriteName)
                                    {
                                        itemsToRemove.Add(item);
                                    }
                                    continue;
                                }

                                // Skip text metadata if overwrite is disabled
                                if (!overWriteMetadata && metaDataType == "String")
                                {
                                    itemsToRemove.Add(item);
                                    continue;
                                }

                                // Skip media (image or document) if overwrite is disabled
                                if (!overWriteMedia && (metaDataType == "Image" || metaDataType == "Document" || metaDataType == "Video"))
                                {
                                    itemsToRemove.Add(item);
                                    continue;
                                }
                            }

                            // remove after iterating
                            foreach (var item in itemsToRemove)
                            {
                                itemsToScrape.Remove(item);
                            }

                            if (itemsToScrape.Count == 0)
                            {
                                await LogHelper.Instance.LogAsync($"Skipping '{romFileNameWithExtension}', nothing to scrape", System.Windows.Media.Brushes.Orange);
                                return;
                            }

                            scraperParameters.ElementsToScrape = itemsToScrape;

                            bool scrapeResult = false;
                            await LogHelper.Instance.LogAsync($"Scraping {romFileNameWithExtension}", System.Windows.Media.Brushes.Green);

                            Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);

                            switch (_currentScraper)
                            {
                                case "ArcadeDB":
                                    var arcadedb_scraper = serviceProvider.GetRequiredService<API_ArcadeDB>();
                                    scrapeResult = await arcadedb_scraper.ScrapeArcadeDBAsync(rowView, scraperParameters);
                                    break;

                                case "EmuMovies":
                                    var emumovies_scraper = serviceProvider.GetRequiredService<API_EmuMovies>();
                                    scrapeResult = await emumovies_scraper.ScrapeEmuMoviesAsync(rowView, scraperParameters, _emumoviesMediaLists);
                                    break;

                                case "ScreenScraper":
                                    var screenscraper_scraper = serviceProvider.GetRequiredService<API_ScreenScraper>();
                                    Tuple<int, int> result = await screenscraper_scraper.ScrapeScreenScraperAsync(rowView, scraperParameters).ConfigureAwait(false);
                                    if (result != null)
                                    {
                                        scrapeResult = true;
                                        scrapeLimitProgress = result.Item1;
                                        scrapeLimitMax = result.Item2;
                                        UpdateProgressLabels(gameName, startTime, scraperCount, scraperTotal, scrapeLimitProgress, scrapeLimitMax);
                                        if (scrapeLimitProgress >= scrapeLimitMax && scrapeLimitMax > 0)
                                        {
                                            await LogHelper.Instance.LogAsync($"Daily scrape limit reached!", System.Windows.Media.Brushes.Red);
                                            _cancellationTokenSource.Cancel();
                                        }
                                    }
                                    else
                                    {
                                        scrapeResult = false;
                                    }
                                    break;
                            }

                            if (!scrapeResult)
                            {
                                await LogHelper.Instance.LogAsync($"Could not scrape '{romFileNameWithExtension}'", System.Windows.Media.Brushes.Red);
                            }
                            else
                            {
                                await LogHelper.Instance.LogAsync($"Finished scrape for {romFileNameWithExtension}", System.Windows.Media.Brushes.Green);
                            }

                            ShowOrHideCounts(true);
                        }
                        catch (OperationCanceledException)
                        {
                            // Gracefully handle cancellation inside the task.
                        }
                        catch (Exception ex)
                        {
                            await LogHelper.Instance.LogAsync($"Error: '{ex.Message}'", System.Windows.Media.Brushes.Red);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, CancellationToken);

                    tasks.Add(task);
                }
            }
            catch (OperationCanceledException)
            {
                await LogHelper.Instance.LogAsync($"Scraping cancelled by user request", System.Windows.Media.Brushes.Red);
            }
            catch (Exception ex)
            {
                await LogHelper.Instance.LogAsync($"Unexpected error: {ex.Message}", System.Windows.Media.Brushes.Red);
            }
            finally
            {
                try
                {
                    // Ensure all tasks are awaited, even if canceled or failed.
                    await Task.WhenAll(tasks);
                    SharedData.DataSet.AcceptChanges();
                    SharedData.ChangeTracker!.EndBulkOperation();

                }
                catch (Exception ex)
                {
                    // Catch any remaining task exceptions after waiting.
                    await LogHelper.Instance.LogAsync($"Error while waiting for tasks to complete: {ex.Message}", System.Windows.Media.Brushes.Red);
                }

                // Ensure StopScraping is always executed after all tasks finish.
                await LogHelper.Instance.LogAsync($"Total time: {FormatElapsedTime(_globalStopwatch.Elapsed)}", System.Windows.Media.Brushes.Green);

                // Empty message means it was ok.
                StopScraping(string.Empty);
            }
        }


        private static string FormatElapsedTime(TimeSpan elapsed)
        {
            if (elapsed.TotalSeconds < 60)
            {
                return $"{elapsed.Seconds} seconds";
            }
            else if (elapsed.TotalMinutes < 60)
            {
                return $"{elapsed.Minutes} minutes {elapsed.Seconds} seconds";
            }
            else
            {
                return $"{elapsed.Hours} hours {elapsed.Minutes} minutes";
            }
        }

        public void UpdateProgressLabels(string romLabel, DateTime startTime, int current, int total, int scrapeLimitProgress, int scrapeLimitMax)
        {
            // Updates in UI thread:
            Dispatcher.BeginInvoke(() =>
            {
                // Update scraper limits
                if (scrapeLimitMax > 0)
                {
                    label_ScrapeLimitCount.Content = $"{scrapeLimitProgress}/{scrapeLimitMax}";
                }

                // Update progress bar counter
                label_ProgressBarCount.Content = $"{current}\\{total}";

                // Update current rom text
                label_CurrentScrape.Content = romLabel;

                // Calcuate remaining time
                double progress = (double)current / total * 100;
                TimeSpan elapsed = DateTime.Now - startTime;
                double remainingMilliseconds = (elapsed.TotalMilliseconds / progress) * (100 - progress);
                TimeSpan remainingTime = TimeSpan.FromMilliseconds(remainingMilliseconds);

                // Reduce the remaing time string by removing 0 time parts
                string remainingTimeString = string.Empty;
                if (remainingTime.Hours > 0)
                {
                    remainingTimeString += $"{remainingTime.Hours:D2}h ";
                }

                if (remainingTime.Minutes > 0)
                {
                    remainingTimeString += $"{remainingTime.Minutes:D2}m ";
                }

                if (remainingTime.Hours == 0)
                {
                    remainingTimeString += $"{remainingTime.Seconds:D2}s";
                }

                // Update time remaining
                label_Percentage.Content = $"{progress:F0}% | Remaining Time: {remainingTimeString}";

            });
        }

        private void Button_Setup_Click(object sender, RoutedEventArgs e)
        {
            ScraperCredentialWindow scraperCredentialDialog = new(_currentScraper);
            scraperCredentialDialog.ShowDialog();
        }

        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {
            button_Stop.IsEnabled = false;
            _cancellationTokenSource.Cancel();
            label_CurrentScrape.Content = "Waiting for task(s) to complete...";
        }

        private void Button_Log_Click(object sender, RoutedEventArgs e)
        {
            // Not implemented - yet!
        }

        private void Button_ClearCache_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentScraper) || string.IsNullOrEmpty(SharedData.CurrentSystem))
            {
                return;
            }

            string cacheFolder = $"{SharedData.ProgramDirectory}\\cache\\{_currentScraper}\\{SharedData.CurrentSystem}";
            if (!Directory.Exists(cacheFolder))
            {
                MessageBox.Show("Cache is already empty.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Do you want to clear the {_currentScraper} cache files for '{SharedData.CurrentSystem}'?\n\n" +
                "Warning: This cannot be undone and the cache will have to be scraped again.",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string[] files = Directory.GetFiles(cacheFolder);
            foreach (string file in files)
            {
                File.Delete(file);
            }

            UpdateCacheCount(_currentScraper, SharedData.CurrentSystem);

            MessageBox.Show("Cache has been cleared.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Button_ShowCounts_Click(object sender, RoutedEventArgs e)
        {
            bool showCounts = button_ShowCounts.Content.ToString() == "Show Counts";

            // Toggle the button text
            button_ShowCounts.Content = showCounts ? "Hide Counts" : "Show Counts";

            // Update and save the setting
            Properties.Settings.Default.ShowCounts = showCounts;
            Properties.Settings.Default.Save();

            ShowOrHideCounts(showCounts);
        }

        public void ShowOrHideCounts(bool showCounts)
        {
            Dispatcher.BeginInvoke(() =>
            {
                var checkBoxes = VisualTreeHelper.GetAllVisualChildren<CheckBox>(grid_MediaControls);
                var countRegex = new Regex(@"\s*\(.*?\)", RegexOptions.Compiled);

                foreach (CheckBox checkBox in checkBoxes)
                {
                    if (checkBox.Tag == null) continue;
                    string tagString = checkBox.Tag.ToString()!;

                    // Skip if not a valid enum
                    if (!Enum.TryParse<MetaDataKeys>(tagString, true, out var metaDataKey)) continue;

                    string nameValue = GamelistMetaData.GetMetaDataDictionary()[metaDataKey].Name;
                    int count = SharedData.DataSet.Tables[0].AsEnumerable()
                        .Count(row => !row.IsNull(nameValue) && !string.IsNullOrWhiteSpace(row[nameValue].ToString()));

                    // Remove existing counts from content
                    string content = checkBox.Content?.ToString() ?? string.Empty;
                    content = countRegex.Replace(content, string.Empty).Trim();

                    // Append count if needed (skip music)
                    if (showCounts && tagString != "music")
                    {
                        content = $"{content} ({count})";
                    }

                    checkBox.Content = content;
                }
            });
        }


        private void Button_ResetSources_Click(object sender, RoutedEventArgs e)
        {
            var allComboBoxes = VisualTreeHelper.GetAllVisualChildren<ComboBox>(grid_MediaControls).ToList();

            // Filter enabled ComboBox controls
            var enabledComboBoxes = allComboBoxes
                .Where(cb => cb.IsEnabled == true);

            foreach (ComboBox combobox in enabledComboBoxes)
            {
                combobox.SelectedIndex = 0;
            }

        }

        private void CheckBox_ScrapeFromCache_Checked(object sender, RoutedEventArgs e)
        {
            checkBox_OnlyScrapeFromCache.IsEnabled = true;
        }

        private void CheckBox_ScrapeFromCache_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBox_OnlyScrapeFromCache.IsEnabled = false;
        }

        private void Button_ToggleCard2_Click(object sender, RoutedEventArgs e)
        {
            // Toggle the expanded state
            SetCard2Expanded(!_card2Expanded);
            _card2Expanded = !_card2Expanded;
        }

        public void SetCard2Expanded(bool expanded)
        {
            // Current width of the animated border
            double from = border_Card2Content.ActualWidth;

            // Target width
            double to = expanded ? _card2OriginalWidth : 0;

            // Update expander button image
            image_ExpanderButton.Source = new BitmapImage(new Uri(
                expanded
                ? "pack://application:,,,/resources/images/buttons/greydoublearrowleft.png"
                : "pack://application:,,,/resources/images/buttons/greydoublearrowright.png",
                UriKind.Absolute));

            // Animate width
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            animation.Completed += (s, e) =>
            {
                // Clear animation so layout can take over
                border_Card2Content.BeginAnimation(WidthProperty, null);

                // Restore normal layout behavior
                if (expanded)
                    border_Card2Content.Width = double.NaN; // Auto
                else
                    border_Card2Content.Width = 0;
            };

            border_Card2Content.BeginAnimation(WidthProperty, animation);
        }

        private string? GetSource(string sectionName, string key)
        {
            return _allIniSections!.TryGetValue(sectionName, out var section)
                && section.TryGetValue(key, out var value)
                ? value
                : null;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource.Dispose();
        }
    }
}
