using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Classes.Helpers;
using System.IO;

namespace Gamelist_Manager.Services
{
    public partial class SharedDataService
    {
        #region Private Fields

        private bool _isEsDeMode;

        #endregion

        #region Observable Properties

        [ObservableProperty]
        private string _esDeRoot = string.Empty;

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

        #region Public Methods

        // Updates the ES-DE root path in memory and persists it to the active profile.
        public void SaveEsDeRoot(string path)
        {
            EsDeRoot = path;
            SettingsService.Instance.SetValue(SettingKeys.EsDeSection, SettingKeys.EsDeRoot, path);
        }

        #endregion

        #region Private Methods

        private void RefreshEsDeMode()
        {
            var settings = SettingsService.Instance;

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
