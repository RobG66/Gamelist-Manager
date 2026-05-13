using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    [ObservableProperty] private bool _isSelectedColumnBool;
    #endregion

    #region Public Properties
    public ObservableCollection<string> AvailableGenres { get; } = ["All Genre"];

    public static string[] TextFilterModes { get; } =
        ["Is Like", "Is Not Like", "Starts With", "Ends With", "Is", "Is Empty", "Is Not Empty"];

    public static string[] BoolFilterModes { get; } =
        ["Is Anything", "Is True", "Is False"];

    public string[] CurrentFilterModes => IsSelectedColumnBool ? BoolFilterModes : TextFilterModes;

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
    public bool IsCustomFilterTextEnabled => !IsSelectedColumnBool && SelectedCustomFilterMode is not ("Is Empty" or "Is Not Empty");
    public string SetSelectedGenreVisibleMenuText => $"Set All '{SelectedGenreLabel}' Genre Visible";
    public string SetSelectedGenreHiddenMenuText => $"Set All '{SelectedGenreLabel}' Genre Hidden";
    #endregion

    #region Private Properties
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
        ApplyFilter();
    }

    partial void OnIsSelectedColumnBoolChanged(bool value)
    {
        OnPropertyChanged(nameof(IsCustomFilterTextEnabled));
    }

    partial void OnSelectedCustomFilterModeChanged(string value)
    {
        if (!IsCustomFilterTextEnabled)
            CustomFilterText = string.Empty;
        OnPropertyChanged(nameof(IsCustomFilterTextEnabled));
        ApplyFilter();
    }

    partial void OnSelectedCustomFilterColumnChanged(string? value)
    {
        var decl = value == null ? null
            : GamelistMetaData.GetColumnDeclarations().FirstOrDefault(d => d.Name == value);
        IsSelectedColumnBool = decl?.DataType == MetaDataType.Bool;
        OnPropertyChanged(nameof(CurrentFilterModes));
        SelectedCustomFilterMode = IsSelectedColumnBool ? "Is Anything" : "Is Like";
        CustomFilterText = string.Empty;
        ApplyFilter();
    }

    partial void OnCustomFilterTextChanged(string value)
    {
        _customFilterDebounceTimer?.Stop();
        if (string.IsNullOrEmpty(value))
        {
            if (!string.IsNullOrWhiteSpace(SelectedCustomFilterColumn))
                ApplyFilter();
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
        ApplyFilter();
    }

    [RelayCommand]
    private void SetShowVisibleOnly()
    {
        ShowAllItems = false;
        ShowVisibleOnly = true;
        ShowHiddenOnly = false;
        ApplyFilter();
    }

    [RelayCommand]
    private void SetShowHiddenOnly()
    {
        ShowAllItems = false;
        ShowVisibleOnly = false;
        ShowHiddenOnly = true;
        ApplyFilter();
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
            CustomFilterText = string.Empty;
            SelectedCustomFilterColumn = null;
            ApplyFilter();
        }
        IsGenreFilterMode = !IsGenreFilterMode;
    }

    [RelayCommand]
    private void ClearCustomFilter()
    {
        _customFilterDebounceTimer?.Stop();
        CustomFilterText = string.Empty;
        SelectedCustomFilterColumn = null;
        ApplyFilter();
    }
    #endregion

    #region Private Methods
    private void InitializeFilterDebounce()
    {
        _customFilterDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _customFilterDebounceTimer.Tick += (_, _) =>
        {
            _customFilterDebounceTimer.Stop();
            ApplyFilter();
        };
    }

    private void ClearFilters()
    {
        _customFilterDebounceTimer?.Stop();
        ShowAllItems = true;
        ShowVisibleOnly = false;
        ShowHiddenOnly = false;
        GenreFilterSelection = "All Genre";
        CustomFilterText = string.Empty;
        SelectedCustomFilterColumn = null;
        ApplyFilter();
    }

    // Captures all filter state as local variables so the returned delegate is a
    // stable snapshot — every item in a single pass sees the same criteria.
    private Func<GameMetadataRow, bool> BuildFilterPredicate()
    {
        bool showAll = ShowAllItems;
        bool showVisible = ShowVisibleOnly;
        bool showHidden = ShowHiddenOnly;
        string genre = GenreFilterSelection;

        Func<GameMetadataRow, bool>? customFilter = null;
        if (!string.IsNullOrWhiteSpace(SelectedCustomFilterColumn))
        {
            if (!IsCustomFilterTextEnabled || !string.IsNullOrEmpty(CustomFilterText))
            {
                customFilter = FilterService.MakeFilter(
                    SelectedCustomFilterColumn,
                    CustomFilterText,
                    SelectedCustomFilterMode ?? "Is Like");
            }
        }

        return game =>
        {
            bool passesVisibility = showAll ||
                                    (showVisible && game.GetValue(MetaDataKeys.hidden) is false) ||
                                    (showHidden && game.GetValue(MetaDataKeys.hidden) is true);

            if (!passesVisibility) return false;

            if (genre != "All Genre")
            {
                var gameGenre = game.GetValue(MetaDataKeys.genre)?.ToString();
                if (genre == "Empty Genre")
                {
                    if (!string.IsNullOrEmpty(gameGenre)) return false;
                }
                else
                {
                    if (!string.Equals(gameGenre, genre, StringComparison.OrdinalIgnoreCase)) return false;
                }
            }

            if (customFilter != null && !customFilter(game))
                return false;

            return true;
        };
    }

    private void ApplyFilter() => _filterSubject.OnNext(BuildFilterPredicate());

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