using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Services;
using Gamelist_Manager.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class MainWindowViewModel
{
    #region Private Fields
    private const string EsDeLabel = "ES-DE";
    private const string EsLabel = "ES";
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
            Button1Text = EsDeLabel,
            Button2Text = "",
            Button3Text = EsLabel
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

    #region Internal Methods

    // Checks that the active profile type matches the gamelist being loaded.
    // If mismatched, prompts the user to switch to or create a matching profile.
    // Returns true if the load should proceed, false if it should be aborted.
    internal async Task<bool> EnsureMatchingProfileAsync(string gamelistPath)
    {
        var gamelistIsEsDe = IsEsDeGamelistPath(gamelistPath);
        var profileIsEsDe = _sharedData.IsEsDeMode;

        if (gamelistIsEsDe == profileIsEsDe)
            return true;

        var requiredType = gamelistIsEsDe ? SettingKeys.ProfileTypeEsDe : SettingKeys.ProfileTypeStandard;
        var requiredLabel = gamelistIsEsDe ? EsDeLabel : EsLabel;

        // Find existing profiles whose type matches what is required.
        var matchingProfiles = _profileService.GetProfiles()
            .Where(p => string.Equals(
                _profileService.GetProfileType(p), requiredType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        string switchTo;

        if (matchingProfiles.Count > 0)
        {
            // There is at least one matching profile — offer to switch.
            var profileNames = string.Join(", ", matchingProfiles);
            var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Profile Mismatch",
                Message = $"This gamelist requires an {requiredLabel} profile.",
                DetailMessage = $"The active profile is not configured for {requiredLabel} gamelists.\n\n" +
                                $"Available {requiredLabel} profiles: {profileNames}\n\n" +
                                $"Switch to '{matchingProfiles[0]}'?",
                IconTheme = DialogIconTheme.Warning,
                Button1Text = "Cancel",
                Button2Text = "",
                Button3Text = "Switch"
            });

            if (result != ThreeButtonResult.Button3) return false;

            switchTo = matchingProfiles[0];
        }
        else
        {
            // No matching profile exists — offer to create one automatically.
            var suggestedName = gamelistIsEsDe ? EsDeLabel : EsLabel;
            var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Profile Mismatch",
                Message = $"This gamelist requires an {requiredLabel} profile.",
                DetailMessage = $"No {requiredLabel} profile exists yet.\n\n" +
                                $"A new profile named '{suggestedName}' will be created and activated. Continue?",
                IconTheme = DialogIconTheme.Warning,
                Button1Text = "Cancel",
                Button2Text = "",
                Button3Text = "Create"
            });

            if (result != ThreeButtonResult.Button3) return false;

            var created = _profileService.CreateTypedProfile(suggestedName, requiredType);
            if (created == null) return false;

            switchTo = created;
        }

        ApplyProfileSwitch(switchTo);
        RefreshProfiles();
        return true;
    }

    // Prompts the user to confirm or override the ES-DE media root if ES-DE mode is active
    // and the current root is not yet set to an existing directory.
    internal async Task PromptEsDeMediaRootIfNeededAsync()
    {
        if (!_sharedData.IsEsDeMode) return;

        if (!string.IsNullOrWhiteSpace(_sharedData.EsDeRoot) &&
            Directory.Exists(_sharedData.EsDeRoot))
            return;

        await PromptEsDeRootAsync();
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

    // Returns true when the gamelist file path contains the ES-DE gamelists folder pattern.
    private static bool IsEsDeGamelistPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.IndexOf("/ES-DE/gamelists/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private async Task PromptEsDeRootAsync(string contextMessage = "An ES-DE gamelist has been detected.")
    {
        var currentRoot = _sharedData.EsDeRoot;
        var hasRoot = !string.IsNullOrWhiteSpace(currentRoot) && Directory.Exists(currentRoot);

        var detailMessage = hasRoot
            ? $"The ES-DE root folder is currently set to:\n{currentRoot}\n\nPress Browse to keep or change this location."
            : "The ES-DE root folder has not been set.\n\nPress Browse to choose the ES-DE root folder.";

        var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "ES-DE Location",
            Message = contextMessage,
            DetailMessage = detailMessage,
            IconTheme = DialogIconTheme.Info,
            Button1Text = "Cancel",
            Button2Text = "",
            Button3Text = "Browse"
        });

        if (result != ThreeButtonResult.Button3) return;

        var chosen = await FolderPickerHelper.BrowseForFolderAsync(
            "Select ES-DE Root Folder",
            _sharedData.EsDeRoot);

        if (string.IsNullOrEmpty(chosen)) return;

        _sharedData.SaveEsDeRoot(chosen);

        // Always re-detect — the user may have changed es_settings.xml outside this app.
        var detected = SettingsService.ReadPathsFromEsDeSettings(chosen);
        _sharedData.RomsFolder = detected.RomDirectory ?? string.Empty;
        _sharedData.EsDeMediaBase = detected.MediaDirectory ?? string.Empty;
    }
    #endregion
}