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

        private MetaDataDecl? Decl => GamelistMetaData.GetDeclByType(Key);

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

        public bool IsMediaEnabled => SharedDataService.Instance.ProfileType != SettingKeys.ProfileTypeEsDe || (Decl?.IsEsDeSupported ?? false);

        public bool EffectiveEnabled => Enabled && (SharedDataService.Instance.ProfileType != SettingKeys.ProfileTypeEsDe || (Decl?.IsEsDeSupported ?? false));

        // In ES-DE mode shows the resolved media directory path; otherwise the relative path.
        // The setter writes back to Path so two-way TextBox binding works in non-ES-DE mode.
        public string DisplayPath
        {
            get
            {
                if (SharedDataService.Instance.ProfileType != SettingKeys.ProfileTypeEsDe)
                    return Path;

                var esDeFolderName = Decl?.EsDeFolderName;
                if (string.IsNullOrEmpty(esDeFolderName))
                    return string.Empty;

                var shared = SharedDataService.Instance;
                var mediaDir = SettingsService.Instance.EsDeMediaDirectory(shared.EsDeMediaBase, shared.CurrentSystem);
                return !string.IsNullOrEmpty(mediaDir)
                    ? System.IO.Path.Combine(mediaDir, esDeFolderName)
                    : esDeFolderName;
            }
            set
            {
                if (SharedDataService.Instance.ProfileType != SettingKeys.ProfileTypeEsDe)
                    Path = value;
            }
        }

        public void NotifyProfileTypeChanged()
        {
            OnPropertyChanged(nameof(DisplayPath));
            OnPropertyChanged(nameof(IsPathReadOnly));
            OnPropertyChanged(nameof(IsNotEsDeMode));
            OnPropertyChanged(nameof(IsMediaEnabled));
            OnPropertyChanged(nameof(IsSuffixEnabled));
            OnPropertyChanged(nameof(EffectiveEnabled));
        }

        public void ResetToDefaults()
        {
            Path = DefaultPath;
            Suffix = DefaultSuffix;
            SfxEnabled = DefaultSfxEnabled;
        }

        partial void OnPathChanged(string value) => OnPropertyChanged(nameof(DisplayPath));
    }
}