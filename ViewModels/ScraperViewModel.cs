using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Api;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;

namespace Gamelist_Manager.ViewModels;

public partial class ScraperViewModel : ViewModelBase, IDisposable
{
    #region Services
    private readonly SettingsService _settingsService = SettingsService.Instance;
    private readonly SharedDataService _sharedData = SharedDataService.Instance;
    #endregion

    #region Private Fields
    private string _currentScraper = ScraperRegistry.ArcadeDB.Name;
    private bool _isLoading;
    private bool _isDisposed;
    private CancellationTokenSource? _cts;
    private DateTime _startTime;
    private int _scrapeSuccessCount;
    private int _scrapeFailedCount;
    private int _dlSuccessCount;
    private int _dlFailedCount;
    private bool _isStopping;
    #endregion

    #region Observable Properties - Basic Settings
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLimitVisible))]
    private int _selectedScraperIndex;
    [ObservableProperty] private bool _scrapeAllMode = true;
    [ObservableProperty] private bool _scrapeSelectedMode;
    [ObservableProperty] private bool _overwriteName = true;
    [ObservableProperty] private bool _overwriteMetadata;
    [ObservableProperty] private bool _overwriteMedia;
    [ObservableProperty] private bool _scrapeFromCache = true;
    [ObservableProperty] private bool _scrapeFromCacheEnabled = true;
    [ObservableProperty] private bool _scrapeHiddenItems;
    [ObservableProperty] private bool _skipNonCachedItems;
    [ObservableProperty] private bool _skipNonCachedItemsEnabled = true;
    [ObservableProperty] private bool _overwriteMetadataEnabled = true;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isScraping;
    [ObservableProperty] private bool _isClearCacheEnabled;
    [ObservableProperty] private string _cacheCountText = "0 items cached";
    [ObservableProperty] private bool _showLogTimestamp;
    #endregion

    #region Observable Properties - Metadata
    [ObservableProperty] private bool _metaName;
    [ObservableProperty] private bool _metaNameEnabled;
    [ObservableProperty] private bool _metaDescription;
    [ObservableProperty] private bool _metaDescriptionEnabled;
    [ObservableProperty] private bool _metaGenre;
    [ObservableProperty] private bool _metaGenreEnabled;
    [ObservableProperty] private bool _metaPlayers;
    [ObservableProperty] private bool _metaPlayersEnabled;
    [ObservableProperty] private bool _metaRating;
    [ObservableProperty] private bool _metaRatingEnabled;
    [ObservableProperty] private bool _metaRegion;
    [ObservableProperty] private bool _metaRegionEnabled;
    [ObservableProperty] private bool _metaLanguage;
    [ObservableProperty] private bool _metaLanguageEnabled;
    [ObservableProperty] private bool _metaReleaseDate;
    [ObservableProperty] private bool _metaReleaseDateEnabled;
    [ObservableProperty] private bool _metaDeveloper;
    [ObservableProperty] private bool _metaDeveloperEnabled;
    [ObservableProperty] private bool _metaPublisher;
    [ObservableProperty] private bool _metaPublisherEnabled;
    [ObservableProperty] private bool _metaArcadeName;
    [ObservableProperty] private bool _metaArcadeNameEnabled;
    [ObservableProperty] private bool _metaFamily;
    [ObservableProperty] private bool _metaFamilyEnabled;
    [ObservableProperty] private bool _metaGameId;
    [ObservableProperty] private bool _metaGameIdEnabled;
    #endregion

    #region Observable Properties - Media
    [ObservableProperty] private bool _mediaTitleshot;
    [ObservableProperty] private bool _mediaTitleshotEnabled;
    [ObservableProperty] private bool _mediaMap;
    [ObservableProperty] private bool _mediaMapEnabled;
    [ObservableProperty] private bool _mediaManual;
    [ObservableProperty] private bool _mediaManualEnabled;
    [ObservableProperty] private bool _mediaBezel;
    [ObservableProperty] private bool _mediaBezelEnabled;
    [ObservableProperty] private bool _mediaFanArt;
    [ObservableProperty] private bool _mediaFanArtEnabled;
    [ObservableProperty] private bool _mediaBoxBack;
    [ObservableProperty] private bool _mediaBoxBackEnabled;
    [ObservableProperty] private bool _mediaMusic;
    [ObservableProperty] private bool _mediaMusicEnabled;
    [ObservableProperty] private bool _mediaImage;
    [ObservableProperty] private bool _mediaImageEnabled;
    [ObservableProperty] private bool _mediaMarquee;
    [ObservableProperty] private bool _mediaMarqueeEnabled;
    [ObservableProperty] private bool _mediaThumbnail;
    [ObservableProperty] private bool _mediaThumbnailEnabled;
    [ObservableProperty] private bool _mediaCartridge;
    [ObservableProperty] private bool _mediaCartridgeEnabled;
    [ObservableProperty] private bool _mediaVideo;
    [ObservableProperty] private bool _mediaVideoEnabled;
    [ObservableProperty] private bool _mediaBoxArt;
    [ObservableProperty] private bool _mediaBoxArtEnabled;
    [ObservableProperty] private bool _mediaWheel;
    [ObservableProperty] private bool _mediaWheelEnabled;
    #endregion

    #region Observable Properties - Media Sources
    public ObservableCollection<string> ImageSources { get; } = new();
    public ObservableCollection<string> MarqueeSources { get; } = new();
    public ObservableCollection<string> ThumbnailSources { get; } = new();
    public ObservableCollection<string> CartridgeSources { get; } = new();
    public ObservableCollection<string> VideoSources { get; } = new();
    public ObservableCollection<string> BoxArtSources { get; } = new();
    public ObservableCollection<string> WheelSources { get; } = new();

    [ObservableProperty] private int _selectedImageSource = -1;
    [ObservableProperty] private int _selectedMarqueeSource = -1;
    [ObservableProperty] private int _selectedThumbnailSource = -1;
    [ObservableProperty] private int _selectedCartridgeSource = -1;
    [ObservableProperty] private int _selectedVideoSource = -1;
    [ObservableProperty] private int _selectedBoxArtSource = -1;
    [ObservableProperty] private int _selectedWheelSource = -1;
    #endregion

    #region Observable Properties - Progress
    [ObservableProperty] private string _currentScrapeText = "N/A";
    [ObservableProperty] private string _threadCountText = "1";
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private double _progressMaximum = 100;
    [ObservableProperty] private string _progressText = "0%";
    [ObservableProperty] private string _limitText = "N/A";
    [ObservableProperty] private string _timeRemainingText = string.Empty;
    [ObservableProperty] private string _scrapeSuccessText = "0";
    [ObservableProperty] private string _scrapeFailedText = "0";
    [ObservableProperty] private string _downloadSuccessText = "0";
    [ObservableProperty] private string _downloadFailedText = "0";

    public ObservableCollection<LogEntry> LogEntries { get; } = new();
    #endregion

    #region Public Properties
    public bool IsLimitVisible => ScraperRegistry.All.ElementAtOrDefault(SelectedScraperIndex) == ScraperRegistry.ScreenScraper;
    public bool IsBusy => _sharedData.IsBusy;
    private bool CanStop => IsScraping && !_isStopping;
    private bool CanStart => !IsScraping;
    #endregion

    #region Constructor
    public ScraperViewModel()
    {
        _sharedData.PropertyChanged += OnSharedDataPropertyChanged;

        LoadSettings();
        RefreshCacheCount();
        Log("Ready to scrape...", LogLevel.Info);
    }
    #endregion

    #region Property Change Callbacks
    partial void OnIsScrapingChanged(bool value)
    {
        _sharedData.IsScraping = value;
        if (value) IsClearCacheEnabled = false;
    }

    partial void OnSelectedScraperIndexChanged(int value)
    {
        if (_isLoading || _settingsService is null) return;

        SaveScraperSettings();

        _currentScraper = value switch
        {
            0 => ScraperRegistry.ArcadeDB.Name,
            1 => ScraperRegistry.EmuMovies.Name,
            2 => ScraperRegistry.ScreenScraper.Name,
            _ => ScraperRegistry.ArcadeDB.Name
        };

        LoadScraperSettings();
        Log($"Scraper changed to: {_currentScraper}", LogLevel.Status);
    }

    partial void OnScrapeFromCacheChanged(bool value)
    {
        if (_isLoading || !ScrapeFromCacheEnabled) return;
        SkipNonCachedItemsEnabled = value;
        if (!value) SkipNonCachedItems = false;
    }
    #endregion

    #region Settings
    private void LoadSettings()
    {
        _isLoading = true;
        try
        {
            ScrapeAllMode = _settingsService.GetBool("Scraper", "ScrapeAllMode", true);
            ScrapeSelectedMode = !ScrapeAllMode;
            OverwriteName = _settingsService.GetBool("Scraper", "OverwriteName", true);
            OverwriteMedia = _settingsService.GetBool("Scraper", "OverwriteMedia", false);
            ScrapeHiddenItems = _settingsService.GetBool("Scraper", "ScrapeHiddenItems", false);
            ShowLogTimestamp = _settingsService.GetBool("Behavior", "ShowLogTimestamp", false);

            string savedScraper = _settingsService.GetValue("Scraper", "SelectedScraper", ScraperRegistry.ArcadeDB.Name);
            _currentScraper = ScraperRegistry.Find(savedScraper)?.Name ?? ScraperRegistry.ArcadeDB.Name;
            SelectedScraperIndex = _currentScraper switch
            {
                var s when s == ScraperRegistry.ArcadeDB.Name     => 0,
                var s when s == ScraperRegistry.EmuMovies.Name    => 1,
                var s when s == ScraperRegistry.ScreenScraper.Name => 2,
                _ => 0
            };

            LoadScraperSettings();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void LoadScraperSettings()
    {
        bool wasLoading = _isLoading;
        _isLoading = true;
        try
        {
            ScrapeFromCacheEnabled = true;
            ScrapeFromCache = true;
            SkipNonCachedItemsEnabled = true;
            SkipNonCachedItems = false;
            OverwriteMetadataEnabled = true;
            OverwriteMetadata = false;

            if (_currentScraper == ScraperRegistry.EmuMovies.Name)
            {
                ScrapeFromCacheEnabled = false;
                ScrapeFromCache = false;
                SkipNonCachedItemsEnabled = false;
                SkipNonCachedItems = false;
                OverwriteMetadataEnabled = false;
                OverwriteMetadata = false;
            }

            var scraperElements = GamelistMetaData.GetScraperElements(_currentScraper);

            MetaNameEnabled = scraperElements.Contains("name");
            MetaDescriptionEnabled = scraperElements.Contains("desc");
            MetaGenreEnabled = scraperElements.Contains("genre");
            MetaPlayersEnabled = scraperElements.Contains("players");
            MetaRatingEnabled = scraperElements.Contains("rating");
            MetaRegionEnabled = scraperElements.Contains("region");
            MetaLanguageEnabled = scraperElements.Contains("lang");
            MetaReleaseDateEnabled = scraperElements.Contains("releasedate");
            MetaDeveloperEnabled = scraperElements.Contains("developer");
            MetaPublisherEnabled = scraperElements.Contains("publisher");
            MetaArcadeNameEnabled = scraperElements.Contains("arcadesystemname");
            MetaFamilyEnabled = scraperElements.Contains("family");
            MetaGameIdEnabled = scraperElements.Contains("id");

            if (!MetaNameEnabled) MetaName = false;
            if (!MetaDescriptionEnabled) MetaDescription = false;
            if (!MetaGenreEnabled) MetaGenre = false;
            if (!MetaPlayersEnabled) MetaPlayers = false;
            if (!MetaRatingEnabled) MetaRating = false;
            if (!MetaRegionEnabled) MetaRegion = false;
            if (!MetaLanguageEnabled) MetaLanguage = false;
            if (!MetaReleaseDateEnabled) MetaReleaseDate = false;
            if (!MetaDeveloperEnabled) MetaDeveloper = false;
            if (!MetaPublisherEnabled) MetaPublisher = false;
            if (!MetaArcadeNameEnabled) MetaArcadeName = false;
            if (!MetaFamilyEnabled) MetaFamily = false;
            if (!MetaGameIdEnabled) MetaGameId = false;

            MediaTitleshotEnabled = scraperElements.Contains("titleshot");
            MediaMapEnabled = scraperElements.Contains("map");
            MediaManualEnabled = scraperElements.Contains("manual");
            MediaBezelEnabled = scraperElements.Contains("bezel");
            MediaFanArtEnabled = scraperElements.Contains("fanart");
            MediaBoxBackEnabled = scraperElements.Contains("boxback");
            MediaMusicEnabled = scraperElements.Contains("music");

            if (!MediaTitleshotEnabled) MediaTitleshot = false;
            if (!MediaMapEnabled) MediaMap = false;
            if (!MediaManualEnabled) MediaManual = false;
            if (!MediaBezelEnabled) MediaBezel = false;
            if (!MediaFanArtEnabled) MediaFanArt = false;
            if (!MediaBoxBackEnabled) MediaBoxBack = false;
            if (!MediaMusicEnabled) MediaMusic = false;

            ApplySource(ImageSources, "ImageSource", v => SelectedImageSource = v, out bool imgEnabled);
            MediaImageEnabled = imgEnabled;
            if (!imgEnabled) MediaImage = false;

            ApplySource(MarqueeSources, "MarqueeSource", v => SelectedMarqueeSource = v, out bool mrqEnabled);
            MediaMarqueeEnabled = mrqEnabled;
            if (!mrqEnabled) MediaMarquee = false;

            ApplySource(ThumbnailSources, "ThumbnailSource", v => SelectedThumbnailSource = v, out bool thmEnabled);
            MediaThumbnailEnabled = thmEnabled;
            if (!thmEnabled) MediaThumbnail = false;

            ApplySource(CartridgeSources, "CartridgeSource", v => SelectedCartridgeSource = v, out bool crtEnabled);
            MediaCartridgeEnabled = crtEnabled;
            if (!crtEnabled) MediaCartridge = false;

            ApplySource(VideoSources, "VideoSource", v => SelectedVideoSource = v, out bool vidEnabled);
            MediaVideoEnabled = vidEnabled;
            if (!vidEnabled) MediaVideo = false;

            ApplySource(BoxArtSources, "BoxArtSource", v => SelectedBoxArtSource = v, out bool boxEnabled);
            MediaBoxArtEnabled = boxEnabled;
            if (!boxEnabled) MediaBoxArt = false;

            ApplySource(WheelSources, "WheelSource", v => SelectedWheelSource = v, out bool whlEnabled);
            MediaWheelEnabled = whlEnabled;
            if (!whlEnabled) MediaWheel = false;

            ApplyPathsEnabledConstraints();
            LoadSavedPreferences();

            if (ScrapeFromCacheEnabled)
            {
                SkipNonCachedItemsEnabled = ScrapeFromCache;
                if (!ScrapeFromCache) SkipNonCachedItems = false;
            }
        }
        finally
        {
            _isLoading = wasLoading;
        }

        RefreshCacheCount();
    }

    private void ApplyPathsEnabledConstraints()
    {
        var media = _sharedData.MediaSettings;
        if (media.GetValueOrDefault("image")?.Enabled != true) { MediaImageEnabled = false; MediaImage = false; }
        if (media.GetValueOrDefault("titleshot")?.Enabled != true) { MediaTitleshotEnabled = false; MediaTitleshot = false; }
        if (media.GetValueOrDefault("marquee")?.Enabled != true) { MediaMarqueeEnabled = false; MediaMarquee = false; }
        if (media.GetValueOrDefault("wheel")?.Enabled != true) { MediaWheelEnabled = false; MediaWheel = false; }
        if (media.GetValueOrDefault("thumbnail")?.Enabled != true) { MediaThumbnailEnabled = false; MediaThumbnail = false; }
        if (media.GetValueOrDefault("cartridge")?.Enabled != true) { MediaCartridgeEnabled = false; MediaCartridge = false; }
        if (media.GetValueOrDefault("video")?.Enabled != true) { MediaVideoEnabled = false; MediaVideo = false; }
        if (media.GetValueOrDefault("music")?.Enabled != true) { MediaMusicEnabled = false; MediaMusic = false; }
        if (media.GetValueOrDefault("map")?.Enabled != true) { MediaMapEnabled = false; MediaMap = false; }
        if (media.GetValueOrDefault("bezel")?.Enabled != true) { MediaBezelEnabled = false; MediaBezel = false; }
        if (media.GetValueOrDefault("manual")?.Enabled != true) { MediaManualEnabled = false; MediaManual = false; }
        if (media.GetValueOrDefault("fanart")?.Enabled != true) { MediaFanArtEnabled = false; MediaFanArt = false; }
        if (media.GetValueOrDefault("boxart")?.Enabled != true) { MediaBoxArtEnabled = false; MediaBoxArt = false; }
        if (media.GetValueOrDefault("boxback")?.Enabled != true) { MediaBoxBackEnabled = false; MediaBoxBack = false; }
    }

    private void LoadSavedPreferences()
    {
        string p = _currentScraper;
        var type = GetType();

        if (ScrapeFromCacheEnabled)
            ScrapeFromCache = _settingsService.GetBool("Scraper", $"{p}_ScrapeFromCache", ScrapeFromCache);
        if (SkipNonCachedItemsEnabled)
            SkipNonCachedItems = _settingsService.GetBool("Scraper", $"{p}_SkipNonCachedItems", false);
        if (OverwriteMetadataEnabled)
            OverwriteMetadata = _settingsService.GetBool("Scraper", $"{p}_OverwriteMetadata", false);

        foreach (PropertyInfo prop in type.GetProperties())
        {
            if (prop.PropertyType != typeof(bool)) continue;
            string name = prop.Name;
            if (name.EndsWith("Enabled")) continue;
            if (!name.StartsWith("Meta") && !name.StartsWith("Media")) continue;
            var enabledProp = type.GetProperty($"{name}Enabled");
            if (enabledProp != null && !(bool)enabledProp.GetValue(this)!) continue;
            prop.SetValue(this, _settingsService.GetBool("Scraper", $"{p}_{name}", false));
        }

        foreach (PropertyInfo prop in type.GetProperties())
        {
            if (prop.PropertyType != typeof(ObservableCollection<string>)) continue;
            string collectionName = prop.Name;
            if (!collectionName.EndsWith("Sources")) continue;
            string keyName = collectionName[..^1];
            var selectedProp = type.GetProperty($"Selected{keyName}");
            if (selectedProp == null) continue;
            var collection = (ObservableCollection<string>)prop.GetValue(this)!;
            string savedValue = _settingsService.GetValue("Scraper", $"{p}_{keyName}", "");
            RestoreSource(collection, savedValue, idx => selectedProp.SetValue(this, idx));
        }
    }

    private void SaveScraperSettings()
    {
        if (_settingsService is null) return;

        string p = _currentScraper;
        var type = GetType();
        var values = new Dictionary<string, string>
        {
            ["ScrapeAllMode"] = ScrapeAllMode.ToString(),
            ["OverwriteName"] = OverwriteName.ToString(),
            ["OverwriteMedia"] = OverwriteMedia.ToString(),
            ["ScrapeHiddenItems"] = ScrapeHiddenItems.ToString(),
            ["SelectedScraper"] = _currentScraper,
            [$"{p}_ScrapeFromCache"] = ScrapeFromCache.ToString(),
            [$"{p}_SkipNonCachedItems"] = SkipNonCachedItems.ToString(),
            [$"{p}_OverwriteMetadata"] = OverwriteMetadata.ToString(),
        };

        foreach (PropertyInfo prop in type.GetProperties())
        {
            if (prop.PropertyType != typeof(bool)) continue;
            string name = prop.Name;
            if (name.EndsWith("Enabled")) continue;
            if (!name.StartsWith("Meta") && !name.StartsWith("Media")) continue;
            values[$"{p}_{name}"] = ((bool)prop.GetValue(this)!).ToString();
        }

        foreach (PropertyInfo prop in type.GetProperties())
        {
            if (prop.PropertyType != typeof(ObservableCollection<string>)) continue;
            string collectionName = prop.Name;
            if (!collectionName.EndsWith("Sources")) continue;
            string keyName = collectionName[..^1];
            var selectedProp = type.GetProperty($"Selected{keyName}");
            if (selectedProp == null) continue;
            var collection = (ObservableCollection<string>)prop.GetValue(this)!;
            int idx = (int)selectedProp.GetValue(this)!;
            values[$"{p}_{keyName}"] = idx >= 0 && idx < collection.Count ? collection[idx] : "";
        }

        _settingsService.SetSection("Scraper", values);
    }
    #endregion

    #region Dispose
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _sharedData.PropertyChanged -= OnSharedDataPropertyChanged;
        _cts?.Cancel();
        _cts?.Dispose();
        SaveScraperSettings();
    }
    #endregion
}