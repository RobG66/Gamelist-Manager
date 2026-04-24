using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    #region Services
    private readonly SharedDataService _sharedData = SharedDataService.Instance;
    private readonly SettingsService _settingsService = SettingsService.Instance;
    private readonly IWindowService _windowService = WindowService.Instance;
    #endregion

    #region Private Fields
    private readonly MediaPreviewViewModel _mediaPreviewViewModel = new();
    private readonly SourceCache<GameMetadataRow, string> _sourceCache;
    private readonly IDisposable _filterSubscription;
    private readonly ReadOnlyObservableCollection<GameMetadataRow> _games;
    private readonly BehaviorSubject<Func<GameMetadataRow, bool>> _filterSubject;
    private DispatcherTimer? _selectionDebounceTimer;
    private IList? _selectedGames;
    private bool _isLoadingData;
    #endregion

    #region Observable Properties
    [ObservableProperty] private bool _isPaneOpen = true;
    [ObservableProperty] private bool _isSaveEnabled;
    [ObservableProperty] private bool _isGamelistLoaded;
    [ObservableProperty] private bool _isAlwaysOnTop;
    [ObservableProperty] private bool _hasGameSelected;
    [ObservableProperty] private bool _showSaveConfirmation;
    [ObservableProperty] private bool _isSystemsComboBoxEnabled;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private ObservableCollection<SystemItem> _systems = [];
    [ObservableProperty] private SystemItem? _selectedSystem;
    [ObservableProperty] private ViewModelBase? _currentBottomPanel;
    [ObservableProperty] private GameMetadataRow? _firstSelectedGame;
    #endregion

    #region Public Properties
    public bool IsDeleteEnabled => _sharedData.EnableDelete;
    public ReadOnlyObservableCollection<GameMetadataRow> Games => _games;
    public bool IsScraping => _sharedData.IsScraping;
    public bool IsBusy => _sharedData.IsBusy;
    public bool IsRemoteVisible => !string.IsNullOrWhiteSpace(_sharedData.Hostname);
    public bool IsNewGamelistEnabled =>
        !string.IsNullOrWhiteSpace(_sharedData.RomsFolder) && Directory.Exists(_sharedData.RomsFolder);
    public bool IsEditModeEnabled
    {
        get => _sharedData.EnableEdit;
        set
        {
            _sharedData.EnableEdit = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEditingAllowed));
        }
    }

    public IList? SelectedGames
    {
        get => _selectedGames;
        set
        {
            _selectedGames = value;
            _sharedData.SelectedItems = value;
            OnPropertyChanged();
            TriggerDebouncedSelectionChanged();
        }
    }
    #endregion

    #region Property Change Callbacks
    private void OnSharedDataPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SharedDataService.Hostname):
                OnPropertyChanged(nameof(IsRemoteVisible));
                break;
            case nameof(SharedDataService.IsScraping):
                OnPropertyChanged(nameof(IsScraping));
                OnPropertyChanged(nameof(IsGridSelectionLocked));
                OnPropertyChanged(nameof(IsStatsCardEnabled));
                OnPropertyChanged(nameof(IsEditingAllowed));
                OnPropertyChanged(nameof(IsEditToggleEnabled));
                OnPropertyChanged(nameof(IsMenuEnabled));
                break;
            case nameof(SharedDataService.IsBusy):
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(IsEditToggleEnabled));
                OnPropertyChanged(nameof(IsMenuEnabled));
                break;
            case nameof(SharedDataService.RomsFolder):
                _ = LoadSystemsAsync();
                OnPropertyChanged(nameof(IsNewGamelistEnabled));
                break;
            case nameof(SharedDataService.EsDeRoot):
                _ = LoadSystemsAsync();
                OnPropertyChanged(nameof(IsNewGamelistEnabled));
                break;
            case nameof(SharedDataService.EnableDelete):
                OnPropertyChanged(nameof(IsDeleteEnabled));
                break;
            case nameof(SharedDataService.EnableEdit):
                OnPropertyChanged(nameof(IsEditModeEnabled));
                OnPropertyChanged(nameof(IsEditingAllowed));
                break;
            case nameof(SharedDataService.RememberColumns):
                OnPropertyChanged(nameof(RememberColumns));
                break;
        }
    }

    partial void OnIsGamelistLoadedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsStatsCardEnabled));
        OnPropertyChanged(nameof(IsEditToggleEnabled));
    }

    private void OnSettingsApplied(object? sender, EventArgs e)
    {
        UpdateScaledLayoutWidths();
        _ = LoadSystemsAsync();
    }
    #endregion

    #region Constructor
    public MainWindowViewModel()
    {
        _sourceCache = new SourceCache<GameMetadataRow, string>(game => game.Path);
        _filterSubject = new BehaviorSubject<Func<GameMetadataRow, bool>>(BuildFilterPredicate());

        _filterSubscription = _sourceCache
            .Connect()
            .Filter(_filterSubject)
            .Bind(out _games)
            .Subscribe(_ => OnGamesCollectionChanged());

        _sharedData.FilteredGamelistData = _games;

        _sharedData.PropertyChanged += OnSharedDataPropertyChanged;
        _sharedData.SettingsApplied += OnSettingsApplied;

        LoadColumnSettings();
        _sharedData.RecentFiles.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasRecentFiles));
        LoadRecentFilesFromSettings();
        OnPropertyChanged(nameof(HasRecentFiles));
        RefreshProfiles();
        _ = LoadSystemsAsync();

        _selectedFindColumn = SearchableColumns.FirstOrDefault();
        _selectedReplaceColumn = SearchableColumns.FirstOrDefault();

        NavPaneWidth = GetScaledNavPaneWidth();

        _ = MediaPreviewViewModel.PreloadLibVLCAsync().ContinueWith(
            _ => OnPropertyChanged(nameof(IsLibVLCMissing)),
            TaskScheduler.FromCurrentSynchronizationContext());

        InitializeStatisticsPipeline();
        InitializeFilterDebounce();
    }
    #endregion

    #region Collection & Selection Handlers
    private void OnGamesCollectionChanged()
    {
        if (_isLoadingData) return;
        CalculateStatistics();

        if (!HasGameSelected && _games.Count > 0)
            RequestSelectFirstItem?.Invoke(this, EventArgs.Empty);
    }

    private void GameItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_isLoadingData) return;
        _sharedData.IsDataChanged = true;
        IsSaveEnabled = true;

        if (sender is GameMetadataRow changedGame)
            _sourceCache.Refresh(changedGame);
    }

    private void TriggerDebouncedSelectionChanged()
    {
        _selectionDebounceTimer?.Stop();

        if (_selectionDebounceTimer == null)
        {
            _selectionDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _selectionDebounceTimer.Tick += (_, _) =>
            {
                _selectionDebounceTimer?.Stop();
                OnRowSelectionChanged();
            };
        }

        _selectionDebounceTimer.Start();
    }

    private void OnRowSelectionChanged()
    {
        if (SelectedGames == null || SelectedGames.Count == 0)
        {
            HasGameSelected = false;
            FirstSelectedGame = null;

            if (IsMediaPreviewVisible)
                _mediaPreviewViewModel.SelectedGame = null;
        }
        else
        {
            HasGameSelected = true;
            FirstSelectedGame = SelectedGames.OfType<GameMetadataRow>().FirstOrDefault();

            if (IsMediaPreviewVisible)
                _mediaPreviewViewModel.SelectedGame = FirstSelectedGame;
        }

        OnPropertyChanged(nameof(GenreFilterMenuText));
        OnPropertyChanged(nameof(IsGenreFilterEnabled));
        OnPropertyChanged(nameof(SetSelectedGenreVisibleMenuText));
        OnPropertyChanged(nameof(SetSelectedGenreHiddenMenuText));
    }
    #endregion

    #region Commands
    [RelayCommand]
    public void TriggerPane() => IsPaneOpen = !IsPaneOpen;

    [RelayCommand]
    private void ToggleEditMode() => IsEditModeEnabled = !IsEditModeEnabled;

    [RelayCommand]
    private void ToggleAlwaysOnTop() => IsAlwaysOnTop = !IsAlwaysOnTop;

    [RelayCommand]
    private void ResetView()
    {
        ClearFilters();
        IsAlwaysOnTop = false;
        ResetColumnVisibility();
        _sourceCache.Refresh();
    }

    [RelayCommand]
    private Task OpenSettingsAsync() => _windowService.ShowSettingsAsync();

    [RelayCommand]
    private Task OpenAboutAsync() => _windowService.ShowAboutAsync();

    [RelayCommand]
    private void ReportIssue()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/RobG66/Gamelist-Manager-Avalonia/issues",
                UseShellExecute = true
            });
        }
        catch { }
    }
    #endregion

    #region Private Methods

    internal void UnloadGamelist()
    {
        _isLoadingData = true;
        _sourceCache.Clear();
        _isLoadingData = false;

        _sharedData.Clear();

        ClearFilters();
        ClearReportColumns();

        IsGamelistLoaded = false;
        IsEditModeEnabled = false;
        IsSaveEnabled = false;
        HasGameSelected = false;
        FirstSelectedGame = null;
        FileStatusText = "No file loaded";
        LastModifiedText = string.Empty;

        ApplyStatistics(new StatisticsSnapshot());

        if (IsMediaPreviewVisible)
            ToggleMediaPreview();
        if (IsScraperVisible)
            ToggleScraper();
    }
    #endregion
}