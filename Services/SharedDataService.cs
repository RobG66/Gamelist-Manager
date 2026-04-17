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

        private Dictionary<string, string>? _fileTypesCache;

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

        #endregion

        #region Public Properties

        public static SharedDataService Instance => _instance ??= new SharedDataService();

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

        private SharedDataService()
        {
            LoadFromSettings();
        }

        #endregion

        #region Public Methods

        public void LoadFromSettings()
        {
            _fileTypesCache = null;

            var settings = SettingsService.Instance;

            RomsFolder = settings.GetValue(SettingKeys.FolderPathsSection, SettingKeys.RomsFolder,
                         settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.RomsFolder, ""));
            Hostname = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.HostName, "batocera");
            MamePath = settings.GetValue(SettingKeys.FolderPathsSection, SettingKeys.MamePath,
                       settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.MamePath, ""));
            UserId = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.UserID, "root");
            Password = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.Password, "linux");

            VideoAutoplay = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.VideoAutoplay, false);
            ConfirmBulkChanges = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.ConfirmBulkChange, true);
            EnableSaveReminder = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.SaveReminder, true);
            VerifyImageDownloads = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.VerifyDownloadedImages, true);
            ShowGamelistStats = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.ShowGamelistStats, true);
            RememberColumns = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.RememberColumns, false);
            RememberAutosize = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.RememberAutoSize, false);
            EnableDelete = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.EnableDelete, false);
            IgnoreDuplicates = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.IgnoreDuplicates, false);
            BatchProcessing = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.BatchProcessing, true);
            ShowLogTimestamp = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.ShowLogTimestamp, false);

            MediaViewerScaledDisplay = settings.GetBool(SettingKeys.MediaViewerSection, SettingKeys.ScaledDisplay, true);

            DefaultVolume = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.Volume, 75);
            MaxUndo = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.MaxUndo, 5);
            SearchDepth = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.SearchDepth, 2);
            MaxBatch = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.BatchProcessingMaximum, 300);
            RecentFilesCount = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.RecentFilesCount, 15);
            LogVerbosity = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.LogVerbosity, 1);

            Theme = settings.GetValue(SettingKeys.AppearanceSection, SettingKeys.Theme, "Default");
            Color = settings.GetValue(SettingKeys.AppearanceSection, SettingKeys.Color, "Blue");
            AlternatingRowColorIndex = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.AlternatingRowColorIndex, 1);
            GridLinesVisibilityIndex = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GridLinesVisibilityIndex, 0);
            AppFontSize = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GlobalFontSize, 12);
            GridFontSize = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GridFontSize, 12);

            // Build runtime media settings from user preferences, falling back to declaration defaults.
            var mediaPaths = settings.GetSection(SettingKeys.MediaPathsSection)
                             ?? new Dictionary<string, string>();

            var mediaSettingsDict = new Dictionary<string, MediaTypeSettings>(StringComparer.OrdinalIgnoreCase);
            foreach (var decl in GamelistMetaData.GetAllMediaFolderTypes())
            {
                var ms = new MediaTypeSettings
                {
                    Type = decl.Type,

                    Enabled = mediaPaths.TryGetValue($"{decl.Type}_enabled", out var enabled)
                        ? bool.TryParse(enabled, out var eb) && eb
                        : decl.DefaultEnabled,

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
            var settings = SettingsService.Instance;
            settings.SetValue(SettingKeys.MediaViewerSection, SettingKeys.ScaledDisplay, MediaViewerScaledDisplay.ToString());
        }

        public IReadOnlyDictionary<string, string> GetFileTypes()
        {
            if (_fileTypesCache != null)
                return _fileTypesCache;

            var iniFile = "filetypes.ini";
            var iniPath = Path.Combine(AppContext.BaseDirectory, "ini", iniFile);
            var sections = IniFileService.ReadIniFile(iniPath);
            _fileTypesCache = sections.TryGetValue("Filetypes", out var filetypes)
                ? filetypes
                : [];

            return _fileTypesCache;
        }

        #endregion

        #region Private Methods

        #endregion
    }
}