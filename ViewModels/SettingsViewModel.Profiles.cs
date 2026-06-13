using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Gamelist_Manager.Messages;
using Gamelist_Manager.Models;
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

    #region Observable Properties

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetActiveProfileCommand))]
    private string _selectedProfileName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateFromTemplate))]
    private string? _selectedTemplateName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateNewProfileCommand))]
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
        CreateNewProfileCommand.NotifyCanExecuteChanged();
        CopyProfileCommand.NotifyCanExecuteChanged();
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

        var activate = await ThreeButtonDialogView.ShowConfirmAsync(
            "Activate Profile",
            $"Would you like to make '{profileName}' the active profile?",
            confirmText: "Yes",
            cancelText: "No",
            icon: DialogIconTheme.Info);
        
        if (!activate) return;

        var gamelistLoaded = WeakReferenceMessenger.Default.Send(new GamelistLoadedRequestMessage()).Response;

        if (gamelistLoaded && !await WeakReferenceMessenger.Default.Send(new CheckUnsavedGamelistChangesMessage()))
            return;

        WeakReferenceMessenger.Default.Send(new UnloadGamelistMessage());

        if (SettingsChanged) SaveSettings();

        await WeakReferenceMessenger.Default.Send(new ApplyProfileSwitchMessage(profileName));

        DoSwitchProfile(saveFirst: false);
    }

    [RelayCommand(CanExecute = nameof(CanCreateProfile))]
    private void CopyProfile()
    {
        if (!ProfileService.Instance.CreateProfile(NewProfileName.Trim(), copyFromActive: true)) return;
        NewProfileName = string.Empty;
        RefreshProfileList();
        WeakReferenceMessenger.Default.Send(new ProfilesChangedMessage());
    }

    [RelayCommand]
    private async Task RenameProfile()
    {
        if (string.IsNullOrWhiteSpace(SelectedProfileName)) return;
        var newName = await RenameDialogView.ShowAsync(SelectedProfileName);
        if (string.IsNullOrEmpty(newName)) return;
        if (!ProfileService.Instance.RenameProfile(SelectedProfileName, newName)) return;
        RefreshProfileList();
        SelectedProfileName = newName;
        WeakReferenceMessenger.Default.Send(new ProfilesChangedMessage());
    }

    [RelayCommand(CanExecute = nameof(CanDeleteProfile))]
    private async Task DeleteProfile()
    {
        var result = await ThreeButtonDialogView.ShowConfirmAsync(
            "Delete Profile",
            $"Delete profile '{SelectedProfileName}'?",
            confirmText: "Delete",
            cancelText: "Cancel",
            icon: DialogIconTheme.Warning,
            detail: "This cannot be undone.");

        if (!result) return;

        if (!ProfileService.Instance.DeleteProfile(SelectedProfileName)) return;
        RefreshProfileList();
        WeakReferenceMessenger.Default.Send(new ProfilesChangedMessage());
    }

    [RelayCommand(CanExecute = nameof(CanSetActiveProfile))]
    private async Task SetActiveProfile()
    {
        var profileName = SelectedProfileName;
        var isDirty = SettingsChanged;
        var gamelistLoaded = WeakReferenceMessenger.Default.Send(new GamelistLoadedRequestMessage()).Response;

        if (gamelistLoaded && !await WeakReferenceMessenger.Default.Send(new CheckUnsavedGamelistChangesMessage()))
            return;

        if (!isDirty && !gamelistLoaded)
        {
            await WeakReferenceMessenger.Default.Send(new ApplyProfileSwitchMessage(profileName));
            DoSwitchProfile(saveFirst: false);
            return;
        }

        var detailMessage = (isDirty, gamelistLoaded) switch
        {
            (true, true) => "You have unsaved settings changes. Do you want to save them first?\n\nThe current gamelist will also be unloaded.",
            (true, false) => "You have unsaved settings changes. Do you want to save them first?",
            _ => "The current gamelist will be unloaded."
        };

        var button2Text = isDirty ? "Don't Save" : "";

        var switchResult = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Switch Profile",
            Message = $"Switch to profile '{profileName}'?",
            DetailMessage = detailMessage,
            IconTheme = DialogIconTheme.Warning,
            Button1Text = "Cancel",
            Button2Text = button2Text,
            Button3Text = isDirty ? "Save" : "Switch"
        });

        if (switchResult == ThreeButtonResult.Button1) return;

        if (isDirty && switchResult == ThreeButtonResult.Button3)
        {
            SaveSettings();
        }

        WeakReferenceMessenger.Default.Send(new UnloadGamelistMessage());
        await WeakReferenceMessenger.Default.Send(new ApplyProfileSwitchMessage(profileName));
        DoSwitchProfile(saveFirst: false);
    }

    [RelayCommand]
    private async Task CreateFromTemplate()
    {
        if (!CanCreateFromTemplate) return;

        if (ProfileList.Contains(NewProfileName.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var overwriteResult = await ThreeButtonDialogView.ShowConfirmAsync(
                "Profile Already Exists",
                $"A profile named '{NewProfileName.Trim()}' already exists.",
                confirmText: "Overwrite",
                cancelText: "Cancel",
                icon: DialogIconTheme.Warning,
                detail: "Do you want to overwrite it?");

            if (!overwriteResult) return;
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
        _isProfileLoading = true;
        try
        {
            LoadSettings();
            RefreshProfileList();
            RefreshMediaFolderDisplayState();
        }
        finally
        {
            _isProfileLoading = false;
        }
        WeakReferenceMessenger.Default.Send(new ProfilesChangedMessage());
        SettingsChanged = false;
    }

    private void DoCreateFromTemplate(bool overwrite = false)
    {
        var connection = IniFileService.GetSection(_templatesPath, SelectedTemplateName!);
        if (connection == null) return;
        if (!ProfileService.Instance.CreateProfileFromTemplate(NewProfileName.Trim(), connection, overwrite)) return;
        NewProfileName = string.Empty;
        SelectedTemplateName = null;
        RefreshProfileList();
        WeakReferenceMessenger.Default.Send(new ProfilesChangedMessage());
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