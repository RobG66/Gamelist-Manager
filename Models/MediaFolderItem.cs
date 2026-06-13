using CommunityToolkit.Mvvm.ComponentModel;

namespace Gamelist_Manager.Models
{
    public partial class MediaFolderItem : ObservableObject
    {
        // --- Identity / defaults ---

        public string Key { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string DefaultPath { get; init; } = string.Empty;
        public string DefaultSuffix { get; init; } = string.Empty;
        public bool DefaultEnabled { get; init; } = true;
        public bool DefaultSuffixEnabled => !string.IsNullOrEmpty(DefaultSuffix);


        // --- Observable state ---

        [ObservableProperty] private bool _enabled;
        [ObservableProperty] private string _path = string.Empty;
        [ObservableProperty] private string _suffix = string.Empty;
        [ObservableProperty] private string _displayPath = string.Empty;

        [ObservableProperty] private bool _isSuffixEnabled;

        public bool IsSuffixInputEnabled => Enabled && IsSuffixEnabled;

        private bool _syncingSuffix;

        // --- Display state (set by SettingsViewModel via RefreshDisplayState) ---

        [ObservableProperty] private bool _areSuffixesAllowed;
        [ObservableProperty] private bool _arePathsReadOnly;
        [ObservableProperty] private bool _canMediaOverrideCheckboxBeEnabled = true;

        // --- Mutations ---

        public void ResetToDefaults()
        {
            Path = DefaultPath;
            Enabled = DefaultEnabled;
            IsSuffixEnabled = DefaultSuffixEnabled;
        }

        public void LoadSuffixState(bool isSuffixEnabled, string suffix)
        {
            _syncingSuffix = true;
            try
            {
                IsSuffixEnabled = isSuffixEnabled;
                Suffix = isSuffixEnabled ? suffix : string.Empty;
            }
            finally
            {
                _syncingSuffix = false;
            }

            OnPropertyChanged(nameof(IsSuffixInputEnabled));
        }

        partial void OnEnabledChanged(bool value) => OnPropertyChanged(nameof(IsSuffixInputEnabled));

        partial void OnIsSuffixEnabledChanged(bool value)
        {
            if (!_syncingSuffix)
                Suffix = value ? DefaultSuffix : string.Empty;

            OnPropertyChanged(nameof(IsSuffixInputEnabled));
        }

        partial void OnPathChanged(string value)
        {
            if (!ArePathsReadOnly)
                DisplayPath = value;
        }

        partial void OnDisplayPathChanged(string value)
        {
            if (!ArePathsReadOnly)
                Path = value;
        }

        public void RefreshDisplayState(ProfileTypeOption profile, MetaDataDecl decl, string esDeMediaBase, string? currentSystem)
        {
            AreSuffixesAllowed = profile.MediaFilenamesUseSuffixes;
            ArePathsReadOnly = !profile.GamelistHasMediaPaths;
            CanMediaOverrideCheckboxBeEnabled = profile.IncludesMediaFolder(decl);

            if (profile.GamelistHasMediaPaths)
                DisplayPath = Path;
            else if (!string.IsNullOrEmpty(decl.EsDeFolderName) && !string.IsNullOrEmpty(esDeMediaBase) && !string.IsNullOrEmpty(currentSystem))
                DisplayPath = System.IO.Path.Combine(esDeMediaBase, currentSystem, decl.EsDeFolderName);
            else
                DisplayPath = decl.EsDeFolderName;
        }
    }
}