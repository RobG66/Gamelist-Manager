using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class ScraperViewModel : ViewModelBase, IDisposable
{
    #region Services

    private readonly SettingsService _settingsService = SettingsService.Instance;
    private readonly SessionState _sessionState = SessionState.Instance;
    private readonly SettingsState _settingsState = SettingsState.Instance;

    #endregion

    #region Private Fields

    private bool _isProfileLoading;
    private bool _isDisposed;
    private CancellationTokenSource? _cts;
    private DateTime _startTime;
    private DateTime _lastProgressUpdate;
    private int _scrapeSuccessCount;
    private int _scrapeFailedCount;
    private int _dlSuccessCount;
    private int _dlFailedCount;
    private bool _isStopping;
    private string _previousSystem = string.Empty;

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
    public bool IsBusy => _sessionState.IsBusy;

    #endregion

    #region Private Properties

    private int ScraperConfigSaveMode => _settingsState.ScraperConfigSave;

    private string ScraperSettingsKey => ScraperConfigSaveMode == 1 && !string.IsNullOrEmpty(_sessionState.CurrentSystem)
        ? $"{CurrentScraper}_{_sessionState.CurrentSystem}"
        : CurrentScraper;

    #endregion

    #region Constructor

    public ScraperViewModel()
    {
        _sessionState.PropertyChanged += OnSessionStatePropertyChanged;
        _settingsState.PropertyChanged += OnSettingsStatePropertyChanged;

        _isProfileLoading = true;
        LoadScraperSettings();
        _isProfileLoading = false;
        _previousSystem = _sessionState.CurrentSystem ?? string.Empty;
        RefreshCacheCount();
        Log("Ready to scrape...", LogLevel.Info);
    }

    #endregion

    #region Event Handlers

    private void OnSettingsStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SettingsState.ShowLogTimestamp):
                ShowLogTimestamp = _settingsState.ShowLogTimestamp;
                break;
        }
    }

    private void OnSessionStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SessionState.CurrentSystem):
                if (ScraperConfigSaveMode == 1 && !IsScraping)
                    SaveScraperSettings(BuildScraperKey(CurrentScraper, _previousSystem));
                _previousSystem = _sessionState.CurrentSystem ?? string.Empty;
                RefreshCacheCount();
                break;
            case nameof(SessionState.IsBusy):
                OnPropertyChanged(nameof(IsBusy));
                break;
            case nameof(SessionState.AvailableMedia):
                if (!IsScraping)
                    LoadScraperSettings();
                break;
            case nameof(SessionState.IsScraping):
                if (IsScraping != _sessionState.IsScraping)
                    IsScraping = _sessionState.IsScraping;
                break;
        }
    }

    #endregion

    #region Property Change Callbacks

    partial void OnIsScrapingChanged(bool value)
    {
        _sessionState.IsScraping = value;
        if (value) IsClearCacheEnabled = false;
    }

    partial void OnCurrentScraperChanging(string value)
    {
        if (_isProfileLoading) return;
        SaveScraperSettings();
    }

    partial void OnCurrentScraperChanged(string value)
    {
        if (_isProfileLoading) return;
        LoadScraperSettings();
        RefreshCacheCount();
        Log($"Scraper changed to: {value}", LogLevel.Status);
    }

    partial void OnScrapeFromCacheChanged(bool value)
    {
        if (_isProfileLoading || !ScrapeFromCacheEnabled) return;
        SkipNonCachedItemsEnabled = value;
        if (!value) SkipNonCachedItems = false;
    }

    #endregion

    #region Private Methods

    private void RefreshCacheCount()
    {
        if (CurrentScraper == ScraperRegistry.EmuMovies.Name)
        {
            IsClearCacheEnabled = false;
            return;
        }

        var system = _sessionState.CurrentSystem ?? string.Empty;
        if (string.IsNullOrEmpty(system))
        {
            CacheCountText = "0 items cached";
            IsClearCacheEnabled = false;
            return;
        }

        var cacheFolder = Path.Combine(AppContext.BaseDirectory, "cache", CurrentScraper, system);
        var files = Directory.Exists(cacheFolder) ? Directory.GetFiles(cacheFolder) : [];

        if (files.Length == 0)
        {
            IsClearCacheEnabled = false;
            CacheCountText = "Cache is empty";
        }
        else
        {
            CacheCountText = $"{files.Length} items cached";
            IsClearCacheEnabled = true;
        }
    }

    private void LoadScraperSettings()
    {
        var key = ScraperSettingsKey;

        if (_isProfileLoading)
        {
            var savedScraper = _settingsState.SelectedScraper;
            CurrentScraper = ScraperRegistry.Find(savedScraper)?.Name ?? ScraperRegistry.ArcadeDB.Name;
            key = ScraperSettingsKey;
        }

        LoadGeneralSettings(key);
        ApplyScraperCapabilities();
        RestoreToggleValues(key);
    }

    private void LoadGeneralSettings(string key)
    {
        ScrapeAllMode = _settingsService.GetBool(SettingKeys.ScrapersSection, $"{key}_ScrapeAllMode", SettingKeys.ScrapeAllMode.Default);
        ScrapeSelectedMode = !ScrapeAllMode;
        OverwriteName = _settingsService.GetBool(SettingKeys.ScrapersSection, $"{key}_OverwriteName", SettingKeys.OverwriteName.Default);
        OverwriteMedia = _settingsService.GetBool(SettingKeys.ScrapersSection, $"{key}_OverwriteMedia", SettingKeys.OverwriteMedia.Default);
        ScrapeHiddenItems = _settingsService.GetBool(SettingKeys.ScrapersSection, $"{key}_ScrapeHiddenItems", SettingKeys.ScrapeHiddenItems.Default);
        ShowLogTimestamp = _settingsState.ShowLogTimestamp;

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
    }

    private void ApplyScraperCapabilities()
    {
        var scraperElements = MetadataService.GetScraperElements(CurrentScraper);
        var availableMedia = _sessionState.AvailableMedia;
        bool HasMedia(string type) => availableMedia.Any(m => m.Type == type);

        foreach (var t in GetBoolToggles())
        {
            bool supported = scraperElements.Contains(t.ElementKey);
            t.SetEnabled(supported);
            if (!supported) t.SetValue(false);
        }

        foreach (var s in GetSourceToggles())
        {
            ApplySource(s.Sources, s.Name, s.SetSelected, out bool enabled);
            var toggle = GetBoolToggles().FirstOrDefault(t => t.ElementKey == s.AvailabilityType);
            toggle.SetEnabled(enabled);
            if (!enabled) toggle.SetValue(false);
        }

        foreach (var t in GetBoolToggles().Where(t => t.IsMedia))
        {
            if (!HasMedia(t.ElementKey)) { t.SetEnabled(false); t.SetValue(false); }
        }
        foreach (var s in GetSourceToggles())
        {
            if (!HasMedia(s.AvailabilityType))
            {
                var toggle = GetBoolToggles().FirstOrDefault(t => t.ElementKey == s.AvailabilityType);
                toggle.SetEnabled(false);
                toggle.SetValue(false);
            }
        }
    }

    private void RestoreToggleValues(string key)
    {
        if (ScrapeFromCacheEnabled)
            ScrapeFromCache = _settingsService.GetBool(SettingKeys.ScrapersSection, $"{key}_ScrapeFromCache", ScrapeFromCache);
        if (SkipNonCachedItemsEnabled)
            SkipNonCachedItems = _settingsService.GetBool(SettingKeys.ScrapersSection, $"{key}_SkipNonCachedItems", false);
        if (OverwriteMetadataEnabled)
            OverwriteMetadata = _settingsService.GetBool(SettingKeys.ScrapersSection, $"{key}_OverwriteMetadata", false);

        foreach (var t in GetBoolToggles())
        {
            if (!t.GetEnabled()) continue;
            t.SetValue(_settingsService.GetBool(SettingKeys.ScrapersSection, $"{key}_{t.Name}", false));
        }

        foreach (var s in GetSourceToggles())
        {
            var savedValue = _settingsService.GetValue(SettingKeys.ScrapersSection, $"{key}_{s.Name}", "");
            RestoreSource(s.Sources, savedValue, s.SetSelected);
        }

        if (ScrapeFromCacheEnabled)
        {
            SkipNonCachedItemsEnabled = ScrapeFromCache;
            if (!ScrapeFromCache) SkipNonCachedItems = false;
        }
    }

    private string BuildScraperKey(string scraper, string system)
        => ScraperConfigSaveMode == 1 && !string.IsNullOrEmpty(system)
            ? $"{scraper}_{system}"
            : scraper;

    private void SaveScraperSettings() => SaveScraperSettings(ScraperSettingsKey);

    private void SaveScraperSettings(string key)
    {
        var panelValues = new Dictionary<string, string>
        {
            [$"{key}_ScrapeAllMode"] = ScrapeAllMode.ToString(),
            [$"{key}_OverwriteName"] = OverwriteName.ToString(),
            [$"{key}_OverwriteMedia"] = OverwriteMedia.ToString(),
            [$"{key}_ScrapeHiddenItems"] = ScrapeHiddenItems.ToString(),
            [$"{key}_ScrapeFromCache"] = ScrapeFromCache.ToString(),
            [$"{key}_SkipNonCachedItems"] = SkipNonCachedItems.ToString(),
            [$"{key}_OverwriteMetadata"] = OverwriteMetadata.ToString(),
        };

        foreach (var t in GetBoolToggles())
            panelValues[$"{key}_{t.Name}"] = t.GetValue().ToString();

        foreach (var s in GetSourceToggles())
        {
            var idx = s.GetSelected();
            panelValues[$"{key}_{s.Name}"] = idx >= 0 && idx < s.Sources.Count ? s.Sources[idx] : "";
        }

        ProfileService.Instance.Save(new Dictionary<string, Dictionary<string, string>>
        {
            [SettingKeys.ScraperOptionsSection] = new() { [SettingKeys.SelectedScraper.Key] = CurrentScraper },
            [SettingKeys.ScrapersSection] = panelValues
        });
    }

    private (string Name, string ElementKey, bool IsMedia, Func<bool> GetEnabled, Action<bool> SetEnabled, Func<bool> GetValue, Action<bool> SetValue)[] GetBoolToggles() =>
    [
        ("MetaName",        "name",             false, () => MetaNameEnabled,        v => MetaNameEnabled = v,        () => MetaName,        v => MetaName = v),
        ("MetaDescription", "desc",             false, () => MetaDescriptionEnabled, v => MetaDescriptionEnabled = v, () => MetaDescription, v => MetaDescription = v),
        ("MetaGenre",       "genre",            false, () => MetaGenreEnabled,       v => MetaGenreEnabled = v,       () => MetaGenre,       v => MetaGenre = v),
        ("MetaPlayers",     "players",          false, () => MetaPlayersEnabled,     v => MetaPlayersEnabled = v,     () => MetaPlayers,     v => MetaPlayers = v),
        ("MetaRating",      "rating",           false, () => MetaRatingEnabled,      v => MetaRatingEnabled = v,      () => MetaRating,      v => MetaRating = v),
        ("MetaRegion",      "region",           false, () => MetaRegionEnabled,      v => MetaRegionEnabled = v,      () => MetaRegion,      v => MetaRegion = v),
        ("MetaLanguage",    "lang",             false, () => MetaLanguageEnabled,    v => MetaLanguageEnabled = v,    () => MetaLanguage,    v => MetaLanguage = v),
        ("MetaReleaseDate", "releasedate",      false, () => MetaReleaseDateEnabled, v => MetaReleaseDateEnabled = v, () => MetaReleaseDate, v => MetaReleaseDate = v),
        ("MetaDeveloper",   "developer",        false, () => MetaDeveloperEnabled,   v => MetaDeveloperEnabled = v,   () => MetaDeveloper,   v => MetaDeveloper = v),
        ("MetaPublisher",   "publisher",        false, () => MetaPublisherEnabled,   v => MetaPublisherEnabled = v,   () => MetaPublisher,   v => MetaPublisher = v),
        ("MetaArcadeName",  "arcadesystemname", false, () => MetaArcadeNameEnabled,  v => MetaArcadeNameEnabled = v,  () => MetaArcadeName,  v => MetaArcadeName = v),
        ("MetaFamily",      "family",           false, () => MetaFamilyEnabled,      v => MetaFamilyEnabled = v,      () => MetaFamily,      v => MetaFamily = v),
        ("MetaGameId",      "id",               false, () => MetaGameIdEnabled,      v => MetaGameIdEnabled = v,      () => MetaGameId,      v => MetaGameId = v),
        ("MediaTitleshot",  "titleshot",        true,  () => MediaTitleshotEnabled,  v => MediaTitleshotEnabled = v,  () => MediaTitleshot,  v => MediaTitleshot = v),
        ("MediaMap",        "map",              true,  () => MediaMapEnabled,        v => MediaMapEnabled = v,        () => MediaMap,        v => MediaMap = v),
        ("MediaManual",     "manual",           true,  () => MediaManualEnabled,     v => MediaManualEnabled = v,     () => MediaManual,     v => MediaManual = v),
        ("MediaBezel",      "bezel",            true,  () => MediaBezelEnabled,      v => MediaBezelEnabled = v,      () => MediaBezel,      v => MediaBezel = v),
        ("MediaFanArt",     "fanart",           true,  () => MediaFanArtEnabled,     v => MediaFanArtEnabled = v,     () => MediaFanArt,     v => MediaFanArt = v),
        ("MediaBoxBack",    "boxback",          true,  () => MediaBoxBackEnabled,    v => MediaBoxBackEnabled = v,    () => MediaBoxBack,    v => MediaBoxBack = v),
        ("MediaMusic",      "music",            true,  () => MediaMusicEnabled,      v => MediaMusicEnabled = v,      () => MediaMusic,      v => MediaMusic = v),
        ("MediaImage",      "image",            true,  () => MediaImageEnabled,      v => MediaImageEnabled = v,      () => MediaImage,      v => MediaImage = v),
        ("MediaMarquee",    "marquee",          true,  () => MediaMarqueeEnabled,    v => MediaMarqueeEnabled = v,    () => MediaMarquee,    v => MediaMarquee = v),
        ("MediaThumbnail",  "thumbnail",        true,  () => MediaThumbnailEnabled,  v => MediaThumbnailEnabled = v,  () => MediaThumbnail,  v => MediaThumbnail = v),
        ("MediaCartridge",  "cartridge",        true,  () => MediaCartridgeEnabled,  v => MediaCartridgeEnabled = v,  () => MediaCartridge,  v => MediaCartridge = v),
        ("MediaVideo",      "video",            true,  () => MediaVideoEnabled,      v => MediaVideoEnabled = v,      () => MediaVideo,      v => MediaVideo = v),
        ("MediaBoxArt",     "boxart",           true,  () => MediaBoxArtEnabled,     v => MediaBoxArtEnabled = v,     () => MediaBoxArt,     v => MediaBoxArt = v),
        ("MediaMix",        "mix",              true,  () => MediaMixEnabled,        v => MediaMixEnabled = v,        () => MediaMix,        v => MediaMix = v),
        ("MediaWheel",      "wheel",            true,  () => MediaWheelEnabled,      v => MediaWheelEnabled = v,      () => MediaWheel,      v => MediaWheel = v),
    ];

    private (string Name, string AvailabilityType, ObservableCollection<string> Sources, Func<int> GetSelected, Action<int> SetSelected)[] GetSourceToggles() =>
    [
        ("ImageSource",     "image",     ImageSources,     () => SelectedImageSource,     v => SelectedImageSource = v),
        ("MarqueeSource",   "marquee",   MarqueeSources,   () => SelectedMarqueeSource,   v => SelectedMarqueeSource = v),
        ("ThumbnailSource", "thumbnail", ThumbnailSources, () => SelectedThumbnailSource, v => SelectedThumbnailSource = v),
        ("CartridgeSource", "cartridge", CartridgeSources, () => SelectedCartridgeSource, v => SelectedCartridgeSource = v),
        ("VideoSource",     "video",     VideoSources,     () => SelectedVideoSource,     v => SelectedVideoSource = v),
        ("BoxArtSource",    "boxart",    BoxArtSources,    () => SelectedBoxArtSource,    v => SelectedBoxArtSource = v),
        ("MixSource",       "mix",       MixSources,       () => SelectedMixSource,       v => SelectedMixSource = v),
        ("WheelSource",     "wheel",     WheelSources,     () => SelectedWheelSource,     v => SelectedWheelSource = v),
    ];

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

    #endregion

    #region Commands

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
        var system = _sessionState.CurrentSystem ?? string.Empty;
        if (string.IsNullOrEmpty(system))
        {
            Log("No system loaded, cannot clear cache.", LogLevel.Warning);
            return;
        }

        var cacheFolder = Path.Combine(AppContext.BaseDirectory, "cache", CurrentScraper, system);
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
            catch (Exception ex) { Log($"Failed to delete {Path.GetFileName(file)}: {ex.Message}", LogLevel.Warning); }
        }

        RefreshCacheCount();
        Log($"Cleared {deleted} cached file(s).", LogLevel.Warning);
    }

    [RelayCommand]
    private async Task OpenScraperSetup()
    {
        var scraperIndex = ScraperRegistry.All
            .Select((s, i) => (s, i))
            .FirstOrDefault(x => string.Equals(x.s.Name, CurrentScraper, StringComparison.OrdinalIgnoreCase))
            .i;

        await WindowService.Instance.ShowSettingsAsync(4, scraperIndex);
    }

    [RelayCommand]
    private void SelectAllMetadata()
    {
        foreach (var t in GetBoolToggles().Where(t => !t.IsMedia && t.GetEnabled()))
            t.SetValue(true);
    }

    [RelayCommand]
    private void SelectNoneMetadata()
    {
        foreach (var t in GetBoolToggles().Where(t => !t.IsMedia))
            t.SetValue(false);
    }

    [RelayCommand]
    private void SelectAllMedia()
    {
        foreach (var t in GetBoolToggles().Where(t => t.IsMedia && t.GetEnabled()))
            t.SetValue(true);
    }

    [RelayCommand]
    private void SelectNoneMedia()
    {
        foreach (var t in GetBoolToggles().Where(t => t.IsMedia))
            t.SetValue(false);
    }

    [RelayCommand]
    private void ResetSources()
    {
        foreach (var s in GetSourceToggles().Where(s => GetBoolToggles().Any(t => t.ElementKey == s.AvailabilityType && t.GetEnabled())))
            s.SetSelected(0);
    }

    #endregion

    #region Command Guards

    private bool CanStart => !IsScraping;
    private bool CanStop => IsScraping && !_isStopping;

    #endregion

    #region Dispose

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _sessionState.PropertyChanged -= OnSessionStatePropertyChanged;
        _settingsState.PropertyChanged -= OnSettingsStatePropertyChanged;
        _cts?.Cancel();
        _cts?.Dispose();
        SaveScraperSettings();
        GC.SuppressFinalize(this);
    }

    #endregion
}