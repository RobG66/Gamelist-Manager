using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using Gamelist_Manager.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Gamelist_Manager.Views;

public partial class MainWindow : Window
{
    private readonly List<DataGridColumn> _reportColumns = [];
    private readonly SettingsState _settingsState = SettingsState.Instance;
    private readonly SessionState _sessionState = SessionState.Instance;
    private readonly SettingsService _settingService = SettingsService.Instance;

    private (int Left, int Top, int Width, int Height) _restoredBounds;

    public MainWindow()
    {
        InitializeComponent();

        GameDataGrid.ContextMenu!.Opening += ContextMenu_Opening;

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0] ?? string.Empty;

        Title = string.IsNullOrEmpty(version)
            ? "Gamelist Manager"
            : $"Gamelist Manager {version}";

        Services.WindowService.Instance.SetOwner(this);

        _settingsState.PropertyChanged += OnSettingsStatePropertyChanged;

        Loaded += MainWindow_Loaded;
        DataContextChanged += MainWindow_DataContextChanged;
        Closing += MainWindow_Closing;
        GameDataGrid.SelectionChanged += GameDataGrid_SelectionChanged;
        AddHandler(KeyDownEvent, MainWindow_KeyDown, RoutingStrategies.Tunnel);

        BuildDataGridColumns();
        RestoreWindowStateFromSettings();

        PositionChanged += (_, _) => TrackRestoredBounds();
        Resized += (_, _) => TrackRestoredBounds();
    }

    private void ContextMenu_Opening(object? sender, CancelEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        GenreVisibleMenuItem.Header = vm.SetSelectedGenreVisibleMenuText;
        GenreHiddenMenuItem.Header = vm.SetSelectedGenreHiddenMenuText;
    }

    private bool IsGridSelectionLocked =>
        DataContext is MainWindowViewModel { IsGridSelectionLocked: true };

    private void GameDataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
            viewModel.SelectedGames = GameDataGrid.SelectedItems;
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_sessionState.IsBusy)
            e.Handled = true;
        else if (IsGridSelectionLocked && e.Key is Key.Up or Key.Down or Key.PageUp or Key.PageDown or Key.Home or Key.End)
            e.Handled = true;
    }

    private MainWindowViewModel? _currentViewModel;

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        try
        {
            if (DataContext is not MainWindowViewModel viewModel)
                return;

            if (_sessionState.IsScraping)
            {
                e.Cancel = true;
                await new ThreeButtonDialogView(new ThreeButtonDialogConfig
                {
                    Title = "Scraping in Progress",
                    Message = "Scraping is currently in progress.",
                    DetailMessage = "Please stop the scraper before closing the application.",
                    IconTheme = DialogIconTheme.Warning,
                    Button1Text = "",
                    Button2Text = "",
                    Button3Text = "OK"
                }).ShowDialog<ThreeButtonResult>(this);
                return;
            }

            if (_sessionState.IsBusy)
            {
                e.Cancel = true;
                await new ThreeButtonDialogView(new ThreeButtonDialogConfig
                {
                    Title = "Save in Progress",
                    Message = "The gamelist is currently being saved.",
                    DetailMessage = "Please wait until the save has completed before closing.",
                    IconTheme = DialogIconTheme.Warning,
                    Button1Text = "",
                    Button2Text = "",
                    Button3Text = "OK"
                }).ShowDialog<ThreeButtonResult>(this);
                return;
            }

            if (_sessionState.IsDataChanged && _settingsState.EnableSaveReminder)
            {
                e.Cancel = true;

                var result = await new ThreeButtonDialogView(new ThreeButtonDialogConfig
                {
                    Title = "Unsaved Changes",
                    Message = "You have unsaved changes to the current gamelist.",
                    DetailMessage = "Do you want to save your changes before exiting?",
                    IconTheme = DialogIconTheme.Warning,
                    Button1Text = "Cancel",
                    Button2Text = "Don't Save",
                    Button3Text = "Save"
                }).ShowDialog<ThreeButtonResult>(this);

                if (result == ThreeButtonResult.Button1) return;
                if (result == ThreeButtonResult.Button3)
                    await viewModel.SaveGamelistCommand.ExecuteAsync(null);
            }

            _settingsState.PropertyChanged -= OnSettingsStatePropertyChanged;
            viewModel.DisposeMediaPreview();
            viewModel.ScraperPanelViewModel?.Dispose();

            SaveWindowStateToSettings();
            Closing -= MainWindow_Closing;
            Close();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during window closing: {ex.Message}");
            Closing -= MainWindow_Closing;
            Close();
        }
    }

    private void MainWindow_DataContextChanged(object? sender, EventArgs e)
    {
        if (_currentViewModel != null)
        {
            _currentViewModel.RequestSelectFirstItem -= ViewModel_RequestSelectFirstItem;
            _currentViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _currentViewModel.ColumnVisibilityChanged -= ApplyColumnVisibility;
            _currentViewModel.ReportColumnsCleared -= OnReportColumnsCleared;
            _currentViewModel.FindReportColumnAdded -= OnFindReportColumnAdded;
            _currentViewModel.RequestNavigateToItem -= ViewModel_RequestNavigateToItem;
            _currentViewModel.RequestClearSelection -= ViewModel_RequestClearSelection;
            _currentViewModel.RequestRestoreSelection -= ViewModel_RequestRestoreSelection;
            _currentViewModel.GetFindRows = null;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            _currentViewModel = viewModel;
            viewModel.RequestSelectFirstItem += ViewModel_RequestSelectFirstItem;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.ColumnVisibilityChanged += ApplyColumnVisibility;
            viewModel.ReportColumnsCleared += OnReportColumnsCleared;
            viewModel.FindReportColumnAdded += OnFindReportColumnAdded;
            viewModel.RequestNavigateToItem += ViewModel_RequestNavigateToItem;
            viewModel.RequestClearSelection += ViewModel_RequestClearSelection;
            viewModel.RequestRestoreSelection += ViewModel_RequestRestoreSelection;

            viewModel.GetFindRows = () =>
                GameDataGrid.CollectionView?.OfType<GameMetadataRow>() ?? viewModel.Games;

            Topmost = viewModel.IsAlwaysOnTop;
            ApplySizeToFitToDataGrid(viewModel.SizeToFit);
            ApplyColumnVisibility();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MainWindowViewModel viewModel) return;

        switch (e.PropertyName)
        {
            case nameof(MainWindowViewModel.IsAlwaysOnTop):
                Topmost = viewModel.IsAlwaysOnTop;
                break;
            case nameof(MainWindowViewModel.SizeToFit):
                ApplySizeToFitToDataGrid(viewModel.SizeToFit);
                break;
            case nameof(MainWindowViewModel.MediaPathsVisible):
                ApplyColumnVisibility();
                break;
            case nameof(MainWindowViewModel.IsGamelistLoaded):
                BuildDataGridColumns();
                ApplySizeToFitToDataGrid(viewModel.SizeToFit);
                ApplyColumnVisibility();
                break;
            case nameof(MainWindowViewModel.DatToolPanelViewModel):
                if (viewModel.DatToolPanelViewModel is { } datTool)
                {
                    datTool.ReportColumnAdded += OnDatReportColumnAdded;
                    datTool.PanelDisposing += OnDatToolPanelDisposing;
                }
                break;
        }
    }

    private void OnSettingsStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SettingsState.AppFontSize):
            case nameof(SettingsState.GridFontSize):
            case nameof(SettingsState.AlternatingRowColorIndex):
            case nameof(SettingsState.GridLinesVisibilityIndex):
                ThemeService.ApplyFontSizes(_settingsState.AppFontSize, _settingsState.GridFontSize);
                ThemeService.ApplyDataGridAppearance(GameDataGrid, _settingsState.AlternatingRowColorIndex, _settingsState.GridLinesVisibilityIndex);
                ThemeService.ApplyDataGridColumnWidths(GameDataGrid, _settingsState.GridFontSize);
                break;
        }
    }

    private async void MainWindow_Loaded(object? sender, EventArgs e)
    {
        ThemeService.ApplyDataGridAppearance(GameDataGrid, _settingsState.AlternatingRowColorIndex, _settingsState.GridLinesVisibilityIndex);
        ThemeService.ApplyDataGridColumnWidths(GameDataGrid, _settingsState.GridFontSize);

        if (DataContext is MainWindowViewModel vm)
        {
            vm.UpdateSearchableColumns(GetVisibleColumnNames());
            await vm.ResolveMissingProfileAsync();
        }

        foreach (var column in GameDataGrid.Columns)
            column.PropertyChanged += DataGridColumn_PropertyChanged;
    }

    private void TrackRestoredBounds()
    {
        if (WindowState == WindowState.Normal)
            _restoredBounds = (Position.X, Position.Y, (int)Width, (int)Height);
    }

    private void SaveWindowStateToSettings()
    {
        if (!_settingsState.SaveWindowState) return;

        // When maximized, persist the pre-maximized bounds so restore works correctly.
        var (left, top, width, height) = WindowState == WindowState.Normal
            ? (Position.X, Position.Y, (int)Width, (int)Height)
            : _restoredBounds;

        var settings = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>
        {
            [SettingKeys.WindowStateSection] = new()
            {
                [SettingKeys.WindowStateValue.Key] = WindowState.ToString(),
                [SettingKeys.WindowLeft.Key] = left.ToString(),
                [SettingKeys.WindowTop.Key] = top.ToString(),
                [SettingKeys.WindowWidth.Key] = width.ToString(),
                [SettingKeys.WindowHeight.Key] = height.ToString(),
            }
        };
        ProfileService.Instance.Save(settings);
    }

    private void RestoreWindowStateFromSettings()
    {
        if (!_settingsState.SaveWindowState) return;
                
        var left = _settingService.GetInt(SettingKeys.WindowStateSection, SettingKeys.WindowLeft.Key, SettingKeys.WindowLeft.Default);
        var top = _settingService.GetInt(SettingKeys.WindowStateSection, SettingKeys.WindowTop.Key, SettingKeys.WindowTop.Default);
        var width = _settingService.GetInt(SettingKeys.WindowStateSection, SettingKeys.WindowWidth.Key, SettingKeys.WindowWidth.Default);
        var height = _settingService.GetInt(SettingKeys.WindowStateSection, SettingKeys.WindowHeight.Key, SettingKeys.WindowHeight.Default);
        var state = _settingService.GetValue(SettingKeys.WindowStateSection, SettingKeys.WindowStateValue.Key, SettingKeys.WindowStateValue.Default);

        if (width > 0 && height > 0)
        {
            Width = width;
            Height = height;
        }

        if (left >= 0 && top >= 0)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Position = new Avalonia.PixelPoint(left, top);
        }

        if (System.Enum.TryParse<WindowState>(state, out var windowState))
            WindowState = windowState;
    }

    private void ViewModel_RequestSelectFirstItem(object? sender, EventArgs e)
    {
        if (GameDataGrid.ItemsSource is System.Collections.ICollection { Count: > 0 })
            GameDataGrid.SelectedIndex = 0;
    }

    private void ViewModel_RequestNavigateToItem(object? sender, GameMetadataRow row)
    {
        if (GameDataGrid.ItemsSource is not IList<GameMetadataRow> items ||
        !items.Contains(row))
            return;

        GameDataGrid.SelectedItems.Clear();
        GameDataGrid.SelectedItems.Add(row);
        GameDataGrid.ScrollIntoView(row, null);
    }

    private void ViewModel_RequestClearSelection(object? sender, EventArgs e)
    {
        GameDataGrid.SelectedItems.Clear();
    }

    private void ViewModel_RequestRestoreSelection(object? sender, List<GameMetadataRow> items)
    {
        if (GameDataGrid.ItemsSource is System.Collections.ICollection { Count: > 0 } src)
            GameDataGrid.ScrollIntoView(((System.Collections.IList)src)[0], null);

        GameDataGrid.SelectedItems.Clear();
        foreach (var item in items)
            GameDataGrid.SelectedItems.Add(item);

        if (items.Count > 0)
            GameDataGrid.ScrollIntoView(items[0], null);
    }

    private void OnDatReportColumnAdded(object? sender, string columnName)
    {
        if (sender is not DatToolViewModel datTool) return;
        if (_reportColumns.Any(c => c.Header?.ToString() == columnName)) return;

        var lookup = datTool.ReportLookup[columnName];
        var template = new FuncDataTemplate<object>((item, _) =>
        {
            if (item is not GameMetadataRow row) return null;
            lookup.TryGetValue(row.Path, out var value);
            return new TextBlock
            {
                Text = value ?? string.Empty,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(4, 0),
            };
        });

        AddReportColumn(columnName, template, new ReportTextComparer(lookup));
    }

    private void OnDatToolPanelDisposing(object? sender, EventArgs e)
    {
        if (sender is DatToolViewModel datTool)
        {
            datTool.ReportColumnAdded -= OnDatReportColumnAdded;
            datTool.PanelDisposing -= OnDatToolPanelDisposing;
        }
    }

    private void OnReportColumnsCleared(object? sender, EventArgs e) => RemoveAllReportColumns();

    private void OnFindReportColumnAdded(object? sender, FindReportColumnEventArgs e)
    {
        var pathSet = e.PathSet;
        var template = new FuncDataTemplate<object>((item, _) =>
        {
            if (item is not GameMetadataRow row) return null;
            return new CheckBox
            {
                IsChecked = pathSet.Contains(row.Path),
                IsEnabled = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
        });

        AddReportColumn(e.ColumnName, template, new ReportCheckboxComparer(pathSet));
    }

    private void AddReportColumn(string header, FuncDataTemplate<object> template, System.Collections.IComparer comparer)
    {
        var column = new DataGridTemplateColumn
        {
            Header = header,
            Width = DataGridLength.Auto,
            IsReadOnly = true,
            CanUserSort = true,
            CellTemplate = template,
            CustomSortComparer = comparer,
        };

        GameDataGrid.Columns.Insert(0, column);
        _reportColumns.Add(column);
    }

    private void RemoveAllReportColumns()
    {
        foreach (var column in _reportColumns)
            GameDataGrid.Columns.Remove(column);
        _reportColumns.Clear();
    }
}

file sealed class ReportTextComparer(Dictionary<string, string> lookup) : System.Collections.IComparer
{
    public int Compare(object? x, object? y)
    {
        var xVal = x is GameMetadataRow rx ? lookup.GetValueOrDefault(rx.Path, string.Empty) : string.Empty;
        var yVal = y is GameMetadataRow ry ? lookup.GetValueOrDefault(ry.Path, string.Empty) : string.Empty;

        if (xVal.Length == 0 && yVal.Length == 0) return 0;
        if (xVal.Length == 0) return 1;
        if (yVal.Length == 0) return -1;

        return StringComparer.OrdinalIgnoreCase.Compare(xVal, yVal);
    }
}

file sealed class ReportCheckboxComparer(HashSet<string> pathSet) : System.Collections.IComparer
{
    public int Compare(object? x, object? y)
    {
        bool xHit = x is GameMetadataRow rx && pathSet.Contains(rx.Path);
        bool yHit = y is GameMetadataRow ry && pathSet.Contains(ry.Path);
        if (xHit == yHit) return 0;
        return xHit ? -1 : 1;
    }
}