using GamelistManager.control;
using System;
using System.Collections.Concurrent;
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
    public partial class ScraperForm : Form
    {
        private DateTime lastUpdateTime;
        private static readonly string cacheFilePath = Path.Combine(Environment.CurrentDirectory, "mediaListsCache.json");
        public ListBox ListBoxControl { get; private set; }
        private GamelistManagerForm gamelistManagerForm;
        private ScraperPreview scraperPreview;
        private CancellationTokenSource CancellationTokenSource;
        private CancellationToken CancellationToken => CancellationTokenSource.Token;
        private static Stopwatch globalStopwatch = new Stopwatch();
        private int scraperCount;
        private bool scraperFinished;
        private int totalCount;
        private int scraperErrors;
        private int scrapMax;
        private int scrapTotal;

        public ScraperForm(GamelistManagerForm form)
        {
            InitializeComponent();
            ListBoxControl = listBoxDownloads;
            CancellationTokenSource = new CancellationTokenSource();
            scraperCount = 0;
            totalCount = 0;
            scraperErrors = 0;
            scrapMax = 0;
            scrapTotal = 0;
            scraperFinished = false;
            gamelistManagerForm = form;
        }

        private List<string> GetElementsToScrape()
        {
            List<string> elements = new List<string>();
            foreach (Control control in panelCheckboxes.Controls)
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
            // Was anything selected for scraping?
            List<string> elementsToScrape = GetElementsToScrape();
            if (elementsToScrape.Count == 0)
            {
                MessageBox.Show("No metadata selection was made", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // Folder name = Batocera system name
            // Variable definition
            string batoceraSystemName = Path.GetFileName(Path.GetDirectoryName(SharedData.XMLFilename));
            string scraperPlatform = comboBoxScrapers.Text;
            string userID = null;
            string userPassword = null;
            string optionsFileName = null;
            string systemID = null; // Batocera system ID
            string boxSource = null;
            string imageSource = null;
            string logoSource = null;
            string region = null;
            string language = null;
            string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename);
            string parentFolderName = Path.GetFileName(parentFolderPath);
            int scraperCount = 0;
            int scrapeErrors = 0;
            int scrapTotal = 0;
            int scrapMax = 0;
            int maxConcurrency = 1; // Max threads
            bool overwrite = checkBoxOverwriteExisting.Checked;
            bool scraperCancelled = false;
            bool hideNonGame = false;
            bool noZZZ = false; // ScreenScraper Specific
            bool scrapeByGameID = false; // ScreenScraper Specific
            string userToken = null; // EmuMovies Specific
            Dictionary<string, List<string>> emumoviesMediaLists = new Dictionary<string, List<string>>();

            // Platform specific stuff
            switch (scraperPlatform)
            {
                case "ScreenScraper":
                    {
                        (userID, userPassword) = CredentialManager.GetCredentials(scraperPlatform);
                        if (string.IsNullOrEmpty(userID) || string.IsNullOrEmpty(userPassword))
                        {
                            MessageBox.Show("The ScreenScraper credentials are not set!", "Missing Credentials", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        optionsFileName = "ini\\screenscraper_options.ini";

                        IniFileReader iniReader1 = new IniFileReader(optionsFileName);
                        Dictionary<string, string> systems1 = iniReader1.GetSection("Systems");
                        systemID = systems1[batoceraSystemName];
                        if (string.IsNullOrEmpty(systemID) || systemID == "0")
                        {
                            MessageBox.Show($"A system ID is missing for system '{batoceraSystemName}' in {optionsFileName}!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        maxConcurrency = 1;
                        elementsToScrape.Add("id");

                        boxSource = RegistryManager.ReadRegistryValue("ScreenScraper", "BoxSource");
                        boxSource = boxSource ?? string.Empty;
                        imageSource = RegistryManager.ReadRegistryValue("ScreenScraper", "ImageSource");
                        imageSource = imageSource ?? string.Empty;
                        logoSource = RegistryManager.ReadRegistryValue("ScreenScraper", "LogoSource");
                        logoSource = logoSource ?? string.Empty;
                        region = RegistryManager.ReadRegistryValue("ScreenScraper", "Region");
                        language = RegistryManager.ReadRegistryValue("ScreenScraper", "language");

                        // Extrapolate short region value
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

                        // Extrapolate short language value
                        match = Regex.Match(language, pattern);
                        if (match.Success)
                        {
                            // Extrapolate short language value
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
                            return;
                        }
                    }
                    break;

                case "EmuMovies":
                    API_EmuMovies aPI_EmuMovies = new API_EmuMovies();
                    (userID, userPassword) = CredentialManager.GetCredentials(scraperPlatform);
                    if (string.IsNullOrEmpty(userID) || string.IsNullOrEmpty(userPassword))
                    {
                        MessageBox.Show("The EmuMovies credentials are not set!", "Missing Credentials", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    optionsFileName = "ini\\emumovies_options.ini";

                    IniFileReader iniReader2 = new IniFileReader(optionsFileName);
                    Dictionary<string, string> systems2 = iniReader2.GetSection("Systems");
                    systemID = systems2[batoceraSystemName];
                    if (string.IsNullOrEmpty(systemID))
                    {
                        MessageBox.Show($"A system ID is missing for system '{batoceraSystemName}' in {optionsFileName}!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    boxSource = RegistryManager.ReadRegistryValue("EmuMovies", "BoxSource");
                    boxSource = boxSource ?? string.Empty;
                    imageSource = RegistryManager.ReadRegistryValue("EmuMovies", "ImageSource");
                    imageSource = imageSource ?? string.Empty;
                    logoSource = RegistryManager.ReadRegistryValue("EmuMovies", "LogoSource");
                    logoSource = logoSource ?? string.Empty;
                    maxConcurrency = 2;
                    userToken = await aPI_EmuMovies.AuthenticateEmuMoviesAsync(userID, userPassword);
                    if (userToken == null)
                    {
                        MessageBox.Show("Error retrieving user authentication token from EmuMovies!", "Authentication Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    List<string> emumoviesMediaTypes = new List<string>();
                    emumoviesMediaTypes = await aPI_EmuMovies.GetMediaTypes(systemID);
                    if (emumoviesMediaTypes == null)
                    {
                        MessageBox.Show("Error retrieving media types EmuMovies!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }


                    if (!File.Exists(cacheFilePath))
                    {
                        foreach (string mediaType in emumoviesMediaTypes)
                        {
                            AddToLog($"Getting {mediaType} medialist");
                            List<string> medialist = await aPI_EmuMovies.GetMediaList(systemID, mediaType);
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
            panelSmall.Controls.Add(scraperPreview);

            // Pre scraping form configuration
            gamelistManagerForm.Enabled = false; // disable main form
            if (listBoxLog.Items.Count > 0) { listBoxLog.Items.Clear(); } // clear log
            if (listBoxDownloads.Items.Count > 0) { listBoxDownloads.Items.Clear(); }
            panelScraperOptions.Enabled = false;
            buttonStart.Enabled = false;
            buttonCancel.Enabled = true;
            buttonSetup.Enabled = false;
            globalStopwatch.Reset();
            globalStopwatch.Start();
            panelCheckboxes.Visible = false;
            labelScrapeLimitCounters.Text = "N/A";
            labelCounts.Text = $"Count:0/0 | Errors:0";
            labelProgress.Text = "0%";

            // Add a scraper column if it does not exist
            if (!SharedData.DataSet.Tables[0].Columns.Contains($"scrap_{scraperPlatform}"))
            {
                SharedData.DataSet.Tables[0].Columns.Add($"scrap_{scraperPlatform}");
            }

            // What roms are being scraped?
            List<DataGridViewRow> dataGridViewRows = new List<DataGridViewRow>();
            dataGridViewRows = radioButtonScrapeAll.Checked
            ? gamelistManagerForm.DataGridView.Rows.Cast<DataGridViewRow>().ToList()
            : gamelistManagerForm.DataGridView.SelectedRows.Cast<DataGridViewRow>().ToList();

            // Setup status bar
            totalCount = dataGridViewRows.Count();  // how many items
            progressBarScrapeProgress.Value = 0;
            progressBarScrapeProgress.Minimum = 0;
            progressBarScrapeProgress.Step = 1;
            progressBarScrapeProgress.Maximum = totalCount;

            // Reset cancellation tokensource
            // Getting close!
            CancellationTokenSource = new CancellationTokenSource();

            // Setup task list
            var tasks = new List<Task>();

            // Static parameters set here, they do not change during scrape
            ScraperParameters baseScraperParameters = new ScraperParameters();
            baseScraperParameters.Overwrite = overwrite;
            baseScraperParameters.ParentFolderPath = parentFolderPath;
            baseScraperParameters.ElementsToScrape = elementsToScrape;
            baseScraperParameters.BoxSource = boxSource;
            baseScraperParameters.ImageSource = imageSource;
            baseScraperParameters.LogoSource = logoSource;
            baseScraperParameters.UserID = userID;
            baseScraperParameters.UserPassword = userPassword;
            baseScraperParameters.Region = region;
            baseScraperParameters.Language = language;
            baseScraperParameters.UserToken = userToken;
            baseScraperParameters.SystemID = systemID;

            lastUpdateTime = DateTime.Now;

            var scrapedData = new ConcurrentBag<(string romPath, ScraperData scraperData)>();

            // Start
            using (var semaphore = new SemaphoreSlim(maxConcurrency))
            {
                try
                {
                    foreach (var row in dataGridViewRows)
                    {
                        CancellationToken.ThrowIfCancellationRequested();

                        // Update count
                        Interlocked.Increment(ref scraperCount);
                        UpdateLabel(scraperCount, totalCount, scrapeErrors, 0, 0);

                        // Update progress bar
                        UpdateProgressBar();

                        // Skip hidden rows if checkbox is checked
                        object hiddenCellValue = row.Cells["Hidden"].Value;
                        if (hiddenCellValue != null && !string.IsNullOrEmpty(hiddenCellValue.ToString()))
                        {
                            if (hiddenCellValue.ToString().ToLower() == "true" && checkBoxDoNotScrapeHidden.Checked)
                            {
                                continue; // Skip this row
                            }
                        }
                        await semaphore.WaitAsync(CancellationToken);

                        var task = Task.Run(async () =>
                        {
                            try
                            {
                                CancellationToken.ThrowIfCancellationRequested();

                                DataGridViewRow currentRow = row;

                                // Variables
                                string romPath = currentRow.Cells["path"].Value.ToString();
                                string romFileNameNoExtension = Path.GetFileNameWithoutExtension(romPath);
                                string romFileNameWithExtension = romPath.Substring(2);
                                string gameID = currentRow.Cells["id"].Value.ToString();
                                string gameName = currentRow.Cells["name"].Value.ToString();

                                var scraperParameters = new ScraperParameters
                                {
                                    Overwrite = baseScraperParameters.Overwrite,
                                    ParentFolderPath = baseScraperParameters.ParentFolderPath,
                                    ElementsToScrape = new List<string>(baseScraperParameters.ElementsToScrape),
                                    BoxSource = baseScraperParameters.BoxSource,
                                    ImageSource = baseScraperParameters.ImageSource,
                                    LogoSource = baseScraperParameters.LogoSource,
                                    UserID = baseScraperParameters.UserID,
                                    UserPassword = baseScraperParameters.UserPassword,
                                    Region = baseScraperParameters.Region,
                                    Language = baseScraperParameters.Language,
                                    UserToken = baseScraperParameters.UserToken,
                                    SystemID = baseScraperParameters.SystemID,
                                    RomFileNameWithoutExtension = romFileNameNoExtension,
                                    RomFileNameWithExtension = romFileNameWithExtension,
                                    GameID = gameID,
                                    Name = gameName
                                };

                                AddToLog($"Scraping rom '{romFileNameWithExtension}'");

                                ScraperData scraperData = null;

                                // Scrape information first for chosen scraper
                                switch (scraperPlatform)
                                {
                                    case "ArcadeDB":
                                        // Scrape file name without extension
                                        // Very straightforward
                                        API_ArcadeDB aPI_ArcadeDB = new API_ArcadeDB();
                                        scraperData = await aPI_ArcadeDB.ScrapeArcadeDBAsync(scraperParameters, ListBoxControl);
                                        break;

                                    case "ScreenScraper":
                                        API_ScreenScraper aPI_ScreenScraper = new API_ScreenScraper();
                                        scraperData = await aPI_ScreenScraper.ScrapeScreenScraperAsync(scraperParameters, ListBoxControl);
                                        break;

                                    case "EmuMovies":
                                        API_EmuMovies aPI_EmuMovies = new API_EmuMovies();
                                        scraperData = await aPI_EmuMovies.ScrapeEmuMoviesAsync(scraperParameters, ListBoxControl, emumoviesMediaLists);
                                        break;
                                }

                                if (scraperData != null)
                                {
                                    scrapedData.Add((romPath, scraperData));
                                }

                                // Check the data
                                if (scraperData == null)
                                {
                                    AddToLog($"Unable to scrape '{romFileNameWithExtension}'");
                                    Interlocked.Increment(ref scrapeErrors);
                                    UpdateLabel(scraperCount, totalCount, scrapeErrors, scrapTotal, scrapMax);
                                }

                                /*
                                // Create checksum
                                string md5 = ChecksumCreator.CreateMD5($"{parentFolderPath}\\{romFileNameWithExtension}");
                                if (!string.IsNullOrEmpty(md5))
                                {
                                    scraperData.md5 = md5;
                                }
                                */

                                // Update Scraper Preview
                                string marquee = null;
                                if (scraperData.marquee != null)
                                {
                                    marquee = scraperData.marquee;
                                }
                                string image = null;
                                if (scraperData.image != null)
                                {
                                    image = scraperData.image;
                                }

                                if (!string.IsNullOrEmpty(image) && (DateTime.Now - lastUpdateTime).TotalSeconds >= 5)
                                {
                                    string imageFileName = $"{parentFolderPath}\\images\\{Path.GetFileName(image)}";
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        scraperPreview.UpdatePictureBox(imageFileName);
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

            // Scraper finished bool false = cancelled
            if (!scraperCancelled)
            {
                AddToLog("Scraping completed!");
            }
            else
            {
                AddToLog("Scraping cancelled!");
            }


            foreach (var (romPath, scraperData) in scrapedData)
            {
                DataRow tableRow = SharedData.DataSet.Tables[0].AsEnumerable()
                    .FirstOrDefault(r => r.Field<string>("path") == romPath);

                if (tableRow != null)
                {
                    var scraperElementsWithData = scraperData.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.GetValue(scraperData) != null && !string.IsNullOrEmpty(p.GetValue(scraperData).ToString()));

                    foreach (var property in scraperElementsWithData)
                    {
                        string elementName = property.Name;
                        string elementValue = property.GetValue(scraperData)?.ToString();

                        var cellValue = tableRow[elementName];
                        if (baseScraperParameters.Overwrite || cellValue == null || cellValue == DBNull.Value || string.IsNullOrEmpty(cellValue.ToString()))
                        {
                            if (elementName == "name" && elementValue.Contains("notgame"))
                            {
                                if (hideNonGame)
                                {
                                    tableRow["hidden"] = true;
                                }
                                if (noZZZ)
                                {
                                    elementValue = (elementValue.Replace("ZZZ(notgame):", "")).Trim();
                                }
                            }
                            tableRow[elementName] = elementValue;
                        }
                    }

                    string now = DateTime.Now.ToString();
                    string iso8601Format = ISO8601Converter.ConvertToISO8601(now);
                    tableRow[$"scrap_{scraperPlatform}"] = iso8601Format;
                }
            }


            SharedData.DataSet.AcceptChanges();


            if (!checkBoxSupressNotify.Checked)
            {
                if (scraperCount > 0)
                {
                    string elapsedTime = $"{globalStopwatch.Elapsed.TotalMinutes:F0} minutes and {globalStopwatch.Elapsed.Seconds} seconds";
                    MessageBox.Show($"Finished scraping {scraperCount} items in {elapsedTime}");

                    if (checkBoxSave.Checked)
                    {
                        SaveReminder();
                    }
                }
            }

            // Post scraping form configuration
            panelScraperOptions.Enabled = true;
            buttonStart.Enabled = true;
            buttonCancel.Enabled = false;
            buttonSetup.Enabled = true;
            globalStopwatch.Stop();
            panelCheckboxes.Visible = true;
            panelSmall.Controls.Remove(scraperPreview);
            scraperPreview.Dispose();
            gamelistManagerForm.Enabled = true;

            // Build new genre list because genres can change
            int currentIndex = gamelistManagerForm.ComboBoxGenre1.SelectedIndex;
            string currentGenre = gamelistManagerForm.ComboBoxGenre1.Text;
            string currentFilter = SharedData.DataSet.Tables["game"].DefaultView.RowFilter;
            gamelistManagerForm.BuildCombobox();

            if (gamelistManagerForm.ComboBoxGenre1.Enabled)
            {
                if (gamelistManagerForm.ComboBoxGenre1.Items.Contains(currentGenre) && currentIndex > 1)
                {
                    gamelistManagerForm.ComboBoxGenre1.Text = currentGenre;
                }
                if (currentIndex == 1)
                {
                    gamelistManagerForm.ComboBoxGenre1.SelectedIndex = 1;
                }
            }
            else
            {
                SharedData.DataSet.Tables["game"].DefaultView.RowFilter = currentFilter;
            }

            // Cleanup
            dataGridViewRows = null;
            tasks.Clear();
            tasks = null;
            GC.Collect();

        }

        private void SaveReminder()
        {
            gamelistManagerForm.SaveFile(SharedData.XMLFilename);
        }


        private void ButtonSelectAll_Click(object sender, EventArgs e)
        {
            foreach (Control control in panelCheckboxes.Controls)
            {
                // Check if the control is a checkbox
                if (control is System.Windows.Forms.CheckBox checkBox && checkBox.Enabled == true)
                {
                    // Set checked
                    checkBox.Checked = true;
                }
            }
        }

        private void ButtonSelectNone_Click(object sender, EventArgs e)
        {
            foreach (Control control in panelCheckboxes.Controls)
            {
                // Check if the control is a checkbox
                if (control is System.Windows.Forms.CheckBox checkBox && checkBox.Enabled == true)
                {
                    // Set unchecked
                    checkBox.Checked = false;
                }
            }
        }

        private void Scraper_Load(object sender, EventArgs e)
        {
            comboBoxScrapers.SelectedIndex = 1;

            // Reset cache
            if (File.Exists(cacheFilePath))
            {
                //File.Delete(cacheFilePath);
            }

            // Show system image if exists
            string romPath = Path.GetFileName(Path.GetDirectoryName(SharedData.XMLFilename));
            Image image = (Bitmap)Properties.Resources.ResourceManager.GetObject(romPath);
            if (image is Image)
            {
                pictureBox1.Image = image;
            }
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

        public void UpdateLabel(int current, int total, int errors, int scrapTotal, int scrapMax)
        {
            if (labelProgress == null || labelCounts == null) return;

            if (labelProgress.InvokeRequired || labelCounts.InvokeRequired || labelScrapeLimitCounters.InvokeRequired)
            {
                labelProgress.Invoke(new Action(() => UpdateLabel(current, total, errors, scrapTotal, scrapMax)));
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

                remainingTimeString += $"{remainingTime.Seconds:D2}s";

                labelProgress.Text = $"{progress:F0}% | Remaining Time: {remainingTimeString}";

                // Update the labelCounts with the current count and totalCount count
                labelCounts.Text = $"Count:{current}/{total} | Errors:{errors}";

                // Add scraper limit information if applicable
                if (scrapMax > 0)
                {
                    labelScrapeLimitCounters.Text = $"{scrapTotal}/{scrapMax}";
                }
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
            List<string> availableScraperElements = new List<string>();

            string comboboxText = comboBoxScrapers.Text;

            switch (comboboxText)
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
                    "video"
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
                    "fanart"
                 };
                    break;
            }

            foreach (Control control in panelCheckboxes.Controls)
            {
                if (control is CheckBox checkBox)
                {
                    string checkboxShortName = control.Name.Replace("checkbox", "").ToLower();
                    if (availableScraperElements.Contains(checkboxShortName))
                    {
                        checkBox.Enabled = true;
                    }
                    else
                    {
                        checkBox.Enabled = false;
                    }
                }
            }
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
            SharedData.IsDataChanged = true;
        }

        private void ToolStripMenuItemCopyLogToClipboard_Click(object sender, EventArgs e)
        {
            string listBoxItems = string.Join(Environment.NewLine, listBoxLog.Items.Cast<object>().Select(item => item.ToString()));

            // Set the string as text data on the clipboard
            Clipboard.SetText(listBoxItems);
        }

        private void ScraperForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (buttonStart.Enabled == false)
            {
                // Cancel the form closing event
                e.Cancel = true;
                MessageBox.Show("Please cancel scraping first!");
            }
        }


    }

}


