using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Services;

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