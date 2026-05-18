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
        public bool DefaultSfxEnabled => !string.IsNullOrEmpty(DefaultSuffix);


        // --- Observable state ---

        [ObservableProperty] private bool _enabled;
        [ObservableProperty] private string _path = string.Empty;
        [ObservableProperty] private string _suffix = string.Empty;
        [ObservableProperty] private string _displayPath = string.Empty;

        private bool _sfxEnabled;
        public bool SfxEnabled
        {
            get => _sfxEnabled;
            set => SetProperty(ref _sfxEnabled, value);
        }

        // --- Display state (set by SettingsViewModel via RefreshDisplayState) ---

        [ObservableProperty] private bool _isNotEsDeMode = true;
        [ObservableProperty] private bool _isPathReadOnly = false;
        [ObservableProperty] private bool _isMediaEnabled = true;
        [ObservableProperty] private bool _isSuffixEnabled = true;

        // --- Mutations ---

        public void ResetToDefaults()
        {
            Path = DefaultPath;
            Suffix = DefaultSuffix;
            SfxEnabled = DefaultSfxEnabled;
            Enabled = DefaultEnabled;
        }

        partial void OnPathChanged(string value)
        {
            if (IsNotEsDeMode)
                DisplayPath = value;
        }

        public void RefreshDisplayState(bool isEsDe, bool isEsDeSupported, string esDeMediaBase, string? currentSystem, string? esDeFolderName)
        {
            IsNotEsDeMode = !isEsDe;
            IsPathReadOnly = isEsDe;
            IsMediaEnabled = !isEsDe || isEsDeSupported;
            IsSuffixEnabled = Enabled && !isEsDe;

            if (!isEsDe)
                DisplayPath = Path;
            else if (!string.IsNullOrEmpty(esDeFolderName) && !string.IsNullOrEmpty(esDeMediaBase) && !string.IsNullOrEmpty(currentSystem))
                DisplayPath = System.IO.Path.Combine(esDeMediaBase, currentSystem, esDeFolderName);
            else
                DisplayPath = esDeFolderName ?? string.Empty;
        }
    }
}