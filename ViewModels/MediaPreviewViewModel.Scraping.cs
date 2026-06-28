using CommunityToolkit.Mvvm.Input;
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

    public bool IsScraperAvailable(ScraperConfig scraper)
    {
        if (!scraper.ArcadeOnly) return true;
        var system = _sessionState.CurrentSystem ?? string.Empty;
        return ArcadeSystemIDHelper.IsInitialized && ArcadeSystemIDHelper.HasArcadeSystemName(system);
    }

    private bool CanScrapeGame() => !_sessionState.IsScraping && SelectedGame != null;

    [RelayCommand(CanExecute = nameof(CanScrapeGame))]
    private async Task ScrapeGame(string scraperName) => await ReScrapeGameAsync(scraperName, null);

    public async Task ReScrapeGameAsync(string scraperName, List<string>? specificElements = null)
    {
        if (SelectedGame == null || string.IsNullOrEmpty(scraperName)) return;

        if (string.IsNullOrEmpty(_sessionState.CurrentRomFolder))
        {
            SetScraperStatus("No gamelist loaded.", "error");
            return;
        }

        if (string.IsNullOrEmpty(_sessionState.CurrentSystem))
        {
            SetScraperStatus("No system selected.", "error");
            return;
        }

        var currentSystem = _sessionState.CurrentSystem;
        var availableMedia = _sessionState.AvailableMedia;

        _sessionState.IsScraping = true;
        bool scrapingVideo = false;
        try
        {
            string? itemLabel = specificElements?.Count == 1
                ? MetadataService.GetMetadataNameByType(specificElements[0])
                : null;

            SetScraperStatus(
                !string.IsNullOrEmpty(itemLabel)
                    ? $"Scraping {itemLabel} with {scraperName}..."
                    : $"Scraping with {scraperName}...",
                null);

            var elementsToScrape = specificElements != null
                ? specificElements.Where(e => availableMedia.Any(m => m.Type == e) || MetadataService.GetDeclByType(e) is { IsMedia: false }).ToList()
                : MetadataService.GetScraperElements(scraperName)
                    .Where(e => availableMedia.Any(m => m.Type == e) || MetadataService.GetDeclByType(e) is { IsMedia: false })
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

            if (elementsToScrape.Contains("video")) scrapingVideo = true;
            if (scrapingVideo) SuspendVideo();

            var baseParameters = ScraperService.CreateScraperParameters(
                _settingsState.VerifyImageDownloads,
                _settingsState.ProfileType,
                availableMedia,
                scraperName,
                currentSystem,
                elementsToScrape);

            baseParameters.LogVerbosity = 0;
            baseParameters.OverwriteMedia = specificElements?.Count == 1 || OverwriteMedia;
            baseParameters.OverwriteMetadata = OverwriteMetadata;

            var scraperService = Startup.Services.GetRequiredService<ScraperService>();
            scraperService.LogAction = (message, level, _, _) =>
            {
                if (level == LogLevel.Error)
                    SetScraperStatus(message, "error");
            };

            if (!await scraperService.InitializeScraperAsync(baseParameters, currentSystem))
                return;

            var (success, data) = await scraperService.ScrapeGameAsync(SelectedGame, baseParameters);

            if (success && data.Data.Count > 0)
            {
                await scraperService.SaveScrapedDataAsync(SelectedGame, data, baseParameters);
                _sessionState.IsDataChanged = true;
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
            if (scrapingVideo && IsMpvInitialized)
                InitializeVideosForCurrentGame();
            _sessionState.IsScraping = false;
        }
    }
}