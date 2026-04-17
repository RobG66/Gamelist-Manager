using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Services;
using Gamelist_Manager.Views;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class MainWindowViewModel
{
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
        var requiredLabel = gamelistIsEsDe ? "ES-DE" : "Standard";

        // Find existing profiles whose type matches what is required.
        var matchingProfiles = _profileService.GetProfiles()
            .Where(p => IsProfileOfType(p, requiredType))
            .ToList();

        string switchTo;

        if (matchingProfiles.Count > 0)
        {
            // There is at least one matching profile — offer to switch.
            var profileNames = string.Join(", ", matchingProfiles);
            var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Profile Mismatch",
                Message = $"This gamelist requires a {requiredLabel} profile.",
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
            var suggestedName = gamelistIsEsDe ? "ES-DE" : "Standard";
            var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Profile Mismatch",
                Message = $"This gamelist requires a {requiredLabel} profile.",
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
    // and the current root is not yet set to an existing directory. Should be called after
    // the gamelist load completes so no other modal windows are open.
    internal async Task PromptEsDeMediaRootIfNeededAsync()
    {
        if (!_sharedData.IsEsDeMode) return;

        if (!string.IsNullOrWhiteSpace(_sharedData.EsDeRoot) &&
            Directory.Exists(_sharedData.EsDeRoot))
            return;

        await PromptEsDeRootAsync();
    }

    // Returns true when the gamelist file path contains the ES-DE gamelists folder pattern.
    private static bool IsEsDeGamelistPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.IndexOf("/ES-DE/gamelists/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool IsProfileOfType(string profileName, string profileType)
    {
        var value = _profileService.GetProfileType(profileName);
        return string.Equals(value, profileType, StringComparison.OrdinalIgnoreCase);
    }

    private async Task PromptEsDeRootAsync(string contextMessage = "An ES-DE gamelist has been detected.")
    {
        var defaultRoot = _sharedData.EsDeRoot;

        var hasRoot = !string.IsNullOrWhiteSpace(defaultRoot);

        var detailMessage = hasRoot
            ? $"The ES-DE root folder is currently set to:\n{defaultRoot}\n\nPress OK to use this location, or Browse to choose a different one."
            : "The ES-DE root folder has not been set.\n\nPress Browse to choose the ES-DE root folder.";

        var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "ES-DE Location",
            Message = contextMessage,
            DetailMessage = detailMessage,
            IconTheme = DialogIconTheme.Info,
            Button1Text = hasRoot ? "Browse" : "Cancel",
            Button2Text = "",
            Button3Text = hasRoot ? "OK" : "Browse"
        });

        // Button1 = Browse (when root exists), Button3 = Browse (when no root set)
        var shouldBrowse = (hasRoot && result == ThreeButtonResult.Button1) ||
                           (!hasRoot && result == ThreeButtonResult.Button3);

        if (shouldBrowse)
        {
            var chosen = await BrowseForEsDeRootAsync();
            if (!string.IsNullOrEmpty(chosen))
            {
                _sharedData.SaveEsDeRoot(chosen);

                if (string.IsNullOrWhiteSpace(_sharedData.RomsFolder))
                {
                    var detected = GamelistService.ReadRomDirectoryFromEsDeSettings(chosen);
                    if (detected != null)
                    {
                        _sharedData.RomsFolder = detected;
                        _settingsService.SetValue(SettingKeys.FolderPathsSection, SettingKeys.RomsFolder, detected);
                    }
                }
            }
        }
    }

    private async Task<string?> BrowseForEsDeRootAsync()
    {
        return await FolderPickerHelper.BrowseForFolderAsync(
            "Select ES-DE Root Folder",
            _sharedData.EsDeRoot);
    }
}
