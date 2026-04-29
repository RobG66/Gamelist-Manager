using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Gamelist_Manager.Classes.Helpers;
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

        Services.WindowService.Instance.SetOwner(this);  // SetOwner is now part of IWindowService

        SharedDataService.Instance.SettingsApplied += OnSettingsApplied;

        Loaded += MainWindow_Loaded;
        DataContextChanged += MainWindow_DataContextChanged;
        Closing += MainWindow_Closing;
        GameDataGrid.SelectionChanged += GameDataGrid_SelectionChanged;
        AddHandler(KeyDownEvent, MainWindow_KeyDown, RoutingStrategies.Tunnel);

        BuildDataGridColumns();
    }

    private void MainWindow_DataContextChanged_ContextMenu(object? sender, EventArgs e)
    {
        if (GameDataGrid.ContextMenu is { } menu)
        {
            menu.DataContext = DataContext;
            menu.Opening += ContextMenu_Opening;
        }
    }

    private void ContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
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
        if (_currentViewModel?.IsBusy == true)
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

            if (viewModel.IsScraping)
            {
                e.Cancel = true;
                var scrapingDialog = new ThreeButtonDialogView(new ThreeButtonDialogConfig
                {
                    Title = "Scraping in Progress",
                    Message = "Scraping is currently in progress.",
                    DetailMessage = "Please stop the scraper before closing the application.",
                    IconTheme = DialogIconTheme.Warning,
                    Button1Text = "",
                    Button2Text = "",
                    Button3Text = "OK"
                });
                await scrapingDialog.ShowDialog<ThreeButtonResult>(this);
                return;
            }

            var sharedData = SharedDataService.Instance;

            if (sharedData.IsDataChanged && sharedData.EnableSaveReminder)
            {
                e.Cancel = true;

                var dialog = new ThreeButtonDialogView(new ThreeButtonDialogConfig
                {
                    Title = "Unsaved Changes",
                    Message = "You have unsaved changes to the current gamelist.",
                    DetailMessage = "Do you want to save your changes before exiting?",
                    IconTheme = DialogIconTheme.Warning,
                    Button1Text = "Cancel",
                    Button2Text = "Don't Save",
                    Button3Text = "Save"
                });

                var result = await dialog.ShowDialog<ThreeButtonResult>(this);

                if (result == ThreeButtonResult.Button1)
                    return;

                if (result == ThreeButtonResult.Button3)
                    await viewModel.SaveGamelistCommand.ExecuteAsync(null);
            }

            viewModel.DisposeMediaPreview();
            viewModel.ScraperPanelViewModel?.Dispose();

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
            _currentViewModel.RequestRestoreSelection -= ViewModel_RequestRestoreSelection;
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
            viewModel.RequestRestoreSelection += ViewModel_RequestRestoreSelection;

            Topmost = viewModel.IsAlwaysOnTop;
            ApplySizeToFitToDataGrid(viewModel.SizeToFit);
            ApplyColumnVisibility();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MainWindowViewModel viewModel)
            return;

        if (e.PropertyName == nameof(MainWindowViewModel.IsAlwaysOnTop))
        {
            Topmost = viewModel.IsAlwaysOnTop;
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.SizeToFit))
        {
            ApplySizeToFitToDataGrid(viewModel.SizeToFit);
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.MediaPathsVisible))
        {
            ApplyColumnVisibility();
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.IsGamelistLoaded))
        {
            BuildDataGridColumns();
            ApplySizeToFitToDataGrid(viewModel.SizeToFit);
            ApplyColumnVisibility();
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.DatToolPanelViewModel))
        {
            // When the DatTool panel opens, subscribe to its column-add and dispose events.
            // Closing the panel does NOT remove report columns — that is handled by
            // ClearReportColumnsCommand or when a new gamelist loads.
            if (viewModel.DatToolPanelViewModel is { } datTool)
            {
                datTool.ReportColumnAdded += OnDatReportColumnAdded;
                datTool.PanelDisposing += OnDatToolPanelDisposing;
            }
        }
    }

    private void ViewModel_RequestSelectFirstItem(object? sender, EventArgs e)
    {
        if (GameDataGrid.ItemsSource is System.Collections.ICollection { Count: > 0 })
            GameDataGrid.SelectedIndex = 0;
    }

    private void ViewModel_RequestNavigateToItem(object? sender, GameMetadataRow row)
    {
        if (GameDataGrid.ItemsSource is not IList<GameMetadataRow> items)
            return;

        var index = items.IndexOf(row);
        if (index < 0) return;

        GameDataGrid.SelectedIndex = index;
        GameDataGrid.ScrollIntoView(row, null);
    }

    private void ViewModel_RequestRestoreSelection(object? sender, List<GameMetadataRow> items)
    {
        // Scroll to the very top first so the DataGrid anchors at row 0,
        // then re-select and scroll to the first selected item
        if (GameDataGrid.ItemsSource is System.Collections.ICollection { Count: > 0 } src)
            GameDataGrid.ScrollIntoView(((System.Collections.IList)src)[0], null);

        GameDataGrid.SelectedItems.Clear();
        foreach (var item in items)
            GameDataGrid.SelectedItems.Add(item);

        if (items.Count > 0)
            GameDataGrid.ScrollIntoView(items[0], null);
    }

    private void OnSettingsApplied(object? sender, EventArgs e)
    {
        var settings = SettingsService.Instance;
        var sharedData = SharedDataService.Instance;

        ThemeService.ApplyFontSizes(sharedData.AppFontSize, sharedData.GridFontSize);

        var alternatingRowColorIndex = settings.GetInt(SettingKeys.AlternatingRowColorIndex);
        var gridLinesVisibilityIndex = settings.GetInt(SettingKeys.GridLinesVisibilityIndex);
        ThemeService.ApplyDataGridAppearance(GameDataGrid, alternatingRowColorIndex, gridLinesVisibilityIndex);
        ThemeService.ApplyDataGridColumnWidths(GameDataGrid, sharedData.GridFontSize);
    }

    private async void MainWindow_Loaded(object? sender, EventArgs e)
    {
        var settings = SettingsService.Instance;
        var alternatingRowColorIndex = settings.GetInt(SettingKeys.AlternatingRowColorIndex);
        var gridLinesVisibilityIndex = settings.GetInt(SettingKeys.GridLinesVisibilityIndex);

        ThemeService.ApplyDataGridAppearance(
            GameDataGrid,
            alternatingRowColorIndex,
            gridLinesVisibilityIndex);

        var dataGridFontSize = settings.GetInt(SettingKeys.GridFontSize);
        ThemeService.ApplyDataGridColumnWidths(GameDataGrid, dataGridFontSize);

        if (DataContext is MainWindowViewModel vm)
        {
            vm.UpdateSearchableColumns(GetVisibleColumnNames());
            await vm.ResolveMissingProfileAsync();
        }

        foreach (var column in GameDataGrid.Columns)
            column.PropertyChanged += DataGridColumn_PropertyChanged;
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

    private void OnReportColumnsCleared(object? sender, EventArgs e)
    {
        RemoveAllReportColumns();
    }

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

// Sorts DAT report column rows
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

// Sorts Find report column rows so checked (matched) rows appear first.
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