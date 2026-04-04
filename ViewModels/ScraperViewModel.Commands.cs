using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class ScraperViewModel
{
    #region Scraper Control
    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task Start()
    {
        _isStopping = false;
        IsScraping = true;
        await StartAsync();
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        _isStopping = true;
        StopCommand.NotifyCanExecuteChanged();
        _cts?.Cancel();
        Log("Stopping scraper...", LogLevel.Warning);
    }

    [RelayCommand]
    private void ClearCache()
    {
        string system = _sharedData.CurrentSystem ?? string.Empty;
        if (string.IsNullOrEmpty(system))
        {
            Log("No system loaded, cannot clear cache.", LogLevel.Warning);
            return;
        }

        string cacheFolder = Path.Combine(AppContext.BaseDirectory, "cache", _currentScraper, system);
        if (!Directory.Exists(cacheFolder))
        {
            RefreshCacheCount();
            Log("Cache is already empty.", LogLevel.Info);
            return;
        }

        int deleted = 0;
        foreach (var file in Directory.EnumerateFiles(cacheFolder))
        {
            try { File.Delete(file); deleted++; }
            catch (System.Exception ex) { Log($"Failed to delete {Path.GetFileName(file)}: {ex.Message}", LogLevel.Warning); }
        }

        RefreshCacheCount();
        Log($"Cleared {deleted} cached file(s).", LogLevel.Warning);
    }
    #endregion

    #region Select All / None
    [RelayCommand]
    private void SelectAllMetadata()
    {
        if (MetaNameEnabled) MetaName = true;
        if (MetaDescriptionEnabled) MetaDescription = true;
        if (MetaGenreEnabled) MetaGenre = true;
        if (MetaPlayersEnabled) MetaPlayers = true;
        if (MetaRatingEnabled) MetaRating = true;
        if (MetaRegionEnabled) MetaRegion = true;
        if (MetaLanguageEnabled) MetaLanguage = true;
        if (MetaReleaseDateEnabled) MetaReleaseDate = true;
        if (MetaDeveloperEnabled) MetaDeveloper = true;
        if (MetaPublisherEnabled) MetaPublisher = true;
        if (MetaArcadeNameEnabled) MetaArcadeName = true;
        if (MetaFamilyEnabled) MetaFamily = true;
        if (MetaGameIdEnabled) MetaGameId = true;
    }

    [RelayCommand]
    private void SelectNoneMetadata()
    {
        MetaName = false;
        MetaDescription = false;
        MetaGenre = false;
        MetaPlayers = false;
        MetaRating = false;
        MetaRegion = false;
        MetaLanguage = false;
        MetaReleaseDate = false;
        MetaDeveloper = false;
        MetaPublisher = false;
        MetaArcadeName = false;
        MetaFamily = false;
        MetaGameId = false;
    }

    [RelayCommand]
    private void SelectAllMedia()
    {
        if (MediaTitleshotEnabled) MediaTitleshot = true;
        if (MediaMapEnabled) MediaMap = true;
        if (MediaManualEnabled) MediaManual = true;
        if (MediaBezelEnabled) MediaBezel = true;
        if (MediaFanArtEnabled) MediaFanArt = true;
        if (MediaBoxBackEnabled) MediaBoxBack = true;
        if (MediaMusicEnabled) MediaMusic = true;
        if (MediaImageEnabled) MediaImage = true;
        if (MediaMarqueeEnabled) MediaMarquee = true;
        if (MediaThumbnailEnabled) MediaThumbnail = true;
        if (MediaCartridgeEnabled) MediaCartridge = true;
        if (MediaVideoEnabled) MediaVideo = true;
        if (MediaBoxArtEnabled) MediaBoxArt = true;
        if (MediaWheelEnabled) MediaWheel = true;
    }

    [RelayCommand]
    private void SelectNoneMedia()
    {
        MediaTitleshot = false;
        MediaMap = false;
        MediaManual = false;
        MediaBezel = false;
        MediaFanArt = false;
        MediaBoxBack = false;
        MediaMusic = false;
        MediaImage = false;
        MediaMarquee = false;
        MediaThumbnail = false;
        MediaCartridge = false;
        MediaVideo = false;
        MediaBoxArt = false;
        MediaWheel = false;
    }

    [RelayCommand]
    private void ResetSources()
    {
        if (MediaImageEnabled) SelectedImageSource = 0;
        if (MediaMarqueeEnabled) SelectedMarqueeSource = 0;
        if (MediaThumbnailEnabled) SelectedThumbnailSource = 0;
        if (MediaCartridgeEnabled) SelectedCartridgeSource = 0;
        if (MediaVideoEnabled) SelectedVideoSource = 0;
        if (MediaBoxArtEnabled) SelectedBoxArtSource = 0;
        if (MediaWheelEnabled) SelectedWheelSource = 0;
    }
    #endregion
}