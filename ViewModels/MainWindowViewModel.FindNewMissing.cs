using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Views;
using System;
using System.Collections.Concurrent;
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
        await FindNewItemsCore(silent: false);
    }

    private async Task FindNewItemsCore(bool silent)
    {
        var scanDir = _sharedData.CurrentRomFolder;
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

        var mediaFolders = GetMediaFolderNames();

        var existingPaths = new HashSet<string>(FilePathHelper.PathComparer);

        if (_sharedData.GamelistData != null)
        {
            foreach (var row in _sharedData.GamelistData)
                existingPaths.Add(FilePathHelper.GamelistPathToFullPath(row.Path, scanDir));
        }

        List<string> newFiles;

        _sharedData.IsBusy = true;
        try
        {
            newFiles = await Task.Run(() =>
                ScanForRoms(scanDir, extensions, mediaFolders, _sharedData.SearchDepth));

            newFiles = newFiles
                .Where(f => !existingPaths.Contains(f))
                .ToList();
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
        finally
        {
            _sharedData.IsBusy = false;
        }

        if (newFiles.Count == 0)
        {
            if (!silent)
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
            }
            return;
        }

        var itemLabel = newFiles.Count == 1 ? "item" : "items";

        var result = await ThreeButtonDialogView.ShowAsync(
            new ThreeButtonDialogConfig
            {
                Title = "Find New Items",
                Message = $"Found {newFiles.Count} new {itemLabel} not in the gamelist.",
                DetailMessage = "Do you want to add them?",
                IconTheme = DialogIconTheme.Question,
                Button1Text = "No",
                Button3Text = "Yes"
            });

        if (result != ThreeButtonResult.Button3)
            return;

        bool newGamelist =
            _sharedData.GamelistData == null ||
            !_sharedData.GamelistData.Any();

        var newRows = newFiles
            .Select(f => BuildNewItemRow(f, scanDir))
            .ToList();

        AddNewFoundItems(newRows);

        if (!newGamelist)
        {
            var newPaths = new HashSet<string>(
                newRows.Select(r => r.Path),
                FilePathHelper.PathComparer);

            RaiseFindReportColumn("Find: New", newPaths);
        }
    }


    [RelayCommand]
    private async Task FindMissingItems()
    {
        await FindMissingItemsCore(silent: false);
    }

    private async Task FindMissingItemsCore(bool silent)
    {
        var directoryToScan = _sharedData.CurrentRomFolder;

        if (string.IsNullOrEmpty(directoryToScan) || _sharedData.GamelistData == null)
            return;

        var allRows = _sharedData.GamelistData.ToList();
        var extensions = GetRomExtensionsForCurrentSystem();

        HashSet<string> missingGamelistPaths;

        _sharedData.IsBusy = true;
        try
        {
            missingGamelistPaths = await Task.Run(() =>
                FindMissingPaths(allRows, directoryToScan, extensions));
        }
        catch (Exception ex)
        {
            await ThreeButtonDialogView.ShowAsync(
                new ThreeButtonDialogConfig
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
        finally
        {
            _sharedData.IsBusy = false;
        }

        if (missingGamelistPaths.Count == 0)
        {
            if (!silent)
            {
                await ThreeButtonDialogView.ShowAsync(
                    new ThreeButtonDialogConfig
                    {
                        Title = "Find Missing Items",
                        Message = "No missing items were found.",
                        IconTheme = DialogIconTheme.Info,
                        Button1Text = "",
                        Button2Text = "",
                        Button3Text = "OK"
                    });
            }
            return;
        }

        var itemLabel = missingGamelistPaths.Count == 1 ? "item" : "items";

        var result = await ThreeButtonDialogView.ShowAsync(
            new ThreeButtonDialogConfig
            {
                Title = "Find Missing Items",
                Message = $"Found {missingGamelistPaths.Count} missing {itemLabel}.",
                DetailMessage = "Mark them in a 'Find: Missing' report column?",
                IconTheme = DialogIconTheme.Question,
                Button1Text = "No",
                Button3Text = "Yes"
            });

        if (result != ThreeButtonResult.Button3)
            return;

        RaiseFindReportColumn("Find: Missing", missingGamelistPaths);
    }

    #endregion


    #region Helpers

    // Returns the set of media subfolder names (e.g. "images", "videos") to exclude from ROM scans.
    private HashSet<string> GetMediaFolderNames()
    {
        return _sharedData.AvailableMedia
            .Where(m => !string.IsNullOrEmpty(m.FolderPath))
            .Select(m => Path.GetFileName(m.FolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)))
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet(FilePathHelper.PathComparer)!;
    }

    // Returns the valid ROM extensions for the current system, or an empty set if not defined.
    private HashSet<string> GetRomExtensionsForCurrentSystem()
    {
        var systemName = _sharedData.CurrentSystem;
        if (string.IsNullOrEmpty(systemName))
            return [];

        var fileTypes = _sharedData.GetFileTypes();

        if (!fileTypes.TryGetValue(systemName, out var csv))
            return [];

        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static List<string> ScanForRoms(
        string rootDir,
        HashSet<string> extensions,
        HashSet<string> mediaFolders,
        int maxDepth)
    {
        var results = new ConcurrentBag<string>();

        ScanDirectory(rootDir, maxDepth);

        return [.. results];

        void ScanDirectory(string dir, int depth)
        {
            string[] files;
            string[] subDirs;

            try { files = Directory.GetFiles(dir); }
            catch { files = []; }

            foreach (var file in files)
            {
                if (extensions.Contains(Path.GetExtension(file)))
                    results.Add(file);
            }

            try { subDirs = Directory.GetDirectories(dir); }
            catch { subDirs = []; }

            foreach (var subDir in subDirs)
            {
                var dirName = Path.GetFileName(subDir);

                // Check if the directory itself is a ROM (e.g. .ps3, .ps3dir, .chd folders)
                // Only matches if the folder extension is in this system's filetypes
                var dirExt = Path.GetExtension(dirName);
                if (!string.IsNullOrEmpty(dirExt) && extensions.Contains(dirExt))
                {
                    results.Add(subDir);
                    continue; // Don't recurse into ROM directories
                }

                // Skip media folders, respect depth limit, recurse into everything else
                if (depth <= 0) continue;
                if (mediaFolders.Contains(dirName)) continue;

                ScanDirectory(subDir, depth - 1);
            }
        }
    }

    private static HashSet<string> FindMissingPaths(
        IReadOnlyList<GameMetadataRow> rows,
        string gamelistDir,
        HashSet<string> extensions)
    {
        var missing = new ConcurrentBag<string>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * 4
        };

        Parallel.ForEach(rows, options, row =>
        {
            if (string.IsNullOrEmpty(row.Path))
                return;

            if (extensions.Count > 0 &&
                !extensions.Contains(Path.GetExtension(row.Path)))
                return;

            var fullPath = FilePathHelper.GamelistPathToFullPath(row.Path, gamelistDir);

            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                missing.Add(row.Path);
        });

        return new HashSet<string>(missing, FilePathHelper.PathComparer);
    }

    private static GameMetadataRow BuildNewItemRow(
        string fullPath,
        string gamelistDir)
    {
        var row = new GameMetadataRow();

        row.SetValue(
            MetaDataKeys.path,
            FilePathHelper.PathToRelativePathWithDotSlashPrefix(
                fullPath,
                gamelistDir));

        row.SetValue(
            MetaDataKeys.name,
            Path.GetFileNameWithoutExtension(fullPath));

        return row;
    }

    #endregion
}


public sealed record FindReportColumnEventArgs(
    string ColumnName,
    HashSet<string> PathSet);