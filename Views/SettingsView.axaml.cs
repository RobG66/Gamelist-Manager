using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Gamelist_Manager.Services;
using Gamelist_Manager.ViewModels;
using System;
using System.ComponentModel;

namespace Gamelist_Manager.Views
{
    public partial class SettingsView : Window
    {
        private const double BASE_WIDTH = 580;
        private const double BASE_HEIGHT = 600;
        private const double BASE_FONT_SIZE = 12.0;

        private SettingsViewModel ViewModel => (SettingsViewModel)DataContext!;

        public SettingsView()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();

            // Scale window dimensions proportionally to font size (base: 580x600 at font 12)
            var scale = SharedDataService.Instance.AppFontSize / BASE_FONT_SIZE;
            Width = Math.Round(BASE_WIDTH * scale);
            Height = Math.Round(BASE_HEIGHT * scale);

            KeyDown += SettingsWindow_KeyDown;
            Closing += SettingsWindow_Closing;

            ViewModel.ProfilesChanged += OnProfilesChanged;
            ViewModel.ConfirmSwitchProfileRequested += OnConfirmSwitchProfileRequested;
            ViewModel.ConfirmDeleteProfileRequested += OnConfirmDeleteProfileRequested;
            ViewModel.DuplicateTemplateProfileRequested += OnDuplicateTemplateProfileRequested;
            ViewModel.ConfirmActivateEsDeProfileRequested += OnConfirmActivateEsDeProfileRequested;
        }

        #region Event Handlers - Profile

        private void OnProfilesChanged(object? sender, EventArgs e)
        {
            if (Owner is MainWindow { DataContext: MainWindowViewModel mainVm })
                mainVm.RefreshProfiles();
        }

        // Confirm before switching profiles — accounts for unsaved settings and/or a loaded gamelist
        private async void OnConfirmSwitchProfileRequested(object? sender, string profileName)
        {
            var mainVm = Owner is MainWindow { DataContext: MainWindowViewModel vm } ? vm : null;
            var isDirty = ViewModel.IsDirty;
            var gamelistLoaded = mainVm?.IsGamelistLoaded ?? false;

            // If the gamelist has unsaved changes, ask about those first via the standard dialog.
            // If the user cancels there, abort the profile switch entirely.
            if (gamelistLoaded && mainVm != null)
            {
                if (!await mainVm.CheckUnsavedChangesAsync())
                    return;
            }

            // Nothing else to warn about — switch immediately.
            if (!isDirty && !gamelistLoaded)
            {
                mainVm?.UnloadGamelist();
                ViewModel.DoSwitchProfile(saveFirst: false);
                return;
            }

            ThreeButtonDialogConfig config;

            if (isDirty && gamelistLoaded)
            {
                config = new ThreeButtonDialogConfig
                {
                    Title = "Switch Profile",
                    Message = $"Switch to profile '{profileName}'?",
                    DetailMessage = "You have unsaved settings changes. Do you want to save them first?\n\nThe current gamelist will also be unloaded.",
                    IconTheme = DialogIconTheme.Warning,
                    Button1Text = "Cancel",
                    Button2Text = "Don't Save",
                    Button3Text = "Save"
                };
            }
            else if (isDirty)
            {
                config = new ThreeButtonDialogConfig
                {
                    Title = "Switch Profile",
                    Message = $"Switch to profile '{profileName}'?",
                    DetailMessage = "You have unsaved settings changes. Do you want to save them first?",
                    IconTheme = DialogIconTheme.Warning,
                    Button1Text = "Cancel",
                    Button2Text = "Don't Save",
                    Button3Text = "Save"
                };
            }
            else
            {
                // gamelistLoaded only — gamelist has no unsaved changes (already handled above)
                config = new ThreeButtonDialogConfig
                {
                    Title = "Switch Profile",
                    Message = $"Switch to profile '{profileName}'?",
                    DetailMessage = "The current gamelist will be unloaded.",
                    IconTheme = DialogIconTheme.Warning,
                    Button1Text = "Cancel",
                    Button2Text = "",
                    Button3Text = "Switch"
                };
            }

            var result = await new ThreeButtonDialogView(config).ShowDialog<ThreeButtonResult>(this);
            if (result == ThreeButtonResult.Button1) return;

            mainVm?.UnloadGamelist();
            mainVm?.ApplyProfileSwitch(profileName);
            ViewModel.DoSwitchProfile(saveFirst: isDirty && result == ThreeButtonResult.Button3);
        }

        private async void OnConfirmDeleteProfileRequested(object? sender, string profileName)
        {
            var dialog = new ThreeButtonDialogView(new ThreeButtonDialogConfig
            {
                Title = "Delete Profile",
                Message = $"Delete profile '{profileName}'?",
                DetailMessage = "This cannot be undone.",
                IconTheme = DialogIconTheme.Warning,
                Button1Text = "Cancel",
                Button2Text = "",
                Button3Text = "Delete"
            });
            var result = await dialog.ShowDialog<ThreeButtonResult>(this);
            if (result == ThreeButtonResult.Button3)
                ViewModel.DoDeleteProfile();
        }

        private async void OnDuplicateTemplateProfileRequested(object? sender, string profileName)
        {
            var dialog = new ThreeButtonDialogView(new ThreeButtonDialogConfig
            {
                Title = "Profile Already Exists",
                Message = $"A profile named '{profileName}' already exists.",
                DetailMessage = "Do you want to overwrite it?",
                IconTheme = DialogIconTheme.Warning,
                Button1Text = "Cancel",
                Button2Text = "",
                Button3Text = "Overwrite"
            });
            var result = await dialog.ShowDialog<ThreeButtonResult>(this);
            if (result == ThreeButtonResult.Button3)
                ViewModel.DoCreateFromTemplate(overwrite: true);
        }

        private async void OnConfirmActivateEsDeProfileRequested(object? sender, string profileName)
        {
            var dialog = new ThreeButtonDialogView(new ThreeButtonDialogConfig
            {
                Title = "Activate Profile",
                Message = $"ES-DE profile '{profileName}' has been created.",
                DetailMessage = "Would you like to make it the active profile now?\n\n" +
                               "This is required to configure the ES-DE root folder.",
                IconTheme = DialogIconTheme.Info,
                Button1Text = "No",
                Button2Text = "",
                Button3Text = "Yes"
            });
            var result = await dialog.ShowDialog<ThreeButtonResult>(this);
            if (result != ThreeButtonResult.Button3) return;

            ViewModel.SelectedProfileName = profileName;
            ViewModel.DoSwitchProfile(saveFirst: ViewModel.IsDirty);

            if (Owner is MainWindow { DataContext: MainWindowViewModel mainVm })
                await mainVm.PromptEsDeRootAsync($"Profile '{profileName}' is now active.");
        }

        #endregion

        #region Event Handlers - Window

        private async void SettingsWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (!ViewModel.IsDirty)
            {
                KeyDown -= SettingsWindow_KeyDown;
                Closing -= SettingsWindow_Closing;
                return;
            }

            e.Cancel = true;

            var dialog = new ThreeButtonDialogView(new ThreeButtonDialogConfig
            {
                Title = "Save Settings",
                Message = "Do you want to save changes to settings?",
                DetailMessage = "You have unsaved changes that will be lost if you don't save.",
                IconTheme = DialogIconTheme.Warning,
                Button1Text = "Cancel",
                Button2Text = "Don't Save",
                Button3Text = "Save"
            });
            var result = await dialog.ShowDialog<ThreeButtonResult>(this);

            if (result == ThreeButtonResult.Button1) return;

            if (result == ThreeButtonResult.Button3)
            {
                var errors = ViewModel.ValidateAndTrimPaths();
                if (errors.Count > 0)
                {
                    await new ThreeButtonDialogView(new ThreeButtonDialogConfig
                    {
                        Title = "Validation Error",
                        Message = "Settings could not be saved due to the following issues:",
                        DetailMessage = string.Join("\n", errors),
                        IconTheme = DialogIconTheme.Error,
                        Button1Text = "",
                        Button2Text = "",
                        Button3Text = "OK"
                    }).ShowDialog<ThreeButtonResult>(this);
                    return;
                }
                ViewModel.SaveSettings();
            }

            KeyDown -= SettingsWindow_KeyDown;
            Closing -= SettingsWindow_Closing;
            Close();
        }

        private void SettingsWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        #endregion

        #region Event Handlers - Buttons

        private async void ButtonSave_Click(object? sender, RoutedEventArgs e)
        {
            var errors = ViewModel.ValidateAndTrimPaths();
            if (errors.Count > 0)
            {
                var dialog = new ThreeButtonDialogView(new ThreeButtonDialogConfig
                {
                    Title = "Validation Error",
                    Message = "Settings could not be saved due to the following issues:",
                    DetailMessage = string.Join("\n", errors),
                    IconTheme = DialogIconTheme.Error,
                    Button1Text = "",
                    Button2Text = "",
                    Button3Text = "OK"
                });
                await dialog.ShowDialog<ThreeButtonResult>(this);
                return;
            }

            ViewModel.SaveSettings();

            // Resize window to match the (possibly changed) font size
            var scale = ViewModel.AppFontSize / BASE_FONT_SIZE;
            Width = Math.Round(BASE_WIDTH * scale);
            Height = Math.Round(BASE_HEIGHT * scale);
        }

        private void ButtonClose_Click(object? sender, RoutedEventArgs e) => Close();

        private void Button_Reset_Click(object? sender, RoutedEventArgs e) => ViewModel.ResetFolderPaths();

        private async void ButtonResetAll_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new ThreeButtonDialogView(new ThreeButtonDialogConfig
            {
                Title = "Reset All Settings",
                Message = "Are you sure you want to reset all settings to defaults?",
                DetailMessage = "This action cannot be undone. All your customizations will be lost.",
                IconTheme = DialogIconTheme.Warning,
                Button1Text = "Cancel",
                Button2Text = "",
                Button3Text = "Reset"
            });

            var result = await dialog.ShowDialog<ThreeButtonResult>(this);
            if (result == ThreeButtonResult.Button3)
            {
                ViewModel.ResetAllSettings();
                ViewModel.IsDirty = true;
            }
        }

        private async void BrowseRomsFolder_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                new Avalonia.Platform.Storage.FolderPickerOpenOptions
                {
                    Title = "Select ROMs Folder",
                    AllowMultiple = false
                });

            if (folders.Count > 0)
                ViewModel.RomsPath = folders[0].Path.LocalPath;
        }

        private async void BrowseMameExecutable_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "Select MAME Executable",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Executable Files")
                        {
                            Patterns = new[] { "*.exe", "*" }
                        }
                    }
                });

            if (files.Count > 0)
                ViewModel.MamePath = files[0].Path.LocalPath;
        }

        private async void BrowseMediaFolder_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: Gamelist_Manager.Models.MediaFolderItem item }) return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                new Avalonia.Platform.Storage.FolderPickerOpenOptions
                {
                    Title = "Select Media Folder",
                    AllowMultiple = false
                });

            if (folders.Count > 0)
                item.Path = folders[0].Path.LocalPath;
        }

        #endregion
    }
}