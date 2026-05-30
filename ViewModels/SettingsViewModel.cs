using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Messages;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Gamelist_Manager.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    #region Fields / Constants

    private readonly SessionState _sessionState = SessionState.Instance;
    private readonly SettingsState _settingsState = SettingsState.Instance;

    private bool _isProfileLoading;

    private static readonly string[] ThemeNames = ["Light", "Dark"];
    private static readonly string[] ColorNames =
    [
        "Blue", "Red", "Orange", "Green", "Yellow",
        "Magenta", "Purple", "Teal", "Lime", "Light Blue", "Indigo"
    ];
    private static readonly string[] AccentVariantNames = ["Base", "Lighter", "Darker"];

    #endregion

    #region Observable Properties

    // Appearance
    [ObservableProperty] private int _selectedThemeIndex;
    [ObservableProperty] private int _selectedColorIndex;
    [ObservableProperty] private int _selectedAccentVariantIndex;
    [ObservableProperty] private int _selectedAlternatingRowColorIndex;
    [ObservableProperty] private int _selectedGridLinesVisibilityIndex;
    [ObservableProperty] private int _appFontSize = 12;
    [ObservableProperty] private int _gridFontSize = 12;

    // Behavior / Options
    [ObservableProperty] private bool _confirmBulkChanges;
    [ObservableProperty] private bool _enableSaveReminder;
    [ObservableProperty] private bool _verifyImageDownloads;
    [ObservableProperty] private bool _overrideConcurrency;
    [ObservableProperty] private int _concurrencyOverride = 1;
    [ObservableProperty] private bool _videoAutoplay;
    [ObservableProperty] private bool _rememberColumns;
    [ObservableProperty] private bool _rememberAutosize;
    [ObservableProperty] private bool _enableDelete;
    [ObservableProperty] private bool _ignoreDuplicates;
    [ObservableProperty] private bool _batchProcessing;
    [ObservableProperty] private bool _removeZZZNotGamePrefix;
    [ObservableProperty] private bool _useSimpleSystemPicker;
    [ObservableProperty] private bool _saveWindowState;
    [ObservableProperty] private bool _showLogTimestamp;
    [ObservableProperty] private bool _logToDisk;
    [ObservableProperty] private bool _checkForNewAndMissingGamesOnLoad;

    [ObservableProperty] private int _scraperConfigSaveIndex;
    [ObservableProperty] private int _maxUndo;
    [ObservableProperty] private int _maxBatch;
    [ObservableProperty] private int _searchDepth;
    [ObservableProperty] private int _recentFilesCount;
    [ObservableProperty] private double _defaultVolume;
    [ObservableProperty] private int _logVerbosityIndex;

    // Remote
    [ObservableProperty] private string _hostname = string.Empty;
    [ObservableProperty] private string _userId = string.Empty;
    [ObservableProperty] private string _password = string.Empty;

    [ObservableProperty] private bool _settingsChanged;

    #endregion

    #region Public Properties

    public bool SuffixesEnabled => _sessionState.ProfileType == SettingKeys.ProfileTypeEs;
    public bool RomsPathVisible => _sessionState.ProfileType != SettingKeys.ProfileTypeEsDe;
    public bool EsDePathsVisible => _sessionState.ProfileType == SettingKeys.ProfileTypeEsDe;
    public bool RemoteTabVisible => _sessionState.ProfileType != SettingKeys.ProfileTypeEsDe;
    public bool MediaPathsReadOnly => _sessionState.ProfileType == SettingKeys.ProfileTypeEsDe;
    public bool ResetToDefaultsVisible => _sessionState.ProfileType != SettingKeys.ProfileTypeEsDe;

    #endregion

    #region Property Change Callbacks

    partial void OnSelectedThemeIndexChanged(int value)
    {
        if (_isProfileLoading) return;
        SelectedAlternatingRowColorIndex = 0;
    }

    #endregion

    #region Constructor

    public SettingsViewModel()
    {
        _isProfileLoading = true;
        try
        {
            InitializeMediaFolderItems();
            LoadSettings();
            RefreshMediaFolderDisplayState();
        }
        finally
        {
            _isProfileLoading = false;
        }

        _sessionState.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SessionState.ProfileType))
            {
                OnPropertyChanged(nameof(SuffixesEnabled));
                OnPropertyChanged(nameof(RomsPathVisible));
                OnPropertyChanged(nameof(EsDePathsVisible));
                OnPropertyChanged(nameof(RemoteTabVisible));
                OnPropertyChanged(nameof(MediaPathsReadOnly));
                OnPropertyChanged(nameof(ResetToDefaultsVisible));
            }
            else if (e.PropertyName == nameof(SessionState.CurrentSystem))
            {
                OnPropertyChanged(nameof(CanOverrideSystem));
                OnPropertyChanged(nameof(SystemOverrideLabel));
            }
        };

        PropertyChanged += (_, e) =>
        {
            if (_isProfileLoading) return;

            if (e.PropertyName is nameof(SettingsChanged)
                               or nameof(SystemOverrideActive)
                               or nameof(SelectedProfileName)
                               or nameof(SelectedProfileIsEsDe)
                               or nameof(SelectedProfileEsDeMediaRoot)
                               or nameof(SelectedSetupScraperIndex)
                               or nameof(IsSetupScreenScraper)
                               or nameof(IsSetupRequiresCredentials)
                               or nameof(IsSetupArcadeDB)
                               or nameof(ScraperUsername)
                               or nameof(ScraperPassword)
                               or nameof(ShowScraperPassword))
                return;

            SettingsChanged = true;
        };

        SettingsChanged = false;
    }

    #endregion

    #region Load / Save / Reset

    public void LoadSettings()
    {
        var s = SettingsState.Instance;
        var isEsDe = _sessionState.ProfileType == SettingKeys.ProfileTypeEsDe;

        // Appearance
        SelectedThemeIndex = NameToIndex(ThemeNames, s.Theme);
        SelectedColorIndex = NameToIndex(ColorNames, s.Color);
        SelectedAccentVariantIndex = NameToIndex(AccentVariantNames, s.AccentVariant);
        SelectedAlternatingRowColorIndex = s.AlternatingRowColorIndex;
        SelectedGridLinesVisibilityIndex = s.GridLinesVisibilityIndex;
        AppFontSize = s.AppFontSize;
        GridFontSize = s.GridFontSize;

        // Behavior
        ConfirmBulkChanges = s.ConfirmBulkChanges;
        EnableSaveReminder = s.EnableSaveReminder;
        VerifyImageDownloads = s.VerifyImageDownloads;
        OverrideConcurrency = s.OverrideConcurrency;
        ConcurrencyOverride = s.ConcurrencyOverride;
        VideoAutoplay = s.VideoAutoplay;
        RememberColumns = s.RememberColumns;
        RememberAutosize = s.RememberAutosize;
        EnableDelete = s.EnableDelete;
        RemoveZZZNotGamePrefix = s.RemoveZZZNotGamePrefix;
        IgnoreDuplicates = s.IgnoreDuplicates;
        BatchProcessing = s.BatchProcessing;
        ShowLogTimestamp = s.ShowLogTimestamp;
        LogToDisk = s.LogToDisk;
        CheckForNewAndMissingGamesOnLoad = s.CheckForNewAndMissingGamesOnLoad;
        UseSimpleSystemPicker = s.UseSimpleSystemPicker;
        SaveWindowState = s.SaveWindowState;

        ScraperConfigSaveIndex = s.ScraperConfigSave;
        MaxUndo = s.MaxUndo;
        SearchDepth = s.SearchDepth;
        RecentFilesCount = s.RecentFilesCount;
        MaxBatch = s.MaxBatch;
        DefaultVolume = s.DefaultVolume;
        LogVerbosityIndex = s.LogVerbosity;

        // Connection
        Hostname = s.Hostname;
        UserId = s.UserId;
        Password = s.Password;

        // Paths
        MamePath = s.MamePath;
        RomsPath = s.RomsFolder;

        foreach (var item in MediaFolderItems)
        {
            item.Path = LoadMediaPath(s.MediaPaths.GetValueOrDefault(item.Key, item.DefaultPath), item.DefaultPath);
            item.Suffix = s.MediaPaths.GetValueOrDefault($"{item.Key}_suffix", item.DefaultSuffix);
            item.SfxEnabled = bool.TryParse(s.MediaPaths.GetValueOrDefault($"{item.Key}_sfx_enabled"), out var sfx) && sfx;
            // TODO : This logic is a bit convoluted - consider refactoring to be clearer
            item.Enabled = (!isEsDe || (MetadataService.GetDeclByType(item.Key)?.IsEsDeSupported ?? false)) &&
                           (bool.TryParse(s.MediaPaths.GetValueOrDefault($"{item.Key}_enabled"), out var en) ? en : item.DefaultEnabled);
        }

        _isProfileLoading = true;
        try
        {
            SystemOverrideActive = SystemHasOverrides;
            if (SystemOverrideActive)
                LoadSystemOverrides();
        }
        finally
        {
            _isProfileLoading = false;
        }

        RefreshSystemsWithOverrides();
        LoadScraperCredentials();
        RefreshProfileList();

        if (isEsDe)
        {
            EsDeRoot = s.EsDeRoot;
            EsDeMediaBase = _sessionState.MediaRootFolder ?? string.Empty;
        }


        SettingsChanged = false;
    }

    public void SaveSettings()
    {
        var settings = new Dictionary<string, Dictionary<string, string>>
        {
            [SettingKeys.AppearanceSection] = new()
            {
                [SettingKeys.Theme.Key] = IndexToName(ThemeNames, SelectedThemeIndex),
                [SettingKeys.Color.Key] = IndexToName(ColorNames, SelectedColorIndex),
                [SettingKeys.AccentVariant.Key] = IndexToName(AccentVariantNames, SelectedAccentVariantIndex),
                [SettingKeys.AlternatingRowColorIndex.Key] = SelectedAlternatingRowColorIndex.ToString(),
                [SettingKeys.GridLinesVisibilityIndex.Key] = SelectedGridLinesVisibilityIndex.ToString(),
                [SettingKeys.GridLineVisibility.Key] = SelectedGridLinesVisibilityIndex switch
                {
                    0 => "Horizontal",
                    1 => "Vertical",
                    2 => "All",
                    3 => "None",
                    _ => "Horizontal"
                },
                [SettingKeys.GlobalFontSize.Key] = AppFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [SettingKeys.GridFontSize.Key] = GridFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            [SettingKeys.BehaviorSection] = new()
            {
                [SettingKeys.ConfirmBulkChange.Key] = ConfirmBulkChanges.ToString(),
                [SettingKeys.SaveReminder.Key] = EnableSaveReminder.ToString(),
                [SettingKeys.OverrideConcurrency.Key] = OverrideConcurrency.ToString(),
                [SettingKeys.ConcurrencyOverride.Key] = ConcurrencyOverride.ToString(),
                [SettingKeys.VideoAutoplay.Key] = VideoAutoplay.ToString(),
                [SettingKeys.RememberColumns.Key] = RememberColumns.ToString(),
                [SettingKeys.RememberAutoSize.Key] = RememberAutosize.ToString(),
                [SettingKeys.EnableDelete.Key] = EnableDelete.ToString(),
                [SettingKeys.IgnoreDuplicates.Key] = IgnoreDuplicates.ToString(),
                [SettingKeys.CheckForNewAndMissingGamesOnLoad.Key] = CheckForNewAndMissingGamesOnLoad.ToString(),
                [SettingKeys.UseSimpleSystemPicker.Key] = UseSimpleSystemPicker.ToString(),
                [SettingKeys.SaveWindowState.Key] = SaveWindowState.ToString()
            },
            [SettingKeys.AdvancedSection] = new()
            {
                [SettingKeys.MaxUndo.Key] = MaxUndo.ToString(),
                [SettingKeys.SearchDepth.Key] = SearchDepth.ToString(),
                [SettingKeys.RecentFilesCount.Key] = RecentFilesCount.ToString(),
                [SettingKeys.BatchProcessingMaximum.Key] = MaxBatch.ToString(),
                [SettingKeys.LogVerbosity.Key] = LogVerbosityIndex.ToString(),
                [SettingKeys.Volume.Key] = DefaultVolume.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            [SettingKeys.ConnectionSection] = new()
            {
                [SettingKeys.HostName.Key] = Hostname,
                [SettingKeys.UserID.Key] = UserId,
                [SettingKeys.Password.Key] = Password
            },
            [SettingKeys.FolderPathsSection] = new()
            {
                [SettingKeys.MamePath.Key] = MamePath,
                [SettingKeys.RomsFolder.Key] = RomsPath
            },
            [SettingKeys.MediaPathsSection] = _sessionState.ProfileType == SettingKeys.ProfileTypeEsDe
            ? MediaFolderItems
                .Where(_ => !SystemOverrideActive)  // don't overwrite global enabled when override is active
                .Select(item => new KeyValuePair<string, string>($"{item.Key}_enabled", item.Enabled.ToString()))
                .ToDictionary(kv => kv.Key, kv => kv.Value)
            : MediaFolderItems
                .SelectMany(item => new[]
                {
                    new KeyValuePair<string, string>(item.Key, item.Path),
                    new KeyValuePair<string, string>($"{item.Key}_suffix", item.Suffix),
                    new KeyValuePair<string, string>($"{item.Key}_sfx_enabled", item.SfxEnabled.ToString()),
                }
                .Concat(SystemOverrideActive
                    ? []
                    : [new KeyValuePair<string, string>($"{item.Key}_enabled", item.Enabled.ToString())]))
                .ToDictionary(kv => kv.Key, kv => kv.Value)};

        if (IsSetupRequiresCredentials &&
            !string.IsNullOrWhiteSpace(ScraperUsername) &&
            !string.IsNullOrWhiteSpace(ScraperPassword))
        {
            CredentialHelper.SaveCredentials(SetupScraperName, ScraperUsername, ScraperPassword);
        }

        if (IsSetupScreenScraper)
        {
            settings[SettingKeys.ScraperOptionsSection] = new()
            {
                [SettingKeys.ScreenScraperLanguage.Key] = SelectedScraperLanguage ?? string.Empty,
                [SettingKeys.ScreenScraperPrimaryRegion.Key] = SelectedScraperPrimaryRegion ?? string.Empty,
                [SettingKeys.ScreenScraperGenreEnglish.Key] = ScraperGenreAlwaysEnglish.ToString(),
                [SettingKeys.ScreenScraperAnyMedia.Key] = ScraperScrapeAnyMedia.ToString(),
                [SettingKeys.ScreenScraperNamesLanguageFirst.Key] = ScraperNamesLanguageFirst.ToString(),
                [SettingKeys.ScreenScraperMediaRegionFirst.Key] = ScraperMediaRegionFirst.ToString(),
                [SettingKeys.ScreenScraperRegionFallback.Key] = JsonSerializer.Serialize(ScraperFallbackRegions.ToList()),
                [SettingKeys.RemoveZZZNotGamePrefix.Key] = RemoveZZZNotGamePrefix.ToString(),
                [SettingKeys.ScraperConfigSave.Key] = ScraperConfigSaveIndex.ToString(),
                [SettingKeys.VerifyDownloadedImages.Key] = VerifyImageDownloads.ToString(),
                [SettingKeys.BatchProcessing.Key] = BatchProcessing.ToString(),
                [SettingKeys.ShowLogTimestamp.Key] = ShowLogTimestamp.ToString(),
                [SettingKeys.LogToDisk.Key] = LogToDisk.ToString(),
                [SettingKeys.OverrideConcurrency.Key] = OverrideConcurrency.ToString(),
                [SettingKeys.ConcurrencyOverride.Key] = ConcurrencyOverride.ToString(),
            };
        }
        else
        {
            settings[SettingKeys.ScraperOptionsSection] = new()
            {
                [SettingKeys.RemoveZZZNotGamePrefix.Key] = RemoveZZZNotGamePrefix.ToString(),
                [SettingKeys.ScraperConfigSave.Key] = ScraperConfigSaveIndex.ToString(),
                [SettingKeys.VerifyDownloadedImages.Key] = VerifyImageDownloads.ToString(),
                [SettingKeys.BatchProcessing.Key] = BatchProcessing.ToString(),
                [SettingKeys.ShowLogTimestamp.Key] = ShowLogTimestamp.ToString(),
                [SettingKeys.LogToDisk.Key] = LogToDisk.ToString(),
                [SettingKeys.OverrideConcurrency.Key] = OverrideConcurrency.ToString(),
                [SettingKeys.ConcurrencyOverride.Key] = ConcurrencyOverride.ToString(),
            };
        }

        settings[SettingKeys.ProfileSection] = new()
        {
            [SettingKeys.ProfileType.Key] = _sessionState.ProfileType,
        };

        if (_sessionState.ProfileType == SettingKeys.ProfileTypeEsDe)
        {
            settings[SettingKeys.EsDeSection] = new()
            {
                [SettingKeys.EsDeRoot.Key] = EsDeRoot,
            };
        }

        ProfileService.Instance.Save(settings);
        if (SystemOverrideActive && !string.IsNullOrEmpty(_sessionState.CurrentSystem))
            SaveSystemOverrides();

        SettingsState.Instance.Reload();
        _sessionState.RefreshAvailableMedia(
            _sessionState.ProfileType,
            _sessionState.CurrentSystem,
            _sessionState.CurrentMediaFolder,
            _settingsState.MediaPaths);
        ThemeService.ApplyTheme(SelectedThemeIndex, SelectedColorIndex, SelectedAccentVariantIndex);
        WeakReferenceMessenger.Default.Send(new SettingsAppliedMessage());
        SettingsChanged = false;
    }

    public void ResetAllSettings()
    {
        SettingsService.Instance.ResetToDefaults();
        _isProfileLoading = true;
        try
        {
            LoadSettings();
            RefreshMediaFolderDisplayState();
        }
        finally
        {
            _isProfileLoading = false;
        }
    }

    public static void ApplyThemeOnStartup()
    {
        var settings = SettingsState.Instance;
        var themeIndex = NameToIndex(ThemeNames, settings.Theme);
        var colorIndex = NameToIndex(ColorNames, settings.Color);
        var variantIndex = NameToIndex(AccentVariantNames, settings.AccentVariant);

        ThemeService.ApplyTheme(themeIndex, colorIndex, variantIndex);
        ThemeService.ApplyFontSizes(settings.AppFontSize, settings.GridFontSize);
    }

    #endregion

    #region Private Helpers

    private static int NameToIndex(string[] names, string name)
    {
        var index = Array.IndexOf(names, name);
        return index >= 0 ? index : 0;
    }

    private static string IndexToName(string[] names, int index)
    {
        return (uint)index < (uint)names.Length ? names[index] : names[0];
    }

    #endregion
}