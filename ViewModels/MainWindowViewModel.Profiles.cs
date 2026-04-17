using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Services;
using Gamelist_Manager.Views;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class MainWindowViewModel
{
    #region Private Fields
    private readonly ProfileService _profileService = ProfileService.Instance;
    #endregion

    #region Observable Properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMultipleProfiles))]
    private string _activeProfileName = string.Empty;
    #endregion

    #region Public Properties
    public ObservableCollection<string> Profiles { get; } = new();
    public bool HasMultipleProfiles => Profiles.Count > 1;
    #endregion

    #region Public Methods
    public void RefreshProfiles()
    {
        Profiles.Clear();
        foreach (var p in _profileService.GetProfiles())
            Profiles.Add(p);
        ActiveProfileName = _profileService.ActiveProfile;
        OnPropertyChanged(nameof(HasMultipleProfiles));
    }

    // Called once from MainWindow_Loaded when no profiles exist on disk.
    public async Task ResolveMissingProfileAsync()
    {
        if (!_profileService.NoProfilesExist)
            return;

        var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "No Profiles Found",
            Message = "No profiles were found.",
            DetailMessage = "A new Default profile will be created. What type should it be?",
            IconTheme = DialogIconTheme.Warning,
            Button1Text = "ES-DE",
            Button2Text = "",
            Button3Text = "Standard"
        });

        var profileType = result == ThreeButtonResult.Button1
            ? SettingKeys.ProfileTypeEsDe
            : SettingKeys.ProfileTypeStandard;

        _profileService.CreateDefaultProfile(profileType);

        if (profileType == SettingKeys.ProfileTypeEsDe)
            await PromptEsDeRootAsync("An ES-DE profile has been created.");

        ApplyProfileSwitch(ProfileService.DefaultProfileName);

        _profileService.ClearNoProfilesFlag();
        RefreshProfiles();
    }
    #endregion

    #region Commands
    [RelayCommand]
    private async Task SwitchProfile(string profileName)
    {
        if (string.Equals(profileName, _profileService.ActiveProfile, StringComparison.OrdinalIgnoreCase))
            return;

        if (!await CheckUnsavedChangesAsync())
            return;

        UnloadGamelist();
        ApplyProfileSwitch(profileName);
    }
    #endregion

    #region Private Methods
    private void ApplyProfileSwitch(string profileName)
    {
        _profileService.SetActiveProfile(profileName);
        _settingsService.SwitchProfile(_profileService.ActiveProfilePath);
        _sharedData.LoadFromSettings();

        LoadBehaviorSettings();
        LoadColumnSettings();
        LoadRecentFilesFromSettings();

        var themeIndex = ThemeService.GetThemeIndex(_sharedData.Theme);
        var colorIndex = ThemeService.GetColorIndex(_sharedData.Color);
        ThemeService.ApplyTheme(themeIndex, colorIndex);

        LoadSystems();
        ActiveProfileName = profileName;
    }
    #endregion
}