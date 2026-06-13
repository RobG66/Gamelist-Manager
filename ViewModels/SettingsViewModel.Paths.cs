using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using Gamelist_Manager.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class SettingsViewModel
{
    #region Observable Properties

    [ObservableProperty] private string _mamePath = string.Empty;
    [ObservableProperty] private string _romsPath = string.Empty;
    [ObservableProperty] private string _esDeRoot = string.Empty;
    [ObservableProperty] private string _esDeMediaBase = string.Empty;
    [ObservableProperty] private bool _systemOverrideActive;
    [ObservableProperty] private ObservableCollection<string> _systemsWithOverrides = new();
    [ObservableProperty] private string? _selectedOverrideSystem;

    private bool SystemHasOverrides =>
     !string.IsNullOrEmpty(_sessionState.CurrentSystem) &&
     (SettingsService.Instance.GetSection(SettingKeys.MediaPathOverridesSection)
         ?.Keys.Any(k => k.StartsWith($"{_sessionState.CurrentSystem}_", StringComparison.OrdinalIgnoreCase))
     ?? false);

    #endregion

    #region Public Properties

    public bool CanOverrideSystem => !string.IsNullOrEmpty(_sessionState.CurrentSystem);

    public string SystemOverrideLabel => string.IsNullOrEmpty(_sessionState.CurrentSystem)
        ? "Cannot enable media override, no system loaded"
        : $"Enable media selection override";

    public ObservableCollection<MediaFolderItem> MediaFolderItems { get; } = new();

    #endregion

    #region Initialization

    private void InitializeMediaFolderItems()
    {
        MediaFolderItems.Clear();
        foreach (var decl in MetadataService.GetAllMediaFolderTypes())
        {
            var item = new MediaFolderItem
            {
                Key = decl.Type,
                Label = decl.Name,
                DefaultPath = decl.DefaultPath,
                DefaultSuffix = decl.DefaultSuffix,
                DefaultEnabled = decl.DefaultEnabled,
            };
            item.ResetToDefaults();
            item.PropertyChanged += (_, e) =>
            {
                if (_isProfileLoading) return;
                if (e.PropertyName is nameof(MediaFolderItem.AreSuffixesAllowed)
                                   or nameof(MediaFolderItem.ArePathsReadOnly)
                                   or nameof(MediaFolderItem.CanMediaOverrideCheckboxBeEnabled)
                                   or nameof(MediaFolderItem.IsSuffixInputEnabled)
                                   or nameof(MediaFolderItem.DisplayPath))
                    return;
                SettingsChanged = true;
            };
            MediaFolderItems.Add(item);
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void CopyOverride()
    {
        if (string.IsNullOrEmpty(SelectedOverrideSystem)) return;

        var overrides = SettingsService.Instance.GetSection(SettingKeys.MediaPathOverridesSection);
        if (overrides == null) return;

        foreach (var item in MediaFolderItems)
        {
            var key = $"{SelectedOverrideSystem}_{item.Key}_enabled";
            if (overrides.TryGetValue(key, out var raw) && bool.TryParse(raw, out var value))
                item.Enabled = value;
        }

        SelectedOverrideSystem = null;
        SettingsChanged = true;
    }

    [RelayCommand]
    private async Task BrowseEsDeRootAsync()
    {
        var chosen = await FolderPickerHelper.BrowseForFolderAsync(
            "Select ES-DE Root Folder",
            EsDeRoot);

        if (string.IsNullOrEmpty(chosen))
            return;

        EsDeRoot = chosen;

        var detected = EsDePathResolver.ReadPathsFromEsDeSettings(chosen);
        RomsPath = detected.RomDirectory ?? string.Empty;
        EsDeMediaBase = detected.MediaDirectory ?? string.Empty;
    }

    [RelayCommand]
    private void ToggleAllSuffixes()
    {
        bool newValue = !MediaFolderItems.All(i => i.IsSuffixEnabled);
        foreach (var item in MediaFolderItems)
            item.IsSuffixEnabled = newValue;
    }

    #endregion

    #region Public Methods

    public void ResetFolderPaths()
    {
        foreach (var item in MediaFolderItems)
            item.ResetToDefaults();
    }

    public List<string> ValidateAndTrimPaths()
    {
        var errors = new List<string>();
        var profile = SettingKeys.GetProfileTypeOption(SettingsState.Instance.ProfileType);

        foreach (var item in MediaFolderItems)
        {
            item.Path = item.Path.Trim();
            item.Suffix = item.Suffix.Trim();
        }

        foreach (var item in MediaFolderItems)
        {
            if (!item.Enabled) continue;

            if (!FilePathHelper.IsValidMediaFolderPath(item.Path))
            {
                errors.Add($"{item.Label}: Folder path is invalid.");
                continue;
            }

            var normalized = FilePathHelper.NormalizePathWithDotSlashPrefix(item.Path);
            if (normalized == null)
            {
                errors.Add($"{item.Label}: Folder path is empty or invalid after normalization.");
                continue;
            }

            item.Path = normalized;

            if (profile.MediaFilenamesUseSuffixes && item.IsSuffixEnabled && !FilePathHelper.IsValidMediaFolderSuffix(item.Suffix))
                errors.Add($"{item.Label}: Suffix must be alphanumeric only (a-z, A-Z, 0-9) and no longer than 20 characters.");
        }

        if (errors.Count == 0)
        {
            var seen = new Dictionary<(string path, string suffix), string>();
            foreach (var item in MediaFolderItems)
            {
                if (!item.Enabled) continue;

                string effectivePath;
                string effectiveSuffix;

                if (!profile.GamelistHasMediaPaths)
                {
                    effectivePath = (MetadataService.GetDeclByType(item.Key)?.EsDeFolderName ?? item.Key).ToLowerInvariant();
                    effectiveSuffix = string.Empty;
                }
                else
                {
                    effectivePath = (FilePathHelper.NormalizePathWithDotSlashPrefix(item.Path) ?? item.Path).ToLowerInvariant();
                    effectiveSuffix = item.IsSuffixEnabled ? item.Suffix.ToLowerInvariant() : string.Empty;
                }

                var key = (effectivePath, effectiveSuffix);
                if (seen.TryGetValue(key, out var firstName))
                    errors.Add($"{item.Label} and {firstName} share the same folder and filenames would be identical");
                else
                    seen[key] = item.Label;
            }
        }

        return errors;
    }

    private void RefreshMediaFolderDisplayState()
    {
        var profile = SettingKeys.GetProfileTypeOption(SettingsState.Instance.ProfileType);
        var esDeMediaBase = EsDeMediaBase;
        var currentSystem = SessionState.Instance.CurrentSystem;

        foreach (var item in MediaFolderItems)
        {
            var decl = MetadataService.GetDeclByType(item.Key);
            if (decl == null) continue;
            item.RefreshDisplayState(profile, decl, esDeMediaBase, currentSystem);
        }
    }

    #endregion

    #region Helpers

    private void RefreshSystemsWithOverrides()
    {
        SystemsWithOverrides.Clear();
        var overrides = SettingsService.Instance.GetSection(SettingKeys.MediaPathOverridesSection);
        if (overrides == null) return;

        var systems = overrides.Keys
            .Select(k => k.Split('_')[0])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(s => !string.Equals(s, _sessionState.CurrentSystem, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase);

        foreach (var system in systems)
            SystemsWithOverrides.Add(system);
    }

    private static string LoadMediaPath(string raw, string defaultPath) =>
        FilePathHelper.NormalizePathWithDotSlashPrefix(raw) ?? defaultPath;

    private void LoadSystemOverrides()
    {
        var system = _sessionState.CurrentSystem;
        if (string.IsNullOrEmpty(system)) return;

        var overrides = SettingsService.Instance.GetSection(SettingKeys.MediaPathOverridesSection);
        if (overrides == null) return;

        foreach (var item in MediaFolderItems)
        {
            var key = $"{system}_{item.Key}_enabled";
            if (overrides.TryGetValue(key, out var raw) && bool.TryParse(raw, out var value))
                item.Enabled = value;
        }
        RefreshMediaFolderDisplayState();
    }
    private void SaveSystemOverrides()
    {
        var system = _sessionState.CurrentSystem;
        if (string.IsNullOrEmpty(system)) return;
      
        var overrides = MediaFolderItems
            .ToDictionary(
                item => $"{system}_{item.Key}_enabled",
                item => item.Enabled.ToString());

        ProfileService.Instance.Save(new Dictionary<string, Dictionary<string, string>>
        {
            [SettingKeys.MediaPathOverridesSection] = overrides
        });

        SettingsService.Instance.InvalidateCache();
    }

    partial void OnSystemOverrideActiveChanged(bool value)
    {
        if (_isProfileLoading) return;
        _ = HandleSystemOverrideToggleAsync(value);
    }

    private async Task HandleSystemOverrideToggleAsync(bool enabling)
    {
        var system = _sessionState.CurrentSystem!;
        var message = enabling
            ? $"Enabling overrides will save current enabled states for '{system}'. Continue?"
            : $"Disabling overrides will permanently remove them for '{system}'. Continue?";

        var result = await ThreeButtonDialogView.ShowConfirmAsync(
            "System Media Overrides",
            message,
            confirmText: enabling ? "Enable" : "Disable",
            cancelText: "Cancel",
            icon: DialogIconTheme.Warning);

        if (!result)
        {
            _isProfileLoading = true;
            try { SystemOverrideActive = !enabling; }
            finally { _isProfileLoading = false; }
            return;
        }

        if (enabling)
        {
            LoadSystemOverrides();
        }
        else
        {
            SettingsService.Instance.ClearSystemMediaOverrides(system);
            var profile = SettingKeys.GetProfileTypeOption(_settingsState.ProfileType);
            foreach (var item in MediaFolderItems)
            {
                var decl = MetadataService.GetDeclByType(item.Key);
                item.Enabled = decl != null && profile.IncludesMediaFolder(decl) &&
                               (bool.TryParse(_settingsState.MediaPaths.GetValueOrDefault($"{item.Key}_enabled"), out var en) ? en : item.DefaultEnabled);
            }
        }

        RefreshSystemsWithOverrides();

        // No need to set SettingsChanged here since the override state is saved immediately and not part of the main profile save flow
    }

    #endregion
}