using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using Gamelist_Manager.Views;
using System.Threading.Tasks;

namespace Gamelist_Manager.Classes.Helpers;

public static class NewProfileHelper
{
    public static async Task RunWizardAsync(string profileName, string profileType, Window? owner = null)
    {
        // Only run for ES profiles. Any other type ignores the wizard.
        if (profileType != SettingKeys.ProfileTypeEs)
            return;

        // --- Prompt 1: Roms Folder ---
        var romResult = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "ES Configuration",
            Message = "Do you want to select your ROMs folder?",
            IconTheme = DialogIconTheme.Info,
            Button1Text = "Cancel",
            Button2Text = "No",
            Button3Text = "Yes"
        }, owner);

        if (romResult == ThreeButtonResult.Button1) return; // Cancel aborts

        if (romResult == ThreeButtonResult.Button3) // Yes
        {
            var romFolder = await FolderPickerHelper.BrowseForFolderAsync("Select ROMs Folder", string.Empty, owner);
            if (!string.IsNullOrEmpty(romFolder))
            {
                var profilePath = ProfileService.Instance.GetProfilePath(profileName);
                var sections = IniFileService.ReadIniFile(profilePath);

                if (!sections.ContainsKey(SettingKeys.RomsFolder.Section))
                    sections[SettingKeys.RomsFolder.Section] = new();

                sections[SettingKeys.RomsFolder.Section][SettingKeys.RomsFolder.Key] = romFolder;
                IniFileService.WriteIniFile(profilePath, sections);

                // Reload settings state if this happens to be the active profile, 
                // but since it's newly created, it might not be active yet.
                // ProfileService will load it fresh when switching.
            }
        }

        // --- Prompt 2: MAME Executable File ---
        var mameResult = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "ES Configuration",
            Message = "Do you want to pick the MAME executable file?",
            IconTheme = DialogIconTheme.Info,
            Button1Text = "Cancel",
            Button2Text = "No",
            Button3Text = "Yes"
        }, owner);

        if (mameResult == ThreeButtonResult.Button1) return; // Cancel aborts

        if (mameResult == ThreeButtonResult.Button3) // Yes
        {
            // MAME executable is a file on all platforms (mame.exe on Win, mame on Linux/Mac)
            var fileTypes = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Executable Files") { Patterns = new[] { "*.exe", "*mame*" } },
                new Avalonia.Platform.Storage.FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            };

            var topLevel = owner
                ?? (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null);

            if (topLevel != null)
            {
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select MAME Executable",
                    AllowMultiple = false,
                    FileTypeFilter = fileTypes
                });

                if (files.Count > 0)
                {
                    var mameFile = files[0].Path.LocalPath;
                    var profilePath = ProfileService.Instance.GetProfilePath(profileName);
                    var sections = IniFileService.ReadIniFile(profilePath);

                    if (!sections.ContainsKey(SettingKeys.MamePath.Section))
                        sections[SettingKeys.MamePath.Section] = new();

                    sections[SettingKeys.MamePath.Section][SettingKeys.MamePath.Key] = mameFile;
                    IniFileService.WriteIniFile(profilePath, sections);
                }
            }
        }
    }
}
