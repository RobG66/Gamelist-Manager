using Gamelist_Manager.Classes.Api;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Classes.IO;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.Services
{
    internal class ScraperService
    {
        private const int BatchProcessingMinimum = 10;
        private readonly API_ArcadeDB _arcadeDb;
        private readonly API_EmuMovies _emuMovies;
        private readonly API_ScreenScraper _screenScraper;
        private readonly FileTransfer _fileTransfer;
        private readonly SharedDataService _sharedData;
        private readonly Dictionary<string, int> _downloadStats = new();
        private readonly object _downloadStatsLock = new();
        private readonly EmuMoviesMediaCacheHelper _mediaCache = new();

        public Action<string, LogLevel, string?, LogLevel>? LogAction { get; set; }

        public ScraperService(
            API_ArcadeDB arcadeDb,
            API_EmuMovies emuMovies,
            API_ScreenScraper screenScraper,
            FileTransfer fileTransfer,
            SharedDataService sharedData)
        {
            _arcadeDb = arcadeDb;
            _emuMovies = emuMovies;
            _screenScraper = screenScraper;
            _fileTransfer = fileTransfer;
            _sharedData = sharedData;
        }

        private void Log(string message, LogLevel level = LogLevel.Default, string? prefix = null, LogLevel prefixLevel = LogLevel.Default)
            => LogAction?.Invoke(message, level, prefix, prefixLevel);

        public string CreateCacheFolder(string scraper, string system)
        {
            string cacheFolder = Path.Combine(AppContext.BaseDirectory, "cache", scraper, system);
            if (!Directory.Exists(cacheFolder))
                Directory.CreateDirectory(cacheFolder);
            return cacheFolder;
        }

        public async Task GetEmuMoviesMediaListsAsync(string systemId, ScraperProperties scraperProperties, CancellationToken cancellationToken = default)
        {
            try
            {
                await _mediaCache.PopulateMediaListsAsync(_emuMovies, systemId, scraperProperties, msg => Log(msg), cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                Log(ex.Message);
            }
        }

        public async Task<(bool Success, ScrapedGameData Data)> ScrapeGameAsync(
            GameMetadataRow row,
            ScraperParameters baseParameters,
            ScraperProperties scraperProperties,
            string currentScraper,
            Action<int, int>? limitCallback = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string romPath = row.GetValue(MetaDataKeys.path)?.ToString() ?? string.Empty;
            string romFileName = Path.GetFileName(romPath);
            string romFileNameNoExtension = Path.GetFileNameWithoutExtension(romPath);
            string romName = row.GetValue(MetaDataKeys.name)?.ToString() ?? romFileNameNoExtension;
            string gameID = row.GetValue(MetaDataKeys.id)?.ToString() ?? string.Empty;

            var itemsToScrape = ScrapeFilterHelper.FilterElementsToScrape(row, baseParameters);

            if (itemsToScrape.Count == 0)
            {
                if (scraperProperties.LogVerbosity == 2)
                    Log($"Skipping '{romFileName}', nothing to scrape");
                return (true, new ScrapedGameData());
            }

            ScrapedGameData scrapedGameData = new();
            bool success = false;
            var messages = new List<string>();

            string? mameArcadeName = MameNamesHelper.Names.TryGetValue(romFileNameNoExtension, out string? arcadeName) ? arcadeName : null;

            string romRegion = string.Empty;
            string romLanguage = string.Empty;

            if (itemsToScrape.Contains(nameof(MetaDataKeys.region)))
            {
                string regionName = !string.IsNullOrEmpty(mameArcadeName) ? mameArcadeName : romFileNameNoExtension;
                romRegion = RegionLanguageHelper.GetRegion(regionName);
                itemsToScrape.Remove(nameof(MetaDataKeys.region));
            }

            if (itemsToScrape.Contains(nameof(MetaDataKeys.lang)))
            {
                string languageName = !string.IsNullOrEmpty(mameArcadeName) ? mameArcadeName : romFileNameNoExtension;
                romLanguage = RegionLanguageHelper.GetLanguage(languageName);
                itemsToScrape.Remove(nameof(MetaDataKeys.lang));
            }

            var scraperParameters = baseParameters.Clone();
            scraperParameters.GameID = gameID;
            scraperParameters.RomName = romName;
            scraperParameters.RomFileName = romFileName;
            scraperParameters.MameArcadeName = mameArcadeName;

            if (itemsToScrape.Count > 0)
            {
                scraperParameters.ElementsToScrape = itemsToScrape;

                if (currentScraper == ScraperRegistry.ArcadeDB.Name)
                {
                    (success, scrapedGameData, messages) = await _arcadeDb.ScrapeArcadeDBAsync(scraperParameters);
                }
                else if (currentScraper == ScraperRegistry.EmuMovies.Name)
                {
                    (success, scrapedGameData, messages) = await _emuMovies.ScrapeEmuMoviesAsync(scraperParameters, scraperProperties.EmuMoviesMediaLists);
                }
                else if (currentScraper == ScraperRegistry.ScreenScraper.Name)
                {
                    var (ssSuccess, ssData, ssMessages, ssLimitProgress, ssLimitMax) = await _screenScraper.ScrapeScreenScraperAsync(scraperParameters);
                    success = ssSuccess;
                    scrapedGameData = ssData;
                    messages = ssMessages;
                    if (ssLimitMax > 0)
                        limitCallback?.Invoke(ssLimitProgress, ssLimitMax);
                }

                if (success)
                {
                    if (scraperProperties.LogVerbosity >= 1)
                        Log($"'{scraperParameters.RomName}'", LogLevel.Default, LogPrefix.Scrape, LogLevel.Success);
                    if (scrapedGameData.Media.Count > 0)
                    {
                        await MediaDownloadHelper.DownloadMediaFilesAsync(scrapedGameData, scraperParameters, _fileTransfer, RecordDownload, LogAction, cancellationToken);
                    }
                }
            }

            if (!string.IsNullOrEmpty(romRegion))
                scrapedGameData.Data[nameof(MetaDataKeys.region)] = romRegion;

            if (!string.IsNullOrEmpty(romLanguage))
                scrapedGameData.Data[nameof(MetaDataKeys.lang)] = romLanguage;

            if (!success)
            {
                if (scraperProperties.LogVerbosity >= 1)
                    Log($"'{scraperParameters.RomName}'", LogLevel.Default, LogPrefix.Scrape, LogLevel.Error);

                if (scraperProperties.LogVerbosity == 2)
                    foreach (var msg in messages)
                        Log(msg);
            }

            return (success, scrapedGameData);
        }

        public async Task SaveScrapedDataAsync(
            GameMetadataRow row,
            ScrapedGameData scrapedData,
            ScraperParameters parameters)
        {
            if (parameters.ElementsToScrape == null || scrapedData?.Data == null)
                return;

            var updates = new Dictionary<MetaDataKeys, string>();

            foreach (string element in parameters.ElementsToScrape)
            {
                if (!scrapedData.Data.TryGetValue(element, out var value) || string.IsNullOrWhiteSpace(value))
                    continue;

                value = value.Trim();

                if (!Enum.TryParse<MetaDataKeys>(element, true, out var key))
                    continue;

                updates[key] = value;
            }

            if (updates.Count == 0)
                return;

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var kvp in updates)
                {
                    var existing = row.GetValue(kvp.Key)?.ToString();
                    if (!string.Equals(existing, kvp.Value, StringComparison.Ordinal))
                        row.SetValue(kvp.Key, kvp.Value);
                }
                row.NotifyDataChanged();
            });
        }

        private void RecordDownload(string mediaType)
        {
            lock (_downloadStatsLock)
            {
                _downloadStats[mediaType] = _downloadStats.GetValueOrDefault(mediaType) + 1;
            }
        }

        public void LogDownloadSummary()
        {
            lock (_downloadStatsLock)
            {
                if (_downloadStats.Count == 0)
                {
                    Log("No media files were downloaded");
                    return;
                }

                Log($"Download summary: {_downloadStats.Values.Sum()} file(s) downloaded");

                foreach (var stat in _downloadStats.OrderByDescending(k => k.Value).ThenBy(k => k.Key))
                    Log($"  {stat.Key}: {stat.Value}");
            }
        }

        public void ClearDownloadStats()
        {
            lock (_downloadStatsLock)
            {
                _downloadStats.Clear();
            }
        }

        public async Task<int> GetItemsInBatchModeAsync(
            ScraperParameters parameters,
            int batchMaximum,
            IReadOnlyList<GameMetadataRow> rows,
            string cacheFolder,
            CancellationToken cancellationToken)
        {
            try
            {
                Log("Starting batch API fetch...");

                var cacheFilesSet = new HashSet<string>(FilePathHelper.PathComparer);
                if (!string.IsNullOrEmpty(cacheFolder) && Directory.Exists(cacheFolder))
                {
                    foreach (var file in Directory.EnumerateFiles(cacheFolder, "*.json"))
                        cacheFilesSet.Add(Path.GetFileName(file));
                }

                var itemsToFetch = new List<string>();
                foreach (var row in rows)
                {
                    string romPath = row.GetValue(MetaDataKeys.path)?.ToString() ?? string.Empty;
                    if (string.IsNullOrEmpty(romPath)) continue;

                    string name = Path.GetFileNameWithoutExtension(romPath);
                    if (!cacheFilesSet.Contains(name + ".json"))
                        itemsToFetch.Add(name);
                }

                int alreadyCached = rows.Count - itemsToFetch.Count;
                if (alreadyCached > 0)
                    Log($"{alreadyCached} items already in cache");

                if (itemsToFetch.Count == 0)
                {
                    Log("All games already cached!");
                    return 0;
                }

                Log($"Fetching {itemsToFetch.Count} games from API in batches...");

                int totalBatches = (int)Math.Ceiling((double)itemsToFetch.Count / batchMaximum);
                int currentBatch = 0;
                int totalFetched = 0;

                for (int i = 0; i < itemsToFetch.Count; i += batchMaximum)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    currentBatch++;
                    int batchSize = Math.Min(batchMaximum, itemsToFetch.Count - i);
                    var batch = itemsToFetch.GetRange(i, batchSize);

                    try
                    {
                        var batchResults = await _arcadeDb.ScrapeArcadeDBBatchAsync(batch, parameters, cancellationToken);
                        int foundCount = batchResults.Count;
                        int notFoundCount = batch.Count - foundCount;
                        totalFetched += foundCount;

                        string resultMsg = notFoundCount > 0
                            ? $"Batch {currentBatch}/{totalBatches}: Fetched {foundCount}, not found {notFoundCount}"
                            : $"Batch {currentBatch}/{totalBatches}: Fetched {foundCount}";
                        Log(resultMsg);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        Log($"Batch {currentBatch}/{totalBatches} error: {ex.Message}");
                    }
                }

                Log($"Batch fetch complete: {totalFetched} games downloaded to cache");
                return totalFetched;
            }
            catch (OperationCanceledException)
            {
                Log("Batch processing cancelled");
                throw;
            }
        }

        public async Task<bool> InitializeScraperAsync(
            ScraperParameters baseParameters,
            ScraperProperties scraperProperties,
            string currentSystem,
            CancellationToken cancellationToken = default)
        {
            string scraperName = scraperProperties.ScraperName;

            if (scraperName == ScraperRegistry.ArcadeDB.Name)
            {
                if (!ArcadeSystemID.IsInitialized)
                {
                    Log("Arcade systems configuration is missing.", LogLevel.Error);
                    return false;
                }
                if (!ArcadeSystemID.HasArcadeSystemName(currentSystem))
                {
                    Log($"'{currentSystem}' is not an arcade system.", LogLevel.Error);
                    return false;
                }
            }

            baseParameters.SystemID ??= _sharedData.GetScraperSystemId(scraperName, currentSystem);
            baseParameters.SSLanguage ??= _sharedData.GetScraperLanguageCode(scraperName);
            if (baseParameters.SSRegions == null)
            {
                string? primaryRegion = _sharedData.GetScraperPrimaryRegionCode(scraperName);
                var regions = _sharedData.GetScraperRegionCodes(scraperName).ToList();
                if (!string.IsNullOrEmpty(primaryRegion))
                {
                    regions.Remove(primaryRegion);
                    regions.Insert(0, primaryRegion);
                }
                baseParameters.SSRegions = regions;
            }

            string resolveIfEmpty(string? current, string sectionName)
                => string.IsNullOrEmpty(current) ? ResolveSourceValue(scraperName, sectionName) : current;

            baseParameters.ImageSource = resolveIfEmpty(baseParameters.ImageSource, nameof(baseParameters.ImageSource));
            baseParameters.MarqueeSource = resolveIfEmpty(baseParameters.MarqueeSource, nameof(baseParameters.MarqueeSource));
            baseParameters.ThumbnailSource = resolveIfEmpty(baseParameters.ThumbnailSource, nameof(baseParameters.ThumbnailSource));
            baseParameters.CartridgeSource = resolveIfEmpty(baseParameters.CartridgeSource, nameof(baseParameters.CartridgeSource));
            baseParameters.VideoSource = resolveIfEmpty(baseParameters.VideoSource, nameof(baseParameters.VideoSource));
            baseParameters.BoxArtSource = resolveIfEmpty(baseParameters.BoxArtSource, nameof(baseParameters.BoxArtSource));
            baseParameters.WheelSource = resolveIfEmpty(baseParameters.WheelSource, nameof(baseParameters.WheelSource));

            baseParameters.CacheFolder = CreateCacheFolder(scraperName, currentSystem);

            if (scraperName == ScraperRegistry.ScreenScraper.Name)
            {
                var (success, maxThreads) = await AuthenticateScreenScraperAsync();
                if (!success) return false;

                scraperProperties.MaxConcurrency = maxThreads;

                var creds = CredentialHelper.GetCredentials(ScraperRegistry.ScreenScraper.Name);
                baseParameters.UserID = creds.UserName;
                baseParameters.UserPassword = creds.Password;

                LogScreenScraperConfiguration(scraperName);
            }
            else if (scraperName == ScraperRegistry.EmuMovies.Name)
            {
                var (success, accessToken) = await AuthenticateEmuMoviesAsync();
                if (!success) return false;

                scraperProperties.MaxConcurrency = ScraperRegistry.EmuMovies.DefaultThreads;
                baseParameters.UserAccessToken = accessToken;

                await GetEmuMoviesMediaListsAsync(baseParameters.SystemID ?? string.Empty, scraperProperties, cancellationToken);
            }

            return true;
        }

        private string ResolveSourceValue(string scraperName, string sectionName)
        {
            string savedDisplayName = _sharedData.GetScraperSourceSetting(scraperName, sectionName);

            if (!string.IsNullOrEmpty(savedDisplayName))
            {
                var sources = _sharedData.GetScraperSources(scraperName, sectionName);
                if (sources.TryGetValue(savedDisplayName, out var apiValue) && !string.IsNullOrEmpty(apiValue))
                    return apiValue;
            }

            return _sharedData.GetScraperDefaultSource(scraperName, sectionName);
        }

        public async Task<bool> RunScrapeAsync(
            ScraperParameters baseParameters,
            ScraperProperties scraperProperties,
            IReadOnlyList<GameMetadataRow> rows,
            int maxBatch,
            Action<int, int, string> onProgress,
            Action<int, int>? onLimitUpdate,
            Action? onDataChanged,
            CancellationToken cancellationToken)
        {
            if (ScraperRegistry.Find(scraperProperties.ScraperName)?.SupportsBatchProcessing == true
                && scraperProperties.BatchProcessing && rows.Count >= BatchProcessingMinimum)
            {
                baseParameters.ScrapeByCache = true;
                await GetItemsInBatchModeAsync(
                    baseParameters, maxBatch, rows,
                    baseParameters.CacheFolder ?? string.Empty, cancellationToken);
            }

            BuildExistingMediaCache(baseParameters, cancellationToken);

            int maxConcurrency = scraperProperties.MaxConcurrency;
            if (maxConcurrency == 0)
            {
                Log("Max concurrency is 0, aborting.", LogLevel.Error);
                return false;
            }

            using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = new List<Task>();
            int doneCount = 0;
            int totalCount = rows.Count;
            bool completed = false;

            try
            {
                foreach (var row in rows)
                {
                    await semaphore.WaitAsync(cancellationToken);

                    int current = Interlocked.Increment(ref doneCount);
                    string romPath = row.GetValue(MetaDataKeys.path)?.ToString() ?? string.Empty;
                    onProgress(current, totalCount, Path.GetFileName(romPath));

                    tasks.Add(Task.Run(() => ProcessRowAsync(
                        row, baseParameters, scraperProperties,
                        onLimitUpdate, onDataChanged, semaphore, cancellationToken), cancellationToken));
                }

                completed = true;
            }
            catch (OperationCanceledException)
            {
                Log("Scraping cancelled.", LogLevel.Warning);
            }
            finally
            {
                try { await Task.WhenAll(tasks); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Log($"Unexpected error during task completion: {ex.Message}", LogLevel.Error); }
            }

            return completed;
        }

        private async Task ProcessRowAsync(
            GameMetadataRow row,
            ScraperParameters baseParameters,
            ScraperProperties scraperProperties,
            Action<int, int>? onLimitUpdate,
            Action? onDataChanged,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (success, data) = await ScrapeGameAsync(
                    row, baseParameters, scraperProperties, scraperProperties.ScraperName,
                    onLimitUpdate, cancellationToken);

                if (success || data.Data.ContainsKey(nameof(MetaDataKeys.region)) || data.Data.ContainsKey(nameof(MetaDataKeys.lang)))
                {
                    await SaveScrapedDataAsync(row, data, baseParameters);
                    onDataChanged?.Invoke();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Log($"Error: {ex.Message}", LogLevel.Error); }
            finally { semaphore.Release(); }
        }

        private void BuildExistingMediaCache(ScraperParameters parameters, CancellationToken cancellationToken)
        {
            if (parameters.MediaPaths == null || string.IsNullOrEmpty(parameters.ParentFolderPath))
                return;

            try
            {
                var folderCache = new Dictionary<string, HashSet<string>>(FilePathHelper.PathComparer);
                foreach (var (_, folder) in parameters.MediaPaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (string.IsNullOrEmpty(folder)) continue;
                    string absoluteFolder = FilePathHelper.GamelistPathToFullPath(folder, parameters.ParentFolderPath);
                    if (folderCache.ContainsKey(absoluteFolder)) continue;
                    var fileSet = new HashSet<string>(FilePathHelper.PathComparer);
                    if (Directory.Exists(absoluteFolder))
                    {
                        foreach (var file in Directory.EnumerateFiles(absoluteFolder))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            fileSet.Add(Path.GetFileName(file));
                        }
                    }
                    folderCache[absoluteFolder] = fileSet;
                }

                var mediaCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var (mediaType, folder) in parameters.MediaPaths)
                {
                    if (string.IsNullOrEmpty(folder)) continue;
                    string absoluteFolder = FilePathHelper.GamelistPathToFullPath(folder, parameters.ParentFolderPath);
                    if (folderCache.TryGetValue(absoluteFolder, out var fileSet))
                        mediaCache[mediaType] = fileSet;
                }
                parameters.ExistingMediaFiles = mediaCache;
            }
            catch (OperationCanceledException)
            {
                parameters.ExistingMediaFiles = null;
                throw;
            }
        }

        private async Task<(bool Success, string? AccessToken)> AuthenticateEmuMoviesAsync()
        {
            var creds = CredentialHelper.GetCredentials(ScraperRegistry.EmuMovies.Name);
            if (string.IsNullOrEmpty(creds.UserName))
            {
                Log($"{ScraperRegistry.EmuMovies.Name} credentials have not been configured yet.");
                return (false, null);
            }

            Log("Verifying EmuMovies credentials...");
            var (success, accessToken, error) = await _emuMovies.AuthenticateAsync(creds.UserName, creds.Password);

            if (!success)
            {
                Log(error);
                return (false, null);
            }

            return (true, accessToken);
        }

        private async Task<(bool Success, int MaxThreads)> AuthenticateScreenScraperAsync()
        {
            var creds = CredentialHelper.GetCredentials(ScraperRegistry.ScreenScraper.Name);
            if (string.IsNullOrEmpty(creds.UserName))
            {
                Log($"{ScraperRegistry.ScreenScraper.Name} credentials have not been configured yet.");
                return (false, 0);
            }

            Log("Verifying ScreenScraper credentials...");
            var (success, maxThreads, error) = await _screenScraper.AuthenticateAsync(creds.UserName, creds.Password);

            if (!success)
            {
                Log(error);
                return (false, 0);
            }

            return (true, maxThreads);
        }

        private void LogScreenScraperConfiguration(string scraperName)
        {
            var language = _sharedData.GetScraperSourceSetting(scraperName, "Language");
            var primaryRegion = _sharedData.GetScraperSourceSetting(scraperName, "PrimaryRegion");
            var fallbackJson = _sharedData.GetScraperSourceSetting(scraperName, "RegionFallback");

            if (!string.IsNullOrEmpty(language))
                Log($"Language: {language}");

            if (!string.IsNullOrEmpty(primaryRegion))
                Log($"Primary region: {primaryRegion}");

            if (!string.IsNullOrEmpty(fallbackJson))
            {
                try
                {
                    var fallback = JsonSerializer.Deserialize<List<string>>(fallbackJson);
                    if (fallback?.Count > 0)
                        Log($"Region fallback: {string.Join(", ", fallback)}");
                }
                catch { /* Malformed fallback JSON, skip */ }
            }
        }
    }
}