using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
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
    private readonly Dictionary<string, DataGridColumn> _columnsByType = new();

    private readonly List<DataGridColumn> _reportColumns = [];

    public MainWindow()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0] ?? string.Empty;
        Title = string.IsNullOrEmpty(version)
            ? "Gamelist Manager"
            : $"Gamelist Manager {version}";

        Services.WindowService.Instance.SetOwner(this);

        SharedDataService.Instance.SettingsApplied += OnSettingsApplied;

        Loaded += MainWindow_Loaded;
        DataContextChanged += MainWindow_DataContextChanged;
        Closing += MainWindow_Closing;
        GameDataGrid.SelectionChanged += GameDataGrid_SelectionChanged;
        AddHandler(KeyDownEvent, MainWindow_KeyDown, RoutingStrategies.Tunnel);

        BuildDataGridColumns();
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
                    Button1Text = "OK",
                    Button2Text = "",
                    Button3Text = ""
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

    private void ApplySizeToFitToDataGrid(bool sizeToFit)
    {
        if (GameDataGrid?.Columns == null) return;

        foreach (var decl in GamelistMetaData.GetColumnDeclarations())
        {
            if (!_columnsByType.TryGetValue(decl.Type, out var column)) continue;
            if (column is not DataGridTextColumn textColumn) continue;

            var starWidth = decl.Key switch
            {
                MetaDataKeys.name or MetaDataKeys.publisher or MetaDataKeys.developer => 1.5,
                MetaDataKeys.genre => 2.0,
                MetaDataKeys.path or MetaDataKeys.family => 1.0,
                _ => -1.0
            };

            if (starWidth < 0) continue;

            textColumn.Width = sizeToFit
                ? new DataGridLength(starWidth, DataGridLengthUnitType.Star)
                : DataGridLength.Auto;
        }

        GameDataGrid.UpdateLayout();
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

    private void OnSettingsApplied(object? sender, EventArgs e)
    {
        var settings = SettingsService.Instance;
        var sharedData = SharedDataService.Instance;

        ThemeService.ApplyFontSizes(sharedData.AppFontSize, sharedData.GridFontSize);

        var alternatingRowColorIndex = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.AlternatingRowColorIndex, 1);
        var gridLinesVisibilityIndex = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GridLinesVisibilityIndex);
        ThemeService.ApplyDataGridAppearance(GameDataGrid, alternatingRowColorIndex, gridLinesVisibilityIndex);
        ThemeService.ApplyDataGridColumnWidths(GameDataGrid, sharedData.GridFontSize);
    }

    private async void MainWindow_Loaded(object? sender, EventArgs e)
    {
        var settings = SettingsService.Instance;
        var alternatingRowColorIndex = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.AlternatingRowColorIndex, 1);
        var gridLinesVisibilityIndex = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GridLinesVisibilityIndex);

        ThemeService.ApplyDataGridAppearance(
            GameDataGrid,
            alternatingRowColorIndex,
            gridLinesVisibilityIndex);

        var dataGridFontSize = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GridFontSize, 12);
        ThemeService.ApplyDataGridColumnWidths(GameDataGrid, dataGridFontSize);

        if (DataContext is MainWindowViewModel vm)
        {
            vm.UpdateSearchableColumns(GetVisibleColumnNames());
            await vm.ResolveMissingProfileAsync();
        }

        foreach (var column in GameDataGrid.Columns)
            column.PropertyChanged += DataGridColumn_PropertyChanged;
    }

    private void DataGridColumn_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(DataGridColumn.IsVisible) && DataContext is MainWindowViewModel vm)
            vm.UpdateSearchableColumns(GetVisibleColumnNames());
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

    private void BuildDataGridColumns()
    {
        var columns = GameDataGrid.Columns;
        columns.Clear();
        _columnsByType.Clear();

        foreach (var decl in GamelistMetaData.GetColumnDeclarations())
        {
            if (decl.Key == MetaDataKeys.music)
                continue;

            DataGridColumn column;

            if (decl.DataType == MetaDataType.Bool)
            {
                var templateColumn = new DataGridTemplateColumn
                {
                    Header = decl.Name,
                    Width = new DataGridLength(62),
                    CanUserReorder = false,
                    CanUserResize = false,
                    SortMemberPath = decl.PropertyName,
                    CellTemplate = CreateCheckBoxTemplate(decl.PropertyName),
                    CellEditingTemplate = CreateCheckBoxTemplate(decl.PropertyName),
                };
                column = templateColumn;
            }
            else if (decl.Key == MetaDataKeys.desc)
            {
                column = new DataGridTextColumn
                {
                    Header = decl.Name,
                    Binding = new Binding(decl.PropertyName),
                    SortMemberPath = decl.PropertyName,
                    Width = new DataGridLength(3, DataGridLengthUnitType.Star),
                    IsVisible = false,
                    IsReadOnly = true,
                };
            }
            else if (decl.IsMedia)
            {
                column = new DataGridTextColumn
                {
                    Header = decl.Name,
                    Binding = new Binding(decl.PropertyName),
                    SortMemberPath = decl.PropertyName,
                    Width = DataGridLength.Auto,
                    IsReadOnly = true,
                };
            }
            else
            {
                var isWide = decl.Key is MetaDataKeys.path or MetaDataKeys.name
                    or MetaDataKeys.genre or MetaDataKeys.publisher or MetaDataKeys.developer
                    or MetaDataKeys.family;

                column = new DataGridTextColumn
                {
                    Header = decl.Name,
                    Binding = new Binding(decl.PropertyName),
                    SortMemberPath = decl.PropertyName,
                    Width = isWide
                        ? new DataGridLength(1, DataGridLengthUnitType.Star)
                        : DataGridLength.SizeToHeader,
                    IsReadOnly = !decl.Editable,
                };
            }

            columns.Add(column);
            _columnsByType[decl.Type] = column;
        }
    }

    private void ApplyColumnVisibility()
    {
        if (DataContext is not MainWindowViewModel vm) return;

        foreach (var decl in GamelistMetaData.GetColumnDeclarations())
        {
            if (!_columnsByType.TryGetValue(decl.Type, out var column)) continue;

            if (decl.AlwaysVisible || decl.Key == MetaDataKeys.desc)
                continue;

            if (decl.IsMedia)
            {
                column.IsVisible = vm.MediaPathsVisible;
            }
            else
            {
                column.IsVisible = vm.GetColumnVisible(decl.Type);
            }
        }

        vm.UpdateSearchableColumns(GetVisibleColumnNames());
    }

    private IEnumerable<string> GetVisibleColumnNames() =>
        GameDataGrid.Columns
            .Where(c => c.IsVisible)
            .Select(c => c.Header?.ToString())
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => h!);

    private static FuncDataTemplate<object> CreateCheckBoxTemplate(string propertyName)
    {
        return new FuncDataTemplate<object>((_, _) =>
        {
            var checkBox = new Avalonia.Controls.CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            checkBox.Bind(Avalonia.Controls.CheckBox.IsCheckedProperty,
                new Binding(propertyName) { Mode = BindingMode.TwoWay });
            checkBox.Bind(Avalonia.Controls.CheckBox.IsEnabledProperty,
                new Binding("!IsReadOnly")
                {
                    RelativeSource = new Avalonia.Data.RelativeSource(Avalonia.Data.RelativeSourceMode.FindAncestor)
                    {
                        AncestorType = typeof(DataGrid)
                    }
                });
            checkBox[!Avalonia.Controls.CheckBox.FontSizeProperty] =
                new DynamicResourceExtension("DataGridFontSizeResource");
            return checkBox;
        });
    }
}

// Sorts DAT report column rows by their text value. Rows with no entry sort to the bottom.
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