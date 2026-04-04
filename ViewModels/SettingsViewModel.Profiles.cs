using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Services;

namespace Gamelist_Manager.ViewModels;

public partial class SettingsViewModel
{
    #region Fields

    private static readonly string _templatesPath =
        Path.Combine(AppContext.BaseDirectory, "ini", "templates.ini");

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
    [NotifyCanExecuteChangedFor(nameof(CreateNewProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateCopyProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(RenameProfileCommand))]
    private string _newProfileName = string.Empty;

    #endregion

    #region Public Properties

    public ObservableCollection<string> ProfileList  { get; } = new();
    public ObservableCollection<string> TemplateList { get; } = new();

    public string ActiveProfileName    => ProfileService.Instance.ActiveProfile;
    public bool   CanCreateFromTemplate => SelectedTemplateName != null;

    #endregion

    #region Events

    public event EventHandler?        ProfilesChanged;
    public event EventHandler<string>? ConfirmDeleteProfileRequested;
    public event EventHandler<string>? ConfirmSwitchProfileRequested;
    public event EventHandler<string>? DuplicateTemplateProfileRequested;

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
        CreateCopyProfileCommand.NotifyCanExecuteChanged();
        RenameProfileCommand.NotifyCanExecuteChanged();
        DeleteProfileCommand.NotifyCanExecuteChanged();
    }

    public void DoDeleteProfile()
    {
        if (!ProfileService.Instance.DeleteProfile(SelectedProfileName)) return;
        RefreshProfileList();
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void DoSwitchProfile(bool saveFirst)
    {
        if (saveFirst) SaveSettings();
        ProfileService.Instance.SetActiveProfile(SelectedProfileName);
        SettingsService.Instance.SwitchProfile(ProfileService.Instance.ActiveProfilePath);
        LoadSettings();
        SaveSettings();
        SharedDataService.Instance.LoadFromSettings();
        RefreshProfileList();
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
        IsDirty = false;
    }

    public void DoCreateFromTemplate(bool overwrite = false)
    {
        var connection = IniFileService.GetSection(_templatesPath, SelectedTemplateName!);
        if (connection == null) return;
        if (!ProfileService.Instance.CreateProfileFromTemplate(NewProfileName.Trim(), connection, overwrite)) return;
        NewProfileName      = string.Empty;
        SelectedTemplateName = null;
        RefreshProfileList();
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Partial Handlers

    partial void OnSelectedTemplateNameChanged(string? value)
    {
        if (value != null) NewProfileName = value;
        OnPropertyChanged(nameof(CanCreateFromTemplate));
    }

    #endregion

    #region CanExecute

    private bool CanCreateNewProfile() =>
        !string.IsNullOrWhiteSpace(NewProfileName) &&
        !ProfileList.Contains(NewProfileName.Trim(), StringComparer.OrdinalIgnoreCase);

    private bool CanCreateCopyProfile() =>
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

    [RelayCommand(CanExecute = nameof(CanCreateNewProfile))]
    private void CreateNewProfile()
    {
        if (!ProfileService.Instance.CreateProfile(NewProfileName.Trim(), copyFromActive: false)) return;
        NewProfileName = string.Empty;
        RefreshProfileList();
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(CanCreateCopyProfile))]
    private void CreateCopyProfile()
    {
        if (!ProfileService.Instance.CreateProfile(NewProfileName.Trim(), copyFromActive: true)) return;
        NewProfileName = string.Empty;
        RefreshProfileList();
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(CanRenameProfile))]
    private void RenameProfile()
    {
        if (!ProfileService.Instance.RenameProfile(SelectedProfileName, NewProfileName.Trim())) return;
        NewProfileName = string.Empty;
        RefreshProfileList();
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteProfile))]
    private void DeleteProfile()
    {
        if (!CanDeleteProfile()) return;
        ConfirmDeleteProfileRequested?.Invoke(this, SelectedProfileName);
    }

    [RelayCommand(CanExecute = nameof(CanSetActiveProfile))]
    private void SetActiveProfile()
    {
        if (IsDirty)
        {
            ConfirmSwitchProfileRequested?.Invoke(this, SelectedProfileName);
            return;
        }
        DoSwitchProfile(saveFirst: false);
    }

    [RelayCommand]
    private void CreateFromTemplate()
    {
        if (!CanCreateFromTemplate) return;
        if (ProfileList.Contains(NewProfileName.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            DuplicateTemplateProfileRequested?.Invoke(this, NewProfileName.Trim());
            return;
        }
        DoCreateFromTemplate();
    }

    #endregion

    #region Helpers

    private void LoadTemplates()
    {
        TemplateList.Clear();
        foreach (var name in IniFileService.ReadIniFile(_templatesPath).Keys.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            TemplateList.Add(name);
        SelectedTemplateName = null;
    }

    #endregion
}
