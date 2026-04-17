using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gamelist_Manager.ViewModels;

public partial class SettingsViewModel
{
    #region Observable Properties

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string _mamePath = string.Empty;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string _romsPath = string.Empty;

    #endregion

    #region Public Members

    public ObservableCollection<MediaFolderItem> MediaFolderItems { get; } = new();

    // False when in ES-DE mode — suffixes have no meaning for ES-DE gamelists.
    public bool SuffixesEnabled => !_sharedData.IsEsDeMode;

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
