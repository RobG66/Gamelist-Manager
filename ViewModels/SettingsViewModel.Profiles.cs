using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

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
    [NotifyCanExecuteChangedFor(nameof(CreateProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(RenameProfileCommand))]
    private string _newProfileName = string.Empty;

    [ObservableProperty]
    private string _newProfileType = SettingKeys.ProfileTypeEs;

    #endregion

    #region Public Properties

    public ObservableCollection<string> ProfileList { get; } = new();
    public ObservableCollection<string> TemplateList { get; } = new();

    public string ActiveProfileName => ProfileService.Instance.ActiveProfile;
    public bool CanCreateFromTemplate => SelectedTemplateName != null;

    public bool IsNewProfileTypeEsDe
    {
        get => NewProfileType == SettingKeys.ProfileTypeEsDe;
        set => NewProfileType = value ? SettingKeys.ProfileTypeEsDe : SettingKeys.ProfileTypeEs;
    }

    // Read-only info about the profile selected in the profile list.
    public bool SelectedProfileIsEsDe => GetSelectedProfileType() == SettingKeys.ProfileTypeEsDe;
    public string SelectedProfileEsDeMediaRoot => GetSelectedProfileMediaRoot();

    #endregion

    #region Events

    public event EventHandler? ProfilesChanged;
    public event EventHandler<string>? ConfirmDeleteProfileRequested;
    public event EventHandler<string>? ConfirmSwitchProfileRequested;
    public event EventHandler<string>? DuplicateTemplateProfileRequested;
    public event EventHandler<string>? ConfirmActivateEsDeProfileRequested;

    #endregion

    #region Public Methods

    public void RefreshProfileList()
    {
        ProfileList.Clear();
        foreach (var p in ProfileService.Instance.GetProfiles())
            ProfileList.Add(p);
        SelectedProfileName = ProfileService.Instance.ActiveProfile;
        OnPropertyChanged(nameof(ActiveProfileName));
        CreateProfileCommand.NotifyCanExecuteChanged();
        CopyProfileCommand.NotifyCanExecuteChanged();
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

        // Refresh settings panel to reflect the newly active profile.
        LoadSettings();
        RefreshProfileList();
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
        IsDirty = false;
    }

    public void DoCreateFromTemplate(bool overwrite = false)
    {
        var connection = IniFileService.GetSection(_templatesPath, SelectedTemplateName!);
        if (connection == null) return;
        if (!ProfileService.Instance.CreateProfileFromTemplate(NewProfileName.Trim(), connection, overwrite)) return;
        NewProfileName = string.Empty;
        SelectedTemplateName = null;
        RefreshProfileList();
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Partial Handlers

    partial void OnNewProfileTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsNewProfileTypeEsDe));
    }

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
    private void CreateProfile()
    {
        var isEsDe = NewProfileType == SettingKeys.ProfileTypeEsDe;
        var created = ProfileService.Instance.CreateTypedProfile(NewProfileName.Trim(), NewProfileType);
        if (created == null) return;
        NewProfileType = SettingKeys.ProfileTypeEs;
        NewProfileName = string.Empty;
        RefreshProfileList();
        ProfilesChanged?.Invoke(this, EventArgs.Empty);

        if (isEsDe)
            ConfirmActivateEsDeProfileRequested?.Invoke(this, created);
    }

    [RelayCommand(CanExecute = nameof(CanCreateProfile))]
    private void CopyProfile()
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
        ConfirmSwitchProfileRequested?.Invoke(this, SelectedProfileName);
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
