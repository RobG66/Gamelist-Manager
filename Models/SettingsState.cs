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
            var s = SettingsService.Instance;

            // Appearance
            Theme = s.GetValue(SettingKeys.Theme);
            Color = s.GetValue(SettingKeys.Color);
            AccentVariant = s.GetValue(SettingKeys.AccentVariant);
            AlternatingRowColorIndex = s.GetInt(SettingKeys.AlternatingRowColorIndex);
            GridLinesVisibilityIndex = s.GetInt(SettingKeys.GridLinesVisibilityIndex);
            AppFontSize = s.GetInt(SettingKeys.GlobalFontSize);
            GridFontSize = s.GetInt(SettingKeys.GridFontSize);

            // Behavior
            ConfirmBulkChanges = s.GetBool(SettingKeys.ConfirmBulkChange);
            EnableSaveReminder = s.GetBool(SettingKeys.SaveReminder);
            VideoAutoplay = s.GetBool(SettingKeys.VideoAutoplay);
            RememberColumns = s.GetBool(SettingKeys.RememberColumns);
            RememberAutosize = s.GetBool(SettingKeys.RememberAutoSize);
            EnableDelete = s.GetBool(SettingKeys.EnableDelete);
            IgnoreDuplicates = s.GetBool(SettingKeys.IgnoreDuplicates);
            CheckForNewAndMissingGamesOnLoad = s.GetBool(SettingKeys.CheckForNewAndMissingGamesOnLoad);
            UseSimpleSystemPicker = s.GetBool(SettingKeys.UseSimpleSystemPicker);
            SaveWindowState = s.GetBool(SettingKeys.SaveWindowState);

            // Scraper Options
            VerifyImageDownloads = s.GetBool(SettingKeys.VerifyDownloadedImages);
            BatchProcessing = s.GetBool(SettingKeys.BatchProcessing);
            MaxBatch = s.GetInt(SettingKeys.BatchProcessingMaximum);
            ShowLogTimestamp = s.GetBool(SettingKeys.ShowLogTimestamp);
            OverrideConcurrency = s.GetBool(SettingKeys.OverrideConcurrency);
            ConcurrencyOverride = s.GetInt(SettingKeys.ConcurrencyOverride);
            LogToDisk = s.GetBool(SettingKeys.LogToDisk);
            SelectedScraper = s.GetValue(SettingKeys.SelectedScraper);
            ScraperConfigSave = s.GetInt(SettingKeys.ScraperConfigSave);
            RemoveZzzNotGamePrefix = s.GetBool(SettingKeys.RemoveZzzNotGamePrefix);
            ScreenScraperLanguage = s.GetValue(SettingKeys.ScreenScraperLanguage);
            ScreenScraperPrimaryRegion = s.GetValue(SettingKeys.ScreenScraperPrimaryRegion);
            ScreenScraperGenreEnglish = s.GetBool(SettingKeys.ScreenScraperGenreEnglish);
            ScreenScraperAnyMedia = s.GetBool(SettingKeys.ScreenScraperAnyMedia);
            ScreenScraperNamesLanguageFirst = s.GetBool(SettingKeys.ScreenScraperNamesLanguageFirst);
            ScreenScraperMediaRegionFirst = s.GetBool(SettingKeys.ScreenScraperMediaRegionFirst);
            ScreenScraperRegionFallback = s.GetValue(SettingKeys.ScreenScraperRegionFallback);

            // Advanced
            MaxUndo = s.GetInt(SettingKeys.MaxUndo);
            SearchDepth = s.GetInt(SettingKeys.SearchDepth);
            RecentFilesCount = s.GetInt(SettingKeys.RecentFilesCount);
            LogVerbosity = s.GetInt(SettingKeys.LogVerbosity);
            DefaultVolume = s.GetInt(SettingKeys.Volume);

            // Connection
            Hostname = s.GetValue(SettingKeys.HostName);
            UserId = s.GetValue(SettingKeys.UserID);
            Password = s.GetValue(SettingKeys.Password);

            // Folder Paths
            MamePath = s.GetValue(SettingKeys.MamePath);
            RootRomFolder = s.GetValue(SettingKeys.RomsFolder);

            // Media Viewer
            MediaViewerScaledDisplay = s.GetBool(SettingKeys.ScaledDisplay);

            // ES-DE / Profile
            EsDeRoot = s.GetValue(SettingKeys.EsDeRoot);
            ProfileType = s.GetValue(SettingKeys.ProfileType);

            // Media Paths
            MediaPaths = s.GetSection(SettingKeys.MediaPathsSection) ?? [];

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
