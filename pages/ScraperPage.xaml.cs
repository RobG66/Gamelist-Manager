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
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private CancellationTokenSource _cancellationTokenSource = new();
        private MainWindow _mainWindow;
        private Dictionary<string, Dictionary<string, string>>? _allIniSections;
        private double _card2OriginalWidth = 0;
        private bool _card2Expanded = true;
        private bool _card2InitialStateExpanded = false;
        private int _scrapeLimitMax = 0;
        private int _scrapeLimitProgress = 0;
        public bool _isScraping = false;
        private static SemaphoreSlim _scrapeSemaphore = new SemaphoreSlim(1, 1);
        private static SemaphoreSlim _downloadSemaphore = new SemaphoreSlim(1, 1);
        private static readonly Regex CountRegex = new Regex(@"\s*\(.*?\)", RegexOptions.Compiled);
        private static IServiceProvider? serviceProvider;
        private List<string> _mediaTypes = new List<string>();
        private readonly Dictionary<string, int> _downloadStats = new();
        private readonly object _downloadStatsLock = new();
        private static readonly Dictionary<(string SystemId, string MediaType), List<string>> _mediaListCache = new();


        // Property for CancellationToken
        private CancellationToken CancellationToken => _cancellationTokenSource.Token;

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

            var serviceCollection = new ServiceCollection();
            var startup = new Startup(new ConfigurationBuilder().Build());
            startup.ConfigureServices(serviceCollection);
            serviceProvider = serviceCollection.BuildServiceProvider();

        }

        private void RecordDownload(string mediaType)
        {
            lock (_downloadStatsLock)
            {
                if (!_downloadStats.ContainsKey(mediaType))
                {
                    _downloadStats[mediaType] = 0;
                }
                _downloadStats[mediaType]++;
            }
        }

        private async Task LogDownloadSummaryAsync()
        {
            if (_downloadStats.Count == 0)
            {
                LogHelper.Instance.Log("No media files were downloaded", System.Windows.Media.Brushes.Orange);
                return;
            }

            int totalDownloads = _downloadStats.Values.Sum();
            LogHelper.Instance.Log($"Download Summary: {totalDownloads} file(s) downloaded", System.Windows.Media.Brushes.Teal);

            // Sort by count descending, then by name
            var sortedStats = _downloadStats
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key);

            foreach (var stat in sortedStats)
            {
                LogHelper.Instance.Log($"  {stat.Key}: {stat.Value}", System.Windows.Media.Brushes.Teal);
            }
        }

        private void SaveScraperSettings(string scraper, string system)
        {
            // --- Save checkboxes ---
            var allCheckBoxes = TreeHelper.GetAllVisualChildren<CheckBox>(grid_MetaAndMediaControls);

            var checkedTags = allCheckBoxes
                .Where(cb => cb.IsChecked == true && cb.IsEnabled && cb.Tag != null)
                .Select(cb => cb.Tag.ToString()!.Trim())
                .ToList();

            var propertyName = $"{scraper}_Checkboxes";
            var property = typeof(Properties.Settings).GetProperty(propertyName);
            property?.SetValue(Properties.Settings.Default, JsonSerializer.Serialize(checkedTags));

            // --- Save comboboxes ---
            var allComboBoxes = TreeHelper.GetAllVisualChildren<ComboBox>(grid_MediaControls)
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
            checkBox_ScrapeFromCache.IsChecked = true;
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
            var allCheckBoxes = TreeHelper.GetAllVisualChildren<CheckBox>(grid_MetaAndMediaControls);

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
            var allComboBoxes = TreeHelper.GetAllVisualChildren<ComboBox>(grid_MediaControls);

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
            var checkBoxes = TreeHelper.GetAllVisualChildren<CheckBox>(grid_MetadataControls);
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
            var checkBoxes = TreeHelper.GetAllVisualChildren<CheckBox>(grid_MediaControls);

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

        public void ComboBox_SelectedScraper_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

            UpdateCacheCount(_currentScraper, SharedData.CurrentSystem);
        
        }

        public void UpdateCacheCount(string scraper, string system)
        {
            string cachePath = Path.Combine(SharedData.ProgramDirectory, "cache", scraper, system);
            
            if (scraper == "EmuMovies")
            {
                stackPanel_CacheCount.Visibility = Visibility.Collapsed;
                button_ClearCache.IsEnabled = false;
                return;
            }

            stackPanel_CacheCount.Visibility = Visibility.Visible;
                        
            if (!Directory.Exists(cachePath) || Directory.GetFiles(cachePath).Length == 0)
            {
                textBlock_cacheNumber.Text = string.Empty;
                textBlock_cacheText.Text = "Cache is empty";
                button_ClearCache.IsEnabled = false;
            }
            else
            {
                var filesCount = Directory.GetFiles(cachePath).Length;
                button_ClearCache.IsEnabled = true;
                stackPanel_CacheCount.Visibility = Visibility.Visible;
                textBlock_cacheNumber.Text = filesCount.ToString();
                textBlock_cacheText.Text = "items in cache";
            }
        }


        private void Button_Start_Click(object sender, RoutedEventArgs e)
        {
            SaveScraperSettings(_currentScraper, SharedData.CurrentSystem);
            StartScraping();
        }

        private async Task StopScraping(string message)
        {
            button_Stop.IsEnabled = false;
            TextSearchHelper.ClearCache();

            _globalStopwatch.Stop();
            _mainWindow.menu_Main.IsEnabled = true;
            _mainWindow.RibbonMenu.IsEnabled = true;
            //_mainWindow.MainDataGrid.IsEnabled = true;
            _mainWindow.MainDataGrid.IsReadOnly = false;
            _mainWindow.MainDataGrid.IsHitTestVisible = true;
            _mainWindow.stackPanel_InfoBar.IsEnabled = true;
            _mainWindow.textBox_Description.IsEnabled = true;

            if (!string.IsNullOrEmpty(message))
            {
                label_CurrentScrape.Content = "Cancelled!";
                LogHelper.Instance.Log(message, System.Windows.Media.Brushes.Red);
            }
            else
            {
                LogHelper.Instance.Log("Scraping Completed!", System.Windows.Media.Brushes.Teal);
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

            await LogHelper.Instance.FlushFileLogAsync();

            _isScraping = false;

        }

        private void PrepareUIForScraping()
        {
            LogHelper.Instance.ClearLog();
        
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
            _mainWindow.RibbonMenu.IsEnabled = false;
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

        }

        private List<string> GetElementsToScrape()
        {
            return TreeHelper
               .GetAllVisualChildren<CheckBox>(grid_MetaAndMediaControls)
               .Where(cb => cb.IsChecked == true && cb.Tag != null)
               .Select(cb => cb.Tag.ToString()!.Trim())
               .Where(tag => !string.IsNullOrEmpty(tag))
               .ToList();
        }

        private DataRow[] GetRowsToScrape()
        {
            bool scrapeAll = radioButton_ScrapeAll.IsChecked == true;
            bool scrapeHidden = checkBox_ScrapeHidden.IsChecked == true;

            // Get the DataRowViews from the grid based on selection mode
            var rows = scrapeAll
                ? _mainWindow.MainDataGrid.Items.OfType<DataRowView>()
                : _mainWindow.MainDataGrid.SelectedItems.OfType<DataRowView>();

            var dataRows = rows.Select(rv => rv.Row);

            // Filter hidden items unless "scrapeHidden" is enabled
            if (!scrapeHidden)
            {
                dataRows = dataRows.Where(r => !Convert.ToBoolean(r["Hidden"]));
            }

            var rowsArray = dataRows.ToArray();

            return rowsArray;
        }

        private string CreateCacheFolder()
        {
            // Create cache folder if it does not exist
            string cacheFolder = $"{SharedData.ProgramDirectory}\\cache\\{_currentScraper}\\{SharedData.CurrentSystem}";
            if (!Directory.Exists(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }
            return cacheFolder;
        }

        
        private async Task<bool> AuthenticateEmuMoviesAsync(ScraperProperties scraperProperties)
        {         
            var creds = CredentialHelper.GetCredentials("EmuMovies");
            if (string.IsNullOrEmpty(creds.UserName))
            {
                LogHelper.Instance.Log("EmuMovies credentials have not been configured yet.", System.Windows.Media.Brushes.Red);
                return false;
            }

            scraperProperties.UserName = creds.UserName;
            scraperProperties.Password = creds.Password;

            var apiEmuMovies = serviceProvider.GetRequiredService<API_EmuMovies>();

            LogHelper.Instance.Log("Verifying EmuMovies credentials...", System.Windows.Media.Brushes.Teal);
            var (userAccessToken, errorMessage) = await apiEmuMovies.AuthenticateAsync(
                scraperProperties.UserName,
                scraperProperties.Password);

            if (string.IsNullOrEmpty(userAccessToken))
            {
                LogHelper.Instance.Log(errorMessage, System.Windows.Media.Brushes.Red);
                return false;
            }
        
            scraperProperties.AccessToken = userAccessToken;
            return true;
        }

        private async Task GetEmuMoviesMediaLists(ScraperProperties scraperProperties)
        {
            var apiEmuMovies = serviceProvider.GetRequiredService<API_EmuMovies>();

            bool hasListsForSystem = _mediaTypes != null && _mediaTypes.Count > 0 &&
            _mediaTypes.All(mt => _mediaListCache.ContainsKey((scraperProperties.SystemID, mt)));

            if (hasListsForSystem)
                return;
            LogHelper.Instance.Log("Downloading media lists...", System.Windows.Media.Brushes.Teal);

            var (mediaTypes, errorMessage) = await apiEmuMovies.GetMediaTypesAsync(scraperProperties.SystemID);

            if (mediaTypes == null || mediaTypes.Count == 0)
            {
                LogHelper.Instance.Log(errorMessage, System.Windows.Media.Brushes.Red);
                return;
            }

            _mediaTypes = mediaTypes;
         
            foreach (string mediaType in _mediaTypes)
            {
                var key = (scraperProperties.SystemID, mediaType);

                if (_mediaListCache.TryGetValue(key, out var cached))
                {
                    scraperProperties.EmuMoviesMediaLists[mediaType] = cached;
                    continue;
                }

                if (!scraperProperties.EmuMoviesMediaLists.TryGetValue(mediaType, out var existingList) ||
                    existingList == null ||
                    existingList.Count == 0)
                {
                    var (mediaList, errorMessage3) = await apiEmuMovies.GetMediaListAsync(
                        scraperProperties.SystemID,
                        mediaType);

                    if (mediaList == null)
                    {
                        mediaList = new List<string>();
                    }

                    scraperProperties.EmuMoviesMediaLists[mediaType] = mediaList;
                    _mediaListCache[key] = mediaList;
                }
                else
                {
                    _mediaListCache[key] = existingList;
                }
            }
        }

        private async Task<bool> AuthenticateScreenScraperAsync(ScraperProperties scraperProperties)
        {
            var creds = CredentialHelper.GetCredentials("ScreenScraper");
            if (string.IsNullOrEmpty(creds.UserName))
            {
                LogHelper.Instance.Log("ScreenScraper credentials have not been configured yet.", System.Windows.Media.Brushes.Red);
                return false;
            }

            scraperProperties.UserName = creds.UserName;
            scraperProperties.Password = creds.Password;

            var apiScreenScraper = serviceProvider.GetRequiredService<API_ScreenScraper>();

            LogHelper.Instance.Log("Verifying ScreenScraper credentials...", System.Windows.Media.Brushes.Teal);
            var (maxThreads, errorMessage) = await apiScreenScraper.AuthenticateAsync(
                scraperProperties.UserName,
                scraperProperties.Password);

            if (maxThreads < 0)
            {
                LogHelper.Instance.Log(errorMessage, System.Windows.Media.Brushes.Red);
                return false;
            }

            scraperProperties.MaxConcurrency = maxThreads;

            return true;
        }

        private string GetScreenScraperLanguage()
        {
            string language = Properties.Settings.Default.Language;

            if (!string.IsNullOrEmpty(language))
            {
                var match = Regex.Match(language, @"\((.*?)\)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return "en";
        }


        private List<string> GetScreenScraperRegions()
        {
            var regions = new List<string>();

            // Primary region
            string primaryRegionCode = "us"; // Default
            string primaryRegion = Properties.Settings.Default.Region;

            if (!string.IsNullOrEmpty(primaryRegion))
            {
                var match = Regex.Match(primaryRegion, @"\((.*?)\)");
                if (match.Success)
                {
                    primaryRegionCode = match.Groups[1].Value;
                }
            }

            regions.Add(primaryRegionCode);

            // Fallback regions
            string fallbackJson = Properties.Settings.Default.Region_Fallback;
            var fallbackRegions = JsonSerializer.Deserialize<List<string>>(fallbackJson) ?? [];

            regions.AddRange(
                fallbackRegions
                    .Select(r => Regex.Match(r, @"\((.*?)\)").Groups[1].Value)
                    .Where(code => !string.IsNullOrEmpty(code))
            );

            // Remove duplicates while preserving order
            return regions.Distinct().ToList();
        }


        private async void StartScraping()
        {
            // Stop if there is no INI loaded
            if (_allIniSections == null)
            {
                MessageBox.Show("INI sections not loaded correctly. Cannot start scraping.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Determine the system name.  Stop if it cannot be found.
            var systemID = string.Empty;
            if (_allIniSections.TryGetValue("Systems", out var section))
            {
                if (!section.TryGetValue(SharedData.CurrentSystem, out systemID))
                {
                    MessageBox.Show($"A system ID is missing for system '{SharedData.CurrentSystem}'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Get elements to scrape
            var elementsToScrape = GetElementsToScrape();
            if (elementsToScrape == null || elementsToScrape.Count == 0)
            {
                MessageBox.Show("There were no checkboxes selected.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Get rows to scrape
            var rowsToScrape = GetRowsToScrape();
            if (!rowsToScrape.Any())
            {
                MessageBox.Show("There were no items to scrape.", "No Items", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool isArcade = false;

            // Validate ArcadeDB scraper usage
            if (_currentScraper == "ArcadeDB")
            {
                // Check if arcade systems configuration is loaded
                if (!ArcadeSystemID.IsInitialized)
                {
                    MessageBox.Show("Arcade systems configuration is missing!\n\n" +
                                   "The arcadesystems.ini file could not be loaded.",
                                   "Configuration Error",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Error);
                    return;
                }

                // Check if current system is an arcade system
                isArcade = ArcadeSystemID.HasArcadeSystemName(SharedData.CurrentSystem);
                if (!isArcade)
                {
                    MessageBox.Show("You cannot scrape this system with the currently selected scraper.\n\n" +
                                   $"'{SharedData.CurrentSystem}' is not an arcade system.",
                                   "Non Arcade System",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Stop);
                    return;
                }
            }

            // From here on, failures will go through StopScraping since the gui is now disabled
            PrepareUIForScraping();

            // Initialize file logger
            string currentDir = Environment.CurrentDirectory;
            string logPath = Path.Combine(currentDir, "logs");
            LogHelper.Instance.StartFileLog(logPath);

            // ScraperProperties holds specific scraper configuration information
            // Which varies between scrapers.
            ScraperProperties scraperProperties = new ScraperProperties();

            // Set system id
            scraperProperties.SystemID = systemID;

            // Authentication check.  Also sets scraper specific configuration information (scraperProperties)
            bool isAuthenticated = false;

            switch (_currentScraper)
            {
                case "ArcadeDB":
                    scraperProperties.MaxConcurrency = 1;
                    isAuthenticated = true;
                    break;
                case "EmuMovies":
                    isAuthenticated = await AuthenticateEmuMoviesAsync(scraperProperties);
                    if (isAuthenticated)
                    {
                        scraperProperties.MaxConcurrency = 2;
                        await GetEmuMoviesMediaLists(scraperProperties);
                        if (scraperProperties.EmuMoviesMediaLists.Count == 0)
                        {
                            LogHelper.Instance.Log("No media lists were retrieved. Cannot start scraping.", System.Windows.Media.Brushes.Red);
                            isAuthenticated = false;
                        }
                    }
                    break;
              
                case "ScreenScraper":
                    isAuthenticated = await AuthenticateScreenScraperAsync(scraperProperties);
                    if (isAuthenticated)
                    {
                        var regions = GetScreenScraperRegions();
                        string language = GetScreenScraperLanguage();
                        scraperProperties.Language = language;
                        scraperProperties.Regions = regions;

                        LogHelper.Instance.Log($"Region: {scraperProperties.Regions[0]}", System.Windows.Media.Brushes.Teal);
                        LogHelper.Instance.Log($"Region Fallback: {string.Join(", ", scraperProperties.Regions.Skip(1))}", System.Windows.Media.Brushes.Teal);
                        LogHelper.Instance.Log($"Language: {scraperProperties.Language}", System.Windows.Media.Brushes.Teal);
                    }
                    break;
            }

            if (!isAuthenticated)
            {
                await StopScraping("Authentication failed. Cannot start scraping.");
                return;
            }

            scraperProperties.CacheFolder = CreateCacheFolder();

            // Configure cache thread counts
            int maxConcurrency = scraperProperties.MaxConcurrency;
            _scrapeSemaphore.Dispose();
            _scrapeSemaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            _downloadSemaphore.Dispose();
            _downloadSemaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

            // Show max thread count
            label_ThreadCount.Content = maxConcurrency.ToString();

            // Show image verification setting            
            string verificationStatus = Properties.Settings.Default.VerifyDownloadedImages ? "Image verification is ON" : "Image verification is OFF";
            LogHelper.Instance.Log(verificationStatus, System.Windows.Media.Brushes.Teal);

            // Setup counters 
            int scraperCount = 0;
            int scraperTotal = rowsToScrape.Length;
            LogHelper.Instance.Log($"Items To Scrape: {scraperTotal}", System.Windows.Media.Brushes.Teal);
            DateTime startTime = DateTime.Now;

            // Setup progress bar
            progressBar_ProgressBar.Value = 0;
            progressBar_ProgressBar.Minimum = 0;
            progressBar_ProgressBar.Maximum = scraperTotal;

            // Reset timer
            _globalStopwatch.Reset();
            _globalStopwatch.Start();

            // Reset cancellation tokensource
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            // Get log verbosity
            int logVerbosity = Properties.Settings.Default.LogVerbosity;
            scraperProperties.LogVerbosity = logVerbosity;

            // Get the media paths dictionary
            string jsonString = Properties.Settings.Default.MediaPaths;
            Dictionary<string, string> mediaPaths = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString)!;

            // Image Sources
            string? imageSource = GetSource("ImageSource", comboBox_ImageSource.Text);
            string? wheelSource = GetSource("WheelSource", comboBox_WheelSource.Text);
            string? thumbnailSource = GetSource("ThumbnailSource", comboBox_ThumbnailSource.Text);
            string? boxartSource = GetSource("BoxArtSource", comboBox_BoxArtSource.Text);
            string? marqueeSource = GetSource("MarqueeSource", comboBox_MarqueeSource.Text);
            string? cartridgeSource = GetSource("CartridgeSource", comboBox_CartridgeSource.Text);
            string? videoSource = GetSource("VideoSource", comboBox_VideoSource.Text);

            // Declare basic scraper parameters
            ScraperParameters baseScraperParameters = new ScraperParameters();
            baseScraperParameters.SystemID = systemID;
            baseScraperParameters.UserID = scraperProperties?.UserName;
            baseScraperParameters.UserPassword = scraperProperties?.Password;
            baseScraperParameters.ParentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename);
            baseScraperParameters.SSLanguage = scraperProperties?.Language;
            baseScraperParameters.ScrapeEnglishGenreOnly = Properties.Settings.Default.ScrapeEnglishGenreOnly;
            baseScraperParameters.ScrapeAnyMedia = Properties.Settings.Default.ScrapeAnyMedia;
            baseScraperParameters.SSRegions = scraperProperties?.Regions;
            baseScraperParameters.ImageSource = imageSource;
            baseScraperParameters.ThumbnailSource = thumbnailSource;
            baseScraperParameters.WheelSource = wheelSource;
            baseScraperParameters.BoxArtSource = boxartSource;
            baseScraperParameters.MarqueeSource = marqueeSource;
            baseScraperParameters.CartridgeSource = cartridgeSource;
            baseScraperParameters.VideoSource = videoSource;
            baseScraperParameters.OverwriteMedia = checkBox_OverwriteMedia.IsChecked ?? false;
            baseScraperParameters.OverwriteMetadata = checkBox_OverwriteMetadata.IsChecked ?? false;
            baseScraperParameters.OverwriteName = checkBox_OverwriteNames.IsChecked ?? false;
            baseScraperParameters.UserAccessToken = scraperProperties?.AccessToken;
            baseScraperParameters.ScraperPlatform = _currentScraper;
            baseScraperParameters.MediaPaths = mediaPaths;
            baseScraperParameters.ElementsToScrape = elementsToScrape;
            baseScraperParameters.ScrapeByCache = checkBox_ScrapeFromCache.IsChecked ?? false;
            baseScraperParameters.SkipNonCached = checkBox_OnlyScrapeFromCache.IsChecked ?? false;
            baseScraperParameters.CacheFolder = scraperProperties?.CacheFolder;
            baseScraperParameters.VerifyImageDownloads = Properties.Settings.Default.VerifyDownloadedImages;


            // Configure batch processing
            bool batchProcessing = Properties.Settings.Default.BatchProcessing
               && _currentScraper == "ArcadeDB"
               && rowsToScrape.Length > 25;

            int batchProcessingMaximum = 100;
            if (batchProcessing)
            {
                batchProcessingMaximum = Properties.Settings.Default.BatchProcessingMaximum;
            }
                        

            // Reset counters
            _scrapeLimitMax = 0;
            _scrapeLimitProgress = 0;
            int totalCount = rowsToScrape.Length;
            lock (_downloadStatsLock)
            {
                _downloadStats.Clear();
            }
                      
            // Stop button is now enabled since scraping will begin
            button_Stop.IsEnabled = true;

            // Arcade Names for Region Scraping
            string filePath = Properties.Settings.Default.MamePath;
            if (MameNamesHelper.Names.Count == 0 && isArcade)
            {
                if (File.Exists(filePath))
                {
                    LogHelper.Instance.Log("Generating arcade names dictionary...", System.Windows.Media.Brushes.Teal);
                    await MameNamesHelper.GenerateAsync(filePath);
                }
                else
                {
                    LogHelper.Instance.Log("MAME executable path is not valid or configured!", System.Windows.Media.Brushes.Orange);
                }
            }

            if (MameNamesHelper.Names.Count == 0 && isArcade)
            {
                LogHelper.Instance.Log("Arcade name dictionary is not created.", System.Windows.Media.Brushes.Orange);
                LogHelper.Instance.Log("Region and Language scraping will be limited.", System.Windows.Media.Brushes.Orange);
            }

            // 3,2,1,go!

            // For enabling stop button
            stackPanel_ScraperButtons.IsEnabled = true;

            _isScraping = true;
            SharedData.ChangeTracker?.StartBulkOperation();

            await Task.Delay(2000);
            var tasks = new List<Task>();

            if (batchProcessing)
            {
                await GetItemsInBatchMode(baseScraperParameters, batchProcessingMaximum, rowsToScrape, scraperProperties);
            }
                      
            LogHelper.Instance.Log("Starting main scraping phase...", System.Windows.Media.Brushes.Teal);

            // Log the semaphore settings
            LogHelper.Instance.Log(
                $"API/Download threads: {maxConcurrency}",
                System.Windows.Media.Brushes.Teal);
            
            try
            {                          
                              
                foreach (DataRow row in rowsToScrape)
                {
                    await _scrapeSemaphore.WaitAsync(CancellationToken);

                    // Capture current count for this task
                    int currentCount = Interlocked.Increment(ref scraperCount);
                    string currentRomName = row["Name"].ToString() ?? "Unknown";
                                   
                    UpdateProgress(
                        currentRomName,
                        startTime,
                        currentCount,
                        totalCount,
                        _scrapeLimitProgress,
                        _scrapeLimitMax);

                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            await ScrapeGameAsync(
                                row,
                                baseScraperParameters,
                                scraperProperties
                            );
                        }
                        catch (OperationCanceledException)
                        {
                            // Gracefully handle cancellation
                        }
                        catch (Exception ex)
                        {
                            if (scraperProperties.LogVerbosity == 2)
                            {
                                LogHelper.Instance.Log($"Error: '{ex.Message}'", System.Windows.Media.Brushes.Red);
                            }
                        }
                        finally
                        {
                            _scrapeSemaphore.Release();
                        }
                    }, CancellationToken);

                    tasks.Add(task);
                }
            }
            catch (OperationCanceledException)
            {
                LogHelper.Instance.Log("Scraping cancelled by user request", System.Windows.Media.Brushes.Red);
            }
            catch (Exception ex)
            {
                if (scraperProperties.LogVerbosity == 2)
                {
                    LogHelper.Instance.Log($"Unexpected error: {ex.Message}", System.Windows.Media.Brushes.Red);
                }
            }
            finally
            {
                try
                {
                    // Ensure all tasks are awaited, even if canceled or failed.
                    await Task.WhenAll(tasks);
                    SharedData.DataSet.AcceptChanges();
                    SharedData.ChangeTracker?.EndBulkOperation();
                }
                catch (Exception ex)
                {
                    // Catch any remaining task exceptions after waiting.
                    if (scraperProperties.LogVerbosity == 2)
                    {
                        LogHelper.Instance.Log($"Error while waiting for tasks to complete: {ex.Message}", System.Windows.Media.Brushes.Red);
                    }
                }

                // Log download summary
                await LogDownloadSummaryAsync();

                // Ensure StopScraping is always executed after all tasks finish.
                LogHelper.Instance.Log($"Total time: {FormatElapsedTime(_globalStopwatch.Elapsed)}", System.Windows.Media.Brushes.Green);

                // Empty message means it was ok.
                await StopScraping(string.Empty);
            }
        }

        private async Task DownloadMediaFilesAsync(
            ScrapedGameData scrapedData,
            ScraperParameters parameters)
            {

            if (scrapedData.Media == null || scrapedData.Media.Count == 0)
            {
                return; // Nothing to download
            }
            
            var fileTransfer = serviceProvider!.GetRequiredService<FileTransfer>();

            foreach (var mediaResult in scrapedData.Media)
            {
                string mediaType = mediaResult.MediaType;
                string url = mediaResult.Url;
                string extension = mediaResult.FileExtension;

                try
                {
                    CancellationToken.ThrowIfCancellationRequested();

                    // Get the destination folder for this media type
                    if (!parameters.MediaPaths.TryGetValue(mediaType, out string? mediaFolder))
                    {
                        LogHelper.Instance.Log(
                            $"No media path configured for {mediaType}",
                            System.Windows.Media.Brushes.Orange);
                        continue;
                    }

                    // Ensure extension has a dot prefix
                    if (!string.IsNullOrEmpty(extension) && !extension.StartsWith("."))
                    {
                        extension = "." + extension;
                    }

                    // Generate filename based on media type
                    string fileNamePrefix = Path.GetFileNameWithoutExtension(parameters.RomFileName);

                    // Some media types need special naming (like thumbnail -> thumb)
                    string mediaSuffix = mediaType == "thumbnail" ? "thumb" : mediaType;

                    string fileName = $"{fileNamePrefix}-{mediaSuffix}{extension}";

                    string fullPath = Path.Combine(parameters.ParentFolderPath, mediaFolder, fileName);

                    // Log download attempt
                    string regionDisplay = !string.IsNullOrEmpty(mediaResult.Region)
                        ? $" ({mediaResult.Region})"
                        : string.Empty;
                    LogHelper.Instance.Log(
                        $"Downloading {mediaType}{regionDisplay}: {fileName}",
                        System.Windows.Media.Brushes.Blue);

                    // Get bearer token from parameters (if needed for authenticated downloads)
                    string bearerToken = parameters.UserAccessToken ?? string.Empty;

                    // Wait for download semaphore
                    await _downloadSemaphore.WaitAsync(CancellationToken);

                    try
                    {
                        // Use the FileTransfer.DownloadFile method
                        bool downloadSuccess = await fileTransfer.DownloadFile(
                            verify: parameters.VerifyImageDownloads,
                            fileDownloadPath: fullPath,
                            url: url,
                            bearerToken: bearerToken
                        );

                        if (downloadSuccess)
                        {
                            // Store the relative path in the scraped data
                            // Use forward slashes for gamelist.xml consistency
                            string relativePath = $"./{mediaFolder}/{fileName}";
                            scrapedData.Data[mediaType] = relativePath;

                            // Track successful download
                            RecordDownload(mediaType);
                        }
                        else
                        {
                            // Determine if this was a verification failure or download failure
                            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".tif", ".ico", ".webp" };
                            bool isImage = imageExtensions.Contains(extension.ToLowerInvariant());

                            if (parameters.VerifyImageDownloads && isImage)
                            {
                                LogHelper.Instance.Log(
                                    $"Discarding bad image '{fileName}' (single color or invalid)",
                                    System.Windows.Media.Brushes.Red);
                            }
                            else
                            {
                                LogHelper.Instance.Log(
                                    $"Failed to download {mediaType}: {fileName}",
                                    System.Windows.Media.Brushes.Red);
                            }
                        }
                    }
                    finally
                    {
                        // Release the semaphore.
                        _downloadSemaphore.Release();
                    }
                }
                catch (OperationCanceledException)
                {
                    throw; // Re-throw to be caught by outer handler
                }
                catch (Exception ex)
                {
                    LogHelper.Instance.Log(
                        $"Error downloading {mediaType}: {ex.Message}",
                        System.Windows.Media.Brushes.Red);
                }
            }
        }

            private async Task ScrapeGameAsync(
                DataRow row,
                ScraperParameters baseParameters,
                ScraperProperties scraperProperties
            )

            {
            CancellationToken.ThrowIfCancellationRequested();

            string romPath = row["Rom Path"].ToString()!;
            string romFileName = Path.GetFileName(romPath);
            string romFileNameNoExtension = Path.GetFileNameWithoutExtension(romFileName);
            string romName = row["Name"].ToString() ?? romFileName;
            string gameID = row["Game Id"].ToString() ?? string.Empty;

            // Filter elements based on existing data and overwrite settings
            var itemsToScrape = FilterElementsToScrape(row, baseParameters);
            
            // Skip the scrape if there is nothing to get
            if (itemsToScrape.Count == 0)
            {
                if (scraperProperties.LogVerbosity == 2)
                {
                    LogHelper.Instance.Log($"Skipping '{romFileName}', nothing to scrape", System.Windows.Media.Brushes.Orange);
                }
                return;
            }

            if (scraperProperties.LogVerbosity >= 1)
            {
                LogHelper.Instance.Log($"Scraping {romName}", System.Windows.Media.Brushes.Green);
            }
            
            // Scraped data container 
            ScrapedGameData scrapedGameData = new ScrapedGameData();

            // Handle region/language
            string? mameArcadeName = MameNamesHelper.Names.TryGetValue(romFileNameNoExtension, out string? arcadeName)
                ? arcadeName : string.Empty;

            string romRegion = string.Empty;
            string romLanguage = string.Empty;

            // Get Region
            if (itemsToScrape.Contains("region"))
            {
                string nameValue = !string.IsNullOrEmpty(mameArcadeName) ? mameArcadeName : romFileNameNoExtension;
                romRegion = RegionLanguageHelper.GetRegion(nameValue);
                itemsToScrape.Remove("region");
            }

            // Get Language
            if (itemsToScrape.Contains("language"))
            {
                string nameValue = !string.IsNullOrEmpty(mameArcadeName) ? mameArcadeName : romFileNameNoExtension;
                romLanguage = RegionLanguageHelper.GetLanguage(nameValue);
                itemsToScrape.Remove("language");
            }

            // Clone parameters for this game
            var scraperParameters = baseParameters.Clone();
            scraperParameters.GameID = gameID;
            scraperParameters.RomName = romName;
            scraperParameters.RomFileName = romFileName;

            if (itemsToScrape.Count > 0)
            {
                scraperParameters.ElementsToScrape = itemsToScrape;

                switch (_currentScraper)
                {
                    case "ArcadeDB":
                        {
                            var arcadedb_scraper = serviceProvider.GetRequiredService<API_ArcadeDB>();
                            scrapedGameData = await arcadedb_scraper.ScrapeArcadeDBAsync(scraperParameters);
                            break;
                        }

                    case "EmuMovies":
                        {
                            var emumovies_scraper = serviceProvider.GetRequiredService<API_EmuMovies>();
                            scrapedGameData = await emumovies_scraper.ScrapeEmuMoviesAsync(scraperParameters, scraperProperties.EmuMoviesMediaLists);
                            break;
                        }

                    case "ScreenScraper":
                        {
                            var screenscraper_scraper = serviceProvider.GetRequiredService<API_ScreenScraper>();
                            var ssResult = await screenscraper_scraper.ScrapeScreenScraperAsync(scraperParameters);
                            scrapedGameData = ssResult.GameData;

                            Interlocked.Exchange(ref _scrapeLimitProgress, ssResult.ScrapeLimitProgress);
                            Interlocked.Exchange(ref _scrapeLimitMax, ssResult.ScrapeLimitMax);

                            if (_scrapeLimitProgress >= _scrapeLimitMax && _scrapeLimitMax > 0)
                            {
                                LogHelper.Instance.Log("Daily scrape limit reached!", System.Windows.Media.Brushes.Red);
                                _cancellationTokenSource.Cancel();
                            }
                            break;
                        }
                }

                // Download media files - if required
                if (scrapedGameData != null && scrapedGameData.WasSuccessful && scrapedGameData.Media.Count > 0)
                {
                    await DownloadMediaFilesAsync(scrapedGameData, scraperParameters);
                }
            }

            // Null avoidance just in case
            if (scrapedGameData == null)
            {
                scrapedGameData = new ScrapedGameData();
            }

            if (!string.IsNullOrEmpty(romRegion))
            {
                scrapedGameData.Data["region"] = romRegion;
            }

            if (!string.IsNullOrEmpty(romLanguage))
            {
                scrapedGameData.Data["lang"] = romLanguage;
            }

            // Apply data to row
            if (scrapedGameData.WasSuccessful)
            {
                await SaveScrapedData(row, scrapedGameData, scraperParameters);
                
                if (scraperProperties.LogVerbosity == 2)
                {
                    LogHelper.Instance.Log($"Successfully scraped '{scraperParameters.RomName}'", System.Windows.Media.Brushes.Green);
                }

                // Optional messaging hook, might remove later
                if (scrapedGameData?.Messages != null)
                {
                    foreach (var msg in scrapedGameData.Messages)
                    {
                        if (scraperProperties.LogVerbosity == 2)
                        {
                            LogHelper.Instance.Log(msg, System.Windows.Media.Brushes.Red);
                        }
                    }
                }
            }
            else
            {
                if (scraperProperties.LogVerbosity >= 1)
                {
                    LogHelper.Instance.Log($"Could not scrape '{scraperParameters.RomName}'", System.Windows.Media.Brushes.Red);
                }

                if (scrapedGameData.Messages != null)
                {
                    foreach (var msg in scrapedGameData.Messages)
                    {
                        if (scraperProperties.LogVerbosity == 2)
                        {
                            LogHelper.Instance.Log(msg, System.Windows.Media.Brushes.Red);
                        }
                    }
                }
            }
        }


        private async Task GetItemsInBatchMode(ScraperParameters baseScraperParameters, int batchProcessingMaximum, DataRow[] rowsToScrape, ScraperProperties scraperProperties)
        {
            try
            {
                Dictionary<string, ScrapedGameData> batchCache = new();

                LogHelper.Instance.Log("Starting batch API fetch...", System.Windows.Media.Brushes.Teal);

                // Find ROMs that don't have cache files yet
                var itemsToFetch = new List<string>();

                string? cacheFolder = scraperProperties.CacheFolder;

                // Initialize the set, case-insensitive
                HashSet<string> cacheFilesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (!string.IsNullOrEmpty(cacheFolder) && Directory.Exists(cacheFolder))
                {
                    foreach (var file in Directory.EnumerateFiles(cacheFolder, "*.json"))
                    {
                        cacheFilesSet.Add(Path.GetFileName(file)); // Keep extension
                    }
                }

                for (int i = 0; i < rowsToScrape.Length; i++)
                {
                    var row = rowsToScrape[i];
                    string? romPath = row["Rom Path"].ToString();

                    // Never empty, but just in case
                    if (string.IsNullOrEmpty(romPath))
                    {
                        continue;
                    }

                    string romFileNameWithoutExtension = Path.GetFileNameWithoutExtension(romPath);
                    string cacheFileName = romFileNameWithoutExtension + ".json";
              
                    if (!cacheFilesSet.Contains(cacheFileName)) 
                    {
                        itemsToFetch.Add(romFileNameWithoutExtension);
                    }
                }

                int alreadyCached = rowsToScrape.Length - itemsToFetch.Count;
                if (alreadyCached > 0)
                {
                    LogHelper.Instance.Log($"{alreadyCached} items already in cache", System.Windows.Media.Brushes.Teal);
                }

                if (itemsToFetch.Count > 0)
                {
                    LogHelper.Instance.Log(
                        $"Fetching {itemsToFetch.Count} games from API in batches...",
                        System.Windows.Media.Brushes.Teal);

                    var arcadedb_scraper = serviceProvider.GetRequiredService<API_ArcadeDB>();
                    int totalBatches = (int)Math.Ceiling((double)itemsToFetch.Count / batchProcessingMaximum);
                    int currentBatch = 0;
                    int totalFetched = 0;

                    for (int i = 0; i < itemsToFetch.Count; i += batchProcessingMaximum)
                    {
                        CancellationToken.ThrowIfCancellationRequested();

                        currentBatch++;
                        int batchSize = Math.Min(batchProcessingMaximum, itemsToFetch.Count - i);
                        var batch = itemsToFetch.GetRange(i, batchSize);

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            label_CurrentScrape.Content = $"Fetching batch {currentBatch}/{totalBatches}";
                            progressBar_ProgressBar.Value = currentBatch;
                            progressBar_ProgressBar.Maximum = totalBatches;
                            label_ProgressBarCount.Content = $"{totalFetched}/{itemsToFetch.Count}";
                            double percentage = (double)currentBatch / totalBatches * 100;
                            label_Percentage.Content = $"{percentage:F0}%";
                        });

                        try
                        {
                            // This downloads from API and saves raw JSON to cache files
                            var batchResults = await arcadedb_scraper.ScrapeArcadeDBBatchAsync(
                                batch,
                                baseScraperParameters
                            );

                            int foundCount = batchResults.Count;
                            int notFoundCount = batch.Count - foundCount;
                            totalFetched += foundCount;

                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                label_ProgressBarCount.Content = $"{totalFetched}/{itemsToFetch.Count}";
                            });

                            string resultMsg = notFoundCount > 0
                                ? $"Batch {currentBatch}/{totalBatches}: Fetched {foundCount}, not found {notFoundCount}"
                                : $"Batch {currentBatch}/{totalBatches}: Fetched {foundCount}";

                            LogHelper.Instance.Log(resultMsg, System.Windows.Media.Brushes.Teal);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            LogHelper.Instance.Log(
                                $"Batch {currentBatch}/{totalBatches} error: {ex.Message}",
                                System.Windows.Media.Brushes.Orange);
                        }
                    }

                    LogHelper.Instance.Log(
                        $"Batch fetch complete: {totalFetched} games downloaded to cache",
                        System.Windows.Media.Brushes.Green);
                }
                else
                {
                    LogHelper.Instance.Log(
                        "All games already cached!",
                        System.Windows.Media.Brushes.Green);
                }
            }
            catch (OperationCanceledException)
            {
                LogHelper.Instance.Log(
                    "Batch processing cancelled by user",
                    System.Windows.Media.Brushes.Orange);

            }
            finally
            {
                // Reset progress bar for main scraping
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    progressBar_ProgressBar.Value = 0;
                    progressBar_ProgressBar.Maximum = rowsToScrape.Length;
                    label_ProgressBarCount.Content = "0/0";
                    label_Percentage.Content = "0%";
                });
            }

        }

        private async Task SaveScrapedData(DataRow row, ScrapedGameData scrapedData, ScraperParameters parameters)
        {
            if (parameters.ElementsToScrape == null || scrapedData?.Data == null)
            {
                return;
            }

            var rowUpdates = new Dictionary<string, string>();

            foreach (string element in parameters.ElementsToScrape)
            {
                // Check if the scraped data contains the element key
                if (!scrapedData.Data.TryGetValue(element, out var value))
                {
                    continue;
                }

                // Only add if value is not null or empty
                if (!string.IsNullOrEmpty(value))
                {
                    // Use the existing MetaLookup to get the column name
                    if (parameters.MetaLookup.TryGetValue(element, out var meta))
                    {
                        rowUpdates[meta.Column] = value;
                    }
                }
            }

            // Only invoke if there are updates to apply
            if (rowUpdates.Count > 0)
            {
                // Safely update DataRow on the UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var kvp in rowUpdates)
                    {
                        if (row.Table.Columns.Contains(kvp.Key))
                        {
                            row[kvp.Key] = kvp.Value;
                        }
                    }
                });
            }
        }

        private List<string> FilterElementsToScrape(DataRow row, ScraperParameters baseParameters)
        {
            var itemsToScrape = new List<string>();
            var metaLookup = baseParameters.MetaLookup;

            foreach (var item in baseParameters.ElementsToScrape!)
            {
                var (type, column) = metaLookup[item];
                var rawValue = row[column];
                string? value = rawValue == null || rawValue == DBNull.Value ? null : rawValue.ToString();
                bool isMediaType = type == "Image" || type == "Document" || type == "Video";

                // Always scrape if value is empty/missing
                if (string.IsNullOrEmpty(value))
                {
                    itemsToScrape.Add(item);
                    continue;
                }

                // Check file existence for media types
                if (isMediaType &&
                    baseParameters.MediaPaths != null &&
                    baseParameters.MediaPaths.TryGetValue(item, out string? folder) &&
                    !string.IsNullOrEmpty(folder))
                {
                    string actualFile = Path.Combine(
                        baseParameters.ParentFolderPath,
                        folder,
                        Path.GetFileName(value)
                    );

                    if (!File.Exists(actualFile))
                    {
                        itemsToScrape.Add(item);
                        continue;
                    }
                }

                // Overwrite rules - only ADD if we SHOULD scrape
                if (item == "name" && baseParameters.OverwriteName)
                {
                    itemsToScrape.Add(item);
                    continue;
                }

                if (type == "String" && baseParameters.OverwriteMetadata)
                {
                    itemsToScrape.Add(item);
                    continue;
                }

                if (isMediaType && baseParameters.OverwriteMedia)
                {
                    itemsToScrape.Add(item);
                    continue;
                }
            }

            return itemsToScrape;
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

        public void UpdateProgress(string romLabel, DateTime startTime, int current, int total, int scrapeLimitProgress, int scrapeLimitMax)
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

                progressBar_ProgressBar.Value = current;

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

            Mouse.OverrideCursor = Cursors.Wait;

            string[] files = Directory.GetFiles(cacheFolder);
            foreach (string file in files)
            {
                File.Delete(file);
            }

            UpdateCacheCount(_currentScraper, SharedData.CurrentSystem);

            Mouse.OverrideCursor = null;

            // MessageBox.Show("Cache has been cleared.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Button_ShowCounts_Click(object sender, RoutedEventArgs e)
        {
            // Flip the setting
            bool showCounts = !Properties.Settings.Default.ShowCounts;

           // Update and save the setting
            Properties.Settings.Default.ShowCounts = showCounts;
            Properties.Settings.Default.Save();

            ShowOrHideCounts(showCounts);
        }

        public void ShowOrHideCounts(bool showCounts)
        {
            Dispatcher.BeginInvoke(() =>
            {
                // Toggle the button text
                button_ShowCounts.Content = showCounts ? "Hide Counts" : "Show Counts";

                var checkBoxes =
                    TreeHelper.GetAllVisualChildren<CheckBox>(grid_MediaControls);

                var metaDict = GamelistMetaData.GetMetaDataDictionary();
                var table = SharedData.DataSet.Tables[0];

                HashSet<string> columnsNeeded = new();

                foreach (CheckBox cb in checkBoxes)
                {
                    if (cb.Tag == null) continue;

                    // Music does not get counts
                    // Because it does not have its own column
                    if (cb.Tag.ToString().Equals("music", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!Enum.TryParse(cb.Tag.ToString()!, true, out MetaDataKeys key))
                        continue;

                    string columnName = metaDict[key].Name;
                    columnsNeeded.Add(columnName);
                }

                Dictionary<string, int> columnCounts = new(columnsNeeded.Count);

                foreach (string colName in columnsNeeded)
                {
                    int count = table.AsEnumerable().Count(row =>
                        !row.IsNull(colName) &&
                        !string.IsNullOrWhiteSpace(row[colName].ToString()));

                    columnCounts[colName] = count;
                }

                foreach (CheckBox checkBox in checkBoxes)
                {
                    if (checkBox.Tag == null) continue;

                    string tag = checkBox.Tag.ToString()!;
                    bool isMusic = tag.Equals("music", StringComparison.OrdinalIgnoreCase);

                    if (!Enum.TryParse(tag, true, out MetaDataKeys key))
                        continue;

                    string metaColumn = metaDict[key].Name;

                    // Clean existing label text
                    string content = checkBox.Content?.ToString() ?? "";
                    content = CountRegex.Replace(content, "").Trim();

                    // Music never gets counts
                    if (showCounts && !isMusic)
                    {
                        if (columnCounts.TryGetValue(metaColumn, out int c))
                            content = $"{content} ({c})";
                    }

                    checkBox.Content = content;
                }
            });
        }

        private void Button_ResetSources_Click(object sender, RoutedEventArgs e)
        {
            var allComboBoxes = TreeHelper.GetAllVisualChildren<ComboBox>(grid_MediaControls).ToList();

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
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                return;
            }

            checkBox_OnlyScrapeFromCache.IsEnabled = true;
        }

        private void CheckBox_ScrapeFromCache_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                return;
            }
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
            _scrapeSemaphore?.Dispose();
        }
    }
}
