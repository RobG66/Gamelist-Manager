using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Messages;
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

    private readonly SessionState _sessionState = SessionState.Instance;
    private readonly SettingsState _settingsState = SettingsState.Instance;
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
    private bool _isSaveEnabled;

    #endregion

    #region Observable Properties

    [ObservableProperty] private bool _useMameInternalNames;
    [ObservableProperty] private bool _isMenuOpen = true;
    [ObservableProperty] private bool _isGamelistLoaded;
    [ObservableProperty] private bool _isAlwaysOnTop;
    [ObservableProperty] private bool _hasGameSelected;
    [ObservableProperty] private bool _showSaveConfirmation;
    [ObservableProperty] private bool _isSystemsComboBoxEnabled;
    [ObservableProperty] private bool _isPersistentSelectionEnabled;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private ObservableCollection<SystemItem> _systems = [];
    [ObservableProperty] private SystemItem? _selectedSystem;
    [ObservableProperty] private ViewModelBase? _currentBottomPanel;
    [ObservableProperty] private GameMetadataRow? _firstSelectedGame;


    #endregion

    #region Public Properties

    public bool IsDeleteEnabled => _settingsState.EnableDelete;
    public bool IsBusy => _sessionState.IsBusy;

    public bool IsSaveEnabled
    {
        get => _isSaveEnabled && !_sessionState.IsBusy && !_sessionState.IsScraping;
        set
        {
            if (_isSaveEnabled == value) return;
            _isSaveEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool ClearMediaPathsButtonEnabled =>
    _sessionState.ProfileType != SettingKeys.ProfileTypeEsDe && HasGameSelected;

    public ReadOnlyObservableCollection<GameMetadataRow> Games => _games;
    public bool IsRemoteVisible => !string.IsNullOrWhiteSpace(_settingsState.Hostname);

    public bool IsNewGamelistEnabled =>
    !string.IsNullOrWhiteSpace(_settingsState.RomsFolder) && Directory.Exists(_settingsState.RomsFolder);

    private bool CanUseMameInternalNames =>
        string.Equals(_sessionState.CurrentSystem, "mame", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(_settingsState.MamePath) &&
        File.Exists(_settingsState.MamePath);

    public bool IsMameInternalNamesOptionVisible =>
        IsGamelistLoaded && CanUseMameInternalNames;

    public bool IsEditModeEnabled
    {
        get => _sessionState.EnableEdit;
        set
        {
            _sessionState.EnableEdit = value;
            if (value)
                IsPersistentSelectionEnabled = false;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEditingAllowed));
            OnPropertyChanged(nameof(IsPersistentSelectionToggleEnabled));
            OnPropertyChanged(nameof(PersistentSelectionMenuHeader));
        }
    }

    public string PersistentSelectionMenuHeader =>
        IsPersistentSelectionEnabled ? "Disable Persistent Selection" : "Enable Persistent Selection";

    public IList? SelectedGames
    {
        get => _selectedGames;
        set
        {
            _selectedGames = value;
            _sessionState.SelectedItems = value;
            OnPropertyChanged();
            TriggerDebouncedSelectionChanged();
        }
    }
        
    #endregion

    #region Property Change Callbacks

    private void OnSessionStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SessionState.IsScraping):
                OnPropertyChanged(nameof(IsSaveEnabled));
                OnPropertyChanged(nameof(IsGridSelectionLocked));
                OnPropertyChanged(nameof(IsStatsCardEnabled));
                OnPropertyChanged(nameof(IsEditingAllowed));
                OnPropertyChanged(nameof(IsEditToggleEnabled));
                OnPropertyChanged(nameof(IsMenuEnabled));
                OnPropertyChanged(nameof(IsPersistentSelectionToggleEnabled));
                break;
            case nameof(SessionState.IsBusy):
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(IsSaveEnabled));
                OnPropertyChanged(nameof(IsEditToggleEnabled));
                OnPropertyChanged(nameof(IsMenuEnabled));
                OnPropertyChanged(nameof(IsPersistentSelectionToggleEnabled));
                break;
            case nameof(SessionState.EnableEdit):
                OnPropertyChanged(nameof(IsEditModeEnabled));
                OnPropertyChanged(nameof(IsEditingAllowed));
                break;
            case nameof(SessionState.ProfileType):
                OnPropertyChanged(nameof(ClearMediaPathsButtonEnabled));
                _ = LoadSystemsAsync();
                break;
            case nameof(SessionState.CurrentSystem):
                OnPropertyChanged(nameof(IsMameInternalNamesOptionVisible));
                if (!IsMameInternalNamesOptionVisible)
                    UseMameInternalNames = false;
                break;
        }
    }

    private void OnSettingsStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SettingsState.Hostname):
                OnPropertyChanged(nameof(IsRemoteVisible));
                break;
            case nameof(SettingsState.RomsFolder):
                OnPropertyChanged(nameof(IsNewGamelistEnabled));
                _ = LoadSystemsAsync();
                break;
            case nameof(SettingsState.EsDeRoot):
                OnPropertyChanged(nameof(IsNewGamelistEnabled));
                _ = LoadSystemsAsync();
                break;
            case nameof(SettingsState.EnableDelete):
                OnPropertyChanged(nameof(IsDeleteEnabled));
                break;
            case nameof(SettingsState.RememberColumns):
                OnPropertyChanged(nameof(RememberColumns));
                break;
            case nameof(SettingsState.MamePath):
                OnPropertyChanged(nameof(IsMameInternalNamesOptionVisible));
                if (!IsMameInternalNamesOptionVisible)
                    UseMameInternalNames = false;
                break;
        }
    }

    partial void OnHasGameSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(ClearMediaPathsButtonEnabled));
    }

    partial void OnIsGamelistLoadedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsStatsCardEnabled));
        OnPropertyChanged(nameof(IsEditToggleEnabled));
        OnPropertyChanged(nameof(IsPersistentSelectionToggleEnabled));
        OnPropertyChanged(nameof(IsMameInternalNamesOptionVisible));
    }

    private void OnSettingsApplied()
    {
        UpdateScaledLayoutWidths();
        _ = LoadSystemsAsync();

        if (IsScraperVisible)
            RefreshScraper();

        // Refresh custom column values as needed
        PopulateMissingMedia();

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

        _sessionState.FilteredGamelistData = _games;

        _sessionState.PropertyChanged += OnSessionStatePropertyChanged;
        _settingsState.PropertyChanged += OnSettingsStatePropertyChanged;

        WeakReferenceMessenger.Default.Register<SettingsAppliedMessage>(this, (_, __) => OnSettingsApplied());
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, GamelistLoadedRequestMessage>(this, (r, m) => m.Reply(r.IsGamelistLoaded));
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, CheckUnsavedGamelistChangesMessage>(this, (r, m) => m.Reply(r.CheckUnsavedChangesAsync()));
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, UnloadGamelistMessage>(this, (r, _) => r.UnloadGamelist());
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, ApplyProfileSwitchMessage>(this, (r, m) => m.Reply(r.ApplyProfileSwitchAsync(m.ProfileName).ContinueWith(_ => true)));
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, ProfilesChangedMessage>(this, (r, _) => r.RefreshProfiles());

        LoadColumnSettings();
        _sessionState.RecentFiles.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasRecentFiles));
        LoadRecentFilesFromSettings();
        OnPropertyChanged(nameof(HasRecentFiles));
        RefreshProfiles();
        _ = LoadSystemsAsync();

        _selectedFindColumn = SearchableColumns.FirstOrDefault();
        _selectedReplaceColumn = SearchableColumns.FirstOrDefault();

        MenuWidth = GetScaledMenuWidth();

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
        _sessionState.IsDataChanged = true;
        IsSaveEnabled = true;

        if (sender is GameMetadataRow changedGame)
            _sourceCache.Refresh(changedGame);
    }

    private void TriggerDebouncedSelectionChanged()
    {
        _selectionDebounceTimer?.Stop();

        if (_selectionDebounceTimer == null)
        {
            _selectionDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
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
    private void ToggleUseMameInternalNames() => UseMameInternalNames = !UseMameInternalNames;

    [RelayCommand]
    public void TriggerMenu() => IsMenuOpen = !IsMenuOpen;

    [RelayCommand]
    private void ToggleEditMode() => IsEditModeEnabled = !IsEditModeEnabled;

    [RelayCommand]
    private void TogglePersistentSelection()
    {
        IsPersistentSelectionEnabled = !IsPersistentSelectionEnabled;
        OnPropertyChanged(nameof(PersistentSelectionMenuHeader));
    }

    [RelayCommand]
    private void ClearPersistentSelection() => SelectedGames?.Clear();

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
                FileName = "https://github.com/RobG66/Gamelist-Manager/issues",
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

        _sessionState.Clear();

        ClearFilters();
        ClearReportColumns();

        IsGamelistLoaded = false;
        IsEditModeEnabled = false;
        IsPersistentSelectionEnabled = false;
        IsSaveEnabled = false;
        HasGameSelected = false;
        FirstSelectedGame = null;
        FileStatusText = "No file loaded";
        LastModifiedText = string.Empty;

        ApplyStatistics(new StatisticsSnapshot());

        if (IsMediaPreviewVisible) ToggleMediaPreview();
        if (IsScraperVisible) ToggleScraper();
    }

    #endregion
}