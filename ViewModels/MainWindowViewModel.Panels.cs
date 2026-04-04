using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;

namespace Gamelist_Manager.ViewModels;

public partial class MainWindowViewModel
{
    #region Observable Properties
    [ObservableProperty] private bool _isMediaPreviewVisible;
    [ObservableProperty] private bool _isScraperVisible;
    [ObservableProperty] private bool _isDatToolVisible;
    [ObservableProperty] private double _navPaneWidth = 155;
    [ObservableProperty] private ScraperViewModel? _scraperPanelViewModel;
    [ObservableProperty] private DatToolViewModel? _datToolPanelViewModel;
    [ObservableProperty] private bool _hasReportColumns;
    #endregion

    #region Report Events
    public event EventHandler? ReportColumnsCleared;
    public event EventHandler<FindReportColumnEventArgs>? FindReportColumnAdded;
    #endregion

    #region Public Properties
    public Bitmap? SystemLogo => SelectedSystem?.Logo ?? _defaultLogo.Value;
    public bool IsLibVLCMissing => !MediaPreviewViewModel.IsLibVLCInstalled;
    public bool IsBottomPanelVisible => IsMediaPreviewVisible || IsScraperVisible || IsDatToolVisible;
    public bool IsGridSelectionLocked => IsMediaPreviewVisible && IsScraping;
    public bool IsStatsCardEnabled => IsGamelistLoaded && !IsScraping;
    public bool IsEditingAllowed => IsEditModeEnabled && !IsScraping && !IsBusy;
    public bool IsEditToggleEnabled => IsGamelistLoaded && !IsScraping && !IsBusy;
    public bool IsMenuEnabled => !IsScraping && !IsBusy;
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
    partial void OnIsScraperVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        OnPropertyChanged(nameof(BottomSplitterHeight));
        OnPropertyChanged(nameof(BottomPanelHeight));
        OnPropertyChanged(nameof(IsGridSelectionLocked));
    }

    partial void OnIsMediaPreviewVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        OnPropertyChanged(nameof(BottomSplitterHeight));
        OnPropertyChanged(nameof(BottomPanelHeight));
        OnPropertyChanged(nameof(IsGridSelectionLocked));
    }

    partial void OnIsDatToolVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        OnPropertyChanged(nameof(BottomSplitterHeight));
        OnPropertyChanged(nameof(BottomPanelHeight));
        OnPropertyChanged(nameof(IsGridSelectionLocked));
    }

    partial void OnSelectedSystemChanged(SystemItem? value)
    {
        OnPropertyChanged(nameof(SystemLogo));
    }
    #endregion

    #region Commands
    [RelayCommand]
    private void ClearReportColumns()
    {
        HasReportColumns = false;
        DatToolPanelViewModel?.ClearReportCommand.Execute(null);
        ReportColumnsCleared?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void ToggleMediaPreview()
    {
        if (!IsMediaPreviewVisible)
        {
            if (IsScraperVisible)
                ToggleScraper();

            if (IsDatToolVisible)
                CloseDatTool();

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
            if (IsMediaPreviewVisible)
            {
                IsMediaPreviewVisible = false;
                _mediaPreviewViewModel.SuspendVideo();
            }

            if (IsDatToolVisible)
                CloseDatTool();

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
    /// <summary>
    /// Toggles the DatTool bottom panel. Opening it closes any other active panel
    /// (media preview or scraper) first. Creates a fresh <see cref="DatToolViewModel"/>
    /// each time so report state does not persist across sessions.
    /// </summary>
    [RelayCommand]
    public void ToggleDatTool()
    {
        if (!IsDatToolVisible)
        {
            if (IsMediaPreviewVisible)
            {
                IsMediaPreviewVisible = false;
                _mediaPreviewViewModel.SuspendVideo();
            }

            if (IsScraperVisible)
                CloseScraper();

            DatToolPanelViewModel?.Dispose();
            DatToolPanelViewModel = new DatToolViewModel();
            DatToolPanelViewModel.CloseRequested += OnDatToolCloseRequested;
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

    public void UpdateScaledLayoutWidths() => NavPaneWidth = GetScaledNavPaneWidth();

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
    private double GetScaledNavPaneWidth() => ScaleToFontSize(155);

    private double ScaleToFontSize(double baseValue)
    {
        const double baseFontSize = 12;
        return Math.Round(baseValue * (_sharedData.AppFontSize / baseFontSize));
    }

    private void CloseScraper()
    {
        IsScraperVisible = false;
        CurrentBottomPanel = null;
        ScraperPanelViewModel?.Dispose();
        ScraperPanelViewModel = null;
    }

    /// <summary>
    /// Tears down the DatTool panel: hides it, unsubscribes events, and disposes the ViewModel.
    /// Report columns are not removed here — that is managed by ClearReportColumnsCommand.
    /// </summary>
    private void CloseDatTool()
    {
        IsDatToolVisible = false;
        CurrentBottomPanel = null;
        if (DatToolPanelViewModel != null)
        {
            DatToolPanelViewModel.CloseRequested -= OnDatToolCloseRequested;
            DatToolPanelViewModel.Dispose();
            DatToolPanelViewModel = null;
        }
    }

    private void OnDatToolCloseRequested(object? sender, EventArgs e) => CloseDatTool();

    private void RaiseFindReportColumn(string columnName, HashSet<string> pathSet)
    {
        HasReportColumns = true;
        FindReportColumnAdded?.Invoke(this, new FindReportColumnEventArgs(columnName, pathSet));
    }
    #endregion
}