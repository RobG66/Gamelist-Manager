using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace Gamelist_Manager.Services
{
    public partial class SharedDataService : ObservableObject
    {
        #region Private Fields

        private static readonly Lazy<SharedDataService> _instance =
            new(() => new SharedDataService(SettingsService.Instance));

        private readonly SettingsService _settings;
        private string _profileType = SettingKeys.ProfileTypeEs;

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
        [ObservableProperty] private bool _videoAutoplay;
        // Session-only: set when the user manually pauses, cleared when they manually play.
        // Used to keep videos paused when navigating between games after a manual pause.
        [ObservableProperty] private bool _videoUserPaused;
        [ObservableProperty] private bool _confirmBulkChanges = true;
        [ObservableProperty] private bool _enableSaveReminder = true;
        [ObservableProperty] private bool _verifyImageDownloads = true;
        [ObservableProperty] private bool _rememberColumns;
        [ObservableProperty] private bool _rememberAutosize;
        [ObservableProperty] private bool _enableDelete;
        [ObservableProperty] private bool _ignoreDuplicates;
        [ObservableProperty] private bool _batchProcessing = true;
        [ObservableProperty] private bool _showLogTimestamp;
        [ObservableProperty] private bool _mediaViewerScaledDisplay = true;
        [ObservableProperty] private int _defaultVolume = 75;
        [ObservableProperty] private int _maxUndo = 5;
        [ObservableProperty] private bool _removeZZZNotGamePrefix = true;
        [ObservableProperty] private bool _overrideConcurrency;
        [ObservableProperty] private int _concurrencyOverride = 1;
        [ObservableProperty] private bool _logToDisk;
        [ObservableProperty] private int _searchDepth = 2;
        [ObservableProperty] private int _maxBatch = 300;
        [ObservableProperty] private int _recentFilesCount = 15;
        [ObservableProperty] private int _logVerbosity = 1;
        [ObservableProperty] private string _theme = "Default";
        [ObservableProperty] private string _color = "Blue";
        [ObservableProperty] private string _accentVariant = "Base";
        [ObservableProperty] private int _alternatingRowColorIndex = 1;
        [ObservableProperty] private int _gridLinesVisibilityIndex;
        [ObservableProperty] private int _appFontSize = 12;
        [ObservableProperty] private int _gridFontSize = 12;
        [ObservableProperty] private string _userId = "root";
        [ObservableProperty] private string _password = "linux";
        [ObservableProperty] private string _esDeRoot = string.Empty;
        [ObservableProperty] private string _esDeMediaBase = string.Empty;
        [ObservableProperty] private bool _checkForNewAndMissingGamesOnLoad;
        [ObservableProperty] private bool _useSimpleSystemPicker;

        #endregion

        #region Public Properties

        public static SharedDataService Instance => _instance.Value;

        public string ProfileType => _profileType;

        // Gamelist data and filtered view are stored here for access across view models.
        // MainWindowViewModel is responsible for keeping these up to date based on user interactions.
        public ObservableCollection<GameMetadataRow>? GamelistData { get; set; }
        public ReadOnlyObservableCollection<GameMetadataRow>? FilteredGamelistData { get; set; }

        // Updated by MainWindowViewModel whenever selection changes.
        public IList? SelectedItems { get; set; }

        public ObservableCollection<RecentFileItem> RecentFiles { get; } = new();

        // Resolved media folders for the current profile, built by SettingsService.
        private IReadOnlyList<AvailableMediaFolder> _availableMedia = [];
        public IReadOnlyList<AvailableMediaFolder> AvailableMedia
        {
            get => _availableMedia;
            private set
            {
                _availableMedia = value;
                OnPropertyChanged(nameof(AvailableMedia));
            }
        }

        // The full path to the current system's ROM/gamelist folder, built by SettingsService.
        public string? CurrentRomFolder { get; private set; }

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

            RomsFolder = settings.GetValue(SettingKeys.FolderPathsSection, SettingKeys.RomsFolder.Key);
            MamePath = settings.GetValue(SettingKeys.FolderPathsSection, SettingKeys.MamePath.Key);

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
            CheckForNewAndMissingGamesOnLoad = settings.GetBool(SettingKeys.CheckForNewAndMissingGamesOnLoad);
            UseSimpleSystemPicker = settings.GetBool(SettingKeys.UseSimpleSystemPicker);
            MediaViewerScaledDisplay = settings.GetBool(SettingKeys.ScaledDisplay);
            RemoveZZZNotGamePrefix = settings.GetBool(SettingKeys.RemoveZZZNotGamePrefix);

            OverrideConcurrency = settings.GetBool(SettingKeys.OverrideConcurrency);
            ConcurrencyOverride = settings.GetInt(SettingKeys.ConcurrencyOverride);
            LogToDisk = settings.GetBool(SettingKeys.LogToDisk);

            DefaultVolume = settings.GetInt(SettingKeys.Volume);
            MaxUndo = settings.GetInt(SettingKeys.MaxUndo);
            SearchDepth = settings.GetInt(SettingKeys.SearchDepth);
            MaxBatch = settings.GetInt(SettingKeys.BatchProcessingMaximum);
            RecentFilesCount = settings.GetInt(SettingKeys.RecentFilesCount);
            LogVerbosity = settings.GetInt(SettingKeys.LogVerbosity);

            Theme = settings.GetValue(SettingKeys.Theme);
            Color = settings.GetValue(SettingKeys.Color);
            AccentVariant = settings.GetValue(SettingKeys.AccentVariant);
            AlternatingRowColorIndex = settings.GetInt(SettingKeys.AlternatingRowColorIndex);
            GridLinesVisibilityIndex = settings.GetInt(SettingKeys.GridLinesVisibilityIndex);
            AppFontSize = settings.GetInt(SettingKeys.GlobalFontSize);
            GridFontSize = settings.GetInt(SettingKeys.GridFontSize);

            RefreshProfileState();
            CurrentRomFolder = _settings.CurrentRomFolder(RomsFolder, CurrentSystem);
            AvailableMedia = _settings.BuildAvailableMedia(
                _profileType,
                ResolveMediaBaseFolder(),
                _settings.GetSection(SettingKeys.MediaPathsSection) ?? new Dictionary<string, string>());

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
            _settings.SetValue(SettingKeys.ScaledDisplay.Section, SettingKeys.ScaledDisplay.Key,
                MediaViewerScaledDisplay.ToString());
        }

        public IReadOnlyDictionary<string, string> GetFileTypes() => _settings.GetFileTypes();

        public void SaveEsDeRoot(string path)
        {
            EsDeRoot = path;
            _settings.SetValue(SettingKeys.EsDeRoot.Section, SettingKeys.EsDeRoot.Key, path);
        }

        #endregion

        #region Property Change Callbacks

        partial void OnEsDeRootChanged(string value)
        {
            RefreshProfileState();
            CurrentRomFolder = _settings.CurrentRomFolder(RomsFolder, CurrentSystem);
            AvailableMedia = _settings.BuildAvailableMedia(
                _profileType,
                ResolveMediaBaseFolder(),
                _settings.GetSection(SettingKeys.MediaPathsSection) ?? new Dictionary<string, string>());
        }

        partial void OnEsDeMediaBaseChanged(string value)
        {
            AvailableMedia = _settings.BuildAvailableMedia(
                _profileType,
                ResolveMediaBaseFolder(),
                _settings.GetSection(SettingKeys.MediaPathsSection) ?? new Dictionary<string, string>());
        }

        partial void OnRomsFolderChanged(string value)
        {
            CurrentRomFolder = _settings.CurrentRomFolder(RomsFolder, CurrentSystem);
            AvailableMedia = _settings.BuildAvailableMedia(
                _profileType,
                ResolveMediaBaseFolder(),
                _settings.GetSection(SettingKeys.MediaPathsSection) ?? new Dictionary<string, string>());
        }

        partial void OnCurrentSystemChanged(string? value)
        {
            CurrentRomFolder = _settings.CurrentRomFolder(RomsFolder, CurrentSystem);
            AvailableMedia = _settings.BuildAvailableMedia(
                _profileType,
                ResolveMediaBaseFolder(),
                _settings.GetSection(SettingKeys.MediaPathsSection) ?? new Dictionary<string, string>());
        }

        #endregion

        #region Private Methods

        private string? ResolveMediaBaseFolder() =>
            _profileType == SettingKeys.ProfileTypeEsDe
                ? _settings.EsDeMediaDirectory(EsDeMediaBase, CurrentSystem)
                : Path.GetDirectoryName(XmlFilename) ?? _settings.CurrentGamelistFolder(_settings.GamelistsRootFolder(_profileType, EsDeRoot, RomsFolder), CurrentSystem);

        private void RefreshProfileState()
        {
            var settings = _settings;

            var rawType = settings.GetValue(SettingKeys.ProfileType);
            var resolvedType = string.Equals(rawType, SettingKeys.ProfileTypeEsDe, StringComparison.OrdinalIgnoreCase)
                ? SettingKeys.ProfileTypeEsDe
                : SettingKeys.ProfileTypeEs;

            if (_profileType != resolvedType)
            {
                _profileType = resolvedType;
                OnPropertyChanged(nameof(ProfileType));
            }

            if (resolvedType == SettingKeys.ProfileTypeEsDe)
            {
                EsDeRoot = settings.GetValue(SettingKeys.EsDeRoot);

                if (!string.IsNullOrWhiteSpace(EsDeRoot) && Directory.Exists(EsDeRoot))
                {
                    var detected = SettingsService.ReadPathsFromEsDeSettings(EsDeRoot);
                    EsDeMediaBase = detected.MediaDirectory ?? string.Empty;
                    RomsFolder = detected.RomDirectory ?? string.Empty;
                }
                else
                {
                    EsDeMediaBase = string.Empty;
                    RomsFolder = string.Empty;
                }
            }
            else
            {
                EsDeRoot = string.Empty;
                EsDeMediaBase = string.Empty;
            }
        }

        #endregion
    }
}