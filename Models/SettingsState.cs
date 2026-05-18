using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Services;
using System;
using System.Collections.Generic;

namespace Gamelist_Manager.Models
{
    /// <summary>
    /// Observable snapshot of all persisted settings.
    /// Call Reload() on startup and after any profile switch or settings save.
    /// View models bind to these properties directly.
    /// </summary>
    public partial class SettingsState : ObservableObject
    {
        private static readonly Lazy<SettingsState> _instance = new(() => new SettingsState());
        public static SettingsState Instance => _instance.Value;

        private SettingsState() => Reload();

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

            // Scraper Options
            VerifyImageDownloads = s.GetBool(SettingKeys.VerifyDownloadedImages);
            BatchProcessing = s.GetBool(SettingKeys.BatchProcessing);
            ShowLogTimestamp = s.GetBool(SettingKeys.ShowLogTimestamp);
            OverrideConcurrency = s.GetBool(SettingKeys.OverrideConcurrency);
            ConcurrencyOverride = s.GetInt(SettingKeys.ConcurrencyOverride);
            LogToDisk = s.GetBool(SettingKeys.LogToDisk);
            SelectedScraper = s.GetValue(SettingKeys.SelectedScraper);
            ScraperConfigSave = s.GetInt(SettingKeys.ScraperConfigSave);
            ScrapeAllMode = s.GetBool(SettingKeys.ScrapeAllMode);
            OverwriteName = s.GetBool(SettingKeys.OverwriteName);
            OverwriteMedia = s.GetBool(SettingKeys.OverwriteMedia);
            ScrapeHiddenItems = s.GetBool(SettingKeys.ScrapeHiddenItems);
            RemoveZZZNotGamePrefix = s.GetBool(SettingKeys.RemoveZZZNotGamePrefix);
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
            MaxBatch = s.GetInt(SettingKeys.BatchProcessingMaximum);
            LogVerbosity = s.GetInt(SettingKeys.LogVerbosity);
            DefaultVolume = s.GetInt(SettingKeys.Volume);

            // Connection
            Hostname = s.GetValue(SettingKeys.HostName);
            UserId = s.GetValue(SettingKeys.UserID);
            Password = s.GetValue(SettingKeys.Password);

            // Folder Paths
            MamePath = s.GetValue(SettingKeys.MamePath);
            RomsFolder = s.GetValue(SettingKeys.RomsFolder);

            // Media Viewer
            MediaViewerScaledDisplay = s.GetBool(SettingKeys.ScaledDisplay);

            // ES-DE / Profile
            EsDeRoot = s.GetValue(SettingKeys.EsDeRoot);

            // Media Paths
            MediaPaths = s.GetSection(SettingKeys.MediaPathsSection) ?? [];
        }

        // --- Save helpers (keeps SettingsService out of ViewModels) ---

        public void Save(SettingDef<bool> key, bool value)
        {
            SettingsService.Instance.SetBool(key.Section, key.Key, value);
            Reload();
        }

        public void Save(SettingDef<string> key, string value)
        {
            SettingsService.Instance.SetValue(key.Section, key.Key, value);
            Reload();
        }

        public void Save(SettingDef<int> key, int value)
        {
            SettingsService.Instance.SetValue(key.Section, key.Key, value.ToString());
            Reload();
        }

        // --- Column visibility (raw section, no SettingDef coverage) ---

        public Dictionary<string, string>? GetColumnVisibility() =>
            SettingsService.Instance.GetSection("ColumnVisibility");

        public void SaveColumnVisibility(Dictionary<string, string> values)
        {
            SettingsService.Instance.SaveAllSettings(new Dictionary<string, Dictionary<string, string>>
            {
                ["ColumnVisibility"] = values
            });
            Reload();
        }

        #region Appearance

        [ObservableProperty] private string _theme = "Light";
        [ObservableProperty] private string _color = "Blue";
        [ObservableProperty] private string _accentVariant = "Base";
        [ObservableProperty] private int _alternatingRowColorIndex = 1;
        [ObservableProperty] private int _gridLinesVisibilityIndex = 0;
        [ObservableProperty] private int _appFontSize = 12;
        [ObservableProperty] private int _gridFontSize = 12;

        #endregion

        #region Behavior

        [ObservableProperty] private bool _confirmBulkChanges = true;
        [ObservableProperty] private bool _enableSaveReminder = true;
        [ObservableProperty] private bool _videoAutoplay = true;
        [ObservableProperty] private bool _rememberColumns = false;
        [ObservableProperty] private bool _rememberAutosize = false;
        [ObservableProperty] private bool _enableDelete = false;
        [ObservableProperty] private bool _ignoreDuplicates = false;
        [ObservableProperty] private bool _checkForNewAndMissingGamesOnLoad = false;
        [ObservableProperty] private bool _useSimpleSystemPicker = false;

        #endregion

        #region Scraper Options

        [ObservableProperty] private bool _verifyImageDownloads = true;
        [ObservableProperty] private bool _batchProcessing = true;
        [ObservableProperty] private bool _showLogTimestamp = false;
        [ObservableProperty] private bool _overrideConcurrency = false;
        [ObservableProperty] private int _concurrencyOverride = 1;
        [ObservableProperty] private bool _logToDisk = false;
        [ObservableProperty] private string _selectedScraper = "";
        [ObservableProperty] private int _scraperConfigSave = 0;
        [ObservableProperty] private bool _scrapeAllMode = true;
        [ObservableProperty] private bool _overwriteName = true;
        [ObservableProperty] private bool _overwriteMedia = false;
        [ObservableProperty] private bool _scrapeHiddenItems = false;
        [ObservableProperty] private bool _removeZZZNotGamePrefix = true;
        [ObservableProperty] private string _screenScraperLanguage = "English (en)";
        [ObservableProperty] private string _screenScraperPrimaryRegion = "USA (us)";
        [ObservableProperty] private bool _screenScraperGenreEnglish = false;
        [ObservableProperty] private bool _screenScraperAnyMedia = true;
        [ObservableProperty] private bool _screenScraperNamesLanguageFirst = false;
        [ObservableProperty] private bool _screenScraperMediaRegionFirst = false;
        [ObservableProperty]
        private string _screenScraperRegionFallback =
            """["USA (us)", "Europe (eu)", "United Kingdom (uk)", "World (wor)", "Japan (jp)", "ScreenScraper (ss)", "Custom (cus)"]""";

        #endregion

        #region Advanced

        [ObservableProperty] private int _maxUndo = 5;
        [ObservableProperty] private int _searchDepth = 2;
        [ObservableProperty] private int _recentFilesCount = 15;
        [ObservableProperty] private int _maxBatch = 300;
        [ObservableProperty] private int _logVerbosity = 1;
        [ObservableProperty] private int _defaultVolume = 75;

        #endregion

        #region Connection

        [ObservableProperty] private string _hostname = "";
        [ObservableProperty] private string _userId = "";
        [ObservableProperty] private string _password = "";

        #endregion

        #region Folder Paths

        [ObservableProperty] private string _mamePath = "";
        [ObservableProperty] private string _romsFolder = "";

        #endregion

        #region Media Viewer

        [ObservableProperty] private bool _mediaViewerScaledDisplay = true;

        #endregion

        #region ES-DE / Profile

        [ObservableProperty] private string _esDeRoot = "";

        #endregion

        #region Media Paths

        [ObservableProperty] private Dictionary<string, string> _mediaPaths = [];

        #endregion
    }
}