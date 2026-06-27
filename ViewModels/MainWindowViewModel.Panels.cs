using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gamelist_Manager.ViewModels;

public partial class MainWindowViewModel
{
    #region Observable Properties
    [ObservableProperty] private bool _isMediaPreviewVisible;
    [ObservableProperty] private bool _isScraperVisible;
    [ObservableProperty] private bool _isDatToolVisible;
    [ObservableProperty] private double _menuWidth = 155;
    private const double BaseMenuWidth = 155;
    [ObservableProperty] private ScraperViewModel? _scraperPanelViewModel;
    [ObservableProperty] private DatToolViewModel? _datToolPanelViewModel;
    [ObservableProperty] private bool _hasReportColumns;
    #endregion

    #region Report Events
    public event EventHandler? ReportColumnsCleared;
    public event EventHandler<FindReportColumnEventArgs>? FindReportColumnAdded;
    #endregion

    #region Public Properties
    [ObservableProperty] private Bitmap? _systemLogo = _defaultLogo.Value;
    public bool IsLibVLCMissing => !Gamelist_Manager.Services.LibVLCService.IsLibVLCInstalled;
    public bool IsBottomPanelVisible => IsMediaPreviewVisible || IsScraperVisible || IsDatToolVisible;
    public bool IsGridSelectionLocked => IsMediaPreviewVisible && _sessionState.IsScraping;
    public bool IsStatsCardEnabled => IsGamelistLoaded && !_sessionState.IsScraping;
    public bool IsEditingAllowed => IsEditModeEnabled && !_sessionState.IsScraping && !_sessionState.IsBusy;
    public bool IsEditToggleEnabled => IsGamelistLoaded && !_sessionState.IsScraping && !_sessionState.IsBusy;
    public bool IsMenuEnabled => !_sessionState.IsScraping && !_sessionState.IsBusy;
    public bool IsPersistentSelectionToggleEnabled => IsGamelistLoaded && !IsEditModeEnabled && !_sessionState.IsScraping && !_sessionState.IsBusy;
    public GridLength BottomSplitterHeight => IsMediaPreviewVisible ? new GridLength(5) : new GridLength(0);
    public GridLength BottomPanelHeight =>
        IsScraperVisible ? GridLength.Auto :
        IsDatToolVisible ? GridLength.Auto :
        IsMediaPreviewVisible ? new GridLength(250) : new GridLength(0);
    #endregion

    #region Private Fields
    private static readonly Lazy<Bitmap?> _defaultLogo = new(() =>
    {
        try
        {
            var uri = new Uri("avares://Gamelist_Manager/Assets/Logos/gamelistmanager.png");
            using var stream = AssetLoader.Open(uri);
            return new Bitmap(stream);
        }
        catch { return null; }
    });
    #endregion

    #region Property Change Callbacks
    partial void OnIsScraperVisibleChanged(bool value) => RaiseBottomPanelLayoutChanged();
    partial void OnIsMediaPreviewVisibleChanged(bool value) => RaiseBottomPanelLayoutChanged();
    partial void OnIsDatToolVisibleChanged(bool value) => RaiseBottomPanelLayoutChanged();

    partial void OnSelectedSystemChanged(SystemItem? value)
    {
        if (SystemLogo != _defaultLogo.Value)
        {
            SystemLogo?.Dispose();
        }

        SystemLogo = value != null
            ? TryLoadSystemLogoFullSize(value.Name) ?? _defaultLogo.Value
            : _defaultLogo.Value;
    }
    #endregion

    #region Commands
    [RelayCommand]
    private void ClearReportColumns()
    {
        HasReportColumns = false;
        ReportColumnsCleared?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void ToggleMediaPreview()
    {
        if (!IsMediaPreviewVisible)
        {
            CloseOtherPanels();

            _mediaPreviewViewModel.SelectedGame =
                SelectedGames?.OfType<GameMetadataRow>().FirstOrDefault();

            CurrentBottomPanel = _mediaPreviewViewModel;
            IsMediaPreviewVisible = true;
        }
        else
        {
            IsMediaPreviewVisible = false;
            _mediaPreviewViewModel.SuspendVideo();
            CurrentBottomPanel = null;
        }
    }

    [RelayCommand]
    public void ToggleScraper()
    {
        if (!IsScraperVisible)
        {
            CloseOtherPanels();

            ScraperPanelViewModel?.Dispose();
            ScraperPanelViewModel = new ScraperViewModel();
            CurrentBottomPanel = ScraperPanelViewModel;
            IsScraperVisible = true;
        }
        else
        {
            CloseScraper();
        }
    }

    [RelayCommand]
    public void ToggleDatTool()
    {
        if (!IsDatToolVisible)
        {
            CloseOtherPanels();

            DatToolPanelViewModel?.Dispose();
            DatToolPanelViewModel = new DatToolViewModel();
            DatToolPanelViewModel.CloseRequested += OnDatToolCloseRequested;
            DatToolPanelViewModel.ReportColumnAdded += OnDatToolReportColumnAdded;
            CurrentBottomPanel = DatToolPanelViewModel;
            IsDatToolVisible = true;
        }
        else
        {
            CloseDatTool();
        }
    }
    #endregion

    #region Public Methods
    public void DisposeMediaPreview() => _mediaPreviewViewModel.Dispose();

    public void UpdateScaledLayoutWidths() => MenuWidth = GetScaledMenuWidth();

    public void RefreshScraper()
    {
        if (!IsScraperVisible) return;

        CloseScraper();
        ScraperPanelViewModel = new ScraperViewModel();
        CurrentBottomPanel = ScraperPanelViewModel;
        IsScraperVisible = true;
    }
    #endregion

    #region Private Methods
    private double GetScaledMenuWidth() => ScaleToFontSize(BaseMenuWidth);

    private double ScaleToFontSize(double baseValue)
    {
        const double baseFontSize = 12;
        return Math.Round(baseValue * (_settingsState.AppFontSize / baseFontSize));
    }

    private void RaiseBottomPanelLayoutChanged()
    {
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        OnPropertyChanged(nameof(BottomSplitterHeight));
        OnPropertyChanged(nameof(BottomPanelHeight));
        OnPropertyChanged(nameof(IsGridSelectionLocked));
    }

    private void CloseOtherPanels()
    {
        if (IsMediaPreviewVisible)
        {
            IsMediaPreviewVisible = false;
            _mediaPreviewViewModel.SuspendVideo();
        }

        if (IsScraperVisible)
            CloseScraper();

        if (IsDatToolVisible)
            CloseDatTool();
    }

    private void CloseScraper()
    {
        IsScraperVisible = false;
        CurrentBottomPanel = null;
        ScraperPanelViewModel?.Dispose();
        ScraperPanelViewModel = null;
    }

    private void CloseDatTool()
    {
        IsDatToolVisible = false;
        CurrentBottomPanel = null;
        if (DatToolPanelViewModel != null)
        {
            DatToolPanelViewModel.CloseRequested -= OnDatToolCloseRequested;
            DatToolPanelViewModel.ReportColumnAdded -= OnDatToolReportColumnAdded;
            DatToolPanelViewModel.Dispose();
            DatToolPanelViewModel = null;
        }
    }

    private void OnDatToolCloseRequested(object? sender, EventArgs e) => CloseDatTool();

    private void OnDatToolReportColumnAdded(object? sender, string _) => HasReportColumns = true;

    private void RaiseFindReportColumn(string columnName, HashSet<string> pathSet)
    {
        HasReportColumns = true;
        FindReportColumnAdded?.Invoke(this, new FindReportColumnEventArgs(columnName, pathSet));
    }
    #endregion
}