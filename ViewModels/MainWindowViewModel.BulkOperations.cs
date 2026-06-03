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
    private async Task ResetNames(string scope)
    {
        // Determine scope and get games first, before any async work
        var games = scope == "all" ? Games.ToList() : SelectedGames?.OfType<GameMetadataRow>().ToList();
        if (games == null || games.Count == 0) return;

        if (_settingsState.ConfirmBulkChanges && games.Count > 1)
        {
            var label = ScopeLabel(scope, games.Count);
            var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Confirm Bulk Operation",
                Message = $"Reset names for {label} items?",
                IconTheme = DialogIconTheme.Question,
                Button1Text = "No",
                Button2Text = "",
                Button3Text = "Yes"
            });
            if (result != ThreeButtonResult.Button3) return;
        }

        IReadOnlyDictionary<string, string>? mameNames = null;

        if (UseMameInternalNames && CanUseMameInternalNames)
        {
            try
            {
                _sessionState.IsBusy = true;

                if (MameNamesHelper.Names.Count == 0)
                    await MameNamesHelper.GenerateAsync(_settingsState.MamePath);

                if (MameNamesHelper.Names.Count > 0)
                    mameNames = MameNamesHelper.Names;
            }
            finally
            {
                _sessionState.IsBusy = false;
            }
        }

        foreach (var game in games)
            ResetGameName(game, mameNames);

        _sourceCache.Refresh();
        _sessionState.IsDataChanged = true;
        IsSaveEnabled = true;
    }

    [RelayCommand]
    private async Task SetColumnValue()
    {
        var options = await SetColumnValueView.ShowAsync(HasGameSelected);
        if (options == null) return;

        var games = options.UseAllItems
            ? Games.ToList()
            : SelectedGames?.OfType<GameMetadataRow>().ToList();

        if (games == null || games.Count == 0) return;

        if (_settingsState.ConfirmBulkChanges && games.Count > 1)
        {
            var label = options.UseAllItems ? $"all {games.Count} visible" : $"{games.Count} selected";
            var decl = MetadataService.GetMetaDataDictionary()[options.Key];
            var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Confirm Bulk Operation",
                Message = $"Set '{decl.Name}' for {label} items?",
                IconTheme = DialogIconTheme.Question,
                Button1Text = "No",
                Button2Text = "",
                Button3Text = "Yes"
            });
            if (result != ThreeButtonResult.Button3) return;
        }

        // For bool columns the dialog always returns true or false — no coercion needed
        var writeValue = options.Value;

        foreach (var game in games)
            game.SetValue(options.Key, writeValue);

        _sourceCache.Refresh();
        _sessionState.IsDataChanged = true;
        IsSaveEnabled = true;
    }
    #endregion

    #region Sort Commands
    [RelayCommand]
    private void SortSelectionToTop()
    {
        if (SelectedGames == null || SelectedGames.Count == 0) return;
        if (_sessionState.GamelistData == null) return;

        // Only promote items that are currently visible through the filter
        var visibleSet = _games.ToHashSet();
        var selected = SelectedGames.OfType<GameMetadataRow>()
            .Where(visibleSet.Contains)
            .ToList();

        if (selected.Count == 0) return;

        var selectedSet = selected.ToHashSet();
        var reordered = selected
            .Concat(_sessionState.GamelistData.Where(g => !selectedSet.Contains(g)))
            .ToList();

        // Clear the DataGrid's selection before modifying the source cache.
        // The DataGrid holds slot-index references that become out-of-bounds the
        // moment the cache is cleared; raising this event lets the view flush its
        // selection synchronously before any collection-changed notifications fire.
        RequestClearSelection?.Invoke(this, EventArgs.Empty);

        // Rebuild the master list in the new order
        _isLoadingData = true;
        _sessionState.GamelistData.Clear();
        foreach (var game in reordered)
            _sessionState.GamelistData.Add(game);

        // AddOrUpdate with a list delegates to the internal dictionary, which
        // iterates in hash order rather than insertion order. Clearing and then
        // adding items one-by-one forces the bound collection to receive Add
        // changesets in the exact sequence we want.
        _sourceCache.Clear();
        foreach (var game in reordered)
            _sourceCache.AddOrUpdate(game);
        _isLoadingData = false;

        // Defer re-selection
        // the filter pipeline and the DataGrid has completed its layout pass
        Dispatcher.UIThread.Post(() =>
            Dispatcher.UIThread.Post(() => RequestRestoreSelection?.Invoke(this, selected),
                DispatcherPriority.Background),
            DispatcherPriority.Background);
    }
    #endregion

    #region Clipboard Commands
    [RelayCommand]
    private async Task CopyRomFilenamesToClipboard()
    {
        if (SelectedGames == null || SelectedGames.Count == 0) return;

        var filenames = SelectedGames.OfType<GameMetadataRow>()
            .Select(g => System.IO.Path.GetFileName(g.Path))
            .Where(f => !string.IsNullOrEmpty(f));

        await _windowService.CopyToClipboardAsync(string.Join(Environment.NewLine, filenames));
    }

    [RelayCommand]
    private async Task CopyRomPathsToClipboard()
    {
        if (SelectedGames == null || SelectedGames.Count == 0) return;
        if (string.IsNullOrEmpty(_sessionState.CurrentRomFolder)) return;

        var paths = SelectedGames.OfType<GameMetadataRow>()
            .Select(g => FilePathHelper.GamelistPathToFullPath(g.Path, _sessionState.CurrentRomFolder!))
            .Where(p => !string.IsNullOrEmpty(p));

        await _windowService.CopyToClipboardAsync(string.Join(Environment.NewLine, paths));
    }
    #endregion

    #region Delete / Remove Commands
    [RelayCommand]
    private async Task DeleteSelectedItems()
    {
        if (SelectedGames == null || !_settingsState.EnableDelete) return;

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

        if (_settingsState.ConfirmBulkChanges && games.Count > 1)
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

        if (_settingsState.ConfirmBulkChanges && games.Count > 1)
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
        _sessionState.GamelistData?.Remove(game);
        _sessionState.IsDataChanged = true;
        IsSaveEnabled = true;
    }

    private void AddGame(GameMetadataRow game)
    {
        _sessionState.GamelistData?.Add(game);
        _sourceCache.AddOrUpdate(game);
        game.PropertyChanged += GameItem_PropertyChanged;
        _sessionState.IsDataChanged = true;
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
        var romDirectory = _sessionState.CurrentRomFolder;
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
        foreach (var media in _sessionState.AvailableMedia)
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

    private static void ResetGameName(
    GameMetadataRow game,
    IReadOnlyDictionary<string, string>? mameNames = null)
    {
        var romName = Path.GetFileNameWithoutExtension(game.Path);
        var displayName = romName;

        if (mameNames != null &&
            !string.IsNullOrWhiteSpace(romName) &&
            mameNames.TryGetValue(romName, out var mameName) &&
            !string.IsNullOrWhiteSpace(mameName))
        {
            displayName = mameName;
        }

        game.SetValue(MetaDataKeys.name, displayName);
        game.NotifyDataChanged();
    }

    private static void ClearGameData(GameMetadataRow game)
    {
        var nameFromPath = System.IO.Path.GetFileNameWithoutExtension(game.Path);

        foreach (var decl in MetadataService.GetMetaDataDictionary().Values)
        {
            if (decl.Key == MetaDataKeys.path) continue;

            if (decl.Key == MetaDataKeys.name)
                game.SetValue(decl.Key, nameFromPath);
            else if (decl.DataType == MetaDataType.Bool)
                game.SetValue(decl.Key, false);
            else
                game.SetValue(decl.Key, null);
        }

        game.NotifyDataChanged();
    }

    private static void ClearMediaPathData(GameMetadataRow game)
    {
        foreach (var decl in MetadataService.GetMediaDeclarations())
            game.SetValue(decl.Key, null);

        game.NotifyDataChanged();
    }

    // Returns the total number of text occurrences replaced across all rows.
    // SetValue triggers GameItem_PropertyChanged which handles dirty flags and per-row cache refresh.
    internal int PerformReplaceInRows(IList<GameMetadataRow> rows, string columnName, string findText, string replaceText)
    {
        if (!Enum.TryParse<MetaDataKeys>(MetadataService.GetMetadataTypeByName(columnName), true, out var metaKey))
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
        if (!Enum.TryParse<MetaDataKeys>(MetadataService.GetMetadataTypeByName(columnName), true, out var metaKey))
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
