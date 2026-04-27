using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Gamelist_Manager.ViewModels;

public partial class ScraperViewModel : ViewModelBase, IDisposable
{
    #region Services
    private readonly SettingsService _settingsService = SettingsService.Instance;
    private readonly SharedDataService _sharedData = SharedDataService.Instance;
    #endregion

    #region Private Fields
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
    private string _currentScraper = ScraperRegistry.All[0].Name;
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
    [ObservableProperty] private bool _mediaMix;
    [ObservableProperty] private bool _mediaMixEnabled;
    [ObservableProperty] private bool _mediaWheel;
    [ObservableProperty] private bool _mediaWheelEnabled;
    #endregion

    #region Observable Properties - Media Sources
    public ObservableCollection<string> ImageSources { get; } = [];
    public ObservableCollection<string> MarqueeSources { get; } = [];
    public ObservableCollection<string> ThumbnailSources { get; } = [];
    public ObservableCollection<string> CartridgeSources { get; } = [];
    public ObservableCollection<string> VideoSources { get; } = [];
    public ObservableCollection<string> BoxArtSources { get; } = [];
    public ObservableCollection<string> MixSources { get; } = [];
    public ObservableCollection<string> WheelSources { get; } = [];
    [ObservableProperty] private int _selectedImageSource = -1;
    [ObservableProperty] private int _selectedMarqueeSource = -1;
    [ObservableProperty] private int _selectedThumbnailSource = -1;
    [ObservableProperty] private int _selectedCartridgeSource = -1;
    [ObservableProperty] private int _selectedVideoSource = -1;
    [ObservableProperty] private int _selectedBoxArtSource = -1;
    [ObservableProperty] private int _selectedMixSource = -1;
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

    public ObservableCollection<LogEntry> LogEntries { get; } = [];
    public ObservableCollection<LogEntry> DownloadLogEntries { get; } = [];
    #endregion

    #region Public Properties
    public List<string> ScraperNames { get; } = ScraperRegistry.All.Select(s => s.Name).ToList();
    public bool IsLimitVisible => CurrentScraper == ScraperRegistry.ScreenScraper.Name;
    public bool IsBusy => _sharedData.IsBusy;
    private bool CanStop => IsScraping && !_isStopping;
    private bool CanStart => !IsScraping;

    // 0 = Save Per Scraper, 1 = Save Per System
    private int ScraperConfigSaveMode => _settingsService.GetInt(SettingKeys.ScraperConfigSave);

    private string ScraperSettingsKey => ScraperConfigSaveMode == 1 && !string.IsNullOrEmpty(_sharedData.CurrentSystem)
        ? $"{CurrentScraper}_{_sharedData.CurrentSystem}"
        : CurrentScraper;
    #endregion

    #region Constructor
    public ScraperViewModel()
    {
        _sharedData.PropertyChanged += OnSharedDataPropertyChanged;
        _sharedData.SettingsApplied += OnSettingsApplied;

        _isLoading = true;
        LoadScraperSettings();
        _isLoading = false;
        RefreshCacheCount();
        Log("Ready to scrape...", LogLevel.Info);
    }
    #endregion

    #region Event Handlers
    private void OnSettingsApplied(object? sender, EventArgs e)
    {
        ShowLogTimestamp = _sharedData.ShowLogTimestamp;
    }

    private void OnSharedDataPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SharedDataService.CurrentSystem):
                RefreshCacheCount();
                break;
            case nameof(SharedDataService.AvailableMedia):
                if (!IsScraping)
                    LoadScraperSettings();
                break;
            case nameof(SharedDataService.IsScraping):
                if (IsScraping != _sharedData.IsScraping)
                    IsScraping = _sharedData.IsScraping;
                break;
            case nameof(SharedDataService.IsBusy):
                OnPropertyChanged(nameof(IsBusy));
                break;
        }
    }
    #endregion

    #region Property Change Callbacks
    partial void OnIsScrapingChanged(bool value)
    {
        _sharedData.IsScraping = value;
        if (value) IsClearCacheEnabled = false;
    }

    partial void OnCurrentScraperChanging(string value)
    {
        if (_isLoading) return;
        SaveScraperSettings();
    }

    partial void OnCurrentScraperChanged(string value)
    {
        if (_isLoading) return;
        LoadScraperSettings();
        RefreshCacheCount();
        Log($"Scraper changed to: {value}", LogLevel.Status);
    }

    partial void OnScrapeFromCacheChanged(bool value)
    {
        if (_isLoading || !ScrapeFromCacheEnabled) return;
        SkipNonCachedItemsEnabled = value;
        if (!value) SkipNonCachedItems = false;
    }
    #endregion


    #region Private Methods

    private void LoadScraperSettings()
    {
        var key = ScraperSettingsKey;

        if (_isLoading)
        {
            string savedScraper = _settingsService.GetValue(SettingKeys.SelectedScraper);
            CurrentScraper = ScraperRegistry.Find(savedScraper)?.Name ?? ScraperRegistry.ArcadeDB.Name;
            key = ScraperSettingsKey; // recompute after CurrentScraper is set
        }

        ScrapeAllMode = _settingsService.GetBool(SettingKeys.ScraperSection, $"{key}_ScrapeAllMode", SettingKeys.ScrapeAllMode.Default);
        ScrapeSelectedMode = !ScrapeAllMode;
        OverwriteName = _settingsService.GetBool(SettingKeys.ScraperSection, $"{key}_OverwriteName", SettingKeys.OverwriteName.Default);
        OverwriteMedia = _settingsService.GetBool(SettingKeys.ScraperSection, $"{key}_OverwriteMedia", SettingKeys.OverwriteMedia.Default);
        ScrapeHiddenItems = _settingsService.GetBool(SettingKeys.ScraperSection, $"{key}_ScrapeHiddenItems", SettingKeys.ScrapeHiddenItems.Default);
        ShowLogTimestamp = _sharedData.ShowLogTimestamp;

        // Reset capability flags to defaults before applying scraper-specific overrides.
        ScrapeFromCacheEnabled = true;
        SkipNonCachedItemsEnabled = true;
        OverwriteMetadataEnabled = true;

        if (CurrentScraper == ScraperRegistry.EmuMovies.Name)
        {
            ScrapeFromCacheEnabled = false;
            ScrapeFromCache = false;
            SkipNonCachedItemsEnabled = false;
            SkipNonCachedItems = false;
            OverwriteMetadataEnabled = false;
            OverwriteMetadata = false;
        }

        var scraperElements = GamelistMetaData.GetScraperElements(CurrentScraper);

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

        ApplySource(MixSources, "MixSource", v => SelectedMixSource = v, out bool mixEnabled);
        MediaMixEnabled = mixEnabled;
        if (!mixEnabled) MediaMix = false;

        ApplySource(WheelSources, "WheelSource", v => SelectedWheelSource = v, out bool whlEnabled);
        MediaWheelEnabled = whlEnabled;
        if (!whlEnabled) MediaWheel = false;

        ApplyMediaAvailability();

        if (ScrapeFromCacheEnabled)
            ScrapeFromCache = _settingsService.GetBool(SettingKeys.ScraperSection, $"{key}_ScrapeFromCache", ScrapeFromCache);
        if (SkipNonCachedItemsEnabled)
            SkipNonCachedItems = _settingsService.GetBool(SettingKeys.ScraperSection, $"{key}_SkipNonCachedItems", false);
        if (OverwriteMetadataEnabled)
            OverwriteMetadata = _settingsService.GetBool(SettingKeys.ScraperSection, $"{key}_OverwriteMetadata", false);

        foreach (var (name, getEnabled, _, setValue) in GetBoolToggles())
        {
            if (!getEnabled()) continue;
            setValue(_settingsService.GetBool(SettingKeys.ScraperSection, $"{key}_{name}", false));
        }

        foreach (var (name, sources, _, setSelected) in GetSourceToggles())
        {
            string savedValue = _settingsService.GetValue(SettingKeys.ScraperSection, $"{key}_{name}", "");
            RestoreSource(sources, savedValue, setSelected);
        }

        if (ScrapeFromCacheEnabled)
        {
            SkipNonCachedItemsEnabled = ScrapeFromCache;
            if (!ScrapeFromCache) SkipNonCachedItems = false;
        }
    }

    private void ApplyMediaAvailability()
    {
        var media = _sharedData.AvailableMedia;
        bool Has(string type) => media.Any(m => m.Type == type);
        if (!Has("image"))     { MediaImageEnabled = false;     MediaImage = false; }
        if (!Has("titleshot")) { MediaTitleshotEnabled = false; MediaTitleshot = false; }
        if (!Has("marquee"))   { MediaMarqueeEnabled = false;   MediaMarquee = false; }
        if (!Has("wheel"))     { MediaWheelEnabled = false;     MediaWheel = false; }
        if (!Has("thumbnail")) { MediaThumbnailEnabled = false; MediaThumbnail = false; }
        if (!Has("cartridge")) { MediaCartridgeEnabled = false; MediaCartridge = false; }
        if (!Has("video"))     { MediaVideoEnabled = false;     MediaVideo = false; }
        if (!Has("music"))     { MediaMusicEnabled = false;     MediaMusic = false; }
        if (!Has("map"))       { MediaMapEnabled = false;       MediaMap = false; }
        if (!Has("bezel"))     { MediaBezelEnabled = false;     MediaBezel = false; }
        if (!Has("manual"))    { MediaManualEnabled = false;    MediaManual = false; }
        if (!Has("fanart"))    { MediaFanArtEnabled = false;    MediaFanArt = false; }
        if (!Has("boxart"))    { MediaBoxArtEnabled = false;    MediaBoxArt = false; }
        if (!Has("mix"))       { MediaMixEnabled = false;       MediaMix = false; }
        if (!Has("boxback"))   { MediaBoxBackEnabled = false;   MediaBoxBack = false; }
    }

    private void SaveScraperSettings()
    {
        if (_settingsService is null) return;

        var key = ScraperSettingsKey;

        var values = new Dictionary<string, string>
        {
            [SettingKeys.SelectedScraper.Key] = CurrentScraper,
        };

        values[$"{key}_ScrapeAllMode"] = ScrapeAllMode.ToString();
        values[$"{key}_OverwriteName"] = OverwriteName.ToString();
        values[$"{key}_OverwriteMedia"] = OverwriteMedia.ToString();
        values[$"{key}_ScrapeHiddenItems"] = ScrapeHiddenItems.ToString();
        values[$"{key}_ScrapeFromCache"] = ScrapeFromCache.ToString();
        values[$"{key}_SkipNonCachedItems"] = SkipNonCachedItems.ToString();
        values[$"{key}_OverwriteMetadata"] = OverwriteMetadata.ToString();

        foreach (var (name, _, getValue, _) in GetBoolToggles())
            values[$"{key}_{name}"] = getValue().ToString();

        foreach (var (name, sources, getSelected, _) in GetSourceToggles())
        {
            int idx = getSelected();
            values[$"{key}_{name}"] = idx >= 0 && idx < sources.Count ? sources[idx] : "";
        }

        _settingsService.SaveAllSettings(new Dictionary<string, Dictionary<string, string>>
        {
            [SettingKeys.ScraperSection] = values
        });
    }


    private (string Name, Func<bool> GetEnabled, Func<bool> GetValue, Action<bool> SetValue)[] GetBoolToggles() =>
    [
        ("MetaName", () => MetaNameEnabled, () => MetaName, v => MetaName = v),
        ("MetaDescription", () => MetaDescriptionEnabled, () => MetaDescription, v => MetaDescription = v),
        ("MetaGenre", () => MetaGenreEnabled, () => MetaGenre, v => MetaGenre = v),
        ("MetaPlayers", () => MetaPlayersEnabled, () => MetaPlayers, v => MetaPlayers = v),
        ("MetaRating", () => MetaRatingEnabled, () => MetaRating, v => MetaRating = v),
        ("MetaRegion", () => MetaRegionEnabled, () => MetaRegion, v => MetaRegion = v),
        ("MetaLanguage", () => MetaLanguageEnabled, () => MetaLanguage, v => MetaLanguage = v),
        ("MetaReleaseDate", () => MetaReleaseDateEnabled, () => MetaReleaseDate, v => MetaReleaseDate = v),
        ("MetaDeveloper", () => MetaDeveloperEnabled, () => MetaDeveloper, v => MetaDeveloper = v),
        ("MetaPublisher", () => MetaPublisherEnabled, () => MetaPublisher, v => MetaPublisher = v),
        ("MetaArcadeName", () => MetaArcadeNameEnabled, () => MetaArcadeName, v => MetaArcadeName = v),
        ("MetaFamily", () => MetaFamilyEnabled, () => MetaFamily, v => MetaFamily = v),
        ("MetaGameId", () => MetaGameIdEnabled, () => MetaGameId, v => MetaGameId = v),
        ("MediaTitleshot", () => MediaTitleshotEnabled, () => MediaTitleshot, v => MediaTitleshot = v),
        ("MediaMap", () => MediaMapEnabled, () => MediaMap, v => MediaMap = v),
        ("MediaManual", () => MediaManualEnabled, () => MediaManual, v => MediaManual = v),
        ("MediaBezel", () => MediaBezelEnabled, () => MediaBezel, v => MediaBezel = v),
        ("MediaFanArt", () => MediaFanArtEnabled, () => MediaFanArt, v => MediaFanArt = v),
        ("MediaBoxBack", () => MediaBoxBackEnabled, () => MediaBoxBack, v => MediaBoxBack = v),
        ("MediaMusic", () => MediaMusicEnabled, () => MediaMusic, v => MediaMusic = v),
        ("MediaImage", () => MediaImageEnabled, () => MediaImage, v => MediaImage = v),
        ("MediaMarquee", () => MediaMarqueeEnabled, () => MediaMarquee, v => MediaMarquee = v),
        ("MediaThumbnail", () => MediaThumbnailEnabled, () => MediaThumbnail, v => MediaThumbnail = v),
        ("MediaCartridge", () => MediaCartridgeEnabled, () => MediaCartridge, v => MediaCartridge = v),
        ("MediaVideo", () => MediaVideoEnabled, () => MediaVideo, v => MediaVideo = v),
        ("MediaBoxArt", () => MediaBoxArtEnabled, () => MediaBoxArt, v => MediaBoxArt = v),
        ("MediaMix", () => MediaMixEnabled, () => MediaMix, v => MediaMix = v),
        ("MediaWheel", () => MediaWheelEnabled, () => MediaWheel, v => MediaWheel = v),
    ];

    private (string Name, ObservableCollection<string> Sources, Func<int> GetSelected, Action<int> SetSelected)[] GetSourceToggles() =>
    [
        ("ImageSource", ImageSources, () => SelectedImageSource, v => SelectedImageSource = v),
        ("MarqueeSource", MarqueeSources, () => SelectedMarqueeSource, v => SelectedMarqueeSource = v),
        ("ThumbnailSource", ThumbnailSources, () => SelectedThumbnailSource, v => SelectedThumbnailSource = v),
        ("CartridgeSource", CartridgeSources, () => SelectedCartridgeSource, v => SelectedCartridgeSource = v),
        ("VideoSource", VideoSources, () => SelectedVideoSource, v => SelectedVideoSource = v),
        ("BoxArtSource", BoxArtSources, () => SelectedBoxArtSource, v => SelectedBoxArtSource = v),
        ("MixSource", MixSources, () => SelectedMixSource, v => SelectedMixSource = v),
        ("WheelSource", WheelSources, () => SelectedWheelSource, v => SelectedWheelSource = v),
    ];
    #endregion

    #region Dispose
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _sharedData.PropertyChanged -= OnSharedDataPropertyChanged;
        _sharedData.SettingsApplied -= OnSettingsApplied;
        _cts?.Cancel();
        _cts?.Dispose();
        SaveScraperSettings();
        GC.SuppressFinalize(this);
    }
    #endregion
}