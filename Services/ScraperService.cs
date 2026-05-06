using Gamelist_Manager.Classes.Api;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.Services
{
    // Bundles the loose Action parameters passed through RunScrapeAsync / ProcessRowAsync.
    internal sealed record ScrapingCallbacks(
        Action<int, int, string> OnProgress,
        Action<int, int>? OnLimitUpdate,
        Action? OnDataChanged,
        Action? OnQuotaExceeded);

    internal partial class ScraperService
    {
        #region Constants

        private const int BatchProcessingMinimum = 10;
        private const int TimeoutRetryCount = 5;
        private const double TimeoutRetryBaseDelaySeconds = 3.0;
        private const double TimeoutRetryMaxDelaySeconds = 12.0;

        #endregion

        #region Private Fields

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Dictionary<string, int> _downloadStats = new();
        private readonly object _downloadStatsLock = new();

        #endregion

        #region Public Properties

        public Action<string, LogLevel, string?, LogLevel>? LogAction { get; set; }

        #endregion

        #region Constructor

        public ScraperService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        #endregion

        #region Private Methods

        private void Log(string message, LogLevel level = LogLevel.Default, string? prefix = null, LogLevel prefixLevel = LogLevel.Default)
            => LogAction?.Invoke(message, level, prefix, prefixLevel);

        private API_ArcadeDB CreateArcadeDb() => new(_httpClientFactory.CreateClient(HttpClientNames.Scraper));
        private API_EmuMovies CreateEmuMovies() => new(_httpClientFactory.CreateClient(HttpClientNames.Scraper));
        private API_ScreenScraper CreateScreenScraper() => new(_httpClientFactory.CreateClient(HttpClientNames.ScreenScraper));
        private FileTransferHelper CreateFileTransfer() => new(_httpClientFactory.CreateClient(HttpClientNames.Scraper));
        private FileTransferHelper CreateScreenScraperFileTransfer() => new(_httpClientFactory.CreateClient(HttpClientNames.ScreenScraper));
        private EmuMoviesMediaCacheHelper CreateMediaCache() => new(_httpClientFactory.CreateClient(HttpClientNames.Scraper), Secrets.EmuMoviesBearerToken);

        #endregion

        #region Public Methods

        public async Task<(bool Success, ScrapedGameData Data)> ScrapeGameAsync(
            GameMetadataRow row,
            ScraperParameters baseParameters,
            ScraperProperties scraperProperties,
            string currentScraper,
            Action<int, int>? limitCallback = null,
            Action? onQuotaExceeded = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string romPath = row.GetValue(MetaDataKeys.path)?.ToString() ?? string.Empty;
            string romFileName = Path.GetFileName(romPath);
            string romFileNameNoExt = Path.GetFileNameWithoutExtension(romPath);
            string romName = row.GetValue(MetaDataKeys.name)?.ToString() ?? romFileNameNoExt;
            string gameID = row.GetValue(MetaDataKeys.id)?.ToString() ?? string.Empty;

            var itemsToScrape = ScrapeFilterHelper.FilterElementsToScrape(row, baseParameters);
            if (itemsToScrape.Count == 0)
            {
                if (scraperProperties.LogVerbosity == 2)
                    Log($"Skipping '{romFileName}', nothing to scrape");
                return (true, new ScrapedGameData());
            }

            string? mameArcadeName = MameNamesHelper.Names.TryGetValue(romFileNameNoExt, out string? arcadeName) ? arcadeName : null;

            // Resolve region / language locally where possible, leaving region in the
            // list only when ScreenScraper must resolve it from <noms>.
            var (romRegion, romLanguage) = ResolveRegionAndLanguage(
                itemsToScrape, mameArcadeName, romFileNameNoExt, currentScraper);

            var scraperParameters = baseParameters.Clone();
            scraperParameters.GameID = gameID;
            scraperParameters.RomName = romName;
            scraperParameters.RomFileName = romFileName;
            scraperParameters.MameArcadeName = mameArcadeName;

            ScrapedGameData scrapedGameData = new();
            bool success = false;
            var messages = new List<string>();

            if (itemsToScrape.Count > 0)
            {
                scraperParameters.ElementsToScrape = itemsToScrape;

                (success, scrapedGameData, messages) = await DispatchToScraperApiAsync(
                    currentScraper, scraperParameters, scraperProperties,
                    limitCallback, onQuotaExceeded, cancellationToken);

                if (success)
                {
                    if (scraperProperties.LogVerbosity >= 1)
                        Log($"'{scraperParameters.RomName}'", LogLevel.Default, LogPrefix.Scrape, LogLevel.Success);

                    if (scrapedGameData.Media.Count > 0)
                    {
                        var fileTransfer = currentScraper == ScraperRegistry.ScreenScraper.Name
                            ? CreateScreenScraperFileTransfer()
                            : CreateFileTransfer();
                        await MediaDownloadHelper.DownloadMediaFilesAsync(scrapedGameData, scraperParameters, fileTransfer, RecordDownload, LogAction, cancellationToken);
                    }
                }
            }

            // Merge locally resolved metadata regardless of API success.
            if (!string.IsNullOrEmpty(romRegion))
                scrapedGameData.Data[nameof(MetaDataKeys.region)] = romRegion;
            if (!string.IsNullOrEmpty(romLanguage))
                scrapedGameData.Data[nameof(MetaDataKeys.lang)] = romLanguage;

            if (!success && !scraperParameters.SkipNonCached)
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
                if (!Enum.TryParse<MetaDataKeys>(element, true, out var key))
                    continue;
                updates[key] = value.Trim();
            }

            if (updates.Count == 0)
                return;

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                foreach (var (key, value) in updates)
                {
                    var existing = row.GetValue(key)?.ToString();
                    if (!string.Equals(existing, value, StringComparison.Ordinal))
                        row.SetValue(key, value);
                }
                row.NotifyDataChanged();
            }, Avalonia.Threading.DispatcherPriority.Background);
        }

        public void LogDownloadSummary()
        {
            lock (_downloadStatsLock)
            {
                if (_downloadStats.Count == 0)
                {
                    Log("No media files were downloaded", LogLevel.Default, LogPrefix.Download);
                    return;
                }

                Log($"Summary: {_downloadStats.Values.Sum()} file(s) downloaded", LogLevel.Default, LogPrefix.Download);
                foreach (var stat in _downloadStats.OrderByDescending(k => k.Value).ThenBy(k => k.Key))
                    Log($"  {stat.Key}: {stat.Value}", LogLevel.Default, LogPrefix.Download);
            }
        }

        public void ClearDownloadStats()
        {
            lock (_downloadStatsLock) { _downloadStats.Clear(); }
        }

        #endregion

        #region Private Methods

        private void RecordDownload(string mediaType)
        {
            lock (_downloadStatsLock)
                _downloadStats[mediaType] = _downloadStats.GetValueOrDefault(mediaType) + 1;
        }

        // Strips region and language from itemsToScrape where they can be resolved locally.
        // Region is left in the list only when ScreenScraper must resolve it from <noms>.
        private static (string Region, string Language) ResolveRegionAndLanguage(
            List<string> itemsToScrape,
            string? mameArcadeName,
            string romFileNameNoExt,
            string currentScraper)
        {
            string romRegion = string.Empty;
            string romLanguage = string.Empty;

            if (itemsToScrape.Contains(nameof(MetaDataKeys.region)))
            {
                string regionName = mameArcadeName ?? romFileNameNoExt;
                romRegion = RegionLanguageHelper.GetRegion(regionName) ?? string.Empty;

                // Leave region in the list for ScreenScraper when it could not be resolved
                // locally — ScreenScraper can derive it from <noms>.
                if (!string.IsNullOrEmpty(romRegion) || currentScraper != ScraperRegistry.ScreenScraper.Name)
                    itemsToScrape.Remove(nameof(MetaDataKeys.region));
            }

            if (itemsToScrape.Contains(nameof(MetaDataKeys.lang)))
            {
                string languageName = mameArcadeName ?? romFileNameNoExt;
                romLanguage = RegionLanguageHelper.GetLanguage(languageName);
                itemsToScrape.Remove(nameof(MetaDataKeys.lang));
            }

            return (romRegion, romLanguage);
        }

        // Dispatches to the correct scraper API and normalises the result tuple.
        private async Task<(bool Success, ScrapedGameData Data, List<string> Messages)> DispatchToScraperApiAsync(
            string currentScraper,
            ScraperParameters scraperParameters,
            ScraperProperties scraperProperties,
            Action<int, int>? limitCallback,
            Action? onQuotaExceeded,
            CancellationToken cancellationToken)
        {
            if (currentScraper == ScraperRegistry.ArcadeDB.Name)
            {
                var (success, data, messages) = await CreateArcadeDb().ScrapeArcadeDBAsync(scraperParameters, cancellationToken);
                return (success, data, messages);
            }

            if (currentScraper == ScraperRegistry.EmuMovies.Name)
            {
                var (success, data, messages) = await CreateEmuMovies().ScrapeEmuMoviesAsync(scraperParameters, scraperProperties.EmuMoviesMediaLists);
                return (success, data, messages);
            }

            if (currentScraper == ScraperRegistry.ScreenScraper.Name)
            {
                var (success, data, messages, limitProgress, limitMax) =
                    await CreateScreenScraper().ScrapeScreenScraperAsync(scraperParameters, cancellationToken);

                if (limitMax > 0)
                {
                    limitCallback?.Invoke(limitProgress, limitMax);
                    if (limitProgress >= limitMax)
                        onQuotaExceeded?.Invoke();
                }

                return (success, data, messages);
            }

            return (false, new ScrapedGameData(), new List<string>());
        }

        #endregion
    }
}