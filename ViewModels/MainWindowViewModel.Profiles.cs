using Avalonia.Controls;
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
        {
            // Profiles exist — active profile was loaded at startup. Check ES-DE root now
            // so a misconfigured profile is caught on first launch, not only on switching.
            await ApplyProfileSwitchAsync(_profileService.ActiveProfile);
            return;
        }

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
            : SettingKeys.ProfileTypeEs;

        _profileService.CreateDefaultProfile(profileType);

        await ApplyProfileSwitchAsync(ProfileService.DefaultProfileName);

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
        var gamelistType = IsEsDeGamelistPath(gamelistPath) ? SettingKeys.ProfileTypeEsDe : SettingKeys.ProfileTypeEs;
        var profileType = _sharedData.ProfileType;

        if (string.Equals(gamelistType, profileType, StringComparison.OrdinalIgnoreCase))
            return true;

        var requiredLabel = gamelistType == SettingKeys.ProfileTypeEsDe ? EsDeLabel : EsLabel;

        // Find existing profiles whose type matches what is required.
        var matchingProfiles = _profileService.GetProfiles()
            .Where(p => string.Equals(
                _profileService.GetProfileType(p), gamelistType, StringComparison.OrdinalIgnoreCase))
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
            var suggestedName = gamelistType == SettingKeys.ProfileTypeEsDe ? EsDeLabel : EsLabel;
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

            var created = _profileService.CreateTypedProfile(suggestedName, gamelistType);
            if (created == null) return false;

            switchTo = created;
        }

        await ApplyProfileSwitchAsync(switchTo);
        RefreshProfiles();

        return true;
    }

    internal async Task PromptEsDeRootAsync(string contextMessage = "An ES-DE gamelist has been detected.", Window? owner = null)
    {
        var currentRoot = _sharedData.EsDeRoot;
        var hasRoot = !string.IsNullOrWhiteSpace(currentRoot) && Directory.Exists(currentRoot);

        // If no root is set yet, check the home folder for a default ES-DE installation.
        if (!hasRoot)
        {
            var homeEsDe = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ES-DE");

            if (Directory.Exists(homeEsDe))
            {
                var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
                {
                    Title = "ES-DE Location",
                    Message = contextMessage,
                    DetailMessage = $"An ES-DE folder was found at:\n{homeEsDe}\n\nUse this as your ES-DE root folder?",
                    IconTheme = DialogIconTheme.Info,
                    Button1Text = "Browse",
                    Button2Text = "",
                    Button3Text = "Use This"
                }, owner);

                if (result == ThreeButtonResult.Button3)
                {
                    await ApplyEsDeRootAsync(homeEsDe);
                    return;
                }

                // Fall through to Browse if they declined the suggestion.
            }
        }

        var detailMessage = hasRoot
            ? $"The ES-DE root folder is currently set to:\n{currentRoot}\n\nPress Browse to keep or change this location."
            : "The ES-DE root folder has not been set.\n\nPress Browse to choose the ES-DE root folder.";

        var browseResult = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "ES-DE Location",
            Message = contextMessage,
            DetailMessage = detailMessage,
            IconTheme = DialogIconTheme.Info,
            Button1Text = "Cancel",
            Button2Text = "",
            Button3Text = "Browse"
        }, owner);

        if (browseResult != ThreeButtonResult.Button3) return;

        var chosen = await FolderPickerHelper.BrowseForFolderAsync(
            "Select ES-DE Root Folder",
            _sharedData.EsDeRoot,
            owner);

        if (string.IsNullOrEmpty(chosen)) return;

        await ApplyEsDeRootAsync(chosen);
    }

    private async Task ApplyEsDeRootAsync(string root)
    {
        _sharedData.SaveEsDeRoot(root);
        var detected = SettingsService.ReadPathsFromEsDeSettings(root);
        _sharedData.RomsFolder = detected.RomDirectory ?? string.Empty;
        _sharedData.EsDeMediaBase = detected.MediaDirectory ?? string.Empty;
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

        if (IsGamelistLoaded)
        {
            var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Switch Profile",
                Message = $"Switch to profile '{profileName}'?",
                DetailMessage = "The current gamelist will be unloaded.",
                IconTheme = DialogIconTheme.Warning,
                Button1Text = "Cancel",
                Button2Text = "",
                Button3Text = "Switch"
            });

            if (result != ThreeButtonResult.Button3)
                return;
        }

        UnloadGamelist();
        await ApplyProfileSwitchAsync(profileName);
    }
    #endregion

    #region Private Methods

    // Activates a profile and updates all dependent state. For ES-DE profiles whose root
    // folder is not yet configured, opens the root folder prompt immediately.
    internal async Task ApplyProfileSwitchAsync(string profileName)
    {
        _profileService.SetActiveProfile(profileName);
        _settingsService.SwitchProfile(_profileService.ActiveProfilePath);
        _sharedData.LoadFromSettings();

        LoadColumnSettings();
        LoadRecentFilesFromSettings();

        var themeIndex = ThemeService.GetThemeIndex(_sharedData.Theme);
        var colorIndex = ThemeService.GetColorIndex(_sharedData.Color);
        ThemeService.ApplyTheme(themeIndex, colorIndex);

        _ = LoadSystemsAsync();
        ActiveProfileName = profileName;

        if (!_profileService.IsEsDeRootConfigured(profileName))
            await PromptEsDeRootAsync($"Profile '{profileName}' is an ES-DE profile but its root folder has not been set.");
    }


    private static bool IsEsDeGamelistPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.IndexOf("/ES-DE/gamelists/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    #endregion
}