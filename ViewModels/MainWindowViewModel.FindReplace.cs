using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using Gamelist_Manager.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class MainWindowViewModel
{
    #region Fields

    private int _findMatchIndex = -1;

    #endregion

    #region Observable Properties

    [ObservableProperty]
    private bool _isSearchPanelVisible;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteReplaceCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteReplaceAllCommand))]
    private bool _isReplaceEnabled;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteFindCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteReplaceCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteReplaceAllCommand))]
    private string _findText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteFindCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteReplaceCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteReplaceAllCommand))]
    private string? _selectedFindColumn;

    [ObservableProperty]
    private string _replaceFromText = string.Empty;

    [ObservableProperty]
    private string _replaceToText = string.Empty;

    [ObservableProperty]
    private string? _selectedReplaceColumn;

    [ObservableProperty]
    private string _replaceItemsButtonText = "All Items";

    [ObservableProperty]
    private bool _replaceAllItems = true;

    #endregion

    #region Public Members

    public ObservableCollection<string> SearchableColumns { get; } = new();
    public event EventHandler<GameMetadataRow>? RequestNavigateToItem;

    public bool IsReplaceAllowed =>
        !string.IsNullOrEmpty(SelectedFindColumn) &&
        GamelistMetaData.GetColumnDeclarations().Any(d => d.Name == SelectedFindColumn && d.Editable);

    #endregion

    #region Partial Handlers

    partial void OnFindTextChanged(string value) => _findMatchIndex = -1;

    partial void OnSelectedFindColumnChanged(string? value)
    {
        _findMatchIndex = -1;
        OnPropertyChanged(nameof(IsReplaceAllowed));
        if (!IsReplaceAllowed)
            IsReplaceEnabled = false;
    }

    partial void OnDescriptionPanelVisibleChanged(bool value)
    {
        if (value)
            SearchableColumns.Add("Description");
        else
            SearchableColumns.Remove("Description");
    }

    #endregion

    #region Column Helpers

    internal void UpdateSearchableColumns(IEnumerable<string> visibleColumnNames)
    {
        SearchableColumns.Clear();

        var nonBoolNames = new HashSet<string>(
            GamelistMetaData.GetColumnDeclarations()
                .Where(d => d.DataType != MetaDataType.Bool)
                .Select(d => d.Name));

        foreach (var name in visibleColumnNames.Where(h => nonBoolNames.Contains(h)))
            SearchableColumns.Add(name);

        if (DescriptionPanelVisible)
            SearchableColumns.Add("Description");
    }

    #endregion

    #region CanExecute

    private bool CanExecuteFind() =>
        !string.IsNullOrWhiteSpace(FindText) && !string.IsNullOrEmpty(SelectedFindColumn);

    private bool CanExecuteReplace() =>
        IsReplaceEnabled &&
        !string.IsNullOrWhiteSpace(FindText) &&
        !string.IsNullOrEmpty(SelectedFindColumn);

    #endregion

    #region Commands

    [RelayCommand]
    private void ToggleSearchPanel() => IsSearchPanelVisible = !IsSearchPanelVisible;

    [RelayCommand]
    private void CloseSearchPanel() => IsSearchPanelVisible = false;

    [RelayCommand]
    private void ToggleReplaceScope()
    {
        ReplaceAllItems = !ReplaceAllItems;
        ReplaceItemsButtonText = ReplaceAllItems ? "All Items" : "Selected Items";
    }

    [RelayCommand(CanExecute = nameof(CanExecuteFind))]
    private async Task ExecuteFind()
    {
        var predicate = FilterService.MakeFilter(SelectedFindColumn!, FindText, "Is Like");
        var matches = _games.Where(predicate).ToList();

        if (matches.Count == 0)
        {
            _findMatchIndex = -1;
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Find",
                Message = $"No match found for \"{FindText}\".",
                IconTheme = DialogIconTheme.Info,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            });
            return;
        }

        _findMatchIndex = (_findMatchIndex + 1) % matches.Count;
        RequestNavigateToItem?.Invoke(this, matches[_findMatchIndex]);
    }

    [RelayCommand(CanExecute = nameof(CanExecuteReplace))]
    private async Task ExecuteReplace()
    {
        var predicate = FilterService.MakeFilter(SelectedFindColumn!, FindText, "Is Like");
        var matches = _games.Where(predicate).ToList();

        if (matches.Count == 0)
        {
            _findMatchIndex = -1;
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Replace",
                Message = $"No match found for \"{FindText}\".",
                IconTheme = DialogIconTheme.Info,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            });
            return;
        }

        if (_findMatchIndex < 0 || _findMatchIndex >= matches.Count)
        {
            _findMatchIndex = 0;
            RequestNavigateToItem?.Invoke(this, matches[0]);
            return;
        }

        PerformReplaceInRows([matches[_findMatchIndex]], SelectedFindColumn!, FindText, ReplaceToText);

        matches = _games.Where(predicate).ToList();

        if (matches.Count == 0)
        {
            _findMatchIndex = -1;
            return;
        }

        _findMatchIndex %= matches.Count;
        RequestNavigateToItem?.Invoke(this, matches[_findMatchIndex]);
    }

    [RelayCommand(CanExecute = nameof(CanExecuteReplace))]
    private async Task ExecuteReplaceAll()
    {
        var predicate = FilterService.MakeFilter(SelectedFindColumn!, FindText, "Is Like");

        var scopeRows = ReplaceAllItems
            ? _games.ToList()
            : SelectedGames?.OfType<GameMetadataRow>().ToList() ?? [];

        var matches = scopeRows.Where(predicate).ToList();

        if (matches.Count == 0)
        {
            _findMatchIndex = -1;
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Replace All",
                Message = $"No match found for \"{FindText}\".",
                IconTheme = DialogIconTheme.Info,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            });
            return;
        }

        if (_sharedData.ConfirmBulkChanges)
        {
            var totalOccurrences = CountReplacementOccurrences(matches, SelectedFindColumn!, FindText);
            var occurrenceLabel = totalOccurrences == 1 ? "occurrence" : "occurrences";
            var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Replace All",
                Message = $"Replace {totalOccurrences} {occurrenceLabel} of \"{FindText}\"?",
                IconTheme = DialogIconTheme.Question,
                Button1Text = "No",
                Button2Text = "",
                Button3Text = "Yes"
            });
            if (result != ThreeButtonResult.Button3) return;
        }

        var replaced = PerformReplaceInRows(matches, SelectedFindColumn!, FindText, ReplaceToText);
        _findMatchIndex = -1;

        var replacedLabel = replaced == 1 ? "occurrence" : "occurrences";
        await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Replace All",
            Message = replaced > 0
                ? $"Replaced {replaced} {replacedLabel} of \"{FindText}\"."
                : "No text was replaced.",
            IconTheme = DialogIconTheme.Info,
            Button1Text = "",
            Button2Text = "",
            Button3Text = "OK"
        });
    }

    #endregion
}
