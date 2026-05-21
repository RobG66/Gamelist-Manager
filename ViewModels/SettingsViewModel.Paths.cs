using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Services;
using Gamelist_Manager.Models;
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

    #endregion

    #region Public Properties

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
                if (!_isProfileLoading && e.PropertyName != nameof(IsDirty))
                    IsDirty = true;
            };
            MediaFolderItems.Add(item);
        }
    }

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

        var detected = EsDePathResolver.ReadPathsFromEsDeSettings(chosen);
        RomsPath = detected.RomDirectory ?? string.Empty;
        EsDeMediaBase = detected.MediaDirectory ?? string.Empty;
    }

    [RelayCommand]
    private void ToggleAllSfx()
    {
        bool newValue = !MediaFolderItems.All(i => i.SfxEnabled);
        foreach (var item in MediaFolderItems)
            item.SfxEnabled = newValue;
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
        var isEsDe = SessionState.Instance.ProfileType == SettingKeys.ProfileTypeEsDe;

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

            if (item.SfxEnabled && !FilePathHelper.IsValidMediaFolderSuffix(item.Suffix))
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

                if (isEsDe)
                {
                    effectivePath = (MetadataService.GetDeclByType(item.Key)?.EsDeFolderName ?? item.Key).ToLowerInvariant();
                    effectiveSuffix = string.Empty;
                }
                else
                {
                    effectivePath = (FilePathHelper.NormalizePathWithDotSlashPrefix(item.Path) ?? item.Path).ToLowerInvariant();
                    effectiveSuffix = item.SfxEnabled ? item.Suffix.ToLowerInvariant() : string.Empty;
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

    private void RefreshMediaFolderDisplayState()
    {
        var isEsDe = SessionState.Instance.ProfileType == SettingKeys.ProfileTypeEsDe;
        var esDeMediaBase = EsDeMediaBase;
        var currentSystem = SessionState.Instance.CurrentSystem;

        foreach (var item in MediaFolderItems)
        {
            var decl = MetadataService.GetDeclByType(item.Key);
            var isEsDeSupported = decl?.IsEsDeSupported ?? false;
            var esDeFolderName = decl?.EsDeFolderName;
            item.RefreshDisplayState(isEsDe, isEsDeSupported, esDeMediaBase, currentSystem, esDeFolderName);
        }
    }

    #endregion

    #region Helpers

    private static string LoadMediaPath(string raw, string defaultPath) =>
        FilePathHelper.NormalizePathWithDotSlashPrefix(raw) ?? defaultPath;

    #endregion
}