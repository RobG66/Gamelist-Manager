using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Services;
using System;
using System.Collections.Generic;
using System.IO;

namespace Gamelist_Manager.Models
{
    // Observable snapshot of all persisted settings.
    // Call Reload() on startup and after any profile switch or settings save.
    // View models bind to these properties directly.
    public partial class SettingsState : ObservableObject
    {
        #region Singleton

        private static readonly Lazy<SettingsState> _instance = new(() => new SettingsState());
        public static SettingsState Instance => _instance.Value;

        #endregion

        #region Constructor

        private SettingsState()
        {
            _esDeDetectedMediaRoot = EsDePathResolver.ReadPathsFromEsDeSettings(EsDeRoot).MediaDirectory;
            Reload();
        }

        #endregion

        #region Reload

        public void Reload()
        {
            var settingsService = SettingsService.Instance;

            // Appearance
            Theme = settingsService.GetValue(SettingKeys.Theme);
            Color = settingsService.GetValue(SettingKeys.Color);
            AccentVariant = settingsService.GetValue(SettingKeys.AccentVariant);
            AlternatingRowColorIndex = settingsService.GetInt(SettingKeys.AlternatingRowColorIndex);
            GridLinesVisibilityIndex = settingsService.GetInt(SettingKeys.GridLinesVisibilityIndex);
            AppFontSize = settingsService.GetInt(SettingKeys.GlobalFontSize);
            GridFontSize = settingsService.GetInt(SettingKeys.GridFontSize);

            // Behavior
            ConfirmBulkChanges = settingsService.GetBool(SettingKeys.ConfirmBulkChange);
            EnableSaveReminder = settingsService.GetBool(SettingKeys.SaveReminder);
            VideoAutoplay = settingsService.GetBool(SettingKeys.VideoAutoplay);
            RememberColumns = settingsService.GetBool(SettingKeys.RememberColumns);
            RememberAutosize = settingsService.GetBool(SettingKeys.RememberAutoSize);
            EnableDelete = settingsService.GetBool(SettingKeys.EnableDelete);
            IgnoreDuplicates = settingsService.GetBool(SettingKeys.IgnoreDuplicates);
            CheckForNewAndMissingGamesOnLoad = settingsService.GetBool(SettingKeys.CheckForNewAndMissingGamesOnLoad);
            UseSimpleSystemPicker = settingsService.GetBool(SettingKeys.UseSimpleSystemPicker);
            SaveWindowState = settingsService.GetBool(SettingKeys.SaveWindowState);

            // Scraper Options
            VerifyImageDownloads = settingsService.GetBool(SettingKeys.VerifyDownloadedImages);
            BatchProcessing = settingsService.GetBool(SettingKeys.BatchProcessing);
            MaxBatch = settingsService.GetInt(SettingKeys.BatchProcessingMaximum);
            ShowLogTimestamp = settingsService.GetBool(SettingKeys.ShowLogTimestamp);
            OverrideConcurrency = settingsService.GetBool(SettingKeys.OverrideConcurrency);
            ConcurrencyOverride = settingsService.GetInt(SettingKeys.ConcurrencyOverride);
            LogToDisk = settingsService.GetBool(SettingKeys.LogToDisk);
            SelectedScraper = settingsService.GetValue(SettingKeys.SelectedScraper);
            ScraperConfigSave = settingsService.GetInt(SettingKeys.ScraperConfigSave);
            RemoveZzzNotGamePrefix = settingsService.GetBool(SettingKeys.RemoveZzzNotGamePrefix);
            ScreenScraperLanguage = settingsService.GetValue(SettingKeys.ScreenScraperLanguage);
            ScreenScraperPrimaryRegion = settingsService.GetValue(SettingKeys.ScreenScraperPrimaryRegion);
            ScreenScraperGenreEnglish = settingsService.GetBool(SettingKeys.ScreenScraperGenreEnglish);
            ScreenScraperAnyMedia = settingsService.GetBool(SettingKeys.ScreenScraperAnyMedia);
            ScreenScraperNamesLanguageFirst = settingsService.GetBool(SettingKeys.ScreenScraperNamesLanguageFirst);
            ScreenScraperMediaRegionFirst = settingsService.GetBool(SettingKeys.ScreenScraperMediaRegionFirst);
            ScreenScraperRegionFallback = settingsService.GetValue(SettingKeys.ScreenScraperRegionFallback);

            // Advanced
            MaxUndo = settingsService.GetInt(SettingKeys.MaxUndo);
            SearchDepth = settingsService.GetInt(SettingKeys.SearchDepth);
            RecentFilesCount = settingsService.GetInt(SettingKeys.RecentFilesCount);
            LogVerbosity = settingsService.GetInt(SettingKeys.LogVerbosity);
            DefaultVolume = settingsService.GetInt(SettingKeys.Volume);

            // Connection
            Hostname = settingsService.GetValue(SettingKeys.HostName);
            UserId = settingsService.GetValue(SettingKeys.UserID);
            Password = settingsService.GetValue(SettingKeys.Password);

            // Folder Paths
            MamePath = settingsService.GetValue(SettingKeys.MamePath);
            RootRomFolder = settingsService.GetValue(SettingKeys.RomsFolder);

            // Media Viewer
            MediaViewerScaledDisplay = settingsService.GetBool(SettingKeys.ScaledDisplay);

            // ES-DE / Profile
            EsDeRoot = settingsService.GetValue(SettingKeys.EsDeRoot);
            ProfileType = settingsService.GetValue(SettingKeys.ProfileType);

            // Media Paths
            MediaPaths = settingsService.GetSection(SettingKeys.MediaPathsSection) ?? [];

            OnPropertyChanged(nameof(RootGamelistFolder));
            OnPropertyChanged(nameof(RootMediaFolder));
        }

        #endregion

        #region Save Helpers

        public void Save(SettingDef<bool> key, bool value)
        {
            ProfileService.Instance.Save(new() { [key.Section] = new() { [key.Key] = value.ToString() } });
            Reload();
        }

        public void Save(SettingDef<string> key, string value)
        {
            ProfileService.Instance.Save(new() { [key.Section] = new() { [key.Key] = value } });
            Reload();
        }

        public void Save(SettingDef<int> key, int value)
        {
            ProfileService.Instance.Save(new() { [key.Section] = new() { [key.Key] = value.ToString() } });
            Reload();
        }

        #endregion

        #region Column Visibility

        public Dictionary<string, string>? GetColumnVisibility() =>
            SettingsService.Instance.GetSection("ColumnVisibility");

        public void SaveColumnVisibility(Dictionary<string, string> values)
        {
            ProfileService.Instance.Save(new() { ["ColumnVisibility"] = values });
            Reload();
        }

        #endregion

        #region Appearance

        [ObservableProperty] private string _theme = "";
        [ObservableProperty] private string _color = "";
        [ObservableProperty] private string _accentVariant = "";
        [ObservableProperty] private int _alternatingRowColorIndex;
        [ObservableProperty] private int _gridLinesVisibilityIndex;
        [ObservableProperty] private int _appFontSize;
        [ObservableProperty] private int _gridFontSize;

        #endregion

        #region Behavior

        [ObservableProperty] private bool _confirmBulkChanges;
        [ObservableProperty] private bool _enableSaveReminder;
        [ObservableProperty] private bool _videoAutoplay;
        [ObservableProperty] private bool _rememberColumns;
        [ObservableProperty] private bool _rememberAutosize;
        [ObservableProperty] private bool _enableDelete;
        [ObservableProperty] private bool _ignoreDuplicates;
        [ObservableProperty] private bool _checkForNewAndMissingGamesOnLoad;
        [ObservableProperty] private bool _useSimpleSystemPicker;
        [ObservableProperty] private bool _saveWindowState;

        #endregion

        #region Scraper Options

        [ObservableProperty] private bool _verifyImageDownloads;
        [ObservableProperty] private bool _batchProcessing;
        [ObservableProperty] private bool _showLogTimestamp;
        [ObservableProperty] private bool _overrideConcurrency;
        [ObservableProperty] private int _concurrencyOverride;
        [ObservableProperty] private bool _logToDisk;
        [ObservableProperty] private string _selectedScraper = "";
        [ObservableProperty] private int _scraperConfigSave;
        [ObservableProperty] private bool _removeZzzNotGamePrefix;
        [ObservableProperty] private string _screenScraperLanguage = "";
        [ObservableProperty] private string _screenScraperPrimaryRegion = "";
        [ObservableProperty] private bool _screenScraperGenreEnglish;
        [ObservableProperty] private bool _screenScraperAnyMedia;
        [ObservableProperty] private bool _screenScraperNamesLanguageFirst;
        [ObservableProperty] private bool _screenScraperMediaRegionFirst;
        [ObservableProperty] private string _screenScraperRegionFallback = "";

        #endregion

        #region Advanced

        [ObservableProperty] private int _maxUndo;
        [ObservableProperty] private int _searchDepth;
        [ObservableProperty] private int _recentFilesCount;
        [ObservableProperty] private int _maxBatch;
        [ObservableProperty] private int _logVerbosity;
        [ObservableProperty] private int _defaultVolume;

        #endregion

        #region Connection

        [ObservableProperty] private string _hostname = "";
        [ObservableProperty] private string _userId = "";
        [ObservableProperty] private string _password = "";

        #endregion

        #region Folder Paths

        [ObservableProperty] private string _mamePath = "";
        [ObservableProperty] private string _rootRomFolder = "";

        #endregion

        #region Media Viewer

        [ObservableProperty] private bool _mediaViewerScaledDisplay;

        #endregion

        #region ES-DE / Profile

        [ObservableProperty] private string _esDeRoot = "";
        [ObservableProperty] private string _profileType = "";

        partial void OnEsDeRootChanged(string value)
        {
            _esDeDetectedMediaRoot = EsDePathResolver.ReadPathsFromEsDeSettings(value).MediaDirectory;
            OnPropertyChanged(nameof(RootGamelistFolder));
            OnPropertyChanged(nameof(RootMediaFolder));
        }

        partial void OnProfileTypeChanged(string value)
        {
            OnPropertyChanged(nameof(RootGamelistFolder));
            OnPropertyChanged(nameof(RootMediaFolder));
        }

        partial void OnRootRomFolderChanged(string value)
        {
            OnPropertyChanged(nameof(RootGamelistFolder));
            OnPropertyChanged(nameof(RootMediaFolder));
        }

        #endregion

        #region Media Paths

        [ObservableProperty] private Dictionary<string, string> _mediaPaths = [];

        #endregion

        #region Derived Root Properties

        private string? _esDeDetectedMediaRoot;

        public string? RootGamelistFolder => ProfileType == SettingKeys.ProfileTypeEsDe
            ? (string.IsNullOrEmpty(EsDeRoot) ? null : Path.Combine(EsDeRoot, "gamelists"))
            : RootRomFolder;

        public string? RootMediaFolder => ProfileType == SettingKeys.ProfileTypeEsDe
            ? _esDeDetectedMediaRoot
            : RootRomFolder;

        #endregion
    }
}
