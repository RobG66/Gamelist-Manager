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
    [ObservableProperty] private bool _removeZzzNotGamePrefix;
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

    private ProfileTypeOption ActiveProfile => SettingKeys.GetProfileTypeOption(_settingsState.ProfileType);

    public bool AreSuffixesAllowed => ActiveProfile.MediaFilenamesUseSuffixes;
    public bool RomsPathVisible => ActiveProfile.ShowsRomsPathInFolderPaths;
    public bool EsDePathsVisible => ActiveProfile.ShowsEsDePathsSection;
    public bool RemoteTabVisible => ActiveProfile.ShowsRemoteTab;
    public bool MediaPathsReadOnly => !ActiveProfile.GamelistHasMediaPaths;
    public bool ResetToDefaultsVisible => ActiveProfile.ShowsResetToDefaults;

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

        _settingsState.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsState.ProfileType))
            {
                OnPropertyChanged(nameof(AreSuffixesAllowed));
                OnPropertyChanged(nameof(RomsPathVisible));
                OnPropertyChanged(nameof(EsDePathsVisible));
                OnPropertyChanged(nameof(RemoteTabVisible));
                OnPropertyChanged(nameof(MediaPathsReadOnly));
                OnPropertyChanged(nameof(ResetToDefaultsVisible));
                RefreshMediaFolderDisplayState();
            }
        };

        _sessionState.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SessionState.CurrentSystem))
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

    #region Public Methods

    public void LoadSettings()
    {
        var profile = ActiveProfile;

        // Appearance
        SelectedThemeIndex = NameToIndex(ThemeNames, _settingsState.Theme);
        SelectedColorIndex = NameToIndex(ColorNames, _settingsState.Color);
        SelectedAccentVariantIndex = NameToIndex(AccentVariantNames, _settingsState.AccentVariant);
        SelectedAlternatingRowColorIndex = _settingsState.AlternatingRowColorIndex;
        SelectedGridLinesVisibilityIndex = _settingsState.GridLinesVisibilityIndex;
        AppFontSize = _settingsState.AppFontSize;
        GridFontSize = _settingsState.GridFontSize;

        // Behavior
        ConfirmBulkChanges = _settingsState.ConfirmBulkChanges;
        EnableSaveReminder = _settingsState.EnableSaveReminder;
        VerifyImageDownloads = _settingsState.VerifyImageDownloads;
        OverrideConcurrency = _settingsState.OverrideConcurrency;
        ConcurrencyOverride = _settingsState.ConcurrencyOverride;
        VideoAutoplay = _settingsState.VideoAutoplay;
        RememberColumns = _settingsState.RememberColumns;
        RememberAutosize = _settingsState.RememberAutosize;
        EnableDelete = _settingsState.EnableDelete;
        RemoveZzzNotGamePrefix = _settingsState.RemoveZzzNotGamePrefix;
        IgnoreDuplicates = _settingsState.IgnoreDuplicates;
        BatchProcessing = _settingsState.BatchProcessing;
        ShowLogTimestamp = _settingsState.ShowLogTimestamp;
        LogToDisk = _settingsState.LogToDisk;
        CheckForNewAndMissingGamesOnLoad = _settingsState.CheckForNewAndMissingGamesOnLoad;
        UseSimpleSystemPicker = _settingsState.UseSimpleSystemPicker;
        SaveWindowState = _settingsState.SaveWindowState;

        ScraperConfigSaveIndex = _settingsState.ScraperConfigSave;
        MaxUndo = _settingsState.MaxUndo;
        SearchDepth = _settingsState.SearchDepth;
        RecentFilesCount = _settingsState.RecentFilesCount;
        MaxBatch = _settingsState.MaxBatch;
        DefaultVolume = _settingsState.DefaultVolume;
        LogVerbosityIndex = _settingsState.LogVerbosity;

        // Connection
        Hostname = _settingsState.Hostname;
        UserId = _settingsState.UserId;
        Password = _settingsState.Password;

        // Paths
        MamePath = _settingsState.MamePath;
        RomsPath = _settingsState.RootRomFolder;

        foreach (var item in MediaFolderItems)
        {
            item.Path = LoadMediaPath(_settingsState.MediaPaths.GetValueOrDefault(item.Key, item.DefaultPath), item.DefaultPath);
            var sfxEnabled = bool.TryParse(_settingsState.MediaPaths.GetValueOrDefault($"{item.Key}_sfx_enabled"), out var sfx) && sfx;
            item.LoadSuffixState(
                sfxEnabled,
                _settingsState.MediaPaths.GetValueOrDefault($"{item.Key}_suffix", item.DefaultSuffix));
            var decl = MetadataService.GetDeclByType(item.Key);
            item.Enabled = decl != null && profile.IncludesMediaFolder(decl) &&
                           (bool.TryParse(_settingsState.MediaPaths.GetValueOrDefault($"{item.Key}_enabled"), out var en) ? en : item.DefaultEnabled);
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

        if (profile.ShowsEsDePathsSection)
        {
            EsDeRoot = _settingsState.EsDeRoot;
            EsDeMediaBase = _settingsState.RootMediaFolder ?? string.Empty;
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
            [SettingKeys.MediaPathsSection] = !ActiveProfile.GamelistHasMediaPaths
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
                .ToDictionary(kv => kv.Key, kv => kv.Value)
        };

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
                [SettingKeys.RemoveZzzNotGamePrefix.Key] = RemoveZzzNotGamePrefix.ToString(),
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
                [SettingKeys.RemoveZzzNotGamePrefix.Key] = RemoveZzzNotGamePrefix.ToString(),
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
            [SettingKeys.ProfileType.Key] = _settingsState.ProfileType,
        };

        if (ActiveProfile.ShowsEsDePathsSection)
        {
            settings[SettingKeys.EsDeSection] = new()
            {
                [SettingKeys.EsDeRoot.Key] = EsDeRoot,
            };
        }

        ProfileService.Instance.Save(settings);
        if (SystemOverrideActive && !string.IsNullOrEmpty(_sessionState.CurrentSystem))
            SaveSystemOverrides();

        _settingsState.Reload();
        _sessionState.RefreshAvailableMedia();
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
        // Need instance since void
        var settingsState = SettingsState.Instance;
        var themeIndex = NameToIndex(ThemeNames, settingsState.Theme);
        var colorIndex = NameToIndex(ColorNames, settingsState.Color);
        var variantIndex = NameToIndex(AccentVariantNames, settingsState.AccentVariant);

        ThemeService.ApplyTheme(themeIndex, colorIndex, variantIndex);
        ThemeService.ApplyFontSizes(settingsState.AppFontSize, settingsState.GridFontSize);
    }

    #endregion

    #region Private Methods

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