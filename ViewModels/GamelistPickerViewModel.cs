using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Gamelist_Manager.ViewModels;

public partial class SystemPickerItem : ObservableObject
{
    public string Name { get; init; } = string.Empty;
    public string FolderPath { get; init; } = string.Empty;
    public Bitmap? Logo { get; init; }
    public bool HasGamelist { get; init; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private int _gameCount = -1; // -1 = not yet counted

    public string GameCountText => GameCount switch
    {
        -1 => string.Empty,
        0  => "empty",
        1  => "1 game",
        _  => $"{GameCount} games"
    };

    partial void OnGameCountChanged(int value) => OnPropertyChanged(nameof(GameCountText));
}

public partial class GamelistPickerViewModel : ObservableObject
{
    // When true: show ALL recognised systems (new gamelist mode).
    // When false: show only systems that already have a gamelist (open mode).
    private readonly bool _showAllSystems;
    private readonly List<SystemPickerItem> _allSystems;
    private CancellationTokenSource? _countCts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredSystems))]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private SystemPickerItem? _selectedSystem;

    public ObservableCollection<SystemPickerItem> FilteredSystems { get; } = [];

    public string ConfirmButtonText { get; }
    public string SubtitleText { get; }
    public bool ShowGamelistBadge { get; }

    public GamelistPickerViewModel(IEnumerable<SystemPickerItem> candidates, bool showAllSystems)
    {
        _showAllSystems = showAllSystems;

        var all = candidates.OrderBy(s => s.Name);
        _allSystems = (showAllSystems ? all : all.Where(s => s.HasGamelist)).ToList();

        ConfirmButtonText = showAllSystems ? "Create Gamelist" : "Open Gamelist";
        SubtitleText = showAllSystems
            ? "Choose a system to create a new gamelist for."
            : "Choose a system to open.";
        ShowGamelistBadge = showAllSystems;

        RefreshFilter();
    }

    partial void OnSearchTextChanged(string value) => RefreshFilter();

    private void RefreshFilter()
    {
        FilteredSystems.Clear();

        var search = SearchText.Trim();
        var matches = string.IsNullOrEmpty(search)
            ? _allSystems
            : _allSystems.Where(s => s.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

        foreach (var item in matches)
            FilteredSystems.Add(item);

        if (SelectedSystem != null && !FilteredSystems.Contains(SelectedSystem))
            SelectedSystem = null;
    }

    [RelayCommand]
    private void SelectSystem(SystemPickerItem? item)
    {
        if (item == null) return;

        if (SelectedSystem != null)
            SelectedSystem.IsSelected = false;

        SelectedSystem = item;
        item.IsSelected = true;
    }

    // Called on double-click — select and confirm in one action
    [RelayCommand]
    private void ConfirmSystem(SystemPickerItem? item)
    {
        if (item == null) return;
        SelectSystem(item);
        Confirm();
    }

    public event EventHandler? ConfirmRequested;
    public event EventHandler? CancelRequested;

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm() => ConfirmRequested?.Invoke(this, EventArgs.Empty);

    private bool CanConfirm() => SelectedSystem != null;

    [RelayCommand]
    private void Cancel() => CancelRequested?.Invoke(this, EventArgs.Empty);

    public void StartCountingGames()
    {
        _countCts?.Cancel();
        _countCts = new CancellationTokenSource();
        var token = _countCts.Token;

        var itemsToCount = _allSystems.Where(s => s.HasGamelist).ToList();

        Task.Run(async () =>
        {
            foreach (var item in itemsToCount)
            {
                if (token.IsCancellationRequested) break;

                var gamelistPath = Path.Combine(item.FolderPath, "gamelist.xml");
                var count = CountGamesInGamelist(gamelistPath);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!token.IsCancellationRequested)
                        item.GameCount = count;
                });

                await Task.Delay(10, token).ContinueWith(_ => { });
            }
        }, token);
    }

    public void CancelCounting()
    {
        _countCts?.Cancel();
        _countCts = null;
    }

    private static int CountGamesInGamelist(string path)
    {
        if (!File.Exists(path)) return 0;
        try
        {
            var count = 0;
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
            using var reader = XmlReader.Create(path, settings);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element &&
                    string.Equals(reader.Name, "game", StringComparison.OrdinalIgnoreCase))
                    count++;
            }
            return count;
        }
        catch { return 0; }
    }
}
