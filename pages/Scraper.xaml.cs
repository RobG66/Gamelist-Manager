using GamelistManager.classes;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;


namespace GamelistManager.pages
{

    public partial class Scraper : Page
    {
        private string _lastScraper = string.Empty;
        private string _currentScraper;
        private static Stopwatch globalStopwatch = new Stopwatch();
        private Dictionary<string, List<string>> _emumoviesMediaLists = new Dictionary<string, List<string>>();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken _cancellationToken => _cancellationTokenSource.Token;
        private static Stopwatch _globalStopwatch = new Stopwatch();
        private MainWindow _mainWindow;
        private readonly object _lockObject = new object();

        public Scraper(MainWindow window)
        {
            InitializeComponent();
            _currentScraper = comboBox_SelectedScraper.Text;
            _mainWindow = window;
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

            MetaDataList metaDataList = new MetaDataList();
            List<string> availableElements = metaDataList.GetScraperElements(scraper);

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
            IniFileReader iniReader = new IniFileReader(file);
            Dictionary<string, Dictionary<string, string>> allSections = iniReader.GetAllSections();

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

            if (_currentScraper == "ArcadeDB")
            {
                button_Setup.IsEnabled = false;
            }
            else
            {
                button_Setup.IsEnabled = true;
            }

            UpdateCacheCount(_currentScraper, SharedData.CurrentSystem);

            Properties.Settings.Default.Save();

        }

        private void UpdateCacheCount(string scraper, string system)
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

        private void StopScraping(string message)
        {
            globalStopwatch.Stop();
            label_CurrentScrape.Content = "Completed!";
            _mainWindow.menu_Main.IsEnabled = true;
            _mainWindow.stackPanel_Filters.IsEnabled = true;
            _mainWindow.stackPanel_UndoRedoButtons.IsEnabled = true;
            _mainWindow.MainDataGrid.IsEnabled = true;

            if (!string.IsNullOrEmpty(message))
            {
                MessageBox.Show(message, "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                label_CurrentScrape.Content = "Cancelled!";
            }

            SharedData.DataSet.AcceptChanges();
            SharedData.ChangeTracker!.EndBulkOperation();
                       
            button_Start.IsEnabled = true;
            button_Stop.IsEnabled = false;
            // Button setup tag was set to preserve original IsEnabled state
            button_Setup.IsEnabled = (button_Setup.Tag as bool?) ?? false;
            button_ClearCache.IsEnabled = (button_ClearCache.Tag as bool?) ?? false;
            comboBox_SelectedScraper.IsEnabled = true;
            stackPanel_AllScraperCheckboxes.IsEnabled = true;
            label_CurrentScrape.Content = "Completed!";
            UpdateCacheCount(_currentScraper, SharedData.CurrentSystem);
        }


        private async void StartScraping()
        {
            label_CurrentScrape.Content = "Starting Up....";
            stackPanel_AllScraperCheckboxes.IsEnabled = false;

            // ParentFolderPath string
            string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename)!;

            // Local Disables
            button_Start.IsEnabled = false;
            button_Stop.IsEnabled = false; // only enabled when the jobs start
            button_Setup.Tag = button_Setup.IsEnabled;
            button_Setup.IsEnabled = false;
            comboBox_SelectedScraper.IsEnabled = false;
            button_ClearCache.Tag = button_ClearCache.IsEnabled;
            button_ClearCache.IsEnabled = false;

            // Parent Window disables
            _mainWindow.menu_Main.IsEnabled = false;
            _mainWindow.stackPanel_Filters.IsEnabled = false;
            _mainWindow.stackPanel_UndoRedoButtons.IsEnabled = false;
            _mainWindow.MainDataGrid.IsEnabled = false;

            // Reset labels and progressbar value
            label_ScrapeErrorCount.Content = "0";
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

            // Get the media paths dictionary
            string jsonString = Properties.Settings.Default.MediaPaths;
            Dictionary<string, string> mediaPaths = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString)!;
                       
            string imagePath = mediaPaths["image"];
            string imageFolder = $"{parentFolderPath}\\{imagePath}";

            string[] imageFiles = Array.Empty<string>();
            if (Directory.Exists(imageFolder))
            {
                imageFiles = Directory.GetFiles(imageFolder, "*-image.png");
            }

            // Create bool values based on gui element state
            bool scrapeAll = button_AllOrSelected.Content.ToString() == "All Items";
            bool scrapeHidden = checkBox_ScrapeHidden.IsChecked == true;
            bool verifyImages = checkBox_VerifyImages.IsChecked == true;

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
            IniFileReader iniReader;
            string optionsFileName = $"ini\\{_currentScraper.ToLower()}_options.ini";
            iniReader = new IniFileReader(optionsFileName);
            var allSections = iniReader.GetAllSections();

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
            bool scrapeByCache = checkBox_ScrapeFromCache.IsChecked == true;

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
                    (userName, userPassword) = CredentialManager.GetCredentials("EmuMovies");
                    if (string.IsNullOrEmpty(userName))
                    {
                        StopScraping($"Credentials for {_currentScraper} are not configured");
                        return;
                    }

                    API_EmuMovies aPI_EmuMovies = new API_EmuMovies();

                    // Get the user access token
                    userAccessToken = await aPI_EmuMovies.AuthenticateEmuMoviesAsync(userName, userPassword);
                    if (userAccessToken == null)
                    {
                        StopScraping("Error retrieving user authentication token from EmuMovies!");
                        return;
                    }

                    // Retrieve media types for curren system
                    List<string> emumoviesMediaTypes = new List<string>();
                    emumoviesMediaTypes = await aPI_EmuMovies.GetMediaTypes(systemID);
                    if (emumoviesMediaTypes == null)
                    {
                        StopScraping($"Error retrieving media lists for {systemID}!");
                        return;
                    }

                    if (!File.Exists($"{cacheFolder}\\cache.json"))
                    {
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
                    (userName, userPassword) = CredentialManager.GetCredentials("ScreenScraper");
                    if (string.IsNullOrEmpty(userName))
                    {
                        StopScraping($"Credentials for {_currentScraper} are not configured");
                        return;
                    }

                    API_ScreenScraper aPI_ScreenScraper = new API_ScreenScraper();

                    // true = success, false = fail
                    bool credCheck = await aPI_ScreenScraper.VerifyScreenScraperCredentialsAsync(userName, userPassword);

                    if (!credCheck)
                    {
                        StopScraping("The ScreenScraper credentials failed to logon to remote server");
                        return;
                    }

                    maxConcurrency = await aPI_ScreenScraper.GetMaxThreadsAsync(userName, userPassword);

                    string pattern = @"\((.*?)\)";

                    string regionValue = Properties.Settings.Default.Region;
                    region = "us"; // Default
                    if (!string.IsNullOrEmpty(regionValue))
                    {
                        Match match = Regex.Match(regionValue, pattern);
                        if (match.Success)
                        {
                            region = match.Groups[1].Value;
                        }
                    }

                    string languageValue = Properties.Settings.Default.Language;
                    language = "en"; // Default
                    if (!string.IsNullOrEmpty(languageValue))
                    {
                        Match match = Regex.Match(languageValue, pattern);
                        if (match.Success)
                        {
                            language = match.Groups[1].Value;
                        }
                    }
                    break;
            };

            // Retrieve source values for media
            string imageSource = string.Empty;
            string boxSource = string.Empty;
            string logoSource = string.Empty;
            string cartridgeSource = string.Empty;
            string videoSource = string.Empty;
            allSections.TryGetValue("ImageSource", out section);
            if (section != null)
            {
                section?.TryGetValue(comboBox_ImageSource.Text, out imageSource!);
            }
            allSections.TryGetValue("BoxSource", out section);
            if (section != null)
            {
                section?.TryGetValue(comboBox_BoxSource.Text, out boxSource!);
            }
            allSections.TryGetValue("LogoSource", out section);
            if (section != null)
            {
                section?.TryGetValue(comboBox_LogoSource.Text, out logoSource!);
            }
            allSections.TryGetValue("CartridgeSource", out section);
            if (section != null)
            {
                section?.TryGetValue(comboBox_CartridgeSource.Text, out cartridgeSource!);
            }
            allSections.TryGetValue("VideoSource", out section);
            if (section != null)
            {
                section?.TryGetValue(comboBox_VideoSource.Text, out videoSource!);
            }

            // Setup counters 
            int scraperCount = 0;
            int scraperErrors = 0;
            int scraperTotal = datagridRowSelection.Count();
            DateTime startTime = DateTime.Now;

            // Setup progress bar
            progressBar_ProgressBar.Value = 0;
            progressBar_ProgressBar.Minimum = 0;
            progressBar_ProgressBar.Maximum = scraperTotal;

            // Show maximum threads
            label_ThreadCount.Content = maxConcurrency.ToString();

            // Reset timer
            globalStopwatch.Reset();
            globalStopwatch.Start();

            // Reset cancellation tokensource
            _cancellationTokenSource = new CancellationTokenSource();

            // Setup task list
            var tasks = new List<Task>();

            // Setup overWrite bool
            bool overWriteNames = checkBox_OverwriteNames.IsChecked == true ? true : false;
            bool overWriteMetadata = checkBox_OverwriteMetadata.IsChecked == true ? true : false;
            bool overWriteMedia = checkBox_OverwriteMedia.IsChecked == true ? true : false;

            // Declare basic scraper parameters
            // These do not change
            ScraperParameters baseScraperParameters = new ScraperParameters();
            baseScraperParameters.SystemID = systemID;
            baseScraperParameters.UserID = userName;
            baseScraperParameters.UserPassword = userPassword;
            baseScraperParameters.ParentFolderPath = parentFolderPath;
            baseScraperParameters.Language = language;
            baseScraperParameters.Region = region;
            baseScraperParameters.ImageSource = imageSource;
            baseScraperParameters.BoxSource = boxSource;
            baseScraperParameters.LogoSource = logoSource;
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
            baseScraperParameters.CacheFolder = cacheFolder;
            baseScraperParameters.ScrapeByGameID = false;

            // Zero daily scrape counters
            int scrapeLimitMax = 0;
            int scrapeLimitProgress = 0;

            // Used for timing Image Previews
            DateTime lastUpdateTime = DateTime.Now;

            // Stop button is now enabled since scraping will begin
            button_Stop.IsEnabled = true;

            // Actual scraping starts here
            using (var semaphore = new SemaphoreSlim(maxConcurrency))
            {
                try
                {
                    foreach (DataRowView rowView in datagridRowSelection)
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        await semaphore.WaitAsync(_cancellationToken);

                        var task = Task.Run(async () =>
                        {
                            try
                            {
                                _cancellationToken.ThrowIfCancellationRequested();

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

                                UpdateProgressLabels(gameName, startTime, scraperCount, scraperTotal, scraperErrors,scrapeLimitProgress,scrapeLimitMax);
                                                     
                                var scraperParameters = baseScraperParameters.Clone();
                                scraperParameters.RomFileNameWithoutExtension = romFileNameNoExtension;
                                scraperParameters.RomFileNameWithExtension = romFileNameWithExtension;
                                scraperParameters.GameID = gameID;
                                scraperParameters.Name = gameName;

                                MetaDataList metaDataList = new MetaDataList();

                                switch (_currentScraper)
                                {
                                    case "ArcadeDB":
                                        API_ArcadeDB aPI_ArcadeDB = new API_ArcadeDB();
                                        metaDataList = await aPI_ArcadeDB.ScrapeArcadeDBAsync(scraperParameters);
                                        break;

                                    case "EmuMovies":
                                        API_EmuMovies aPI_EmuMovies = new API_EmuMovies();
                                        metaDataList = await aPI_EmuMovies.ScrapeEmuMoviesAsync(scraperParameters, _emumoviesMediaLists);
                                        break;

                                    case "ScreenScraper":
                                        API_ScreenScraper aPI_ScreenScraper = new API_ScreenScraper();
                                        // Always scrape the ID
                                        elementsToScrape.Add("id");
                                        Tuple<int, int, MetaDataList> result = await aPI_ScreenScraper.ScrapeScreenScraperAsync(scraperParameters);
                                        if (result != null)
                                        {
                                            metaDataList = result.Item3;
                                            scrapeLimitProgress = result.Item1;
                                            scrapeLimitMax = result.Item2;
                                            UpdateProgressLabels(gameName, startTime, scraperCount, scraperTotal, scraperErrors, scrapeLimitProgress, scrapeLimitMax);
                                            if (scrapeLimitProgress >= scrapeLimitMax)
                                            {
                                                _cancellationTokenSource.Cancel();
                                            }
                                        }
                                        break;
                                }

                                if (metaDataList == null)
                                {
                                    Interlocked.Increment(ref scraperErrors);
                                    UpdateErrorCount(scraperErrors);
                                }
                                else
                                {
                                    lock (_lockObject)
                                    {
                                        DataRow? tableRow = SharedData.DataSet.Tables[0].AsEnumerable()
                                            .FirstOrDefault(r => r.Field<string>("Rom Path") == romPath);

                                        foreach (var elementName in elementsToScrape)
                                        {
                                            string scrapedValue = metaDataList.GetMetadataValue(elementName!)?.ToString() ?? string.Empty;

                                            if (string.IsNullOrEmpty(scrapedValue))
                                            {
                                                continue;
                                            }

                                            string nameValue;
                                            if (Enum.TryParse<MetaDataKeys>(elementName, true, out var metaDataKey) && metaDataList.GetMetaDataDictionary().ContainsKey(metaDataKey))
                                            {
                                                nameValue = metaDataList.GetMetaDataDictionary()[metaDataKey].Name;
                                            }
                                            else
                                            {
                                                continue;
                                            }

                                            string existingValue = tableRow?.Field<string>(nameValue) is string value && !string.IsNullOrEmpty(value) ? value : string.Empty;
                                            
                                            if (elementName == "name" && !overWriteNames && !string.IsNullOrEmpty(existingValue))
                                            { 
                                                continue;
                                            }

                                            if (elementName != "name" && !overWriteMetadata && !string.IsNullOrEmpty(existingValue))
                                            {
                                                continue;
                                            }

                                            tableRow![nameValue] = scrapedValue;
                                        }

                                        SharedData.DataSet.AcceptChanges();
                                        ShowOrHideCounts(true);
                                    }

                                    string imagePreviewFile = string.Empty;
                                    bool shouldUpdateImage = false;

                                    Dispatcher.Invoke(() =>
                                    {
                                        // Safely access UI elements
                                        shouldUpdateImage = (DateTime.Now - lastUpdateTime).TotalSeconds >= 10 || image_Preview.Source == null;
                                    });

                                    if (shouldUpdateImage)
                                    {
                                        if (imageFiles.Length > 0)
                                        {
                                            Random random = new Random();
                                            int randomIndex = random.Next(imageFiles.Length);
                                            imagePreviewFile = imageFiles[randomIndex];
                                        }
                                    }

                                    if ((DateTime.Now - lastUpdateTime).TotalSeconds >= 5)
                                    {
                                        string scrapedImage = metaDataList.GetMetadataValue("image")?.ToString() ?? string.Empty;
                                        if (elementsToScrape.Contains("image") && !string.IsNullOrEmpty(scrapedImage))
                                        {
                                            imagePreviewFile = $"{parentFolderPath}\\images\\{Path.GetFileName(scrapedImage)}";
                                        }
                                    }


                                    if (!string.IsNullOrEmpty(imagePreviewFile))
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            var bitmap = new BitmapImage();
                                            bitmap.BeginInit();
                                            bitmap.UriSource = new Uri(imagePreviewFile, UriKind.Absolute);
                                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                            bitmap.EndInit();

                                            image_Preview.Source = bitmap;
                                            lastUpdateTime = DateTime.Now;
                                        });
                                    }
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                StopScraping("Scraping cancelled!");
                                return;
                            }
                            catch
                            {
                                Interlocked.Increment(ref scraperErrors);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }, _cancellationToken);

                        tasks.Add(task);
                    }

                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException)
                {
                    StopScraping("Scraping cancelled!");
                    return;
                }
                catch (Exception ex)
                {
                    StopScraping($"Unexpected error during scraping process: {ex.Message}");
                    return;
                }
            }
            StopScraping(string.Empty);
        }


        public void UpdateErrorCount(int errorCount)
        {
            Dispatcher.BeginInvoke(() =>
            {
                label_ScrapeErrorCount.Content = errorCount.ToString();
            });
        }

        public void UpdateProgressLabels(string romLabel, DateTime startTime, int current, int total, int errors, int scrapeLimitProgress, int scrapeLimitMax)
        {
            // Updates in UI thread:
            Dispatcher.BeginInvoke(() =>
            {
                // Update scraper limits
                if (scrapeLimitMax == 0)
                {
                    label_ScrapeLimitCount.Content = "N/A";
                }
                else
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
            ScraperCredentialDialog scraperCredentialDialog = new ScraperCredentialDialog(_currentScraper);
            scraperCredentialDialog.ShowDialog();
        }

        private void button_Stop_Click(object sender, RoutedEventArgs e)
        {
            button_Stop.IsEnabled = false;
            _cancellationTokenSource.Cancel();
            globalStopwatch.Stop();
        }

        private void button_Log_Click(object sender, RoutedEventArgs e)
        {

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


                MetaDataList metaDataList = new MetaDataList();

                foreach (CheckBox checkBox in checkBoxes)
                {
                    var elementName = checkBox.Tag.ToString()!;
                    var metaDataKey = Enum.Parse<MetaDataKeys>(elementName, true);
                    var nameValue = metaDataList.GetMetaDataDictionary()[metaDataKey].Name;
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
    }
}
