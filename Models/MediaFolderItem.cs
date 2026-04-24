using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Services;

namespace Gamelist_Manager.Models
{
    public partial class MediaFolderItem : ObservableObject
    {
        public string Key { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string DefaultPath { get; init; } = string.Empty;
        public string DefaultSuffix { get; init; } = string.Empty;
        public bool DefaultEnabled { get; init; } = true;
        public bool DefaultSfxEnabled => !string.IsNullOrEmpty(DefaultSuffix);

        // ES-DE subfolder name for this type (empty = not supported by ES-DE).
        public string EsDeFolderName { get; init; } = string.Empty;
        public bool IsEsDeSupported => !string.IsNullOrEmpty(EsDeFolderName);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSuffixEnabled))]
        [NotifyPropertyChangedFor(nameof(SfxEnabled))]
        private bool _enabled;

        [ObservableProperty] private string _path = string.Empty;
        [ObservableProperty] private string _suffix = string.Empty;

        private bool _sfxEnabled;

        // Returns false when suffixes are not applicable so the checkbox appears unchecked.
        // The backing field retains the user's preference and is restored when re-enabled.
        public bool SfxEnabled
        {
            get => _sfxEnabled && IsSuffixEnabled;
            set => SetProperty(ref _sfxEnabled, value);
        }

        // Suffix controls are only editable when the row is enabled and suffixes are not
        // globally disabled (e.g. in ES-DE mode, where suffixes have no meaning).
        public bool IsSuffixEnabled => Enabled && SharedDataService.Instance.ProfileType != SettingKeys.ProfileTypeEsDe;

        // Path textbox is read-only in ES-DE mode — paths are fixed by the ES-DE layout.
        public bool IsPathReadOnly => SharedDataService.Instance.ProfileType == SettingKeys.ProfileTypeEsDe;

        // Used to hide suffix columns and browse button in ES-DE mode.
        public bool IsNotEsDeMode => SharedDataService.Instance.ProfileType != SettingKeys.ProfileTypeEsDe;

        // Checkbox is disabled for types not supported by ES-DE — they cannot be enabled.
        public bool IsCheckboxEnabled => SharedDataService.Instance.ProfileType != SettingKeys.ProfileTypeEsDe || IsEsDeSupported;

        // In ES-DE mode, unsupported types are always treated as disabled.
        public bool EffectiveEnabled => Enabled && (SharedDataService.Instance.ProfileType != SettingKeys.ProfileTypeEsDe || IsEsDeSupported);

        // In ES-DE mode shows the resolved media directory path; otherwise the relative path.
        public string DisplayPath
        {
            get
            {
                if (SharedDataService.Instance.ProfileType != SettingKeys.ProfileTypeEsDe)
                    return Path;

                if (string.IsNullOrEmpty(EsDeFolderName))
                    return string.Empty;

                var mediaDir = SharedDataService.Instance.EsDeMediaDirectory;
                return !string.IsNullOrEmpty(mediaDir)
                    ? System.IO.Path.Combine(mediaDir, EsDeFolderName)
                    : EsDeFolderName;
            }
        }

        public void NotifyProfileTypeChanged()
        {
            OnPropertyChanged(nameof(DisplayPath));
            OnPropertyChanged(nameof(IsPathReadOnly));
            OnPropertyChanged(nameof(IsNotEsDeMode));
            OnPropertyChanged(nameof(IsCheckboxEnabled));
            OnPropertyChanged(nameof(IsSuffixEnabled));
            OnPropertyChanged(nameof(EffectiveEnabled));
        }

        public void ResetToDefaults()
        {
            Path = DefaultPath;
            Suffix = DefaultSuffix;
            SfxEnabled = DefaultSfxEnabled;
        }
    }
}