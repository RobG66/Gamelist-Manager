using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Gamelist_Manager.Models;
using Gamelist_Manager.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace Gamelist_Manager.Views;

public partial class MainWindow
{
    #region Private Fields

    private readonly Dictionary<string, DataGridColumn> _columnsByType = new();

    #endregion

    #region Private Methods

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
                    Width = DataGridLength.SizeToHeader,
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

    private IEnumerable<string> GetVisibleColumnNames() =>
        GameDataGrid.Columns
            .Where(c => c.IsVisible)
            .Select(c => c.Header?.ToString())
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => h!);

    private FuncDataTemplate<object> CreateCheckBoxTemplate(string propertyName)
    {
        var dataGrid = GameDataGrid;
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
                new Binding(nameof(DataGrid.IsReadOnly))
                {
                    Source = dataGrid,
                    Converter = Avalonia.Data.Converters.BoolConverters.Not
                });
            checkBox[!Avalonia.Controls.CheckBox.FontSizeProperty] =
                new DynamicResourceExtension("DataGridFontSizeResource");
            return checkBox;
        });
    }

    #endregion

    #region Event Handlers

    private void DataGridColumn_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(DataGridColumn.IsVisible) && DataContext is MainWindowViewModel vm)
            vm.UpdateSearchableColumns(GetVisibleColumnNames());
    }

    #endregion
}
