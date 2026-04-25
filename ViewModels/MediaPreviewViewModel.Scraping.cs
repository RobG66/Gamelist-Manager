using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Api;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class MediaPreviewViewModel
{
    public IReadOnlyList<ScraperConfig> Scrapers => ScraperRegistry.All;

    // Returns false for arcade-only scrapers when the current system is not an arcade system.
    public bool IsScraperAvailable(ScraperConfig scraper)
    {
        if (!scraper.ArcadeOnly) return true;
        var system = _sharedData.CurrentSystem ?? string.Empty;
        return ArcadeSystemIDHelper.IsInitialized && ArcadeSystemIDHelper.HasArcadeSystemName(system);
    }

    private bool CanScrapeGame() => !IsScraping && SelectedGame != null;

    [RelayCommand(CanExecute = nameof(CanScrapeGame))]
    private async Task ScrapeGame(string scraperName) => await ReScrapeGameAsync(scraperName, null);

    public async Task ReScrapeGameAsync(string scraperName, List<string>? specificElements = null)
    {
        if (SelectedGame == null || string.IsNullOrEmpty(scraperName)) return;

        if (string.IsNullOrEmpty(_sharedData.GamelistDirectory))
        {
            SetScraperStatus("No gamelist loaded.", "error");
            return;
        }

        if (string.IsNullOrEmpty(_sharedData.CurrentSystem))
        {
            SetScraperStatus("No system selected.", "error");
            return;
        }

        var currentSystem = _sharedData.CurrentSystem;
        var gamelistDirectory = _sharedData.GamelistDirectory;
        var mediaSettings = _sharedData.MediaSettings;

        _sharedData.IsScraping = true;
        bool scrapingVideo = false;
        try
        {
            string? itemLabel = specificElements?.Count == 1
                ? GamelistMetaData.GetMetadataNameByType(specificElements[0])
                : null;

            SetScraperStatus(
                !string.IsNullOrEmpty(itemLabel)
                    ? $"Scraping {itemLabel} with {scraperName}..."
                    : $"Scraping with {scraperName}...",
                null);

            var elementsToScrape = specificElements != null
                ? specificElements.Where(e => mediaSettings.TryGetValue(e, out var d) && d.Enabled).ToList()
                : GamelistMetaData.GetScraperElements(scraperName)
                    .Where(e => !mediaSettings.ContainsKey(e) || (mediaSettings.TryGetValue(e, out var d) && d.Enabled))
                    .ToList();

            if (elementsToScrape.Count == 0)
            {
                SetScraperStatus(
                    specificElements != null
                        ? "No media path configured for this item."
                        : "No media paths configured in Settings.",
                    "error");
                return;
            }

            var scraperProperties = new ScraperProperties
            {
                ScraperName = scraperName,
                LogVerbosity = 0
            };

            if (elementsToScrape.Contains("video")) scrapingVideo = true;
            if (scrapingVideo) SuspendVideo();

            var baseParameters = ScraperParameters.Create(
                gamelistDirectory,
                _sharedData.VerifyImageDownloads,
                _sharedData.ProfileType,
                mediaSettings,
                _sharedData.EsDeMediaDirectory,
                scraperName,
                currentSystem,
                elementsToScrape);

            // Always overwrite single media item scrapes
            baseParameters.OverwriteMedia = specificElements?.Count == 1 || OverwriteMedia;
            baseParameters.OverwriteMetadata = OverwriteMetadata;

            var scraperService = Startup.Services.GetRequiredService<ScraperService>();
            scraperService.LogAction = (message, level, _, _) =>
            {
                if (level == LogLevel.Error)
                    SetScraperStatus(message, "error");
            };

            if (!await scraperService.InitializeScraperAsync(
                baseParameters, scraperProperties, currentSystem))
                return;

            var (success, data) = await scraperService.ScrapeGameAsync(
                SelectedGame, baseParameters, scraperProperties, scraperName);

            if (success && data.Data.Count > 0)
            {
                await scraperService.SaveScrapedDataAsync(SelectedGame, data, baseParameters);
                _sharedData.IsDataChanged = true;
                SetScraperStatus(
                    !string.IsNullOrEmpty(itemLabel)
                        ? $"{itemLabel} rescrape complete."
                        : "Rescrape complete.",
                    "ok");
            }
            else if (success)
            {
                SetScraperStatus(
                    !string.IsNullOrEmpty(itemLabel)
                        ? $"No media found for {itemLabel}."
                        : "No media found.",
                    null);
            }
            else
            {
                SetScraperStatus(
                    baseParameters.OverwriteMedia
                        ? "Could not scrape media for this game."
                        : "No new media scraped.",
                    null);
            }
        }
        catch (Exception ex)
        {
            SetScraperStatus($"Error: {ex.Message}", "error");
        }
        finally
        {
            if (scrapingVideo && IsLibVLCInitialized && LibVLC != null)
                InitializeVideosForCurrentGame();
            _sharedData.IsScraping = false;
        }
    }
}