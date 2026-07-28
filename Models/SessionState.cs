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
        #region Singleton

        private static readonly Lazy<SessionState> _instance = new(() => new SessionState());
        public static SessionState Instance => _instance.Value;

        #endregion

        #region Constructor

        private bool _handlingSettingsChange;

        private SessionState()
        {
            SettingsState.Instance.PropertyChanged += (_, e) =>
            {
                if (_handlingSettingsChange) return;
                _handlingSettingsChange = true;
                try
                {
                    switch (e.PropertyName)
                    {
                        case nameof(SettingsState.RootRomFolder):
                            OnPropertyChanged(nameof(CurrentRomFolder));
                            break;
                        case nameof(SettingsState.RootMediaFolder):
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

        #endregion

        #region Current Gamelist

        [ObservableProperty] private string? _xmlFilename;
        [ObservableProperty] private string? _currentSystem;
        [ObservableProperty] private bool _isDataChanged;

        partial void OnXmlFilenameChanged(string? value)
        {
            OnPropertyChanged(nameof(CurrentRomFolder));
            OnPropertyChanged(nameof(CurrentMediaFolder));
        }

        partial void OnCurrentSystemChanged(string? value)
        {
            OnPropertyChanged(nameof(CurrentRomFolder));
            OnPropertyChanged(nameof(CurrentMediaFolder));
        }

        #endregion

        #region Operation Flags

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _isScraping;
        [ObservableProperty] private bool _enableEdit;

        #endregion

        #region Video Playback

        [ObservableProperty] private bool _videoUserPaused;

        #endregion

        #region Grid

        public IList? SelectedItems { get; set; }
        public ObservableCollection<GameMetadataRow>? GamelistData { get; private set; }

        private ReadOnlyObservableCollection<GameMetadataRow>? _filteredGamelistData;
        public ReadOnlyObservableCollection<GameMetadataRow>? FilteredGamelistData
        {
            get => _filteredGamelistData;
            set => SetProperty(ref _filteredGamelistData, value);
        }

        #endregion

        #region Recent Files

        public ObservableCollection<RecentFileItem> RecentFiles { get; } = [];

        #endregion

        #region Available Media

        public IReadOnlyList<AvailableMediaFolder> AvailableMedia { get; private set; } = [];

        public void RefreshAvailableMedia()
        {
            AvailableMedia = MediaPathResolver.BuildAvailableMedia(
                SettingsState.Instance.ProfileType, CurrentSystem, CurrentMediaFolder, SettingsState.Instance.MediaPaths);
            OnPropertyChanged(nameof(AvailableMedia));
        }

        #endregion

        #region Profile

        public IReadOnlyList<MetaDataDecl> AvailableColumns => MetadataService.GetColumnDeclarations();
        public IReadOnlyList<MetaDataDecl> AvailableToggleableColumns => MetadataService.GetToggleableColumns();
        public IEnumerable<MetaDataDecl> XmlPersistedFields => MetadataService.GetXmlPersistedFields();
        public bool GamelistHasMediaPaths =>
            SettingKeys.GetProfileTypeOption(SettingsState.Instance.ProfileType).GamelistHasMediaPaths;

        #endregion

        #region Derived Current Properties

        public string? CurrentRomFolder => SettingsState.Instance.ProfileType == SettingKeys.ProfileTypeEsDe
            ? FilePathHelper.CurrentRomFolder(SettingsState.Instance.RootRomFolder, CurrentSystem)
            : Path.GetDirectoryName(XmlFilename);

        public string? CurrentMediaFolder => string.IsNullOrEmpty(SettingsState.Instance.RootMediaFolder) || string.IsNullOrEmpty(CurrentSystem)
            ? null
            : Path.Combine(SettingsState.Instance.RootMediaFolder, CurrentSystem);

        #endregion

        #region Mutations

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

        #endregion
    }
}
