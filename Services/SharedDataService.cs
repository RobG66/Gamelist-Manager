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
        [ObservableProperty] private bool _isDataChanged;
        [ObservableProperty] private bool _isScraping;
        [ObservableProperty] private bool _isBusy;

        [ObservableProperty] private string _romsFolder = string.Empty;
        [ObservableProperty] private string _hostname = "batocera";
        [ObservableProperty] private string _mamePath = string.Empty;
        [ObservableProperty] private bool _enableEdit;
        [ObservableProperty] private bool _videoAutoplay = false;
        [ObservableProperty] private bool _confirmBulkChanges = true;
        [ObservableProperty] private bool _enableSaveReminder = true;
        [ObservableProperty] private bool _verifyImageDownloads = true;
        [ObservableProperty] private bool _rememberColumns = false;
        [ObservableProperty] private bool _rememberAutosize = false;
        [ObservableProperty] private bool _enableDelete;
        [ObservableProperty] private bool _ignoreDuplicates;
        [ObservableProperty] private bool _batchProcessing = true;
        [ObservableProperty] private bool _showLogTimestamp;
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
        [ObservableProperty] private string _esDeMediaBase = string.Empty;

        #endregion

        #region ES-DE Properties

        // Whether the active profile is configured for ES-DE. Cached on LoadFromSettings()
        // so it doesn't re-read the INI on every access.
        public bool IsEsDeMode => _isEsDeMode;

        // Resolved media path for the currently loaded system.
        // Uses the configured EsDeMediaBase (from es_settings.xml) with the system name appended.
        public string EsDeMediaDirectory =>
            !string.IsNullOrEmpty(EsDeMediaBase) && !string.IsNullOrEmpty(CurrentSystem)
                ? Path.Combine(EsDeMediaBase, CurrentSystem)
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

            // Folder paths with fallback to old Connection section
            RomsFolder = settings.GetValue(SettingKeys.FolderPathsSection, SettingKeys.RomsFolder.Key,
                         settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.RomsFolder.Key, ""));
            MamePath = settings.GetValue(SettingKeys.FolderPathsSection, SettingKeys.MamePath.Key,
                       settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.MamePath.Key, ""));

            Hostname = settings.GetValue(SettingKeys.HostName);
            UserId = settings.GetValue(SettingKeys.UserID);
            Password = settings.GetValue(SettingKeys.Password);

            VideoAutoplay = settings.GetBool(SettingKeys.VideoAutoplay);
            ConfirmBulkChanges = settings.GetBool(SettingKeys.ConfirmBulkChange);
            EnableSaveReminder = settings.GetBool(SettingKeys.SaveReminder);
            VerifyImageDownloads = settings.GetBool(SettingKeys.VerifyDownloadedImages);
            RememberColumns = settings.GetBool(SettingKeys.RememberColumns);
            RememberAutosize = settings.GetBool(SettingKeys.RememberAutoSize);
            EnableDelete = settings.GetBool(SettingKeys.EnableDelete);
            IgnoreDuplicates = settings.GetBool(SettingKeys.IgnoreDuplicates);
            BatchProcessing = settings.GetBool(SettingKeys.BatchProcessing);
            ShowLogTimestamp = settings.GetBool(SettingKeys.ShowLogTimestamp);

            MediaViewerScaledDisplay = settings.GetBool(SettingKeys.ScaledDisplay);

            DefaultVolume = settings.GetInt(SettingKeys.Volume);
            MaxUndo = settings.GetInt(SettingKeys.MaxUndo);
            SearchDepth = settings.GetInt(SettingKeys.SearchDepth);
            MaxBatch = settings.GetInt(SettingKeys.BatchProcessingMaximum);
            RecentFilesCount = settings.GetInt(SettingKeys.RecentFilesCount);
            LogVerbosity = settings.GetInt(SettingKeys.LogVerbosity);

            Theme = settings.GetValue(SettingKeys.Theme);
            Color = settings.GetValue(SettingKeys.Color);
            AlternatingRowColorIndex = settings.GetInt(SettingKeys.AlternatingRowColorIndex);
            GridLinesVisibilityIndex = settings.GetInt(SettingKeys.GridLinesVisibilityIndex);
            AppFontSize = settings.GetInt(SettingKeys.GlobalFontSize);
            GridFontSize = settings.GetInt(SettingKeys.GridFontSize);

            // Build runtime media settings from user preferences, falling back to declaration defaults.
            var mediaPaths = settings.GetSection(SettingKeys.MediaPathsSection)
                             ?? new Dictionary<string, string>();

            var mediaSettingsDict = new Dictionary<string, MediaTypeSettings>(StringComparer.OrdinalIgnoreCase);

            // Determine ES-DE mode once before iterating media types.
            bool isEsDe = string.Equals(
                settings.GetValue(SettingKeys.ProfileType),
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
            _settings.SetValue(SettingKeys.ScaledDisplay.Section, SettingKeys.ScaledDisplay.Key, MediaViewerScaledDisplay.ToString());
        }

        public IReadOnlyDictionary<string, string> GetFileTypes()
        {
            return _settings.GetFileTypes();
        }

        // Updates the ES-DE root path in memory and persists it to the active profile.
        public void SaveEsDeRoot(string path)
        {
            EsDeRoot = path;
            _settings.SetValue(SettingKeys.EsDeRoot.Section, SettingKeys.EsDeRoot.Key, path);
        }

        #endregion

        #region Property Change Callbacks

        partial void OnEsDeRootChanged(string value)
        {
            OnPropertyChanged(nameof(EsDeMediaDirectory));
        }

        partial void OnEsDeMediaBaseChanged(string value)
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
                settings.GetValue(SettingKeys.ProfileType),
                SettingKeys.ProfileTypeEsDe,
                System.StringComparison.OrdinalIgnoreCase);

            if (_isEsDeMode != newValue)
            {
                _isEsDeMode = newValue;
                OnPropertyChanged(nameof(IsEsDeMode));
            }

            EsDeRoot = settings.GetValue(SettingKeys.EsDeRoot);

            // Always re-detect from es_settings.xml — the user may have changed paths outside this app.
            var detected = SettingsService.ReadPathsFromEsDeSettings(EsDeRoot);
            EsDeMediaBase = detected.MediaDirectory ?? string.Empty;
        }

        #endregion
    }
}