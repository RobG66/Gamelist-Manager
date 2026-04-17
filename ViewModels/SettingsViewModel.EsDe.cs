using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Services;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class SettingsViewModel
{
    #region Observable Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsDePathsVisible))]
    private bool _isEsDeProfile;

    [ObservableProperty]
    private string _esDeRoot = string.Empty;

    #endregion

    #region Public Properties

    // Controls visibility of the ES-DE Root row in the Paths tab.
    public bool EsDePathsVisible => IsEsDeProfile;

    #endregion

    #region Commands

    [RelayCommand]
    private async Task BrowseEsDeRootAsync()
    {
        var chosen = await FolderPickerHelper.BrowseForFolderAsync(
            "Select ES-DE Root Folder",
            EsDeRoot);

        if (string.IsNullOrEmpty(chosen))
            return;

        EsDeRoot = chosen;

        if (string.IsNullOrWhiteSpace(RomsPath))
        {
            var detected = GamelistService.ReadRomDirectoryFromEsDeSettings(chosen);
            if (detected != null)
                RomsPath = detected;
        }
    }

    #endregion

    #region Internal Methods

    internal void LoadEsDeSettings()
    {
        var profileType = SettingsService.Instance.GetValue(SettingKeys.EsDeSection, SettingKeys.ProfileType, SettingKeys.ProfileTypeStandard);
        IsEsDeProfile = string.Equals(profileType, SettingKeys.ProfileTypeEsDe, System.StringComparison.OrdinalIgnoreCase);
        EsDeRoot = SettingsService.Instance.GetValue(SettingKeys.EsDeSection, SettingKeys.EsDeRoot, string.Empty);
    }

    internal void SaveEsDeSettings()
    {
        SettingsService.Instance.SetValue(SettingKeys.EsDeSection, SettingKeys.ProfileType,
            IsEsDeProfile ? SettingKeys.ProfileTypeEsDe : SettingKeys.ProfileTypeStandard);
        SettingsService.Instance.SetValue(SettingKeys.EsDeSection, SettingKeys.EsDeRoot, EsDeRoot);
    }

    #endregion
}
