using GamelistManager.classes.api;
using GamelistManager.classes.core;
using GamelistManager.classes.helpers;
using GamelistManager.classes.io;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.IO;
using System.Windows;

namespace GamelistManager.classes.services
{
    public class ScraperService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, int> _downloadStats = new();
        private readonly object _downloadStatsLock = new();
        private static List<string> _mediaTypes = new List<string>();
        private static readonly Dictionary<(string SystemId, string MediaType), List<string>> _mediaListCache = new();


        public ScraperService()
        {
            var serviceCollection = new ServiceCollection();
            var startup = new Startup(new ConfigurationBuilder().Build());
            startup.ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        public string CreateCacheFolder(string scraper)
        {
            // Create cache folder if it does not exist
            string cacheFolder = $"{SharedData.ProgramDirectory}\\cache\\{scraper}\\{SharedData.CurrentSystem}";
            if (!Directory.Exists(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }
            return cacheFolder;
        }

        public async Task GetEmuMoviesMediaLists(ScraperProperties scraperProperties)
        {
            var apiEmuMovies = _serviceProvider.GetRequiredService<API_EmuMovies>();

            bool hasListsForSystem = _mediaTypes != null && _mediaTypes.Count > 0 &&
                _mediaTypes.All(mt => _mediaListCache.ContainsKey((scraperProperties.SystemID, mt)));

            if (hasListsForSystem)
            {
                // Populate scraperProperties from the existing cache (no API call needed)
                foreach (string mediaType in _mediaTypes!)
                {
                    var key = (scraperProperties.SystemID, mediaType);
                    if (_mediaListCache.TryGetValue(key, out var cachedList))
                    {
                        scraperProperties.EmuMoviesMediaLists[mediaType] = cachedList;
                    }
                }
                LogHelper.Instance.Log("Using cached media lists...", System.Windows.Media.Brushes.Teal);
                return;
            }

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
        }

        // Scrapes a single game
        public async Task<ScrapedGameData> ScrapeGameAsync(
            DataRow row,
            ScraperParameters baseParameters,
            ScraperProperties scraperProperties,
            string currentScraper,
            Action<int, int>? limitCallback = null,  
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
                return new ScrapedGameData { WasSuccessful = true };
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
            if (itemsToScrape.Contains("lang"))
            {
                string nameValue = !string.IsNullOrEmpty(mameArcadeName) ? mameArcadeName : romFileNameNoExtension;
                romLanguage = RegionLanguageHelper.GetLanguage(nameValue);
                itemsToScrape.Remove("lang");
            }

            // Clone parameters for this game
            var scraperParameters = baseParameters.Clone();
            scraperParameters.GameID = gameID;
            scraperParameters.RomName = romName;
            scraperParameters.RomFileName = romFileName;

            if (itemsToScrape.Count > 0)
            {
                scraperParameters.ElementsToScrape = itemsToScrape;

                switch (currentScraper)
                {
                    case "ArcadeDB":
                        {
                            var arcadedb_scraper = _serviceProvider.GetRequiredService<API_ArcadeDB>();
                            scrapedGameData = await arcadedb_scraper.ScrapeArcadeDBAsync(scraperParameters);
                            break;
                        }

                    case "EmuMovies":
                        {
                            var emumovies_scraper = _serviceProvider.GetRequiredService<API_EmuMovies>();
                            scrapedGameData = await emumovies_scraper.ScrapeEmuMoviesAsync(scraperParameters, scraperProperties.EmuMoviesMediaLists);
                            break;
                        }

                    case "ScreenScraper":
                        {
                            var screenscraper_scraper = _serviceProvider.GetRequiredService<API_ScreenScraper>();
                            var ssResult = await screenscraper_scraper.ScrapeScreenScraperAsync(scraperParameters);

                            if (ssResult.ScrapeLimitMax > 0)
                            {
                                limitCallback?.Invoke(ssResult.ScrapeLimitProgress, ssResult.ScrapeLimitMax);
                            }

                            scrapedGameData = ssResult.GameData;
                            break;
                        }
                }

                // Download media files - if required
                if (scrapedGameData != null && scrapedGameData.WasSuccessful && scrapedGameData.Media.Count > 0)
                {
                    await DownloadMediaFilesAsync(scrapedGameData, scraperParameters, cancellationToken);
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
            
            // Log results
            if (scrapedGameData.WasSuccessful)
            {
                if (scraperProperties.LogVerbosity == 2)
                {
                    LogHelper.Instance.Log($"Successfully scraped '{scraperParameters.RomName}'", System.Windows.Media.Brushes.Green);
                }

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

            return scrapedGameData;
        }



        // Downloads media files from scraped data
        public async Task DownloadMediaFilesAsync(
            ScrapedGameData scrapedData,
            ScraperParameters parameters,
            CancellationToken cancellationToken = default)
        {
            if (scrapedData.Media == null || scrapedData.Media.Count == 0)
            {
                return;
            }

            var fileTransfer = _serviceProvider.GetRequiredService<FileTransfer>();

            foreach (var mediaResult in scrapedData.Media)
            {
                string mediaType = mediaResult.MediaType;
                string url = mediaResult.Url;
                string extension = mediaResult.FileExtension;

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

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

                    // Get bearer token from parameters
                    string bearerToken = parameters.UserAccessToken ?? string.Empty;

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
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogHelper.Instance.Log(
                        $"Error downloading {mediaType}: {ex.Message}",
                        System.Windows.Media.Brushes.Red);
                }
            }
        }

        // Saves scraped data to a DataRow
        public async Task SaveScrapedDataAsync(
    DataRow row,
    ScrapedGameData scrapedData,
    ScraperParameters parameters)
        {
            if (parameters.ElementsToScrape == null || scrapedData?.Data == null)
                return;

            var rowUpdates = new Dictionary<string, string>();

            foreach (string element in parameters.ElementsToScrape)
            {
                if (!scrapedData.Data.TryGetValue(element, out var value))
                    continue;

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                value = value.Trim();

                if (!parameters.MetaLookup.TryGetValue(element, out var meta))
                    continue;

                rowUpdates[meta.Column] = value;
            }

            if (rowUpdates.Count == 0)
                return;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var kvp in rowUpdates)
                {
                    if (!row.Table.Columns.Contains(kvp.Key))
                        continue;

                    var existing = row[kvp.Key]?.ToString();

                    // Prevent overwriting with the same value
                    if (string.Equals(existing, kvp.Value, StringComparison.Ordinal))
                        continue;

                    row[kvp.Key] = kvp.Value;
                }
            });
        }


        // Gets download statistics summary
        public async Task LogDownloadSummaryAsync()
        {
            if (_downloadStats.Count == 0)
            {
                LogHelper.Instance.Log("No media files were downloaded", System.Windows.Media.Brushes.Orange);
                return;
            }

            int totalDownloads = _downloadStats.Values.Sum();
            LogHelper.Instance.Log($"Download Summary: {totalDownloads} file(s) downloaded", System.Windows.Media.Brushes.Teal);

            var sortedStats = _downloadStats
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key);

            foreach (var stat in sortedStats)
            {
                LogHelper.Instance.Log($"  {stat.Key}: {stat.Value}", System.Windows.Media.Brushes.Teal);
            }
        }

        // Clears download statistics
        public async Task ClearDownloadStats()
        {
             _downloadStats.Clear();
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

                    if (!File.Exists(actualFile) || baseParameters.OverwriteMedia)
                    {
                        itemsToScrape.Add(item);
                        continue;
                    }
                }

                // Overwrite rules
                if (item == "name" && baseParameters.OverwriteName)
                {
                    itemsToScrape.Add(item);
                    continue;
                }

                if (type == "String" && baseParameters.OverwriteMetadata)
                {
                    itemsToScrape.Add(item);
                }
            }

            return itemsToScrape;
        }

        // Fetches items in batch mode (ArcadeDB only) and saves to cache
        public async Task<int> GetItemsInBatchModeAsync(
            ScraperParameters parameters,
            int batchMaximum,
            DataRow[] rows,
            string cacheFolder,
            Action<int, int, int, int>? progressCallback,
            CancellationToken cancellationToken)
        {
            try
            {
                LogHelper.Instance.Log("Starting batch API fetch...", System.Windows.Media.Brushes.Teal);

                // Find ROMs that don't have cache files yet
                var itemsToFetch = new List<string>();

                // Initialize the set, case-insensitive
                HashSet<string> cacheFilesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (!string.IsNullOrEmpty(cacheFolder) && Directory.Exists(cacheFolder))
                {
                    foreach (var file in Directory.EnumerateFiles(cacheFolder, "*.json"))
                    {
                        cacheFilesSet.Add(Path.GetFileName(file));
                    }
                }

                for (int i = 0; i < rows.Length; i++)
                {
                    var row = rows[i];
                    string? romPath = row["Rom Path"].ToString();

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

                int alreadyCached = rows.Length - itemsToFetch.Count;
                if (alreadyCached > 0)
                {
                    LogHelper.Instance.Log($"{alreadyCached} items already in cache", System.Windows.Media.Brushes.Teal);
                }

                if (itemsToFetch.Count > 0)
                {
                    LogHelper.Instance.Log(
                        $"Fetching {itemsToFetch.Count} games from API in batches...",
                        System.Windows.Media.Brushes.Teal);

                    var arcadedb_scraper = _serviceProvider.GetRequiredService<API_ArcadeDB>();
                    int totalBatches = (int)Math.Ceiling((double)itemsToFetch.Count / batchMaximum);
                    int currentBatch = 0;
                    int totalFetched = 0;

                    for (int i = 0; i < itemsToFetch.Count; i += batchMaximum)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        currentBatch++;
                        int batchSize = Math.Min(batchMaximum, itemsToFetch.Count - i);
                        var batch = itemsToFetch.GetRange(i, batchSize);

                        // Report progress
                        progressCallback?.Invoke(currentBatch, totalBatches, totalFetched, itemsToFetch.Count);

                        try
                        {
                            // This downloads from API and saves raw JSON to cache files
                            var batchResults = await arcadedb_scraper.ScrapeArcadeDBBatchAsync(
                                batch,
                                parameters
                            );

                            int foundCount = batchResults.Count;
                            int notFoundCount = batch.Count - foundCount;
                            totalFetched += foundCount;

                            // Report progress after fetch
                            progressCallback?.Invoke(currentBatch, totalBatches, totalFetched, itemsToFetch.Count);

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

                    return totalFetched;
                }
                else
                {
                    LogHelper.Instance.Log(
                        "All games already cached!",
                        System.Windows.Media.Brushes.Green);
                    return 0;
                }
            }
            catch (OperationCanceledException)
            {
                LogHelper.Instance.Log(
                    "Batch processing cancelled by user",
                    System.Windows.Media.Brushes.Orange);
                throw;
            }
        }

        public async Task<bool> AuthenticateEmuMoviesAsync(ScraperProperties scraperProperties)
        {
            var creds = CredentialHelper.GetCredentials("EmuMovies");
            if (string.IsNullOrEmpty(creds.UserName))
            {
                LogHelper.Instance.Log("EmuMovies credentials have not been configured yet.", System.Windows.Media.Brushes.Red);
                return false;
            }

            scraperProperties.UserName = creds.UserName;
            scraperProperties.Password = creds.Password;

            var apiEmuMovies = _serviceProvider.GetRequiredService<API_EmuMovies>();

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

        public async Task<bool> AuthenticateScreenScraperAsync(ScraperProperties scraperProperties)
        {
            var creds = CredentialHelper.GetCredentials("ScreenScraper");
            if (string.IsNullOrEmpty(creds.UserName))
            {
                LogHelper.Instance.Log("ScreenScraper credentials have not been configured yet.", System.Windows.Media.Brushes.Red);
                return false;
            }

            scraperProperties.UserName = creds.UserName;
            scraperProperties.Password = creds.Password;

            var apiScreenScraper = _serviceProvider.GetRequiredService<API_ScreenScraper>();

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

    }
}