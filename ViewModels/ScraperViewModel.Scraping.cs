using Avalonia.Threading;
using Gamelist_Manager.Classes.Api;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class ScraperViewModel
{
    private async Task StartAsync()
    {
        if (_sharedData.GamelistData == null || _sharedData.GamelistData.Count == 0)
        {
            Log("No gamelist loaded.", LogLevel.Error);
            IsScraping = false;
            return;
        }

        if (string.IsNullOrEmpty(_sharedData.CurrentSystem))
        {
            Log("No system selected.", LogLevel.Error);
            IsScraping = false;
            return;
        }

        if (string.IsNullOrEmpty(_sharedData.CurrentRomFolder))
        {
            Log("No gamelist directory found.", LogLevel.Error);
            IsScraping = false;
            return;
        }

        var elementsToScrape = BuildElementsToScrape();
        if (elementsToScrape.Count == 0)
        {
            Log("No elements selected to scrape.", LogLevel.Error);
            IsScraping = false;
            return;
        }

        var currentSystem = _sharedData.CurrentSystem;
        var gamelistDirectory = _sharedData.CurrentRomFolder;
        var currentScraper = CurrentScraper;
        var maxBatch = _sharedData.MaxBatch;
        var removeZzzNotGamePrefix = _sharedData.RemoveZZZNotGamePrefix;
        var logVerbosity = _sharedData.LogVerbosity;
        var batchProcessing = _sharedData.BatchProcessing;
        var verifyImageDownloads = _sharedData.VerifyImageDownloads;
        var overrideConcurrency = _sharedData.OverrideConcurrency;
        var concurrencyOverride = _sharedData.ConcurrencyOverride;
        var profileType = _sharedData.ProfileType;
        var availableMedia = _sharedData.AvailableMedia;

        SaveScraperSettings();
        ResetScrapeUI();

        _cts = new CancellationTokenSource();
        var scraperService = Startup.Services.GetRequiredService<ScraperService>();
        scraperService.LogAction = Log;
        scraperService.ClearDownloadStats();
        Log($"Starting {currentScraper} scraper...", LogLevel.Success);
        StartLogFileSession(currentScraper, currentSystem ?? "unknown");

        try
        {
            var baseParameters = ScraperParameters.Create(
                gamelistDirectory,
                verifyImageDownloads,
                profileType,
                availableMedia,
                currentScraper,
                currentSystem,
                elementsToScrape);

            baseParameters.OverwriteName = OverwriteName;
            baseParameters.OverwriteMetadata = OverwriteMetadata;
            baseParameters.OverwriteMedia = OverwriteMedia;
            baseParameters.ScrapeByCache = ScrapeFromCache;
            baseParameters.SkipNonCached = SkipNonCachedItems;
            baseParameters.RemoveZzzNotGamePrefix = removeZzzNotGamePrefix;

            var scraperProperties = new ScraperProperties
            {
                ScraperName = currentScraper,
                LogVerbosity = logVerbosity,
                BatchProcessing = batchProcessing,
            };

            if (!await scraperService.InitializeScraperAsync(
                baseParameters, scraperProperties,
                currentSystem, _cts.Token))
            {
                return;
            }

            if (overrideConcurrency && concurrencyOverride >= 1)
            {
                int naturalMax = scraperProperties.MaxConcurrency;
                scraperProperties.MaxConcurrency = Math.Min(naturalMax, concurrencyOverride);

                if (concurrencyOverride >= naturalMax)
                    Log($"Concurrency override ({concurrencyOverride}) ignored — scraper max is {naturalMax}.", LogLevel.Info);
                else
                    Log($"Concurrency override: {concurrencyOverride} of {naturalMax} threads.", LogLevel.Info);
            }

            var rowsToScrape = GetRowsToScrape();
            if (rowsToScrape.Count == 0)
            {
                Log("Nothing to scrape.", LogLevel.Warning);
                return;
            }

            ProgressMaximum = rowsToScrape.Count;
            ThreadCountText = scraperProperties.MaxConcurrency.ToString();
            _startTime = DateTime.Now;

            string modeLabel = ScrapeSelectedMode ? "selected" : "visible";
            Log($"Scraping {rowsToScrape.Count} {modeLabel} item(s) with {scraperProperties.MaxConcurrency} thread(s)...", LogLevel.Status);

            bool completed = await scraperService.RunScrapeAsync(
                baseParameters, scraperProperties, rowsToScrape,
                maxBatch,
                new ScrapingCallbacks(
                    OnProgress: (current, total, item) =>
                    {
                        var now = DateTime.Now;
                        if (current == total || (now - _lastProgressUpdate).TotalMilliseconds >= 100)
                        {
                            _lastProgressUpdate = now;
                            Dispatcher.UIThread.Post(() => UpdateScrapeProgress(current, total, item), Avalonia.Threading.DispatcherPriority.Background);
                        }
                    },
                    OnLimitUpdate: (progress, max) => Dispatcher.UIThread.Post(() => LimitText = $"{progress}/{max}"),
                    OnDataChanged: () => _sharedData.IsDataChanged = true,
                    OnQuotaExceeded: () =>
                    {
                        _cts?.Cancel();
                        Dispatcher.UIThread.Post(() => Log("Daily API quota reached — scraping stopped.", LogLevel.Warning));
                    }),
                _cts.Token);

            scraperService.LogDownloadSummary();
            if (completed)
            {
                var elapsed = DateTime.Now - _startTime;
                string timeStr = elapsed.TotalHours >= 1
                    ? elapsed.ToString(@"h\:mm\:ss")
                    : elapsed.ToString(@"m\:ss");
                Log($"Scraping complete! ({timeStr})", LogLevel.Success);
            }
        }
        catch (OperationCanceledException)
        {
            Log("Scraping cancelled.", LogLevel.Warning);
        }
        catch (Exception ex)
        {
            Log($"Scraping error: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            await StopLogFileSession();
            await Task.Delay(2000);
            IsScraping = false;
            CurrentScrapeText = "N/A";
            TimeRemainingText = string.Empty;
            _cts?.Dispose();
            _cts = null;
            RefreshCacheCount();
        }
    }

    private void ResetScrapeUI()
    {
        LogEntries.Clear();
        DownloadLogEntries.Clear();
        _scrapeSuccessCount = 0;
        _scrapeFailedCount = 0;
        _dlSuccessCount = 0;
        _dlFailedCount = 0;
        ScrapeSuccessText = "0";
        ScrapeFailedText = "0";
        DownloadSuccessText = "0";
        DownloadFailedText = "0";
        ProgressValue = 0;
        ProgressMaximum = 100;
        ProgressText = "0%";
        TimeRemainingText = string.Empty;
        CurrentScrapeText = "N/A";
        LimitText = "N/A";
        ThreadCountText = "1";
    }

    private List<GameMetadataRow> GetRowsToScrape()
    {
        return (ScrapeSelectedMode
                ? _sharedData.SelectedItems?.OfType<GameMetadataRow>() ?? Enumerable.Empty<GameMetadataRow>()
                : _sharedData.FilteredGamelistData
                  ?? _sharedData.GamelistData?.AsEnumerable()
                  ?? Enumerable.Empty<GameMetadataRow>())
            .Where(r => ScrapeHiddenItems || r.GetValue(MetaDataKeys.hidden) is not true)
            .ToList();
    }

    private void UpdateScrapeProgress(int current, int total, string item)
    {
        CurrentScrapeText = item;
        ProgressValue = current;
        ProgressText = $"{current}/{total}";
        double pct = (double)current / total * 100;
        if (pct > 0)
        {
            TimeSpan elapsed = DateTime.Now - _startTime;
            double remainingMs = elapsed.TotalMilliseconds / pct * (100 - pct);
            TimeSpan remaining = TimeSpan.FromMilliseconds(remainingMs);
            string t = string.Empty;
            if (remaining.Hours > 0) t += $"{remaining.Hours:D2}h ";
            if (remaining.Minutes > 0) t += $"{remaining.Minutes:D2}m ";
            if (remaining.Hours == 0) t += $"{remaining.Seconds:D2}s";
            TimeRemainingText = t.TrimEnd() + " remaining...";
        }
    }

    private List<string> BuildElementsToScrape()
        => GetBoolToggles().Where(t => t.GetValue()).Select(t => t.ElementKey).ToList();
}
