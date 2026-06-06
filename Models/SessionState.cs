using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace Gamelist_Manager.Models
{
    public partial class SessionState : ObservableObject
    {
        private static readonly Lazy<SessionState> _instance = new(() => new SessionState());
        public static SessionState Instance => _instance.Value;

        private bool _handlingSettingsChange;
        private string? _esDeDetectedMediaRoot;

        private SessionState()
        {
            // Prime the ES-DE media root cache on construction
            _esDeDetectedMediaRoot = EsDePathResolver.ReadPathsFromEsDeSettings(
                SettingsState.Instance.EsDeRoot).MediaDirectory;

            SettingsState.Instance.PropertyChanged += (_, e) =>
            {
                if (_handlingSettingsChange) return;
                _handlingSettingsChange = true;
                try
                {
                    switch (e.PropertyName)
                    {
                        case nameof(SettingsState.RomsFolder):
                            OnPropertyChanged(nameof(CurrentRomFolder));
                            OnPropertyChanged(nameof(GamelistsRootFolder));
                            break;
                        case nameof(SettingsState.EsDeRoot):
                            _esDeDetectedMediaRoot = EsDePathResolver.ReadPathsFromEsDeSettings(
                                SettingsState.Instance.EsDeRoot).MediaDirectory;
                            OnPropertyChanged(nameof(GamelistsRootFolder));
                            OnPropertyChanged(nameof(MediaRootFolder));
                            OnPropertyChanged(nameof(CurrentMediaFolder));
                            break;
                    }
                }
                finally
                {
                    _handlingSettingsChange = false;
                }
            };
        }

        // --- Current gamelist ---

        [ObservableProperty] private string? _xmlFilename;
        [ObservableProperty] private string? _currentSystem;
        [ObservableProperty] private bool _isDataChanged;

        partial void OnXmlFilenameChanged(string? value)
        {
            OnPropertyChanged(nameof(CurrentRomFolder));
            OnPropertyChanged(nameof(MediaRootFolder));
            OnPropertyChanged(nameof(CurrentMediaFolder));
        }

        partial void OnCurrentSystemChanged(string? value)
        {
            OnPropertyChanged(nameof(CurrentRomFolder));
            OnPropertyChanged(nameof(CurrentMediaFolder));
        }

        // --- Operation flags ---

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _isScraping;
        [ObservableProperty] private bool _enableEdit;

        // --- Video playback ---

        [ObservableProperty] private bool _videoUserPaused;

        // --- Grid ---

        public IList? SelectedItems { get; set; }
        public ObservableCollection<GameMetadataRow>? GamelistData { get; private set; }

        private ReadOnlyObservableCollection<GameMetadataRow>? _filteredGamelistData;
        public ReadOnlyObservableCollection<GameMetadataRow>? FilteredGamelistData
        {
            get => _filteredGamelistData;
            set => SetProperty(ref _filteredGamelistData, value);
        }

        // --- Recent files ---

        public ObservableCollection<RecentFileItem> RecentFiles { get; } = [];

        // --- Available media ---

        public IReadOnlyList<AvailableMediaFolder> AvailableMedia { get; private set; } = [];

        public void RefreshAvailableMedia()
        {
            AvailableMedia = MediaPathResolver.BuildAvailableMedia(
                ProfileType, CurrentSystem, CurrentMediaFolder, SettingsState.Instance.MediaPaths);
            OnPropertyChanged(nameof(AvailableMedia));
        }

        // --- Profile type ---

        [ObservableProperty] private string _profileType = SettingKeys.ProfileTypeEs;

        partial void OnProfileTypeChanged(string value)
        {
            OnPropertyChanged(nameof(AvailableColumns));
            OnPropertyChanged(nameof(AvailableToggleableColumns));
            OnPropertyChanged(nameof(XmlPersistedFields));
            OnPropertyChanged(nameof(GamelistsRootFolder));
            OnPropertyChanged(nameof(CurrentRomFolder));
            OnPropertyChanged(nameof(MediaRootFolder));
            OnPropertyChanged(nameof(CurrentMediaFolder));
        }

        // --- Profile-filtered metadata ---

        public IReadOnlyList<MetaDataDecl> AvailableColumns => MetadataService.GetColumnDeclarations();
        public IReadOnlyList<MetaDataDecl> AvailableToggleableColumns => MetadataService.GetToggleableColumns();

        public IEnumerable<MetaDataDecl> XmlPersistedFields => MetadataService.GetXmlPersistedFields();

        // --- Derived properties ---
        
        public string? CurrentRomFolder => ProfileType == SettingKeys.ProfileTypeEsDe
            ? FilePathHelper.CurrentRomFolder(SettingsState.Instance.RomsFolder, CurrentSystem)
            : Path.GetDirectoryName(XmlFilename);

        public string? GamelistsRootFolder => ProfileType == SettingKeys.ProfileTypeEsDe
            ? (string.IsNullOrEmpty(SettingsState.Instance.EsDeRoot)
                ? null
                : Path.Combine(SettingsState.Instance.EsDeRoot, "gamelists"))
            : SettingsState.Instance.RomsFolder;

        public string? MediaRootFolder => ProfileType == SettingKeys.ProfileTypeEsDe
            ? _esDeDetectedMediaRoot
            : Path.GetDirectoryName(Path.GetDirectoryName(XmlFilename));

        public string? CurrentMediaFolder => string.IsNullOrEmpty(MediaRootFolder) || string.IsNullOrEmpty(CurrentSystem)
            ? null
            : Path.Combine(MediaRootFolder, CurrentSystem);

        // --- Mutations ---

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
            AvailableMedia = [];
            OnPropertyChanged(nameof(AvailableMedia));
        }
    }
}