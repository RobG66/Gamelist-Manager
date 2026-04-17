using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class MainWindowViewModel
{
    #region Commands

    [RelayCommand]
    private async Task FindNewItems()
    {
        var scanDir = _sharedData.RomScanDirectory;
        var systemName = _sharedData.CurrentSystem;

        if (string.IsNullOrEmpty(scanDir) || string.IsNullOrEmpty(systemName))
            return;

        var fileTypes = _sharedData.GetFileTypes();

        if (!fileTypes.TryGetValue(systemName, out var extensionsCsv))
        {
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Find New Items",
                Message = $"No file types are defined for system '{systemName}'.",
                IconTheme = DialogIconTheme.Info,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            });
            return;
        }

        var extensions = extensionsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingPaths = new HashSet<string>(FilePathHelper.PathComparer);
        if (_sharedData.GamelistData != null)
        {
            foreach (var row in _sharedData.GamelistData)
                existingPaths.Add(FilePathHelper.GamelistPathToFullPath(row.Path, scanDir));
        }

        List<string> newFiles;
        try
        {
            newFiles = await Task.Run(() =>
                EnumerateFilesUpToDepth(scanDir, _sharedData.SearchDepth)
                    .Where(f => extensions.Contains(Path.GetExtension(f)))
                    .Where(f => !existingPaths.Contains(f))
                    .ToList());
        }
        catch (Exception ex)
        {
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Find New Items",
                Message = $"Error scanning directory: {ex.Message}",
                IconTheme = DialogIconTheme.Error,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            });
            return;
        }

        if (newFiles.Count == 0)
        {
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Find New Items",
                Message = "No new items were found.",
                IconTheme = DialogIconTheme.Info,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            });
            return;
        }

        var itemLabel = newFiles.Count == 1 ? "item" : "items";
        var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Find New Items",
            Message = $"Found {newFiles.Count} new {itemLabel} not in the gamelist.",
            DetailMessage = "Add them with genre 'New Item'?",
            IconTheme = DialogIconTheme.Question,
            Button1Text = "No",
            Button2Text = "",
            Button3Text = "Yes"
        });

        if (result != ThreeButtonResult.Button3) return;

        var newRows = newFiles.Select(f => BuildNewItemRow(f, scanDir)).ToList();
        AddNewFoundItems(newRows);

        var newPaths = new HashSet<string>(newRows.Select(r => r.Path), FilePathHelper.PathComparer);
        RaiseFindReportColumn("Find: New", newPaths);
    }

    [RelayCommand]
    private async Task FindMissingItems()
    {
        var gamelistDir = _sharedData.GamelistDirectory;
        if (string.IsNullOrEmpty(gamelistDir) || _sharedData.GamelistData == null)
            return;

        var allRows = _sharedData.GamelistData.ToList();

        HashSet<string> missingGamelistPaths;
        try
        {
            missingGamelistPaths = await Task.Run(() =>
            {
                var set = new HashSet<string>(FilePathHelper.PathComparer);
                foreach (var row in allRows)
                {
                    var fullPath = FilePathHelper.GamelistPathToFullPath(row.Path, gamelistDir);
                    if (!File.Exists(fullPath))
                        set.Add(row.Path);
                }
                return set;
            });
        }
        catch (Exception ex)
        {
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Find Missing Items",
                Message = $"Error checking files: {ex.Message}",
                IconTheme = DialogIconTheme.Error,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            });
            return;
        }

        if (missingGamelistPaths.Count == 0)
        {
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Find Missing Items",
                Message = "No missing items were found.",
                IconTheme = DialogIconTheme.Info,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            });
            return;
        }

        var itemLabel = missingGamelistPaths.Count == 1 ? "item" : "items";
        var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Find Missing Items",
            Message = $"Found {missingGamelistPaths.Count} {itemLabel} with missing files.",
            DetailMessage = "Mark them in a 'Find: Missing' report column?",
            IconTheme = DialogIconTheme.Question,
            Button1Text = "No",
            Button2Text = "",
            Button3Text = "Yes"
        });

        if (result != ThreeButtonResult.Button3) return;

        RaiseFindReportColumn("Find: Missing", missingGamelistPaths);
    }

    #endregion

    #region Helpers

    private static GameMetadataRow BuildNewItemRow(string fullPath, string gamelistDir)
    {
        var row = new GameMetadataRow();
        row.SetValue(MetaDataKeys.path, FilePathHelper.PathToRelativePathWithDotSlashPrefix(fullPath, gamelistDir));
        row.SetValue(MetaDataKeys.name, Path.GetFileNameWithoutExtension(fullPath));
        row.SetValue(MetaDataKeys.genre, "New Item");
        return row;
    }

    private static IEnumerable<string> EnumerateFilesUpToDepth(string directory, int depth)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
            yield return file;

        if (depth > 0)
        {
            foreach (var subDir in Directory.EnumerateDirectories(directory))
                foreach (var file in EnumerateFilesUpToDepth(subDir, depth - 1))
                    yield return file;
        }
    }

    #endregion
}

public sealed record FindReportColumnEventArgs(string ColumnName, HashSet<string> PathSet);
