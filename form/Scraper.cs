using GamelistManager.control;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GamelistManager
{
    public partial class Scraper : Form
    {
        private DateTime lastUpdateTime;
        private static readonly string cacheFilePath = Path.Combine(Environment.CurrentDirectory, "mediaListsCache.json");
        public ListBox ListBoxControl { get; private set; }
        private GamelistManager gamelistManager;
        private ScraperPreview scraperPreview;
        private CancellationTokenSource CancellationTokenSource;
        private CancellationToken CancellationToken => CancellationTokenSource.Token;
        private static Stopwatch globalStopwatch;
        private int scraperCount;
        private bool scraperFinished;
        private int totalCount;
        private int scraperErrors;
        private Dictionary<string, List<string>> emumoviesMediaLists;
        private string batoceraSystemName;
        private string previousScraperName;
 
        public Scraper(GamelistManager form)
        {
            InitializeComponent();
            ListBoxControl = listBoxDownloads;
            CancellationTokenSource = new CancellationTokenSource();
            scraperCount = 0;
            totalCount = 0;
            scraperErrors = 0;
            scraperFinished = false;
            gamelistManager = form;
            globalStopwatch = new Stopwatch();
            emumoviesMediaLists = new Dictionary<string, List<string>>();
            batoceraSystemName = Path.GetFileName(Path.GetDirectoryName(SharedData.XMLFilename));
         }

        private List<string> GetElementsToScrape()
        {
            List<string> elements = new List<string>();
            foreach (Control control in panelSmall.Controls)
            {
                if (control is CheckBox checkBox && checkBox.Checked && checkBox.Enabled)
                {
                    string elementName = checkBox.Name.Replace("checkbox", "").ToLower();
                    elements.Add(elementName);
                }
            }
            return elements;
        }

        private async void StartScraping()
        {
            // Pre scraping form configuration
            gamelistManager.Enabled = false; // disable main form
            if (listBoxLog.Items.Count > 0) { listBoxLog.Items.Clear(); } // clear log
            if (listBoxDownloads.Items.Count > 0) { listBoxDownloads.Items.Clear(); }
            panelScraperOptions.Enabled = false;
            buttonStart.Enabled = false;
            buttonCancel.Enabled = false;
            buttonSetup.Enabled = false;
            panelSmall.Visible = false;
            labelScrapeCount.Text = "Limit: N/A";
            labelCounts.Text = $"0/0";
            labelProgress.Text = "0%";
            labelDownloadCountValue.Text = "0";
            contextMenuStripLog.Enabled = false;
            labelThreads.Text = $"Threads: -";

            // Was anything selected for scraping?
            List<string> elementsToScrape = GetElementsToScrape();
            if (elementsToScrape.Count == 0)
            {
                MessageBox.Show("No metadata selection was made", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                StopScraping();
                return;
            }

            // Make a list of rows or selected rows
            List<DataGridViewRow> selectedRowsList = new List<DataGridViewRow>();
            if (radioButtonScrapeAll.Checked)
            {
                selectedRowsList = gamelistManager.DataGridView.Rows.Cast<DataGridViewRow>().ToList();
            }
            else
            {
                selectedRowsList = gamelistManager.DataGridView.SelectedRows.Cast<DataGridViewRow>().ToList();
            }

            if (checkBoxDoNotScrapeHidden.Checked)
            {
                // Filter out rows where "hidden" column is true
                var filteredRows = selectedRowsList.Where(row =>
                {
                    // Replace "hidden" with the actual name of your column
                    var hiddenValue = row.Cells["hidden"].Value;
                    if (hiddenValue != null && hiddenValue is bool boolHidden)
                    {
                        return !boolHidden; // Include rows where "hidden" is false
                    }
                    return true; // Include rows where "hidden" is DBNull or null
                }).ToList();

                selectedRowsList = filteredRows;
            }

            if (selectedRowsList.Count == 0)
            {
                MessageBox.Show("No roms to scrape!", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                StopScraping();
                return;
            }

            // Sort alphabetically
            selectedRowsList = selectedRowsList.OrderBy(row => row.Cells["path"].Value.ToString()).ToList();

            // Extract paths from selectedRowsList
            List<string> romPathList = selectedRowsList.Select(row => row.Cells["path"].Value?.ToString()).ToList();

            // Add any missing columns
            foreach (string element in elementsToScrape)
            {
                if (!SharedData.DataSet.Tables["game"].Columns.Contains(element))
                {
                    SharedData.DataSet.Tables["game"].Columns.Add(element, typeof(string));
                }
            }

            // Initialize and set variables
            string scraperPlatform = comboBoxScrapers.Text;
            string userID = null;
            string userPassword = null;
            string optionsFileName = null;
            string systemID = null; // Batocera system ID
            string boxSource = null;
            string cartridgeSource = null;
            string imageSource = null;
            string logoSource = null;
            string region = null;
            string language = null;
            string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename);
            int scraperCount = 0;
            int scrapeErrors = 0;
            int maxConcurrency = 1; // Max scraping threads
            bool overwrite = checkBoxOverwriteExisting.Checked;
            bool scraperCancelled = false;
            bool hideNonGame = false;
            bool noZZZ = false; // ScreenScraper Specific
            bool scrapeByGameID = false; // ScreenScraper Specific
            string userAccessToken = null; // EmuMovies Specific
            string imageFolder = $"{parentFolderPath}\\images";
            string[] imageFiles = new string[0];
            Dictionary<string, string> section;
            Dictionary<string, Dictionary<string, string>> allSections;

            // For scraper preview
            // If not scraping images, then show existing
            if (Directory.Exists(imageFolder))
            {
                imageFiles = Directory.GetFiles(imageFolder, "*-image.png");
            }

            IniFileReader iniReader;
                        
            // Platform specific stuff
            switch (scraperPlatform)
            {
                case "ScreenScraper":
                    {
                        (userID, userPassword) = CredentialManager.GetCredentials(scraperPlatform);
                        if (string.IsNullOrEmpty(userID) || string.IsNullOrEmpty(userPassword))
                        {
                            MessageBox.Show("The ScreenScraper credentials are not set!", "Missing Credentials", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            StopScraping();
                            return;
                        }

                        AddToLog("Reading configuration file");
                        optionsFileName = "ini\\screenscraper_options.ini";

                        iniReader = new IniFileReader(optionsFileName);
                        allSections = iniReader.GetAllSections();
                        allSections.TryGetValue("Systems", out section);
                                                
                        systemID = section[batoceraSystemName];
                        if (string.IsNullOrEmpty(systemID) || systemID == "0")
                        {
                            MessageBox.Show($"A system ID is missing for system '{batoceraSystemName}' in {optionsFileName}!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            StopScraping();
                            return;
                        }


                        API_ScreenScraper aPI_ScreenScraper = new API_ScreenScraper();
                        maxConcurrency = await aPI_ScreenScraper.GetMaxThreads(userID, userPassword);

                        // Always scrape ID
                        elementsToScrape.Add("id");

                        // Get source values
                        allSections.TryGetValue("ImageSource", out section);
                        section.TryGetValue(comboboxImageSource.Text, out imageSource);
                        allSections.TryGetValue("BoxSource", out section);
                        section.TryGetValue(comboboxImageSource.Text, out boxSource);
                        allSections.TryGetValue("LogoSource", out section);
                        section.TryGetValue(comboboxImageSource.Text, out logoSource);

                        region = RegistryManager.ReadRegistryValue("ScreenScraper", "Region");
                        language = RegistryManager.ReadRegistryValue("ScreenScraper", "language");

                        // Extrapolate short region elementValue
                        string pattern = @"\((.*?)\)";
                        Match match = Regex.Match(region, pattern);
                        if (match.Success)
                        {
                            region = match.Groups[1].Value;
                        }
                        else
                        {
                            region = "us";
                        }

                        // Extrapolate short language elementValue
                        match = Regex.Match(language, pattern);
                        if (match.Success)
                        {
                            // Extrapolate short language elementValue
                            language = match.Groups[1].Value;
                        }
                        else
                        {
                            language = "en";
                        }

                        bool.TryParse(RegistryManager.ReadRegistryValue("ScreenScraper", "HideNonGame"), out hideNonGame);
                        bool.TryParse(RegistryManager.ReadRegistryValue("ScreenScraper", "NoZZZ"), out noZZZ);
                        bool.TryParse(RegistryManager.ReadRegistryValue("ScreenScraper", "ScrapeByGameID"), out scrapeByGameID);
                    }
                    break;

                case "ArcadeDB":
                    {
                        maxConcurrency = 1;
                        if ((batoceraSystemName != "mame" && batoceraSystemName != "fbneo"))
                        {
                            MessageBox.Show($"ArcadeDB only supports Mame and FBNeo scraping", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            StopScraping();
                            return;
                        }
                        AddToLog("Reading configuration file");
                        optionsFileName = "ini\\arcadedb_options.ini";
                        iniReader = new IniFileReader(optionsFileName);
                        allSections = iniReader.GetAllSections();
                        
                        allSections.TryGetValue("ImageSource", out section);
                        section.TryGetValue(comboboxImageSource.Text, out imageSource);
                        allSections.TryGetValue("BoxSource", out section);
                        section.TryGetValue(comboboxBoxSource.Text, out boxSource);
                        allSections.TryGetValue("LogoSource", out section);
                        section.TryGetValue(comboboxLogoSource.Text, out logoSource);
                    }
                    break;

                case "EmuMovies":
                    API_EmuMovies aPI_EmuMovies = new API_EmuMovies();
                    (userID, userPassword) = CredentialManager.GetCredentials(scraperPlatform);
                    if (string.IsNullOrEmpty(userID) || string.IsNullOrEmpty(userPassword))
                    {
                        MessageBox.Show("The EmuMovies credentials are not set!", "Missing Credentials", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        StopScraping();
                        return;
                    }
                    AddToLog("Reading configuration file");
                    optionsFileName = "ini\\emumovies_options.ini";

                    iniReader = new IniFileReader(optionsFileName);
                    allSections = iniReader.GetAllSections();
                    allSections.TryGetValue("Systems", out section);

                    systemID = section[batoceraSystemName];
                    if (string.IsNullOrEmpty(systemID))
                    {
                        MessageBox.Show($"A system ID is missing for system '{batoceraSystemName}' in {optionsFileName}!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        StopScraping();
                        return;
                    }

                    allSections.TryGetValue("ImageSource", out section);
                    section.TryGetValue(comboboxImageSource.Text, out imageSource);
                    allSections.TryGetValue("BoxSource", out section);
                    section.TryGetValue(comboboxBoxSource.Text, out boxSource);
                    allSections.TryGetValue("LogoSource", out section);
                    section.TryGetValue(comboboxLogoSource.Text, out logoSource);
                    allSections.TryGetValue("CartridgeSource", out section);
                    section.TryGetValue(comboboxCartridgeSource.Text, out cartridgeSource);
                    maxConcurrency = 2;

                    AddToLog("Getting access token");
                    userAccessToken = await aPI_EmuMovies.AuthenticateEmuMoviesAsync(userID, userPassword);
                    if (userAccessToken == null)
                    {
                        MessageBox.Show("Error retrieving user authentication token from EmuMovies!", "Authentication Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        StopScraping();
                        return;
                    }

                    List<string> emumoviesMediaTypes = new List<string>();
                    AddToLog("Retrieving media types");
                    emumoviesMediaTypes = await aPI_EmuMovies.GetMediaTypes(systemID);
                    if (emumoviesMediaTypes == null)
                    {
                        MessageBox.Show($"Error retrieving {systemID} media types!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        StopScraping();
                        return;
                    }

                    if (!File.Exists(cacheFilePath))
                    {
                        foreach (string mediaType in emumoviesMediaTypes)
                        {
                            AddToLog($"Downloading {mediaType} medialist");
                            List<string> medialist = await aPI_EmuMovies.GetMediaList(systemID, mediaType);
                            AddToLog($"{medialist.Count} images found");
                            emumoviesMediaLists[mediaType] = medialist;
                        }
                        string json = JsonSerializer.Serialize(emumoviesMediaLists, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(cacheFilePath, json);
                    }
                    else
                    {
                        string json = File.ReadAllText(cacheFilePath);
                        var mediaLists = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);

                        if (mediaLists != null)
                        {
                            foreach (var kvp in mediaLists)
                            {
                                emumoviesMediaLists[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                    break;
            }

            // Image preview control during scraping
            scraperPreview = new ScraperPreview();
            scraperPreview.Dock = DockStyle.Fill;
            panelSmall.Controls.Add(scraperPreview);
            System.Drawing.Image image = (Bitmap)Properties.Resources.ResourceManager.GetObject(batoceraSystemName);
            if (image is System.Drawing.Image)
            {
                scraperPreview.UpdatePictureBoxWithImage(image);
            }

            // Setup progress bar
            totalCount = romPathList.Count();  // how many items
            progressBarScrapeProgress.Value = 0;
            progressBarScrapeProgress.Minimum = 0;
            progressBarScrapeProgress.Step = 1;
            progressBarScrapeProgress.Maximum = totalCount;

            // Reset timer
            globalStopwatch.Reset();
            globalStopwatch.Start();

            // Update thread count
            labelThreads.Text = $"Threads: {maxConcurrency}";


            // Reset cancellation tokensource
            // Getting close!
            CancellationTokenSource = new CancellationTokenSource();

            // Enable cancel button now after cancellation token set
            buttonCancel.Enabled = true;

            // Setup task list
            var tasks = new List<Task>();

            // Static parameters set here, they do not change during scrape
            ScraperParameters baseScraperParameters = new ScraperParameters();
            baseScraperParameters.Overwrite = overwrite;
            baseScraperParameters.ParentFolderPath = parentFolderPath;
            baseScraperParameters.ElementsToScrape = elementsToScrape;
            baseScraperParameters.BoxSource = boxSource;
            baseScraperParameters.CartridgeSource = cartridgeSource;
            baseScraperParameters.ImageSource = imageSource;
            baseScraperParameters.LogoSource = logoSource;
            baseScraperParameters.UserID = userID;
            baseScraperParameters.UserPassword = userPassword;
            baseScraperParameters.Region = region;
            baseScraperParameters.Language = language;
            baseScraperParameters.UserAccessToken = userAccessToken;
            baseScraperParameters.SystemID = systemID;
            baseScraperParameters.NoZZZ = noZZZ;
            baseScraperParameters.HideNonGame = hideNonGame;
            baseScraperParameters.ScraperPlatform = scraperPlatform;

            lastUpdateTime = DateTime.Now;

            // Enable cancel
            buttonCancel.Enabled = true;

            // Start
            using (var semaphore = new SemaphoreSlim(maxConcurrency))
            {
                try
                {
                    foreach (DataRow row in SharedData.DataSet.Tables["game"].Rows)
                    {
                        CancellationToken.ThrowIfCancellationRequested();

                        string rowPath = row.Field<string>("path");

                        if (!romPathList.Any(selectedPath => selectedPath == rowPath))
                        {
                            continue;
                        }

                        await semaphore.WaitAsync(CancellationToken);

                        var task = Task.Run(async () =>
                        {
                            try
                            {
                                CancellationToken.ThrowIfCancellationRequested();

                                // Variables
                                string romPath = row.Field<string>("path").ToString();
                                string romFileNameNoExtension = Path.GetFileNameWithoutExtension(romPath);
                                string romFileNameWithExtension = romPath.Substring(2);

                                // gameID is ScreenScraper specific
                                string gameID = null;
                                if (scrapeByGameID)
                                {
                                    gameID = row["id"]?.ToString() ?? null;
                                }
                                
                                string gameName = row.Field<string>("name").ToString();
                                int scrapeTotal = 0;
                                int scrapeMax = 0;

                                // All settings and variables in a new var
                                var scraperParameters = baseScraperParameters.Clone();
                                scraperParameters.RomFileNameWithoutExtension = romFileNameNoExtension;
                                scraperParameters.RomFileNameWithExtension = romFileNameWithExtension;
                                scraperParameters.GameID = gameID;
                                scraperParameters.Name = gameName;
                                

                                string showID = null;
                                if (!string.IsNullOrEmpty(gameID))
                                {
                                    showID = $"[{gameID}] ";
                                }
                                AddToLog($"Scraping: {showID}'{romFileNameWithExtension}'");

                                ScraperData scraperData = new ScraperData();

                                // Scrape information first for chosen scraper
                                switch (scraperPlatform)
                                {
                                    case "ArcadeDB":
                                        // Scrape file name without extension
                                        API_ArcadeDB aPI_ArcadeDB = new API_ArcadeDB();
                                        scraperData = await aPI_ArcadeDB.ScrapeArcadeDBAsync(scraperParameters, ListBoxControl);
                                        break;

                                    case "ScreenScraper":
                                        API_ScreenScraper aPI_ScreenScraper = new API_ScreenScraper();
                                        Tuple<int, int, ScraperData> result = await aPI_ScreenScraper.ScrapeScreenScraperAsync(scraperParameters, ListBoxControl);
                                        scraperData = result.Item3;
                                        scrapeTotal = result.Item1;
                                        scrapeMax = result.Item2;
                                        break;

                                    case "EmuMovies":
                                        API_EmuMovies aPI_EmuMovies = new API_EmuMovies();
                                        scraperData = await aPI_EmuMovies.ScrapeEmuMoviesAsync(scraperParameters, ListBoxControl, emumoviesMediaLists);
                                        break;
                                }

                                // Update count
                                Interlocked.Increment(ref scraperCount);
                                UpdateLabel(scraperCount, totalCount, scrapeErrors, scrapeTotal, scrapeMax);

                                // Update progress bar
                                UpdateProgressBar();

                                if (scraperData == null)
                                {
                                    AddToLog($"Cannot scrape '{romFileNameWithExtension}'");
                                    Interlocked.Increment(ref scrapeErrors);
                                    UpdateLabel(scraperCount, totalCount, scrapeErrors, scrapeTotal, scrapeMax);
                                }

                                if (checkBoxDiscardBadImages.Checked && scraperData != null)
                                {
                                    Type type = typeof(ScraperData);
                                    PropertyInfo[] properties = type.GetProperties();

                                    foreach (PropertyInfo property in properties)
                                    {
                                        string value = property.GetValue(scraperData) as string;

                                        if (!string.IsNullOrEmpty(value) && (value.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || value.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)))
                                        {
                                            string fullPath = value.TrimStart('.', '/');
                                            fullPath = fullPath.Replace('/', '\\');

                                            // Combine with parent folder path
                                            fullPath = Path.Combine(scraperParameters.ParentFolderPath, fullPath);
                                            string result = ImageChecker.CheckImage(fullPath);
                                            if (result != "ok")
                                            {
                                                property.SetValue(scraperData, null);
                                                File.Delete(fullPath);
                                                AddToLog($"Deleted bad image: {Path.GetFileName(value)}");
                                            }
                                        }
                                    }
                                }

                                lock (SharedData.lockObject)
                                {
                                    if (scraperData != null)
                                    {
                                        // Process scraped items
                                        foreach (string elementName in scraperParameters.ElementsToScrape)
                                        {
                                            PropertyInfo property = typeof(ScraperData).GetProperty(elementName);
                                            // Get the value of the property from scraperData
                                            object value = property.GetValue(scraperData);

                                            string elementValue = null;
                                            // Convert the value to string if not null
                                            elementValue = value?.ToString();

                                            if (string.IsNullOrEmpty(elementValue))
                                            {
                                                continue;
                                            }

                                            string existingValue = row[elementName] as string;
                                            if (!overwrite && !string.IsNullOrEmpty(existingValue))
                                            {
                                                continue;
                                            }
                                            if (elementName == "name" && elementValue.Contains("notgame"))
                                            {
                                                if (hideNonGame)
                                                {
                                                    row["hidden"] = true;
                                                }
                                                if (noZZZ)
                                                {
                                                    elementValue = (elementValue.Replace("ZZZ(notgame):", "")).Trim();
                                                }
                                            }

                                            row[elementName] = elementValue.ToString();
                                        }
                                    }

                                    // Scraper information is deserialized in the dataset
                                    // It is put back in order when saved
                                    string now = DateTime.Now.ToString();
                                    string iso8601Format = ISO8601Converter.ConvertToISO8601(now);
                                    row[$"scrap_{baseScraperParameters.ScraperPlatform}"] = iso8601Format;

                                    SharedData.DataSet.AcceptChanges();
                                }

                                if (scraperData == null)
                                {
                                    return;
                                }

                                // Update Scraper Preview
                                string imagePreviewFile = null;
                                if ((DateTime.Now - lastUpdateTime).TotalSeconds >= 10)
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
                                    if (elementsToScrape.Contains("image") && !string.IsNullOrEmpty(scraperData.image))
                                    {
                                        string scrapedImageString = scraperData.image;
                                        imagePreviewFile = $"{parentFolderPath}\\images\\{Path.GetFileName(scrapedImageString)}";
                                    }
                                }

                                if (!string.IsNullOrEmpty(imagePreviewFile))
                                {
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        scraperPreview.UpdatePictureBox(imagePreviewFile);
                                        lastUpdateTime = DateTime.Now;
                                    });
                                }

                            }
                            catch (OperationCanceledException)
                            {
                                AddToLog("Operation was canceled.");
                            }
                            catch (Exception ex)
                            {
                                Interlocked.Increment(ref scrapeErrors);
                                AddToLog($"Error scraping row: {ex.Message}");
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }, CancellationToken);

                        tasks.Add(task);
                    }

                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException)
                {
                    scraperCancelled = true;
                }
                catch (Exception ex)
                {
                    AddToLog($"Unexpected error during scraping process: {ex.Message}");
                    scraperCancelled = true;
                }
            }

            // Build new genre list because genres can change
            int currentIndex = gamelistManager.ComboBoxGenre1.SelectedIndex;
            string currentGenre = gamelistManager.ComboBoxGenre1.Text;
            string currentFilter = SharedData.DataSet.Tables["game"].DefaultView.RowFilter;
            gamelistManager.BuildCombobox();

            if (gamelistManager.ComboBoxGenre1.Enabled)
            {
                if (gamelistManager.ComboBoxGenre1.Items.Contains(currentGenre) && currentIndex > 1)
                {
                    gamelistManager.ComboBoxGenre1.Text = currentGenre;
                }
                if (currentIndex == 1)
                {
                    gamelistManager.ComboBoxGenre1.SelectedIndex = 1;
                }
            }
            else
            {
                SharedData.DataSet.Tables["game"].DefaultView.RowFilter = currentFilter;
            }

            // Finish message
            AddToLog(scraperCancelled ? "Scraping cancelled!" : "Scraping completed!");

            // Popup Notification
            if (scraperCount > 0)
            {
                string elapsedTime = $"{globalStopwatch.Elapsed.TotalMinutes:F0} minutes and {globalStopwatch.Elapsed.Seconds} seconds";
                MessageBox.Show($"Finished scraping {scraperCount} items in {elapsedTime}");

                if (checkBoxSave.Checked)
                {
                    SaveReminder();
                }
            }

            // Memory Cleanup
            tasks.Clear();
            tasks = null;
            GC.Collect();

            // Remove image preview
            panelSmall.Controls.Remove(scraperPreview);
            scraperPreview.Dispose();

            // Finally reset the form controls
            StopScraping();
        }


        private void StopScraping()
        {
            panelScraperOptions.Enabled = true;
            buttonStart.Enabled = true;
            buttonCancel.Enabled = false;
            // ArcadeDB has no setup
            if (comboBoxScrapers.Text != "ArcadeDB")
            {
                buttonSetup.Enabled = true;
            }
            globalStopwatch.Stop();
            panelSmall.Visible = true;
            gamelistManager.Enabled = true;
            contextMenuStripLog.Enabled = true;
        }

        private void SaveReminder()
        {
            gamelistManager.SaveFile(SharedData.XMLFilename);
        }

        private void Scraper_Load(object sender, EventArgs e)
        {

            string lastScraper = RegistryManager.ReadRegistryValue(null, "Last Scraper");

            if (!string.IsNullOrEmpty(lastScraper) && comboBoxScrapers.Items.Contains(lastScraper))
            {
                comboBoxScrapers.Text = lastScraper;
            }
            else
            {
                comboBoxScrapers.SelectedIndex = 0;
                lastScraper = comboBoxScrapers.Text;
            }
            
            // Reset cache
            if (File.Exists(cacheFilePath))
            {
                File.Delete(cacheFilePath);
            }

            // Show system image if exists
            string romPath = Path.GetFileName(Path.GetDirectoryName(SharedData.XMLFilename));
            System.Drawing.Image image = (Bitmap)Properties.Resources.ResourceManager.GetObject(romPath);
            if (image is System.Drawing.Image)
            {
                pictureBox1.Image = image;
            }

            LoadScraperSettings(lastScraper, batoceraSystemName);
            previousScraperName = lastScraper;
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

        private DateTime startTime = DateTime.Now;

        public void UpdateLabel(int current, int total, int errors, int scrapeTotal, int scrapeMax)
        {
            if (labelProgress == null || labelCounts == null) return;

            if (labelProgress.InvokeRequired || labelCounts.InvokeRequired || labelScrapeCount.InvokeRequired)
            {
                labelProgress.Invoke(new Action(() => UpdateLabel(current, total, errors, scrapeTotal, scrapeMax)));
            }
            else
            {
                double progress = (double)current / total * 100;

                TimeSpan elapsed = DateTime.Now - startTime;

                double remainingMilliseconds = (elapsed.TotalMilliseconds / progress) * (100 - progress);
                TimeSpan remainingTime = TimeSpan.FromMilliseconds(remainingMilliseconds);

                // Display only non-zero components
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

                labelProgress.Text = $"{progress:F0}% | Remaining Time: {remainingTimeString}";

                // Update the labelCounts with the current count and totalCount count
                labelCounts.Text = errors > 0 ? $"{current}/{total} | Errors:{errors}" : $"{current}/{total}";

                // Add scraper limit information if applicable
                if (scrapeMax > 0)
                {
                    labelScrapeCount.Text = $"Limit: {scrapeTotal}/{scrapeMax}";
                }

                int downloadCount = listBoxDownloads.Items.Count;
                labelDownloadCountValue.Text = downloadCount.ToString();

            }
        }

        public void UpdateProgressBar()
        {
            if (progressBarScrapeProgress.InvokeRequired)
            {
                progressBarScrapeProgress.Invoke(new Action(() => progressBarScrapeProgress.Value++));
            }
            else
            {
                progressBarScrapeProgress.Value++;
            }
        }

        
        private void ComboBox_SelectScraper_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Button_Stop_Click(object sender, EventArgs e)
        {
            CancellationTokenSource.Cancel();
            buttonCancel.Enabled = false;
            AddToLog("Cancelling scraper task......");
            globalStopwatch.Stop();
        }

        private void ButtonSetup_Click(object sender, EventArgs e)
        {
            string scraperName = comboBoxScrapers.Text;
            ScraperSetup screenScraperSetup = new ScraperSetup(scraperName)
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(this.Location.X + 50, this.Location.Y + 50)
            };
            screenScraperSetup.ShowDialog();
        }

        private void ButtonStartStop_Click(object sender, EventArgs e)
        {            
            StartScraping();
        }

        private void ToolStripMenuItemCopyLogToClipboard_Click(object sender, EventArgs e)
        {
            string listBoxItems = string.Join(Environment.NewLine, listBoxLog.Items.Cast<object>().Select(item => item.ToString()));

            // Set the string as text data on the clipboard
            Clipboard.SetText(listBoxItems);
        }

        private void Scraper_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (buttonStart.Enabled == false)
            {
                // Cancel the form closing event
                e.Cancel = true;
                MessageBox.Show("Please cancel scraping first!");
            }

            string scraper = comboBoxScrapers.Text;
            RegistryManager.WriteRegistryValue(null, "Last Scraper", scraper);
            SaveScraperSettings(scraper,batoceraSystemName);
        }

        private void SaveScraperSettings(string scraper, string system)
        {
            if (string.IsNullOrEmpty(scraper) || string.IsNullOrEmpty(system))
            {
                return;
            }

            // Save checkbox state
            List<string> elements = new List<string>();
            foreach (Control control in panelSmall.Controls)
            {
                if (control is CheckBox checkBox && checkBox.Checked && checkBox.Enabled)
                {
                    string elementName = checkBox.Name.Replace("checkbox", "").ToLower();
                    elements.Add(elementName);
                }
            }

            string checkboxString = String.Join(",", elements);
            RegistryManager.WriteRegistryValue(scraper, "Checkboxes", checkboxString);

            // Save source settings
            List<string> sources = new List<string>();
            foreach (Control control in panelSmall.Controls)
            {
                if (control is ComboBox comboBox)
                {
                    string comboboxvalue = comboBox.Text;
                    string name = comboBox.Name.Replace("combobox", "");
                    sources.Add($"{name}={comboboxvalue}");
                }
            }
            string sourcesString = string.Join(",", sources);
            RegistryManager.WriteRegistryValue(scraper, system, sourcesString);
        }

        private void LoadScraperSettings(string scraper, string system)
        {
            if (string.IsNullOrEmpty(scraper) || string.IsNullOrEmpty(system))
            {
                return; 
            }

            string regValue = null;
            regValue = RegistryManager.ReadRegistryValue(scraper, system);
            Dictionary<string, string> comboboxSavedValues = null;
            if (!string.IsNullOrEmpty(regValue))
            {
                comboboxSavedValues = regValue.Split(',')
                   .Select(part => part.Split('='))
                   .Where(parts => parts.Length == 2)
                   .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());
            }
                      
            string file = $".\\ini\\{scraper.ToLower()}_options.ini";
            IniFileReader iniReader = new IniFileReader(file);
            Dictionary<string, Dictionary<string, string>> allSections = iniReader.GetAllSections();
            
            foreach (Control control in panelSmall.Controls)
            {
                if (control is ComboBox comboBox)
                {
                    string name = control.Name.Replace("combobox", "");
                    comboBox.Items.Clear();
                    if (allSections.TryGetValue(name, out var section))
                    {
                        comboBox.Enabled = true;
                        comboBox.Items.AddRange(section.Keys.ToArray());
                        if (comboboxSavedValues != null && comboboxSavedValues.ContainsKey(name))
                        {
                            comboboxSavedValues.TryGetValue(name, out string savedValue);
                            if (comboBox.Items.Contains(savedValue))
                            {
                                comboBox.Text = savedValue;
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
                        comboBox.Enabled = false;
                    }
                }
            }

                    
            // Setup checkboxes
            List<string> availableScraperElements = new List<string>();
            switch (scraper)
            {
                case "ArcadeDB":
                    buttonSetup.Enabled = false;
                    availableScraperElements = new List<string>{
                    "name",
                    "desc",
                    "genre",
                    "players",
                    "rating",
                    "lang",
                    "releasedate",
                    "publisher",
                    "marquee",
                    "image",
                    "video",
                    "thumbnail",
                    "titleshot"
                    };
                    break;

                case "ScreenScraper":
                    buttonSetup.Enabled = true;
                    availableScraperElements = new List<string>{
                    "name",
                    "desc",
                    "genre",
                    "players",
                    "rating",
                    "lang",
                    "releasedate",
                    "publisher",
                    "marquee",
                    "image",
                    "video",
                    "developer",
                    "map",
                    "region",
                    "manual",
                    "bezel",
                    "thumbnail",
                    "boxback",
                    "fanart",
                    "titleshot",
                    "arcadesystemname",
                    "genreid"
                };
                    break;

                case "EmuMovies":
                    buttonSetup.Enabled = true;
                    availableScraperElements = new List<string>{
                    "marquee",
                    "image",
                    "video",
                    "map",
                    "manual",
                    "bezel",
                    "thumbnail",
                    "boxback",
                    "fanart",
                    "cartridge",
                    "titleshot"
                 };
                    break;
            }

            // Reset checkboxes
            regValue = RegistryManager.ReadRegistryValue(scraper, "Checkboxes");
            string[] savedCheckstates = new string[0];
            if (regValue != null)
            {
                savedCheckstates = regValue.Split(',');
            }

            foreach (Control control in panelSmall.Controls)
            {
                if (control is CheckBox checkBox)
                {
                    checkBox.Checked = false;
                    string checkboxShortName = control.Name.Replace("checkbox", "").ToLower();
                    if (availableScraperElements.Contains(checkboxShortName))
                    {
                        checkBox.Enabled = true;
                        if (savedCheckstates.Contains(checkboxShortName))
                        {
                            checkBox.Checked = true;
                        }
                        else
                        {
                            checkBox.Checked = false;
                        }
                    }
                    else
                    {
                        checkBox.Enabled = false;
                        checkBox.Checked = false;
                    }
                }
            }
        }

        private void comboBoxScrapers_SelectionChangeCommitted(object sender, EventArgs e)
        {
            string scraper = comboBoxScrapers.Text;
            SaveScraperSettings(previousScraperName, batoceraSystemName);
            LoadScraperSettings(scraper,batoceraSystemName);
            previousScraperName = scraper;
        }


        private void buttonSelectAll_Click(object sender, EventArgs e)
        {
            foreach (Control control in panelSmall.Controls)
            {
                // Check if the control is a checkbox
                if (control is System.Windows.Forms.CheckBox checkBox && checkBox.Enabled == true)
                {
                    // Set checked
                    checkBox.Checked = true;
                }
            }
        }

        private void buttonSelectNone_Click_1(object sender, EventArgs e)
        {

            foreach (Control control in panelSmall.Controls)
            {
                // Check if the control is a checkbox
                if (control is System.Windows.Forms.CheckBox checkBox && checkBox.Enabled == true)
                {
                    // Set unchecked
                    checkBox.Checked = false;
                }
            }
        }
    }
    
    }




