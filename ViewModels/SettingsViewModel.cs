using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Classes.Helpers;
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

    private readonly SharedDataService _sharedData = SharedDataService.Instance;
    private bool _isLoading;

    private static readonly string[] ThemeNames = ["Light", "Dark"];
    private static readonly string[] ColorNames =
    [
        "Blue", "Red", "Orange", "Green", "Yellow",
        "Magenta", "Purple", "Teal", "Lime", "Light Blue", "Indigo"
    ];

    #endregion

    #region Observable Properties

    // Appearance
    [ObservableProperty] private int _selectedThemeIndex;
    [ObservableProperty] private int _selectedColorIndex;
    [ObservableProperty] private int _selectedAlternatingRowColorIndex;
    [ObservableProperty] private int _selectedGridLinesVisibilityIndex;
    [ObservableProperty] private int _appFontSize = 12;
    [ObservableProperty] private int _gridFontSize = 12;

    // Behavior / Options
    [ObservableProperty] private bool _confirmBulkChanges;
    [ObservableProperty] private bool _enableSaveReminder;
    [ObservableProperty] private bool _verifyImageDownloads;
    [ObservableProperty] private bool _videoAutoplay;
    [ObservableProperty] private bool _rememberColumns;
    [ObservableProperty] private bool _rememberAutosize;
    [ObservableProperty] private bool _enableDelete;
    [ObservableProperty] private bool _ignoreDuplicates;
    [ObservableProperty] private bool _batchProcessing;
    [ObservableProperty] private bool _removeZZZNotGamePrefix;
    [ObservableProperty] private bool _useSimpleSystemPicker;
    [ObservableProperty] private bool _showLogTimestamp;
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

    [ObservableProperty] private bool _isDirty;

    #endregion

    #region Property Change Callbacks

    partial void OnSelectedThemeIndexChanged(int value)
    {
        if (_isLoading) return;
        SelectedAlternatingRowColorIndex = 0;
    }

    #endregion

    #region Constructor

    public SettingsViewModel()
    {
        _isLoading = true;

        InitializeMediaFolderItems();
        LoadSettings();

        _isLoading = false;

        PropertyChanged += (_, e) =>
        {
            if (_isLoading) return;

            if (e.PropertyName is nameof(IsDirty)
                               or nameof(SelectedProfileName)
                               or nameof(SelectedProfileIsEsDe)
                               or nameof(SelectedProfileEsDeMediaRoot))
                return;

            IsDirty = true;
        };
    }

    #endregion

    #region Load / Save / Reset

    public void LoadSettings()
    {
        _isLoading = true;
        try
        {
            var settings = SettingsService.Instance;

            // Appearance
            SelectedThemeIndex = NameToIndex(ThemeNames, settings.GetValue(SettingKeys.Theme));
            SelectedColorIndex = NameToIndex(ColorNames, settings.GetValue(SettingKeys.Color));
            SelectedAlternatingRowColorIndex = settings.GetInt(SettingKeys.AlternatingRowColorIndex);
            SelectedGridLinesVisibilityIndex = settings.GetInt(SettingKeys.GridLinesVisibilityIndex);
            AppFontSize = settings.GetInt(SettingKeys.GlobalFontSize);
            GridFontSize = settings.GetInt(SettingKeys.GridFontSize);

            // Behavior
            ConfirmBulkChanges = settings.GetBool(SettingKeys.ConfirmBulkChange);
            EnableSaveReminder = settings.GetBool(SettingKeys.SaveReminder);
            VerifyImageDownloads = settings.GetBool(SettingKeys.VerifyDownloadedImages);
            VideoAutoplay = settings.GetBool(SettingKeys.VideoAutoplay);
            RememberColumns = settings.GetBool(SettingKeys.RememberColumns);
            RememberAutosize = settings.GetBool(SettingKeys.RememberAutoSize);
            EnableDelete = settings.GetBool(SettingKeys.EnableDelete);
            RemoveZZZNotGamePrefix = settings.GetBool(SettingKeys.RemoveZZZNotGamePrefix);
            IgnoreDuplicates = settings.GetBool(SettingKeys.IgnoreDuplicates);
            BatchProcessing = settings.GetBool(SettingKeys.BatchProcessing);
            ShowLogTimestamp = settings.GetBool(SettingKeys.ShowLogTimestamp);
            CheckForNewAndMissingGamesOnLoad = settings.GetBool(SettingKeys.CheckForNewAndMissingGamesOnLoad);
            UseSimpleSystemPicker = settings.GetBool(SettingKeys.UseSimpleSystemPicker);

            ScraperConfigSaveIndex = settings.GetInt(SettingKeys.ScraperConfigSave);
            MaxUndo = settings.GetInt(SettingKeys.MaxUndo);
            SearchDepth = settings.GetInt(SettingKeys.SearchDepth);
            RecentFilesCount = settings.GetInt(SettingKeys.RecentFilesCount);
            MaxBatch = settings.GetInt(SettingKeys.BatchProcessingMaximum);
            DefaultVolume = settings.GetInt(SettingKeys.Volume);
            LogVerbosityIndex = settings.GetInt(SettingKeys.LogVerbosity);

            // Remote
            Hostname = settings.GetValue(SettingKeys.HostName) ?? string.Empty;
            UserId = settings.GetValue(SettingKeys.UserID) ?? string.Empty;
            Password = settings.GetValue(SettingKeys.Password) ?? string.Empty;

            // Paths / Profiles
            MamePath = settings.GetValue(SettingKeys.FolderPathsSection, SettingKeys.MamePath.Key) ?? string.Empty;

            var profileType = settings.GetValue(SettingKeys.ProfileType);
            var isEsDe = string.Equals(profileType, SettingKeys.ProfileTypeEsDe, StringComparison.OrdinalIgnoreCase);

            foreach (var item in MediaFolderItems)
            {
                item.Path = LoadMediaPath(settings.GetValue(SettingKeys.MediaPathsSection, item.Key, item.DefaultPath), item.DefaultPath);
                item.Suffix = settings.GetValue(SettingKeys.MediaPathsSection, $"{item.Key}_suffix", item.DefaultSuffix);
                item.SfxEnabled = settings.GetBool(SettingKeys.MediaPathsSection, $"{item.Key}_sfx_enabled", item.DefaultSfxEnabled);

                item.Enabled = (!isEsDe || (GamelistMetaData.GetDeclByType(item.Key)?.IsEsDeSupported ?? false)) &&
                               settings.GetBool(SettingKeys.MediaPathsSection, $"{item.Key}_enabled", item.DefaultEnabled);
            }

            LoadScraperCredentials();
            RefreshProfileList();

            if (_sharedData.ProfileType == SettingKeys.ProfileTypeEsDe)
            {
                EsDeRoot = settings.GetValue(SettingKeys.EsDeRoot);
                EsDeMediaBase = _sharedData.EsDeMediaBase;
                RomsPath = _sharedData.RomsFolder;
            }
            else if (_sharedData.ProfileType == SettingKeys.ProfileTypeEs)
            {
                RomsPath = settings.GetValue(SettingKeys.FolderPathsSection, SettingKeys.RomsFolder.Key) ?? string.Empty;
            }

            foreach (var item in MediaFolderItems)
                item.NotifyProfileTypeChanged();

            OnPropertyChanged(nameof(IsEsDeProfile));
            OnPropertyChanged(nameof(EsDePathsVisible));
            OnPropertyChanged(nameof(SuffixesEnabled));

            IsDirty = false;
        }
        finally
        {
            _isLoading = false;
        }
    }

    public void SaveSettings()
    {
        var settings = new Dictionary<string, Dictionary<string, string>>
        {
            [SettingKeys.AppearanceSection] = new()
            {
                [SettingKeys.Theme.Key] = IndexToName(ThemeNames, SelectedThemeIndex),
                [SettingKeys.Color.Key] = IndexToName(ColorNames, SelectedColorIndex),
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
                [SettingKeys.VerifyDownloadedImages.Key] = VerifyImageDownloads.ToString(),
                [SettingKeys.VideoAutoplay.Key] = VideoAutoplay.ToString(),
                [SettingKeys.RememberColumns.Key] = RememberColumns.ToString(),
                [SettingKeys.RememberAutoSize.Key] = RememberAutosize.ToString(),
                [SettingKeys.EnableDelete.Key] = EnableDelete.ToString(),
                [SettingKeys.IgnoreDuplicates.Key] = IgnoreDuplicates.ToString(),
                [SettingKeys.BatchProcessing.Key] = BatchProcessing.ToString(),
                [SettingKeys.ShowLogTimestamp.Key] = ShowLogTimestamp.ToString(),
                [SettingKeys.RemoveZZZNotGamePrefix.Key] = RemoveZZZNotGamePrefix.ToString(),
                [SettingKeys.ScraperConfigSave.Key] = ScraperConfigSaveIndex.ToString(),
                [SettingKeys.CheckForNewAndMissingGamesOnLoad.Key] = CheckForNewAndMissingGamesOnLoad.ToString(),
                [SettingKeys.UseSimpleSystemPicker.Key] = UseSimpleSystemPicker.ToString()
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
            [SettingKeys.MediaPathsSection] = _sharedData.ProfileType == SettingKeys.ProfileTypeEsDe
                ? MediaFolderItems
                    .Select(item => new KeyValuePair<string, string>($"{item.Key}_enabled", item.Enabled.ToString()))
                    .ToDictionary(kv => kv.Key, kv => kv.Value)
                : MediaFolderItems
                    .SelectMany(item => new[]
                    {
                        new KeyValuePair<string, string>(item.Key,                      item.Path),
                        new KeyValuePair<string, string>($"{item.Key}_enabled",         item.Enabled.ToString()),
                        new KeyValuePair<string, string>($"{item.Key}_suffix",          item.Suffix),
                        new KeyValuePair<string, string>($"{item.Key}_sfx_enabled",     item.SfxEnabled.ToString()),
                    })
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
            settings[SettingKeys.ScraperSection] = new()
            {
                ["ScreenScraper_Language"] = SelectedScraperLanguage ?? string.Empty,
                ["ScreenScraper_PrimaryRegion"] = SelectedScraperPrimaryRegion ?? string.Empty,
                ["ScreenScraper_GenreEnglish"] = ScraperGenreAlwaysEnglish.ToString(),
                ["ScreenScraper_AnyMedia"] = ScraperScrapeAnyMedia.ToString(),
                ["ScreenScraper_NamesLanguageFirst"] = ScraperNamesLanguageFirst.ToString(),
                ["ScreenScraper_MediaRegionFirst"] = ScraperMediaRegionFirst.ToString(),
                ["ScreenScraper_RegionFallback"] = JsonSerializer.Serialize(ScraperFallbackRegions.ToList()),
            };
        }

        settings[SettingKeys.ProfileSection] = new()
        {
            [SettingKeys.ProfileType.Key] = _sharedData.ProfileType,
        };

        if (_sharedData.ProfileType == SettingKeys.ProfileTypeEsDe)
        {
            settings[SettingKeys.EsDeSection] = new()
            {
                [SettingKeys.EsDeRoot.Key] = EsDeRoot,
            };
        }

        SettingsService.Instance.SaveAllSettings(settings);

        ThemeService.ApplyTheme(SelectedThemeIndex, SelectedColorIndex);
        _sharedData.LoadFromSettings();

        IsDirty = false;
    }

    public void ResetAllSettings()
    {
        SettingsService.Instance.ResetToDefaults();
        LoadSettings();
    }

    public static void ApplyThemeOnStartup()
    {
        var shared = SharedDataService.Instance;

        var themeIndex = NameToIndex(ThemeNames, shared.Theme);
        var colorIndex = NameToIndex(ColorNames, shared.Color);

        ThemeService.ApplyTheme(themeIndex, colorIndex);
        ThemeService.ApplyFontSizes(shared.AppFontSize, shared.GridFontSize);
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