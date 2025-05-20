using GamelistManager.classes;
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
using System.Windows.Threading;


namespace GamelistManager.pages
{
    public partial class Scraper : Page
    {
        // Fields
        private string _lastScraper = string.Empty;
        private string _currentScraper = string.Empty;
        private static Stopwatch _globalStopwatch = new Stopwatch();
        private static readonly Stopwatch GlobalStopwatch = new Stopwatch();
        private Dictionary<string, List<string>> _emumoviesMediaLists = new();
        private CancellationTokenSource _cancellationTokenSource = new();
        private MainWindow _mainWindow;

        // Property for CancellationToken
        private CancellationToken CancellationToken => _cancellationTokenSource.Token;

        // Constructor
        public Scraper(MainWindow window)
        {
            InitializeComponent();
            _currentScraper = comboBox_SelectedScraper.Text; // Ensure comboBox_SelectedScraper is initialized
            _mainWindow = window ?? throw new ArgumentNullException(nameof(window));
            Logger.Instance.Initialize(LogListBox); // Ensure LogListBox is initialized before calling Initialize
        }


        private void SaveScraperSettings(string scraper, string system)
        {
            // Get all CheckBox controls which have tags
            var allCheckBoxes = VisualTreeHelperExtensions.GetAllVisualChildren<CheckBox>(stackPanel_AllScraperCheckboxes);

            // Make a comma separated string for all checkboxes that are enabled and checked
            string checkBoxString = string.Join(",",
             allCheckBoxes
            .Where(cb => cb.IsChecked == true && cb.IsEnabled == true)
            .Select(cb => cb.Tag)
            .ToList());

            // Set the checkbox string for current scraper
            var propertyName = $"{scraper}_Checkboxes";
            var property = typeof(Properties.Settings).GetProperty(propertyName);
            if (property != null)
            {
                property.SetValue(Properties.Settings.Default, checkBoxString);
            }

            // Get all comboboxes
            var allComboBoxes = VisualTreeHelperExtensions.GetAllVisualChildren<ComboBox>(stackPanel_AllScraperCheckboxes).ToList();

            // Filter enabled ComboBox controls
            var enabledComboBoxes = allComboBoxes
                .Where(cb => cb.IsEnabled == true);

            // Build a dictionary for media sources
            Dictionary<string, string> mediaSources = new Dictionary<string, string>();
            foreach (var comboBox in enabledComboBoxes)
            {
                string name = comboBox.Tag.ToString()!;
                string value = comboBox.Text;
                mediaSources.Add(name, value);
            }


            Dictionary<string, Dictionary<string, Dictionary<string, string>>> allMediaSources = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            string jsonString = Properties.Settings.Default.MediaSources;

            if (!string.IsNullOrEmpty(jsonString))
            {
                try
                {
                    // Deserialize the JSON string into the nested dictionary structure
                    allMediaSources = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(jsonString)
                    ?? new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(); // Ensure a non-null dictionary
                }
                catch
                {
                    // Handle deserialization errors (optional logging or handling)
                    // For now, we'll just fall back to an empty dictionary
                    allMediaSources = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
                }
            }

            // Ensure that the scraper key exists in allMediaSources
            if (!allMediaSources.ContainsKey(scraper))
            {
                allMediaSources[scraper] = new Dictionary<string, Dictionary<string, string>>();
            }

            // Get the nested dictionary for the scraper
            var nestedDictionary = allMediaSources[scraper];

            // Check if the system key exists within the scraper's nested dictionary
            if (!nestedDictionary.ContainsKey(system))
            {
                // If the system key doesn't exist, add the mediaSources dictionary
                nestedDictionary.Add(system, mediaSources);
            }
            else
            {
                // If the system key exists, replace the existing dictionary with mediaSources
                nestedDictionary[system] = mediaSources;
            }

            // Update property value            
            string newJsonString = JsonSerializer.Serialize(allMediaSources);
            Properties.Settings.Default.MediaSources = newJsonString;

            // Save the changes to the settings
            Properties.Settings.Default.Save();

        }

        private void LoadScraperSettings(string scraper, string system)
        {
            var allCheckBoxes = VisualTreeHelperExtensions.GetAllVisualChildren<CheckBox>(stackPanel_AllScraperCheckboxes);

            List<string> availableElements = GamelistMetaData.GetScraperElements(scraper);

            // Set previous checkbox states
            var propertyName = $"{scraper}_Checkboxes";
            var property = typeof(Properties.Settings).GetProperty(propertyName);
            var checkboxString = property?.GetValue(Properties.Settings.Default) as string ?? string.Empty;
            var checkedCheckboxes = !string.IsNullOrEmpty(checkboxString) ? checkboxString.Split(',') : Array.Empty<string>();

            foreach (var child in allCheckBoxes)
            {
                string? checkBoxTagValue = child.Tag?.ToString();
                if (!string.IsNullOrEmpty(checkBoxTagValue) && availableElements.Contains(checkBoxTagValue))
                {
                    child.IsEnabled = true;
                    child.IsChecked = checkedCheckboxes.Contains(checkBoxTagValue);
                }
                else
                {
                    child.IsEnabled = false;
                    child.IsChecked = false;
                }
            }

            var allComboBoxes = VisualTreeHelperExtensions.GetAllVisualChildren<ComboBox>(stackPanel_AllScraperCheckboxes).ToList();

            Dictionary<string, Dictionary<string, Dictionary<string, string>>> allMediaSources = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            string jsonString = Properties.Settings.Default.MediaSources;
            if (!string.IsNullOrEmpty(jsonString))
            {
                try
                {
                    allMediaSources = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(jsonString)
                    ?? new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(); // Ensure a non-null dictionary
                }
                catch
                {
                }
            }

            Dictionary<string, string> mediaSources = new Dictionary<string, string>();
            // Check if the scraper exists in allMediaSources
            if (allMediaSources.ContainsKey(scraper))
            {
                var systemDictionary = allMediaSources[scraper];
                // Check if the system exists in the scraper's dictionary
                if (systemDictionary.ContainsKey(system))
                {
                    mediaSources = systemDictionary[system];
                }
            }

            string file = $".\\ini\\{scraper.ToLower()}_options.ini";
            Dictionary<string, Dictionary<string, string>> allSections = IniFileReader.ReadIniFile(file);

            // Loop through the comboboxes
            foreach (ComboBox comboBox in allComboBoxes)
            {
                comboBox.Items.Clear();
                string sectionName = comboBox.Tag.ToString()!;

                // If the ini files has a section for the tag value of the comboBox
                if (!string.IsNullOrEmpty(sectionName) && allSections.TryGetValue(sectionName, out var section))
                {
                    comboBox.IsEnabled = true;
                    // Add each sourceItem in the section to the comboBox
                    foreach (var sourceItem in section)
                    {
                        string sourceName = sourceItem.Key;
                        comboBox.Items.Add(sourceName);
                    }
                    // Check current sourceItem against saved value
                    if (mediaSources.TryGetValue(sectionName, out var previousSelection))
                    {
                        int index = comboBox.Items
                        .OfType<object>()
                        .Select((item, idx) => new { Item = item, Index = idx })
                        .FirstOrDefault(x => x.Item.ToString() == previousSelection)?.Index ?? -1;

                        if (index != -1)
                        {
                            comboBox.SelectedIndex = index;
                        }
                        else
                        {
                            comboBox.SelectedIndex = 0;
                        }
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

            string lastScraperValue = Properties.Settings.Default.LastScraper;

            // Ensure that the ComboBox is populated before setting the selected sourceItem
            if (!string.IsNullOrEmpty(lastScraperValue))
            {
                var item = comboBox_SelectedScraper.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (i.Content as string) == lastScraperValue);
                if (item != null)
                {
                    comboBox_SelectedScraper.SelectedItem = item;
                }
            }
            else
            {
                comboBox_SelectedScraper.SelectedIndex = 0;
            }

            bool showCounts = Properties.Settings.Default.ShowCounts;
            ShowOrHideCounts(showCounts);

        }

        private void button_SelectAllMetaData_Click(object sender, RoutedEventArgs e)
        {
            SetMetaCheckboxes(true);
        }

        private void button_SelectAllMedia_Click(object sender, RoutedEventArgs e)
        {
            SetMediaCheckboxes(true);
        }

        private void button_SelectNoMedia_Click(object sender, RoutedEventArgs e)
        {
            SetMediaCheckboxes(false);
        }

        private void button_SelectNoMetaData_Click(object sender, RoutedEventArgs e)
        {
            SetMetaCheckboxes(false);
        }

        private void SetMetaCheckboxes(bool isChecked)
        {
            var checkBoxes = VisualTreeHelperExtensions.GetAllVisualChildren<CheckBox>(stackPanel_MetadataCheckboxes);
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
            var checkBoxes = VisualTreeHelperExtensions.GetAllVisualChildren<CheckBox>(stackPanel_MediaCheckboxes);

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

        private void comboBox_SelectedScraper_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedText = string.Empty;

            ComboBox? comboBox = sender as ComboBox;

            if (comboBox == null || comboBox.SelectedItem == null)
            {
                // Should never be empty, but always check anyhow
                return;
            }

            // Always read the index on a selectionchanged event, not the text
            // The index will be updated, but text might not be yet
            ComboBoxItem selectedItem = (ComboBoxItem)comboBox.Items[comboBox.SelectedIndex];
            _currentScraper = selectedItem.Content.ToString()!;

            // Save last scraper value
            Properties.Settings.Default.LastScraper = _currentScraper;

            // Save previous scraper settings
            if (!string.IsNullOrEmpty(_lastScraper))
            {
                SaveScraperSettings(_lastScraper, SharedData.CurrentSystem);
            }

            // Load selected scraper settings
            LoadScraperSettings(_currentScraper, SharedData.CurrentSystem);

            // Set valueholder for last scraper value
            _lastScraper = _currentScraper;

            checkBox_ScrapeFromCache.IsEnabled = true;
            checkBox_OverwriteMetadata.IsEnabled = true;

            if (_currentScraper == "ArcadeDB")
            {
                button_Setup.IsEnabled = false;
            }
            else
            {
                button_Setup.IsEnabled = true;
            }

            if (_currentScraper == "EmuMovies")
            {
                checkBox_ScrapeFromCache.IsEnabled = false;
                checkBox_ScrapeFromCache.IsChecked = false;
                checkBox_OnlyScrapeFromCache.IsEnabled = false;
                checkBox_OnlyScrapeFromCache.IsChecked = false;
                checkBox_OverwriteMetadata.IsChecked = false;
                checkBox_OverwriteMetadata.IsEnabled = false;
            }

            UpdateCacheCount(_currentScraper, SharedData.CurrentSystem);

            Properties.Settings.Default.Save();

        }

        public void UpdateCacheCount(string scraper, string system)
        {
            button_ClearCache.IsEnabled = false;

            string cachePath = $@"{SharedData.ProgramDirectory}\cache\{scraper}\{system}";

            if (!Path.Exists(cachePath))
            {
                label_cacheCount.Content = "Cache Is Empty";
            }
            else
            {
                var files = Directory.GetFiles(cachePath);
                if (files.Length == 0)
                {
                    label_cacheCount.Content = "Cache Is Empty";
                }
                else
                {
                    button_ClearCache.IsEnabled = true;
                    if (scraper != "EmuMovies")
                    {
                        label_cacheCount.Content = $"{files.Length.ToString()} items in cache";
                    }
                    else
                    {
                        label_cacheCount.Content = $"Medialists are cached";
                    }
                }
            }
        }

        private void button_Start_Click(object sender, RoutedEventArgs e)
        {
            SaveScraperSettings(_currentScraper, SharedData.CurrentSystem);
            SharedData.ChangeTracker!.StartBulkOperation();
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
                await Logger.Instance.LogAsync(message, System.Windows.Media.Brushes.Red);
            }
            else
            {
                await Logger.Instance.LogAsync("Scraping Completed!", System.Windows.Media.Brushes.Blue);
                label_CurrentScrape.Content = "Completed!";
            }

            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker!.EndBulkOperation();

            stackPanel_ScraperSelector.IsEnabled = true;
            stackPanel_OverwriteOptions.IsEnabled = true;
            stackPanel_ScraperSettings.IsEnabled = true;
            stackPanel_AllScraperCheckboxes.IsEnabled = true;
            UpdateCacheCount(_currentScraper, SharedData.CurrentSystem);

            button_Start.IsEnabled = true;
            stackPanel_ScraperButtons.IsEnabled = true;
        }


        private async void StartScraping()
        {
            Logger.Instance.ClearLog();

            string verificationStatus = Properties.Settings.Default.VerifyDownloadedImages ? "Image verification is ON" : "Image verification is OFF";
            await Logger.Instance.LogAsync(verificationStatus, System.Windows.Media.Brushes.Blue);


            // ParentFolderPath string
            string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename)!;

            // Local Disables
            button_Start.IsEnabled = false;
            button_Stop.IsEnabled = false; // only enabled when the jobs start
            stackPanel_ScraperSelector.IsEnabled = false;
            stackPanel_OverwriteOptions.IsEnabled = false;
            stackPanel_ScraperSettings.IsEnabled = false;
            stackPanel_ScraperButtons.IsEnabled = false;
            stackPanel_AllScraperCheckboxes.IsEnabled = false;

            // Parent Window disables
            _mainWindow.menu_Main.IsEnabled = false;
            _mainWindow.stackPanel_InfoBar.IsEnabled = false;
            _mainWindow.textBox_Description.IsEnabled = false;
            //_mainWindow.MainDataGrid.IsEnabled = false;
            _mainWindow.MainDataGrid.IsReadOnly = true;
            _mainWindow.MainDataGrid.IsHitTestVisible = false;

            // Reset labels and progressbar value
            label_CurrentScrape.Content = "N/A";
            label_Percentage.Content = "0%";
            progressBar_ProgressBar.Value = 0;
            label_ThreadCount.Content = "1";
            label_ProgressBarCount.Content = "N/A";
            label_ScrapeLimitCount.Content = "N/A";

            // Make a list of elements to be scraped
            // Enumerate checked checkboxes and get tag value
            // These tags match metadata key names
            var elementsToScrape = VisualTreeHelperExtensions.GetAllVisualChildren<CheckBox>(stackPanel_AllScraperCheckboxes)
            .Where(cb => cb.Tag != null && cb.IsChecked == true)
            .Select(cb => cb.Tag.ToString())
            .ToList();

            if (elementsToScrape.Count == 0)
            {
                StopScraping("There were no items selected!");
                return;
            }

            // Create bool values based on gui element state
            bool scrapeAll = button_AllOrSelected.Content.ToString() == "All Items";
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
            if (datagridRowSelection.Count() == 0)
            {
                StopScraping("There were no items to scrape!");
                return;
            }

            // Read the configuration file for current scraper
            string optionsFileName = $"ini\\{_currentScraper.ToLower()}_options.ini";
            var allSections = IniFileReader.ReadIniFile(optionsFileName);

            // Is there a systems section?
            // ArcadeDB for example does use this
            allSections.TryGetValue("Systems", out var section);
            var systemID = string.Empty;
            if (section != null)
            {
                if (!section.TryGetValue(SharedData.CurrentSystem, out systemID))
                {
                    StopScraping($"A system ID is missing for system '{SharedData.CurrentSystem}'");
                    return;
                }
            }

            int maxConcurrency = 1;
            string region = string.Empty;
            string language = string.Empty;
            string userName = string.Empty;
            string userPassword = string.Empty;
            string userAccessToken = string.Empty;
            bool scrapeByCache = checkBox_ScrapeFromCache.IsChecked == true ? true : false;
            bool skipNonCached = checkBox_OnlyScrapeFromCache.IsChecked == true ? true : false;
            bool overWriteNames = checkBox_OverwriteNames.IsChecked == true ? true : false;
            bool overWriteMetadata = checkBox_OverwriteMetadata.IsChecked == true ? true : false;
            bool overWriteMedia = checkBox_OverwriteMedia.IsChecked == true ? true : false;
            bool englishGenreOnly = Properties.Settings.Default.ScrapeEnglishGenreOnly; // ScreenScraper Specific

            string cacheFolder = $"{SharedData.ProgramDirectory}\\cache\\{_currentScraper}\\{SharedData.CurrentSystem}";
            if (!Directory.Exists(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }

            switch (_currentScraper)
            {
                case "ArcadeDB":
                    maxConcurrency = 1;
                    break;


                case "EmuMovies":
                    label_CurrentScrape.Content = "Authenticating...";
                    Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                    (userName, userPassword) = CredentialManager.GetCredentials("EmuMovies");
                    if (string.IsNullOrEmpty(userName))
                    {
                        StopScraping($"Credentials for {_currentScraper} are not configured");
                        return;
                    }

                    await Logger.Instance.LogAsync($"Verifying {_currentScraper} credentials...", System.Windows.Media.Brushes.Blue);

                    // Get the user access token
                    API_EmuMovies aPI_EmuMovies = new API_EmuMovies(new HttpClient());
                    userAccessToken = await aPI_EmuMovies.AuthenticateEmuMoviesAsync(userName, userPassword);
                    if (string.IsNullOrEmpty(userAccessToken))
                    {
                        StopScraping("Error retrieving user authentication token from EmuMovies!");
                        return;
                    }

                    // Retrieve media types for current system
                    List<string> emumoviesMediaTypes = new List<string>();
                    emumoviesMediaTypes = await aPI_EmuMovies.GetMediaTypes(systemID);
                    if (emumoviesMediaTypes == null)
                    {
                        StopScraping($"Error retrieving media lists for {systemID}!");
                        return;
                    }

                    if (!File.Exists($"{cacheFolder}\\cache.json"))
                    {
                        label_CurrentScrape.Content = "Downloading media lists...";
                        await Logger.Instance.LogAsync("Downloading media lists...", System.Windows.Media.Brushes.Blue);
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
                    maxConcurrency = 2;
                    break;

                case "ScreenScraper":
                    label_CurrentScrape.Content = "Authenticating...";

                    (userName, userPassword) = CredentialManager.GetCredentials("ScreenScraper");
                    if (string.IsNullOrEmpty(userName))
                    {
                        StopScraping($"Credentials for {_currentScraper} are not configured");
                        return;
                    }

                    await Logger.Instance.LogAsync($"Verifying {_currentScraper} credentials...", System.Windows.Media.Brushes.Blue);
                    Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

                    API_ScreenScraper aPI_ScreenScraper = new API_ScreenScraper(new HttpClient());
                    bool credCheck = await aPI_ScreenScraper.VerifyScreenScraperCredentialsAsync(userName, userPassword);

                    if (!credCheck)
                    {
                        StopScraping("The ScreenScraper credentials failed to logon to remote server");
                        await Logger.Instance.LogAsync($"Logon failed, please check the credentials!", System.Windows.Media.Brushes.Red);
                        return;
                    }

                    maxConcurrency = await aPI_ScreenScraper.GetMaxThreadsAsync(userName, userPassword);

                    await Logger.Instance.LogAsync($"Max Threads: {maxConcurrency}", System.Windows.Media.Brushes.Blue);

                    string pattern = @"\((.*?)\)";

                    string regionValue = Properties.Settings.Default.Region;

                    if (!string.IsNullOrEmpty(regionValue))
                    {
                        Match match = Regex.Match(regionValue, pattern);
                        if (match.Success)
                        {
                            region = match.Groups[1].Value;
                        }
                        else
                        {
                            region = "us";
                        }
                    }

                    await Logger.Instance.LogAsync($"Region: {region}", System.Windows.Media.Brushes.Blue);

                    language = Properties.Settings.Default.Language;

                    if (!string.IsNullOrEmpty(language))
                    {
                        Match match = Regex.Match(language, pattern);
                        if (match.Success)
                        {
                            language = match.Groups[1].Value;
                        }
                        else
                        {
                            language = "en";
                        }
                    }
                    else
                    {
                        language = "en";
                    }

                    await Logger.Instance.LogAsync($"Language : {language}", System.Windows.Media.Brushes.Blue);
                    break;
            }
            ;

            // Retrieve source values for media
            string imageSource = string.Empty;
            string boxartSource = string.Empty;
            string marqueeSource = string.Empty;
            string cartridgeSource = string.Empty;
            string videoSource = string.Empty;
            string thumbnailSource = string.Empty;

            allSections.TryGetValue("ImageSource", out section);
            if (section != null)
            {
                section?.TryGetValue(comboBox_imageSource.Text, out imageSource!);
            }

            allSections.TryGetValue("ThumbnailSource", out section);
            if (section != null)
            {
                section?.TryGetValue(comboBox_thumbnailSource.Text, out thumbnailSource!);
            }

            allSections.TryGetValue("BoxartSource", out section);
            if (section != null)
            {
                section?.TryGetValue(comboBox_boxartSource.Text, out boxartSource!);
            }

            allSections.TryGetValue("MarqueeSource", out section);
            if (section != null)
            {
                section?.TryGetValue(comboBox_marqueeSource.Text, out marqueeSource!);
            }

            allSections.TryGetValue("CartridgeSource", out section);
            if (section != null)
            {
                section?.TryGetValue(comboBox_cartridgeSource.Text, out cartridgeSource!);
            }

            allSections.TryGetValue("VideoSource", out section);
            if (section != null)
            {
                section?.TryGetValue(comboBox_videoSource.Text, out videoSource!);
            }

            // Setup counters 
            int scraperCount = 0;
            int scraperTotal = datagridRowSelection.Count();
            await Logger.Instance.LogAsync($"Items To Scrape: {scraperTotal}", System.Windows.Media.Brushes.Blue);
            DateTime startTime = DateTime.Now;

            // Setup progress bar
            progressBar_ProgressBar.Value = 0;
            progressBar_ProgressBar.Minimum = 0;
            progressBar_ProgressBar.Maximum = scraperTotal;

            // Show maximum threads
            label_ThreadCount.Content = maxConcurrency.ToString();

            // Reset timer
            _globalStopwatch.Reset();
            _globalStopwatch.Start();

            // Reset cancellation tokensource
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
            baseScraperParameters.Language = language;
            baseScraperParameters.ScrapeEnglishGenreOnly = englishGenreOnly;
            baseScraperParameters.Region = region;
            baseScraperParameters.ImageSource = imageSource;
            baseScraperParameters.ThumbnailSource = thumbnailSource;
            baseScraperParameters.BoxartSource = boxartSource;
            baseScraperParameters.MarqueeSource = marqueeSource;
            baseScraperParameters.CartridgeSource = cartridgeSource;
            baseScraperParameters.VideoSource = videoSource;
            baseScraperParameters.OverwriteMedia = overWriteMedia;
            baseScraperParameters.OverwriteMetadata = overWriteMetadata;
            baseScraperParameters.OverwriteNames = overWriteNames;
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

            // Used for timing Image Previews
            DateTime lastUpdateTime = DateTime.Now;

            // StopPlaying button is now enabled since scraping will begin
            button_Stop.IsEnabled = true;

            // Arcade Names for Region Scraping
            string filePath = "./ini/arcadenames.ini";
            var mameNames = IniFileReader.GetSection(filePath, "ArcadeNames");

            //
            // Actual scraping starts here
            //

            var serviceCollection = new ServiceCollection();
            var startup = new Startup(new ConfigurationBuilder().Build());
            startup.ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            label_CurrentScrape.Content = "Ready!";

            await Logger.Instance.LogAsync("Starting in 5 seconds!", System.Windows.Media.Brushes.Blue);

            await Task.Delay(5000);

            var tasks = new List<Task>();

            using (var semaphore = new SemaphoreSlim(maxConcurrency))
            {
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
                                string romFileNameWithExtension = romPath.Substring(2);
                                string gameName = rowView["Name"]?.ToString()!; // Never empty
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

                                scraperParameters.ArcadeName = mameNames.TryGetValue(romFileNameNoExtension, out string? arcadeName) ? arcadeName : null;

                                bool scrapeResult = false;
                                await Logger.Instance.LogAsync($"Scraping {romFileNameWithExtension}", System.Windows.Media.Brushes.Green);
                                scrapeResult = false;

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
                                                await Logger.Instance.LogAsync($"Daily scrape limit reached!", System.Windows.Media.Brushes.Red);
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
                                    await Logger.Instance.LogAsync($"Could not scrape '{romFileNameWithExtension}'", System.Windows.Media.Brushes.Red);
                                }
                                else
                                {
                                    await Logger.Instance.LogAsync($"Finished scrape for {romFileNameWithExtension}", System.Windows.Media.Brushes.Green);
                                }

                                ShowOrHideCounts(true);
                            }
                            catch (OperationCanceledException)
                            {
                                // Gracefully handle cancellation inside the task.
                            }
                            catch (Exception ex)
                            {
                                await Logger.Instance.LogAsync($"Error: '{ex.Message}'", System.Windows.Media.Brushes.Red);
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
                    await Logger.Instance.LogAsync($"Scraping cancelled by user request", System.Windows.Media.Brushes.Red);
                }
                catch (Exception ex)
                {
                    await Logger.Instance.LogAsync($"Unexpected error: {ex.Message}", System.Windows.Media.Brushes.Red);
                }
                finally
                {
                    try
                    {
                        // Ensure all tasks are awaited, even if canceled or failed.
                        await Task.WhenAll(tasks);
                    }
                    catch (Exception ex)
                    {
                        // Catch any remaining task exceptions after waiting.
                        await Logger.Instance.LogAsync($"Error while waiting for tasks to complete: {ex.Message}", System.Windows.Media.Brushes.Red);
                    }

                    // Ensure StopScraping is always executed after all tasks finish.
                    await Logger.Instance.LogAsync($"Total time: {FormatElapsedTime(_globalStopwatch.Elapsed)}", System.Windows.Media.Brushes.Green);

                    // Empty message means it was ok.
                    StopScraping(string.Empty);
                }
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

        private void button_Setup_Click(object sender, RoutedEventArgs e)
        {
            ScraperCredentialWindow scraperCredentialDialog = new(_currentScraper);
            scraperCredentialDialog.ShowDialog();
        }

        private void button_Stop_Click(object sender, RoutedEventArgs e)
        {
            button_Stop.IsEnabled = false;
            _cancellationTokenSource.Cancel();
            label_CurrentScrape.Content = "Waiting for task(s) to complete...";
        }

        private void button_Log_Click(object sender, RoutedEventArgs e)
        {
            // Not implemented - yet!
        }

        private void button_ClearCache_Click(object sender, RoutedEventArgs e)
        {

            if (string.IsNullOrEmpty(_currentScraper) || string.IsNullOrEmpty(SharedData.CurrentSystem))
            {
                return;
            }

            string cacheFolder = $"{SharedData.ProgramDirectory}\\cache\\{_currentScraper}\\{SharedData.CurrentSystem}";
            if (!Directory.Exists(cacheFolder))
            {
                MessageBox.Show("Cache is already empty", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult result = MessageBox.Show($"Do you want to clear the {_currentScraper} cache files for '{SharedData.CurrentSystem}'?\n\n" +
            "Warning: This cannot be undone and the cache will have to be scraped again." +
            "", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
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

            MessageBox.Show("Cache has been cleared!", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void button_ShowCounts_Click(object sender, RoutedEventArgs e)
        {
            bool showCounts = button_ShowCounts.Content.ToString() == "Show Counts";
            Properties.Settings.Default.ShowCounts = showCounts;
            Properties.Settings.Default.Save();
            ShowOrHideCounts(showCounts);
        }

        public void ShowOrHideCounts(bool showCounts)
        {

            Dispatcher.BeginInvoke(() =>
            {

                var checkBoxes = VisualTreeHelperExtensions.GetAllVisualChildren<CheckBox>(stackPanel_MediaCheckboxes);

                if (showCounts)
                {
                    button_ShowCounts.Content = "Hide Counts";
                }
                else
                {
                    button_ShowCounts.Content = "Show Counts";
                }


                foreach (CheckBox checkBox in checkBoxes)
                {
                    var elementName = checkBox.Tag.ToString()!;
                    var metaDataKey = Enum.Parse<MetaDataKeys>(elementName, true);
                    var nameValue = GamelistMetaData.GetMetaDataDictionary()[metaDataKey].Name;
                    int count = SharedData.DataSet.Tables[0].AsEnumerable()
                    .Count(row => !row.IsNull(nameValue) && !string.IsNullOrWhiteSpace(row[nameValue].ToString()));

                    string currentContent = checkBox.Content.ToString()!;
                    string pattern = @"\s*\(.*?\)";
                    checkBox.Content = Regex.Replace(currentContent, pattern, string.Empty).Trim();

                    if (showCounts && checkBox.Tag.ToString() != "music")
                    {
                        currentContent = checkBox.Content.ToString()!;
                        checkBox.Content = $"{currentContent} ({count})";
                    }
                }

            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            button_AllOrSelected.Content = button_AllOrSelected.Content.ToString() == "Selected Items"
            ? "All Items"
            : "Selected Items";
        }

        private void button_ResetSources_Click(object sender, RoutedEventArgs e)
        {
            var allComboBoxes = VisualTreeHelperExtensions.GetAllVisualChildren<ComboBox>(stackPanel_AllScraperCheckboxes).ToList();

            // Filter enabled ComboBox controls
            var enabledComboBoxes = allComboBoxes
                .Where(cb => cb.IsEnabled == true);

            foreach (ComboBox combobox in enabledComboBoxes)
            {
                combobox.SelectedIndex = 0;
            }

        }

        private void checkBox_ScrapeFromCache_Checked(object sender, RoutedEventArgs e)
        {
            checkBox_OnlyScrapeFromCache.IsEnabled = true;
        }

        private void checkBox_ScrapeFromCache_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBox_OnlyScrapeFromCache.IsEnabled = false;
        }
    }
}