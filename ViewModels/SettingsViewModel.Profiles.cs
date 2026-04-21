using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Services;
using Gamelist_Manager.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class SettingsViewModel
{
    #region Private Fields

    private static readonly string _templatesPath =
        Path.Combine(AppContext.BaseDirectory, "ini", "templates.ini");

    #endregion

    #region Injected Delegates

    // Set by SettingsView.OnOpened once Owner is available.
    // Keeps the ViewModel free of any View or MainWindowViewModel reference.
    public Func<Task<bool>>? CheckMainUnsavedChangesAsync { get; set; }
    public Func<bool>? GetIsGamelistLoaded { get; set; }
    public Func<string, Task>? ApplyMainProfileSwitch { get; set; }
    public Action? UnloadMainGamelist { get; set; }
    public Action? NotifyProfilesChanged { get; set; }

    #endregion

    #region Observable Properties

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(RenameProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetActiveProfileCommand))]
    private string _selectedProfileName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateFromTemplate))]
    private string? _selectedTemplateName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(RenameProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateNewProfileCommand))]
    [NotifyPropertyChangedFor(nameof(CanCreate))]
    private string _newProfileName = string.Empty;

    [ObservableProperty]
    private ProfileTypeOption _selectedProfileType = SettingKeys.AllProfileTypes[0];

    #endregion

    #region Public Properties

    public ObservableCollection<string> ProfileList { get; } = new();
    public ObservableCollection<string> TemplateList { get; } = new();
    public IReadOnlyList<ProfileTypeOption> ProfileTypes => SettingKeys.AllProfileTypes;

    public string ActiveProfileName => ProfileService.Instance.ActiveProfile;
    public bool CanCreateFromTemplate => SelectedTemplateName != null;
    public bool CanCreate => CanCreateProfile();

    // Read-only info about the profile selected in the profile list.
    public bool SelectedProfileIsEsDe => GetSelectedProfileType() == SettingKeys.ProfileTypeEsDe;
    public string SelectedProfileEsDeMediaRoot => GetSelectedProfileMediaRoot();

    #endregion

    #region Public Methods

    public void RefreshProfileList()
    {
        ProfileList.Clear();
        foreach (var p in ProfileService.Instance.GetProfiles())
            ProfileList.Add(p);
        SelectedProfileName = ProfileService.Instance.ActiveProfile;
        OnPropertyChanged(nameof(ActiveProfileName));
        OnPropertyChanged(nameof(CanCreate));
        CopyProfileCommand.NotifyCanExecuteChanged();
        RenameProfileCommand.NotifyCanExecuteChanged();
        DeleteProfileCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Partial Handlers

    partial void OnSelectedProfileNameChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedProfileIsEsDe));
        OnPropertyChanged(nameof(SelectedProfileEsDeMediaRoot));
    }

    partial void OnSelectedTemplateNameChanged(string? value)
    {
        if (value != null) NewProfileName = value;
        OnPropertyChanged(nameof(CanCreateFromTemplate));
    }

    #endregion

    #region CanExecute

    private bool CanCreateProfile() =>
        !string.IsNullOrWhiteSpace(NewProfileName) &&
        !ProfileList.Contains(NewProfileName.Trim(), StringComparer.OrdinalIgnoreCase);

    private bool CanRenameProfile() =>
        !string.IsNullOrWhiteSpace(SelectedProfileName) &&
        !string.IsNullOrWhiteSpace(NewProfileName) &&
        !string.Equals(NewProfileName.Trim(), SelectedProfileName, StringComparison.OrdinalIgnoreCase) &&
        !ProfileList.Contains(NewProfileName.Trim(), StringComparer.OrdinalIgnoreCase);

    private bool CanDeleteProfile() =>
        !string.IsNullOrWhiteSpace(SelectedProfileName) &&
        !string.Equals(SelectedProfileName, ProfileService.Instance.ActiveProfile, StringComparison.OrdinalIgnoreCase) &&
        ProfileList.Count > 1;

    private bool CanSetActiveProfile() =>
        !string.IsNullOrWhiteSpace(SelectedProfileName) &&
        !string.Equals(SelectedProfileName, ProfileService.Instance.ActiveProfile, StringComparison.OrdinalIgnoreCase);

    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanCreateProfile))]
    private async Task CreateNewProfile()
    {
        var profileType = SelectedProfileType.Key;
        var profileName = ProfileService.Instance.CreateTypedProfile(NewProfileName.Trim(), profileType);
        if (profileName == null) return;

        NewProfileName = string.Empty;
        RefreshProfileList();

        var activate = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Activate Profile",
            Message = $"Would you like to make '{profileName}' the active profile?",
            IconTheme = DialogIconTheme.Info,
            Button1Text = "No",
            Button2Text = "",
            Button3Text = "Yes"
        });

        if (activate != ThreeButtonResult.Button3) return;

        if (ApplyMainProfileSwitch != null)
            await ApplyMainProfileSwitch(profileName);
        DoSwitchProfile(saveFirst: false);
    }

    [RelayCommand(CanExecute = nameof(CanCreateProfile))]
    private void CopyProfile()
    {
        if (!ProfileService.Instance.CreateProfile(NewProfileName.Trim(), copyFromActive: true)) return;
        NewProfileName = string.Empty;
        RefreshProfileList();
        NotifyProfilesChanged?.Invoke();
    }

    [RelayCommand(CanExecute = nameof(CanRenameProfile))]
    private void RenameProfile()
    {
        if (!ProfileService.Instance.RenameProfile(SelectedProfileName, NewProfileName.Trim())) return;
        NewProfileName = string.Empty;
        RefreshProfileList();
        NotifyProfilesChanged?.Invoke();
    }

    [RelayCommand(CanExecute = nameof(CanDeleteProfile))]
    private async Task DeleteProfile()
    {
        var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Delete Profile",
            Message = $"Delete profile '{SelectedProfileName}'?",
            DetailMessage = "This cannot be undone.",
            IconTheme = DialogIconTheme.Warning,
            Button1Text = "Cancel",
            Button2Text = "",
            Button3Text = "Delete"
        });

        if (result != ThreeButtonResult.Button3) return;

        if (!ProfileService.Instance.DeleteProfile(SelectedProfileName)) return;
        RefreshProfileList();
        NotifyProfilesChanged?.Invoke();
    }

    [RelayCommand(CanExecute = nameof(CanSetActiveProfile))]
    private async Task SetActiveProfile()
    {
        var profileName = SelectedProfileName;
        var isDirty = IsDirty;
        var gamelistLoaded = GetIsGamelistLoaded?.Invoke() ?? false;

        // If the gamelist has unsaved changes, ask about those first.
        // If the user cancels, abort the profile switch entirely.
        if (gamelistLoaded && CheckMainUnsavedChangesAsync != null)
        {
            if (!await CheckMainUnsavedChangesAsync())
                return;
        }

        // Nothing to warn about — switch immediately.
        if (!isDirty && !gamelistLoaded)
        {
            if (ApplyMainProfileSwitch != null)
                await ApplyMainProfileSwitch(profileName);
            DoSwitchProfile(saveFirst: false);
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
            // Gamelist loaded only — already confirmed no unsaved gamelist changes above.
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

        var switchResult = await ThreeButtonDialogView.ShowAsync(config);
        if (switchResult == ThreeButtonResult.Button1) return;

        UnloadMainGamelist?.Invoke();
        if (ApplyMainProfileSwitch != null)
            await ApplyMainProfileSwitch(profileName);
        DoSwitchProfile(saveFirst: isDirty && switchResult == ThreeButtonResult.Button3);
    }

    [RelayCommand]
    private async Task CreateFromTemplate()
    {
        if (!CanCreateFromTemplate) return;

        if (ProfileList.Contains(NewProfileName.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var overwriteResult = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Profile Already Exists",
                Message = $"A profile named '{NewProfileName.Trim()}' already exists.",
                DetailMessage = "Do you want to overwrite it?",
                IconTheme = DialogIconTheme.Warning,
                Button1Text = "Cancel",
                Button2Text = "",
                Button3Text = "Overwrite"
            });

            if (overwriteResult != ThreeButtonResult.Button3) return;
            DoCreateFromTemplate(overwrite: true);
            return;
        }

        DoCreateFromTemplate();
    }

    #endregion

    #region Private Methods

    private void DoSwitchProfile(bool saveFirst)
    {
        if (saveFirst) SaveSettings();
        LoadSettings();
        RefreshProfileList();
        NotifyProfilesChanged?.Invoke();
        IsDirty = false;
    }

    private void DoCreateFromTemplate(bool overwrite = false)
    {
        var connection = IniFileService.GetSection(_templatesPath, SelectedTemplateName!);
        if (connection == null) return;
        if (!ProfileService.Instance.CreateProfileFromTemplate(NewProfileName.Trim(), connection, overwrite)) return;
        NewProfileName = string.Empty;
        SelectedTemplateName = null;
        RefreshProfileList();
        NotifyProfilesChanged?.Invoke();
    }

    private void LoadTemplates()
    {
        TemplateList.Clear();
        foreach (var name in IniFileService.ReadIniFile(_templatesPath).Keys.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            TemplateList.Add(name);
        SelectedTemplateName = null;
    }

    private string GetSelectedProfileType()
    {
        if (string.IsNullOrWhiteSpace(SelectedProfileName)) return SettingKeys.ProfileTypeEs;
        return ProfileService.Instance.GetProfileType(SelectedProfileName);
    }

    private string GetSelectedProfileMediaRoot()
    {
        if (string.IsNullOrWhiteSpace(SelectedProfileName)) return string.Empty;
        var path = ProfileService.Instance.GetProfilePath(SelectedProfileName);
        var section = IniFileService.GetSection(path, SettingKeys.EsDeSection);
        return section != null && section.TryGetValue(SettingKeys.EsDeRoot.Key, out var v) ? v : string.Empty;
    }

    #endregion
}
