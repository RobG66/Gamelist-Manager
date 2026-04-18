using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class SettingsViewModel
{
    #region Observable Properties

    [ObservableProperty]
    private string _mamePath = string.Empty;

    [ObservableProperty]
    private string _romsPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsDePathsVisible))]
    private bool _isEsDeProfile;

    [ObservableProperty]
    private string _esDeRoot = string.Empty;

    [ObservableProperty]
    private string _esDeMediaBase = string.Empty;

    #endregion

    #region Public Properties

    public ObservableCollection<MediaFolderItem> MediaFolderItems { get; } = new();

    // False when in ES-DE mode — suffixes have no meaning for ES-DE gamelists.
    public bool SuffixesEnabled => !_sharedData.IsEsDeMode;

    // Controls visibility of the ES-DE Root row in the Paths tab.
    public bool EsDePathsVisible => IsEsDeProfile;

    #endregion

    #region Initialization

    private void InitializeMediaFolderItems()
    {
        foreach (var decl in GamelistMetaData.GetAllMediaFolderTypes())
        {
            var item = new MediaFolderItem
            {
                Key = decl.Type,
                Label = decl.Name,
                DefaultPath = decl.DefaultPath,
                DefaultSuffix = decl.DefaultSuffix,
                DefaultEnabled = decl.DefaultEnabled,
                EsDeFolderName = decl.EsDeFolderName,
            };
            item.Enabled = item.DefaultEnabled;
            item.Path = item.DefaultPath;
            item.Suffix = item.DefaultSuffix;
            item.SfxEnabled = item.DefaultSfxEnabled;
            item.PropertyChanged += (_, e) =>
            {
                if (!_isLoading && e.PropertyName != nameof(IsDirty))
                    IsDirty = true;
            };
            MediaFolderItems.Add(item);
        }
    }

    #endregion

    #region Helpers

    private static string LoadMediaPath(string raw, string defaultPath) =>
        FilePathHelper.NormalizePathWithDotSlashPrefix(raw) ?? defaultPath;

    #endregion

    #region Commands

    [RelayCommand]
    private async Task BrowseEsDeRootAsync()
    {
        var chosen = await FolderPickerHelper.BrowseForFolderAsync(
            "Select ES-DE Root Folder",
            EsDeRoot);

        if (string.IsNullOrEmpty(chosen))
            return;

        EsDeRoot = chosen;

        // Always re-detect — the user may have changed es_settings.xml outside this app.
        var detected = SettingsService.ReadPathsFromEsDeSettings(chosen);
        RomsPath = detected.RomDirectory ?? string.Empty;
        EsDeMediaBase = detected.MediaDirectory ?? string.Empty;
    }

    #endregion

    #region Internal Methods

    internal void LoadEsDeSettings()
    {
        var profileType = SettingsService.Instance.GetValue(SettingKeys.EsDeSection, SettingKeys.ProfileType, SettingKeys.ProfileTypeStandard);
        IsEsDeProfile = string.Equals(profileType, SettingKeys.ProfileTypeEsDe, System.StringComparison.OrdinalIgnoreCase);
        EsDeRoot = SettingsService.Instance.GetValue(SettingKeys.EsDeSection, SettingKeys.EsDeRoot, string.Empty);

        // Always re-detect — the user may have changed es_settings.xml outside this app.
        var detected = SettingsService.ReadPathsFromEsDeSettings(EsDeRoot);
        RomsPath = detected.RomDirectory ?? string.Empty;
        EsDeMediaBase = detected.MediaDirectory ?? string.Empty;
    }

    internal void SaveEsDeSettings()
    {
        SettingsService.Instance.SetValue(SettingKeys.EsDeSection, SettingKeys.ProfileType,
            IsEsDeProfile ? SettingKeys.ProfileTypeEsDe : SettingKeys.ProfileTypeStandard);
        SettingsService.Instance.SetValue(SettingKeys.EsDeSection, SettingKeys.EsDeRoot, EsDeRoot);
    }

    #endregion

    #region Public Methods

    public void ResetFolderPaths()
    {
        foreach (var item in MediaFolderItems)
            item.ResetToDefaults();
    }

    [RelayCommand]
    private void ToggleAllSfx()
    {
        bool newValue = !MediaFolderItems.All(i => i.SfxEnabled);
        foreach (var item in MediaFolderItems)
            item.SfxEnabled = newValue;
    }

    public List<string> ValidateAndTrimPaths()
    {
        var errors = new List<string>();

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
                errors.Add($"{item.Label}: Folder path is invalid. Use a relative path with no drive letters, UNC paths, or directory traversal (e.g. ./images).");
                continue;
            }

            var normalized = FilePathHelper.NormalizePathWithDotSlashPrefix(item.Path);
            if (normalized == null)
            {
                errors.Add($"{item.Label}: Folder path is empty or invalid after normalization.");
                continue;
            }

            item.Path = normalized;

            if (item.SfxEnabled && !FilePathHelper.IsValidMediaFolderSuffix(item.Suffix))
                errors.Add($"{item.Label}: Suffix must be alphanumeric only (a-z, A-Z, 0-9) and no longer than 20 characters.");
        }

        if (errors.Count == 0)
        {
            var seen = new Dictionary<(string path, string suffix), string>();
            foreach (var item in MediaFolderItems)
            {
                if (!item.EffectiveEnabled) continue;

                // In ES-DE mode each type lives in its own named subfolder, so use that
                // as the collision key rather than the generic relative path.
                string effectivePath;
                string effectiveSuffix;
                if (_sharedData.IsEsDeMode)
                {
                    effectivePath = item.EsDeFolderName.ToLowerInvariant();
                    effectiveSuffix = "";
                }
                else
                {
                    effectivePath = (FilePathHelper.NormalizePathWithDotSlashPrefix(item.Path) ?? item.Path).ToLowerInvariant();
                    effectiveSuffix = item.SfxEnabled ? item.Suffix.ToLowerInvariant() : "";
                }

                var key = (effectivePath, effectiveSuffix);
                if (seen.TryGetValue(key, out var firstName))
                    errors.Add($"{item.Label} and {firstName} share the same folder and filename suffix, which would cause a collision.");
                else
                    seen[key] = item.Label;
            }
        }

        return errors;
    }

    #endregion
}
