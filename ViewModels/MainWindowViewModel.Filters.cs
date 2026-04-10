using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gamelist_Manager.ViewModels;

public partial class MainWindowViewModel
{
    #region Observable Properties
    [ObservableProperty] private bool _showAllItems = true;
    [ObservableProperty] private bool _showVisibleOnly;
    [ObservableProperty] private bool _showHiddenOnly;
    [ObservableProperty] private string _genreFilterSelection = "All Genre";
    [ObservableProperty] private bool _isGenreFilterMode = true;
    [ObservableProperty] private string? _selectedCustomFilterColumn;
    [ObservableProperty] private string _selectedCustomFilterMode = "Is Like";
    [ObservableProperty] private string _customFilterText = string.Empty;
    #endregion

    #region Public Properties
    public ObservableCollection<string> AvailableGenres { get; } = ["All Genre"];

    public string[] FilterModes { get; } =
        ["Is Like", "Is Not Like", "Starts With", "Ends With", "Is", "Is Empty", "Is Not Empty"];

    public string GenreFilterMenuText
    {
        get
        {
            if (GenreFilterSelection != "All Genre")
                return GenreFilterSelection == "Empty Genre"
                    ? "Show Empty Genre Only"
                    : $"Show '{GenreFilterSelection}' Only";

            if (FirstSelectedGame != null)
                return string.IsNullOrEmpty(FirstSelectedGame.GetValue(MetaDataKeys.genre)?.ToString())
                    ? "Show Empty Genre Only"
                    : $"Show {FirstSelectedGame.GetValue(MetaDataKeys.genre)} Only";

            return "Show Selected Genre Only";
        }
    }

    public bool IsGenreFilterEnabled => FirstSelectedGame != null;
    public bool IsGenreFilterActive => GenreFilterSelection != "All Genre";
    public bool IsCustomFilterTextEnabled => SelectedCustomFilterMode is not ("Is Empty" or "Is Not Empty");
    public string SetSelectedGenreVisibleMenuText => $"Set All '{SelectedGenreLabel}' Genre Visible";
    public string SetSelectedGenreHiddenMenuText => $"Set All '{SelectedGenreLabel}' Genre Hidden";
    #endregion

    #region Private Properties
    private Func<GameMetadataRow, bool>? _customFilterPredicate;
    private DispatcherTimer? _customFilterDebounceTimer;

    private string SelectedGenreLabel =>
        FirstSelectedGame == null ? "Selected Genre" :
        string.IsNullOrEmpty(FirstSelectedGame.GetValue(MetaDataKeys.genre)?.ToString()) ? "Empty" :
        FirstSelectedGame.GetValue(MetaDataKeys.genre)!.ToString()!;
    #endregion

    #region Property Change Callbacks
    partial void OnGenreFilterSelectionChanged(string value)
    {
        OnPropertyChanged(nameof(GenreFilterMenuText));
        OnPropertyChanged(nameof(IsGenreFilterActive));
        if (_isLoadingData) return;
        _sourceCache.Refresh();
    }

    partial void OnSelectedCustomFilterModeChanged(string value)
    {
        if (!IsCustomFilterTextEnabled)
            CustomFilterText = string.Empty;
        OnPropertyChanged(nameof(IsCustomFilterTextEnabled));
        TryApplyCustomFilterImmediately();
    }

    partial void OnSelectedCustomFilterColumnChanged(string? value)
    {
        TryApplyCustomFilterImmediately();
    }

    partial void OnCustomFilterTextChanged(string value)
    {
        _customFilterDebounceTimer?.Stop();
        if (string.IsNullOrEmpty(value))
        {
            if (_customFilterPredicate != null)
            {
                _customFilterPredicate = null;
                _sourceCache.Refresh();
            }
            return;
        }
        _customFilterDebounceTimer?.Start();
    }
    #endregion

    #region Commands
    [RelayCommand]
    private void SetShowAllItems()
    {
        ShowAllItems = true;
        ShowVisibleOnly = false;
        ShowHiddenOnly = false;
        _sourceCache.Refresh();
    }

    [RelayCommand]
    private void SetShowVisibleOnly()
    {
        ShowAllItems = false;
        ShowVisibleOnly = true;
        ShowHiddenOnly = false;
        _sourceCache.Refresh();
    }

    [RelayCommand]
    private void SetShowHiddenOnly()
    {
        ShowAllItems = false;
        ShowVisibleOnly = false;
        ShowHiddenOnly = true;
        _sourceCache.Refresh();
    }

    [RelayCommand]
    private void SetShowAllGenres() => GenreFilterSelection = "All Genre";

    [RelayCommand]
    private void SetShowSelectedGenreOnly()
    {
        if (SelectedGames == null || SelectedGames.Count == 0) return;

        var firstGame = SelectedGames.OfType<GameMetadataRow>().FirstOrDefault();
        if (firstGame == null) return;

        GenreFilterSelection = string.IsNullOrEmpty(firstGame.GetValue(MetaDataKeys.genre)?.ToString())
            ? "Empty Genre"
            : firstGame.GetValue(MetaDataKeys.genre)!.ToString()!;
    }

    [RelayCommand]
    private void ClearGenreFilter() => GenreFilterSelection = "All Genre";

    [RelayCommand]
    private void ToggleFilterMode()
    {
        if (IsGenreFilterMode)
        {
            // switching TO custom mode — reset genre filter
            GenreFilterSelection = "All Genre";
        }
        else
        {
            // switching TO genre mode — reset custom filter
            _customFilterDebounceTimer?.Stop();
            _customFilterPredicate = null;
            CustomFilterText = string.Empty;
            SelectedCustomFilterColumn = null;
            _sourceCache.Refresh();
        }
        IsGenreFilterMode = !IsGenreFilterMode;
    }

    [RelayCommand]
    private void ClearCustomFilter()
    {
        _customFilterDebounceTimer?.Stop();
        _customFilterPredicate = null;
        CustomFilterText = string.Empty;
        SelectedCustomFilterColumn = null;
        _sourceCache.Refresh();
    }
    #endregion

    #region Private Methods
    private void InitializeFilterDebounce()
    {
        _customFilterDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _customFilterDebounceTimer.Tick += (_, _) =>
        {
            _customFilterDebounceTimer.Stop();
            TryApplyCustomFilterImmediately();
        };
    }

    private void ClearFilters()
    {
        _customFilterDebounceTimer?.Stop();
        ShowAllItems = true;
        ShowVisibleOnly = false;
        ShowHiddenOnly = false;
        GenreFilterSelection = "All Genre";
        _customFilterPredicate = null;
        CustomFilterText = string.Empty;
        SelectedCustomFilterColumn = null;
    }

    private void TryApplyCustomFilterImmediately()
    {
        if (string.IsNullOrWhiteSpace(SelectedCustomFilterColumn))
            return;

        if (IsCustomFilterTextEnabled && string.IsNullOrEmpty(CustomFilterText))
            return;

        _customFilterPredicate = FilterService.MakeFilter(
            SelectedCustomFilterColumn,
            CustomFilterText,
            SelectedCustomFilterMode ?? "Is Like");
        _sourceCache.Refresh();
    }

    private bool FilterPredicate(GameMetadataRow game)
    {
        bool passesVisibilityFilter = ShowAllItems ||
                                     (ShowVisibleOnly && game.GetValue(MetaDataKeys.hidden) is false) ||
                                     (ShowHiddenOnly && game.GetValue(MetaDataKeys.hidden) is true);

        if (!passesVisibilityFilter) return false;

        if (GenreFilterSelection != "All Genre")
        {
            var gameGenre = game.GetValue(MetaDataKeys.genre)?.ToString();
            if (GenreFilterSelection == "Empty Genre")
            {
                if (!string.IsNullOrEmpty(gameGenre)) return false;
            }
            else
            {
                if (!string.Equals(gameGenre, GenreFilterSelection, StringComparison.OrdinalIgnoreCase)) return false;
            }
        }

        if (_customFilterPredicate != null && !_customFilterPredicate(game))
            return false;

        return true;
    }

    private void PopulateAvailableGenres()
    {
        // Keep "All Genre" permanently at index 0 — never remove it.
        // Calling Clear() fires a CollectionChanged Reset, which causes Avalonia's ComboBox
        // to deselect the current item. The TwoWay binding then writes null back to
        // GenreFilterSelection, sometimes deferred after _isLoadingData is already false.
        // Removing only the items after index 0 avoids that entirely.
        while (AvailableGenres.Count > 1)
            AvailableGenres.RemoveAt(1);

        if (_sourceCache.Items.Any(g => string.IsNullOrEmpty(g.GetValue(MetaDataKeys.genre)?.ToString())))
            AvailableGenres.Add("Empty Genre");

        foreach (var genre in _sourceCache.Items
            .Select(g => g.GetValue(MetaDataKeys.genre)?.ToString())
            .Where(g => !string.IsNullOrEmpty(g))
            .GroupBy(g => g!, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Key))
        {
            AvailableGenres.Add(genre);
        }

        GenreFilterSelection = "All Genre";
    }
    #endregion
}