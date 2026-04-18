using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Gamelist_Manager.Services
{
    public partial class SharedDataService : ObservableObject
    {
        #region Private Fields

        private static SharedDataService? _instance;
        private readonly SettingsService _settings;
        private bool _isEsDeMode;

        #endregion

        #region Observable Properties

        [ObservableProperty] private string? _xmlFilename;
        [ObservableProperty] private string? _currentSystem;
        [ObservableProperty] private bool _isDataChanged = false;
        [ObservableProperty] private bool _isScraping = false;
        [ObservableProperty] private bool _isBusy = false;

        [ObservableProperty] private string _romsFolder = string.Empty;
        [ObservableProperty] private string _hostname = "batocera";
        [ObservableProperty] private string _mamePath = string.Empty;
        [ObservableProperty] private bool _enableEdit = false;
        [ObservableProperty] private bool _videoAutoplay = false;
        [ObservableProperty] private bool _confirmBulkChanges = true;
        [ObservableProperty] private bool _enableSaveReminder = true;
        [ObservableProperty] private bool _verifyImageDownloads = true;
        [ObservableProperty] private bool _showGamelistStats = true;
        [ObservableProperty] private bool _rememberColumns = false;
        [ObservableProperty] private bool _rememberAutosize = false;
        [ObservableProperty] private bool _enableDelete = false;
        [ObservableProperty] private bool _ignoreDuplicates = false;
        [ObservableProperty] private bool _batchProcessing = true;
        [ObservableProperty] private bool _showLogTimestamp = false;
        [ObservableProperty] private bool _mediaViewerScaledDisplay = true;
        [ObservableProperty] private int _defaultVolume = 75;
        [ObservableProperty] private int _maxUndo = 5;
        [ObservableProperty] private int _searchDepth = 2;
        [ObservableProperty] private int _maxBatch = 300;
        [ObservableProperty] private int _recentFilesCount = 15;
        [ObservableProperty] private int _logVerbosity = 1;
        [ObservableProperty] private string _theme = "Default";
        [ObservableProperty] private string _color = "Blue";
        [ObservableProperty] private int _alternatingRowColorIndex = 1;
        [ObservableProperty] private int _gridLinesVisibilityIndex;
        [ObservableProperty] private double _appFontSize = 12;
        [ObservableProperty] private double _gridFontSize = 12;
        [ObservableProperty] private string _userId = "root";
        [ObservableProperty] private string _password = "linux";
        [ObservableProperty] private string _esDeRoot = string.Empty;

        #endregion

        #region ES-DE Properties

        // Whether the active profile is configured for ES-DE. Cached on LoadFromSettings()
        // so it doesn't re-read the INI on every access.
        public bool IsEsDeMode => _isEsDeMode;

        // Resolved media base path for the currently loaded system: <EsDeRoot>/downloaded_media/<systemname>
        public string EsDeMediaDirectory =>
            !string.IsNullOrEmpty(EsDeRoot) && !string.IsNullOrEmpty(CurrentSystem)
                ? Path.Combine(EsDeRoot, "downloaded_media", CurrentSystem)
                : string.Empty;

        #endregion

        #region Public Properties

        public static SharedDataService Instance => _instance ??= new SharedDataService(SettingsService.Instance);

        // The raw gamelist rows — single source of truth for all controls and ViewModels.
        public ObservableCollection<GameMetadataRow>? GamelistData { get; set; }

        public ObservableCollection<RecentFileItem> RecentFiles { get; } = new();

        // Set by MainWindowViewModel after the DynamicData pipeline is bound.
        public ReadOnlyObservableCollection<GameMetadataRow>? FilteredGamelistData { get; set; }

        // Updated by MainWindowViewModel whenever selection changes.
        public IList? SelectedItems { get; set; }

        public string? GamelistDirectory =>
            !string.IsNullOrEmpty(XmlFilename)
                ? Path.GetDirectoryName((string?)XmlFilename)
                : null;

        public string? RomScanDirectory
        {
            get
            {
                if (IsEsDeMode)
                {
                    if (string.IsNullOrEmpty(RomsFolder) || string.IsNullOrEmpty(CurrentSystem))
                        return null;
                    return Path.Combine(RomsFolder, CurrentSystem);
                }
                return GamelistDirectory;
            }
        }

        // Keyed by media type string (e.g. "image", "video").
        public IReadOnlyDictionary<string, MediaTypeSettings> MediaSettings { get; private set; }
            = new Dictionary<string, MediaTypeSettings>();

        public event EventHandler? SettingsApplied;

        #endregion

        #region Constructor

        private SharedDataService(SettingsService settings)
        {
            _settings = settings;
            LoadFromSettings();
        }

        #endregion

        #region Public Methods

        public void LoadFromSettings()
        {
            var settings = _settings;

            RomsFolder = settings.GetValue(SettingKeys.FolderPathsSection, SettingKeys.RomsFolder,
                         settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.RomsFolder, ""));
            Hostname = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.HostName, SettingKeys.DefaultHostName);
            MamePath = settings.GetValue(SettingKeys.FolderPathsSection, SettingKeys.MamePath,
                       settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.MamePath, ""));
            UserId = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.UserID, SettingKeys.DefaultUserID);
            Password = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.Password, SettingKeys.DefaultPassword);

            VideoAutoplay = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.VideoAutoplay, false);
            ConfirmBulkChanges = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.ConfirmBulkChange, SettingKeys.DefaultConfirmBulkChange);
            EnableSaveReminder = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.SaveReminder, SettingKeys.DefaultSaveReminder);
            VerifyImageDownloads = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.VerifyDownloadedImages, SettingKeys.DefaultVerifyDownloadedImages);
            ShowGamelistStats = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.ShowGamelistStats, SettingKeys.DefaultShowGamelistStats);
            RememberColumns = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.RememberColumns, SettingKeys.DefaultRememberColumns);
            RememberAutosize = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.RememberAutoSize, SettingKeys.DefaultRememberAutoSize);
            EnableDelete = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.EnableDelete, SettingKeys.DefaultEnableDelete);
            IgnoreDuplicates = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.IgnoreDuplicates, SettingKeys.DefaultIgnoreDuplicates);
            BatchProcessing = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.BatchProcessing, SettingKeys.DefaultBatchProcessing);
            ShowLogTimestamp = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.ShowLogTimestamp, false);

            MediaViewerScaledDisplay = settings.GetBool(SettingKeys.MediaViewerSection, SettingKeys.ScaledDisplay, true);

            DefaultVolume = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.Volume, SettingKeys.DefaultVolume);
            MaxUndo = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.MaxUndo, SettingKeys.DefaultMaxUndo);
            SearchDepth = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.SearchDepth, SettingKeys.DefaultSearchDepth);
            MaxBatch = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.BatchProcessingMaximum, SettingKeys.DefaultBatchProcessingMaximum);
            RecentFilesCount = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.RecentFilesCount, SettingKeys.DefaultRecentFilesCount);
            LogVerbosity = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.LogVerbosity, SettingKeys.DefaultLogVerbosity);

            Theme = settings.GetValue(SettingKeys.AppearanceSection, SettingKeys.Theme, SettingKeys.DefaultTheme);
            Color = settings.GetValue(SettingKeys.AppearanceSection, SettingKeys.Color, SettingKeys.DefaultColor);
            AlternatingRowColorIndex = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.AlternatingRowColorIndex, SettingKeys.DefaultAlternatingRowColorIndex);
            GridLinesVisibilityIndex = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GridLinesVisibilityIndex, SettingKeys.DefaultGridLinesVisibilityIndex);
            AppFontSize = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GlobalFontSize, SettingKeys.DefaultGlobalFontSize);
            GridFontSize = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GridFontSize, SettingKeys.DefaultGridFontSize);

            // Build runtime media settings from user preferences, falling back to declaration defaults.
            var mediaPaths = settings.GetSection(SettingKeys.MediaPathsSection)
                             ?? new Dictionary<string, string>();

            var mediaSettingsDict = new Dictionary<string, MediaTypeSettings>(StringComparer.OrdinalIgnoreCase);

            // Determine ES-DE mode once before iterating media types.
            bool isEsDe = string.Equals(
                settings.GetValue(SettingKeys.EsDeSection, SettingKeys.ProfileType, SettingKeys.ProfileTypeStandard),
                SettingKeys.ProfileTypeEsDe,
                System.StringComparison.OrdinalIgnoreCase);

            foreach (var decl in GamelistMetaData.GetAllMediaFolderTypes())
            {
                bool isEnabled = mediaPaths.TryGetValue($"{decl.Type}_enabled", out var enabled)
                    ? bool.TryParse(enabled, out var eb) && eb
                    : decl.DefaultEnabled;

                // In ES-DE mode, types without a dedicated folder are always disabled.
                if (isEsDe && string.IsNullOrEmpty(decl.EsDeFolderName))
                    isEnabled = false;

                var ms = new MediaTypeSettings
                {
                    Type = decl.Type,
                    Enabled = isEnabled,

                    Path = mediaPaths.TryGetValue(decl.Type, out var path)
                        ? path : decl.DefaultPath,

                    Suffix = mediaPaths.TryGetValue($"{decl.Type}_suffix", out var suffix)
                        ? suffix : decl.DefaultSuffix,

                    SfxEnabled = mediaPaths.TryGetValue($"{decl.Type}_sfx_enabled", out var sfxEnabled)
                        ? bool.TryParse(sfxEnabled, out var seb) && seb
                        : !string.IsNullOrEmpty(decl.DefaultSuffix),
                };

                mediaSettingsDict[decl.Type] = ms;
            }
            MediaSettings = mediaSettingsDict;

            RefreshEsDeMode();

            SettingsApplied?.Invoke(this, EventArgs.Empty);
        }

        public void SetGamelist(string xmlPath, string systemName, ObservableCollection<GameMetadataRow> data)
        {
            XmlFilename = xmlPath;
            CurrentSystem = systemName;
            GamelistData = data;
            IsDataChanged = false;
        }


        public void Clear()
        {
            XmlFilename = null;
            CurrentSystem = null;
            GamelistData = null;
            IsDataChanged = false;
        }

        public void SaveMediaViewerPreferences()
        {
            _settings.SetValue(SettingKeys.MediaViewerSection, SettingKeys.ScaledDisplay, MediaViewerScaledDisplay.ToString());
        }

        public IReadOnlyDictionary<string, string> GetFileTypes()
        {
            return _settings.GetFileTypes();
        }

        // Updates the ES-DE root path in memory and persists it to the active profile.
        public void SaveEsDeRoot(string path)
        {
            EsDeRoot = path;
            _settings.SetValue(SettingKeys.EsDeSection, SettingKeys.EsDeRoot, path);
        }

        #endregion

        #region Property Change Callbacks

        partial void OnEsDeRootChanged(string value)
        {
            OnPropertyChanged(nameof(EsDeMediaDirectory));
        }

        partial void OnCurrentSystemChanged(string? value)
        {
            OnPropertyChanged(nameof(EsDeMediaDirectory));
        }

        #endregion

        #region Private Methods

        private void RefreshEsDeMode()
        {
            var settings = _settings;

            var newValue = string.Equals(
                settings.GetValue(SettingKeys.EsDeSection, SettingKeys.ProfileType, SettingKeys.ProfileTypeStandard),
                SettingKeys.ProfileTypeEsDe,
                System.StringComparison.OrdinalIgnoreCase);

            if (_isEsDeMode != newValue)
            {
                _isEsDeMode = newValue;
                OnPropertyChanged(nameof(IsEsDeMode));
            }

            EsDeRoot = settings.GetValue(SettingKeys.EsDeSection, SettingKeys.EsDeRoot, string.Empty);
        }

        #endregion
    }
}