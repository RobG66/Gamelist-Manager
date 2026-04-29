using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using Gamelist_Manager.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class MainWindowViewModel
{
    #region Visibility Commands
    [RelayCommand]
    private Task SetItemsVisible(string scope) => SetItemsVisibility(scope, false);

    [RelayCommand]
    private Task SetItemsHidden(string scope) => SetItemsVisibility(scope, true);
    #endregion

    #region Clear Commands
    [RelayCommand]
    private Task ClearData(string scope) => ClearItems(scope, ClearGameData, "Clear data");

    [RelayCommand]
    private Task ClearMediaPaths(string scope) => ClearItems(scope, ClearMediaPathData, "Clear media paths");

    [RelayCommand]
    private Task ResetNames(string scope) => ClearItems(scope, ResetGameName, "Reset names");
    #endregion

    #region Sort Commands
    [RelayCommand]
    private void SortSelectionToTop()
    {
        if (SelectedGames == null || SelectedGames.Count == 0) return;
        if (_sharedData.GamelistData == null) return;

        // Only promote items that are currently visible through the filter
        var visibleSet = _games.ToHashSet();
        var selected = SelectedGames.OfType<GameMetadataRow>()
            .Where(visibleSet.Contains)
            .ToList();

        if (selected.Count == 0) return;

        var selectedSet = selected.ToHashSet();
        var reordered = selected
            .Concat(_sharedData.GamelistData.Where(g => !selectedSet.Contains(g)))
            .ToList();

        // Rebuild the master list in the new order
        _isLoadingData = true;
        _sharedData.GamelistData.Clear();
        foreach (var game in reordered)
            _sharedData.GamelistData.Add(game);

        // AddOrUpdate with a list delegates to the internal dictionary, which
        // iterates in hash order rather than insertion order. Clearing and then
        // adding items one-by-one forces the bound collection to receive Add
        // changesets in the exact sequence we want.
        _sourceCache.Clear();
        foreach (var game in reordered)
            _sourceCache.AddOrUpdate(game);
        _isLoadingData = false;

        _sharedData.IsDataChanged = true;
        IsSaveEnabled = true;

        // Defer re-selection until DynamicData has pushed the changeset through
        // the filter pipeline and the DataGrid has completed its layout pass
        Dispatcher.UIThread.Post(() =>
            Dispatcher.UIThread.Post(() => RequestRestoreSelection?.Invoke(this, selected),
                DispatcherPriority.Background),
            DispatcherPriority.Background);
    }
    #endregion

    #region Delete / Remove Commands
    [RelayCommand]
    private async Task DeleteSelectedItems()
    {
        if (SelectedGames == null || !_sharedData.EnableDelete) return;

        var itemLabel = SelectedGames.Count == 1 ? "item" : "items";
        var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Confirm Deletion",
            Message = $"Do you want to permanently delete {SelectedGames.Count} {itemLabel}?",
            IconTheme = DialogIconTheme.Question,
            Button1Text = "No",
            Button2Text = "",
            Button3Text = "Yes"
        });
        if (result != ThreeButtonResult.Button3) return;

        var toDelete = SelectedGames.OfType<GameMetadataRow>().ToList();
        foreach (var game in toDelete)
        {
            if (!game.Path.StartsWith("./")) continue;
            DeleteGameFile(game);
            DeleteMediaFiles(game);
            RemoveGame(game);
        }

        _sourceCache.Refresh();
    }

    [RelayCommand]
    private async Task RemoveSelectedItems()
    {
        if (SelectedGames == null || SelectedGames.Count == 0) return;

        var itemLabel = SelectedGames.Count == 1 ? "item" : "items";
        var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Confirm Remove",
            Message = $"Remove {SelectedGames.Count} {itemLabel} from the gamelist?",
            IconTheme = DialogIconTheme.Question,
            Button1Text = "No",
            Button2Text = "",
            Button3Text = "Yes"
        });
        if (result != ThreeButtonResult.Button3) return;

        foreach (var game in SelectedGames.OfType<GameMetadataRow>().ToList())
            RemoveGame(game);

        _sourceCache.Refresh();
    }
    #endregion

    #region Private Methods
    private async Task SetItemsVisibility(string scope, bool hidden)
    {
        var games = GetGamesByScope(scope);
        if (games == null || games.Count == 0) return;

        if (_sharedData.ConfirmBulkChanges && games.Count > 1)
        {
            var action = hidden ? "hidden" : "visible";
            var label = ScopeLabel(scope, games.Count);
            var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Confirm Bulk Operation",
                Message = $"Set {label} items {action}?",
                IconTheme = DialogIconTheme.Question,
                Button1Text = "No",
                Button2Text = "",
                Button3Text = "Yes"
            });
            if (result != ThreeButtonResult.Button3) return;
        }

        foreach (var game in games)
            game.SetValue(MetaDataKeys.hidden, hidden);

        _sourceCache.Refresh();
    }

    private async Task ClearItems(string scope, Action<GameMetadataRow> clearAction, string actionLabel)
    {
        var games = scope == "all" ? Games.ToList() : SelectedGames?.OfType<GameMetadataRow>().ToList();
        if (games == null || games.Count == 0) return;

        if (_sharedData.ConfirmBulkChanges && games.Count > 1)
        {
            var label = ScopeLabel(scope, games.Count);
            var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Confirm Bulk Operation",
                Message = $"{actionLabel} for {label} items?",
                IconTheme = DialogIconTheme.Question,
                Button1Text = "No",
                Button2Text = "",
                Button3Text = "Yes"
            });
            if (result != ThreeButtonResult.Button3) return;
        }

        _mediaPreviewViewModel.SuspendVideo();

        foreach (var game in games)
            clearAction(game);

        _sourceCache.Refresh();
        _mediaPreviewViewModel.InitializeLibVLC();
    }

    private List<GameMetadataRow>? GetGamesByScope(string scope) => scope switch
    {
        "all" => _sourceCache.Items.ToList(),
        "selected" => SelectedGames?.OfType<GameMetadataRow>().ToList(),
        "genre" => GetGamesByGenre(FirstSelectedGame),
        _ => null
    };

    private List<GameMetadataRow> GetGamesByGenre(GameMetadataRow? sourceGame)
    {
        if (sourceGame == null) return [];
        var genreFilter = sourceGame.GetValue(MetaDataKeys.genre)?.ToString();
        return _sourceCache.Items.Where(g =>
        {
            var gameGenre = g.GetValue(MetaDataKeys.genre)?.ToString();
            return string.IsNullOrEmpty(genreFilter)
                ? string.IsNullOrEmpty(gameGenre)
                : string.Equals(gameGenre, genreFilter, StringComparison.OrdinalIgnoreCase);
        }).ToList();
    }

    private static string ScopeLabel(string scope, int count) => scope switch
    {
        "all" => $"all {count} visible",
        "genre" => $"all {count} genre",
        _ => $"{count} selected"
    };

    private void RemoveGame(GameMetadataRow game)
    {
        game.PropertyChanged -= GameItem_PropertyChanged;
        _sourceCache.Remove(game);
        _sharedData.GamelistData?.Remove(game);
        _sharedData.IsDataChanged = true;
        IsSaveEnabled = true;
    }

    private void AddGame(GameMetadataRow game)
    {
        _sharedData.GamelistData?.Add(game);
        _sourceCache.AddOrUpdate(game);
        game.PropertyChanged += GameItem_PropertyChanged;
        _sharedData.IsDataChanged = true;
        IsSaveEnabled = true;
    }

    private void AddNewFoundItems(IReadOnlyList<GameMetadataRow> items)
    {
        foreach (var item in items)
            AddGame(item);
        PopulateAvailableGenres();
    }

    private void DeleteGameFile(GameMetadataRow game)
    {
        var romDirectory = _sharedData.CurrentRomFolder;
        // Should never be empty, but safe to check before doing any file operations
        if (string.IsNullOrEmpty(romDirectory)) return;

        var romFullPath = FilePathHelper.GamelistPathToFullPath(game.Path, romDirectory);

        var normalizedRomDir = romDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                               + Path.DirectorySeparatorChar;

        if (!romFullPath.StartsWith(normalizedRomDir, FilePathHelper.PathComparison))
        {
            return;
        }

        try
        {
            if (File.Exists(romFullPath))
                File.Delete(romFullPath);
            else if (Directory.Exists(romFullPath))
                Directory.Delete(romFullPath, recursive: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to delete ROM: {ex.Message}");
        }
    }

    private void DeleteMediaFiles(GameMetadataRow game)
    {
        foreach (var media in _sharedData.AvailableMedia)
        {
            if (!Enum.TryParse<MetaDataKeys>(media.Type, out var key)) continue;

            var storedPath = game.GetValue(key)?.ToString()?.Trim();
            if (string.IsNullOrEmpty(storedPath)) continue;

            // Stored paths are relative (e.g. "./images/boxart.png"); extract just the filename.
            var fileName = Path.GetFileName(storedPath);
            if (string.IsNullOrEmpty(fileName)) continue;

            var fullPath = Path.Combine(media.FolderPath, fileName);
            if (!File.Exists(fullPath)) continue;

            try { File.Delete(fullPath); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Failed to delete media file: {ex.Message}"); }
        }
    }

    private static void ResetGameName(GameMetadataRow game)
    {
        game.SetValue(MetaDataKeys.name, System.IO.Path.GetFileNameWithoutExtension(game.Path));
        game.NotifyDataChanged();
    }

    private static void ClearGameData(GameMetadataRow game)
    {
        var nameFromPath = System.IO.Path.GetFileNameWithoutExtension(game.Path);
        var metaDataDict = GamelistMetaData.GetMetaDataDictionary();

        foreach (var entry in metaDataDict.Values)
        {
            if (entry.Key == MetaDataKeys.path) continue;

            if (entry.Key == MetaDataKeys.name)
                game.SetValue(entry.Key, nameFromPath);
            else if (entry.DataType == MetaDataType.Bool)
                game.SetValue(entry.Key, false);
            else
                game.SetValue(entry.Key, null);
        }

        game.NotifyDataChanged();
    }

    private static void ClearMediaPathData(GameMetadataRow game)
    {
        foreach (var decl in GamelistMetaData.GetColumnDeclarations().Where(d => d.IsMedia))
            game.SetValue(decl.Key, null);

        game.NotifyDataChanged();
    }

    // Returns the total number of text occurrences replaced across all rows.
    // SetValue triggers GameItem_PropertyChanged which handles dirty flags and per-row cache refresh.
    internal int PerformReplaceInRows(IList<GameMetadataRow> rows, string columnName, string findText, string replaceText)
    {
        if (!Enum.TryParse<MetaDataKeys>(GamelistMetaData.GetMetadataTypeByName(columnName), true, out var metaKey))
            return 0;

        int total = 0;
        foreach (var row in rows)
        {
            var current = row.GetValue(metaKey)?.ToString() ?? string.Empty;
            var updated = current.Replace(findText, replaceText, StringComparison.OrdinalIgnoreCase);
            if (!string.Equals(current, updated, StringComparison.Ordinal))
            {
                total += CountOccurrences(current, findText);
                row.SetValue(metaKey, updated);
            }
        }

        return total;
    }

    internal int CountReplacementOccurrences(IList<GameMetadataRow> rows, string columnName, string findText)
    {
        if (!Enum.TryParse<MetaDataKeys>(GamelistMetaData.GetMetadataTypeByName(columnName), true, out var metaKey))
            return 0;

        return rows.Sum(row => CountOccurrences(row.GetValue(metaKey)?.ToString() ?? string.Empty, findText));
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0, index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
    #endregion
}
