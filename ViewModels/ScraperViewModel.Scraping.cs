using Avalonia.Threading;
using Gamelist_Manager.Classes.Api;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        var profileType = _sharedData.ProfileType;
        var availableMedia = _sharedData.AvailableMedia;

        SaveScraperSettings();
        ResetScrapeUI();

        _cts = new CancellationTokenSource();
        var scraperService = Startup.Services.GetRequiredService<ScraperService>();
        scraperService.LogAction = Log;
        scraperService.ClearDownloadStats();
        Log($"Starting {currentScraper} scraper...", LogLevel.Success);

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
                (current, total, item) => Dispatcher.UIThread.Post(() => UpdateScrapeProgress(current, total, item)),
                (progress, max) => Dispatcher.UIThread.Post(() => LimitText = $"{progress}/{max}"),
                () => _sharedData.IsDataChanged = true,
                _cts.Token);

            scraperService.LogDownloadSummary();
            if (completed)
                Log("Scraping complete!", LogLevel.Success);
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
    {
        var elements = new List<string>();
        if (MetaName) elements.Add("name");
        if (MetaDescription) elements.Add("desc");
        if (MetaGenre) elements.Add("genre");
        if (MetaPlayers) elements.Add("players");
        if (MetaRating) elements.Add("rating");
        if (MetaRegion) elements.Add("region");
        if (MetaLanguage) elements.Add("lang");
        if (MetaReleaseDate) elements.Add("releasedate");
        if (MetaDeveloper) elements.Add("developer");
        if (MetaPublisher) elements.Add("publisher");
        if (MetaArcadeName) elements.Add("arcadesystemname");
        if (MetaFamily) elements.Add("family");
        if (MetaGameId) elements.Add("id");
        if (MediaTitleshot) elements.Add("titleshot");
        if (MediaMap) elements.Add("map");
        if (MediaManual) elements.Add("manual");
        if (MediaBezel) elements.Add("bezel");
        if (MediaFanArt) elements.Add("fanart");
        if (MediaBoxBack) elements.Add("boxback");
        if (MediaMusic) elements.Add("music");
        if (MediaImage) elements.Add("image");
        if (MediaMarquee) elements.Add("marquee");
        if (MediaThumbnail) elements.Add("thumbnail");
        if (MediaCartridge) elements.Add("cartridge");
        if (MediaVideo) elements.Add("video");
        if (MediaBoxArt) elements.Add("boxart");
        if (MediaMix) elements.Add("mix");
        if (MediaWheel) elements.Add("wheel");
        return elements;
    }

    private static void RestoreSource(ObservableCollection<string> collection, string savedValue, Action<int> setIndex)
    {
        if (string.IsNullOrEmpty(savedValue) || collection.Count == 0) return;
        var idx = collection.IndexOf(savedValue);
        if (idx >= 0) setIndex(idx);
    }

    private void ApplySource(ObservableCollection<string> collection, string sectionName, Action<int> setIndex, out bool enabled)
    {
        collection.Clear();
        var sources = ScraperConfigService.Instance.GetScraperSources(CurrentScraper, sectionName);
        if (sources.Count > 0)
        {
            foreach (var key in sources.Keys)
                collection.Add(key);
            setIndex(0);
            enabled = true;
        }
        else
        {
            setIndex(-1);
            enabled = false;
        }
    }
}