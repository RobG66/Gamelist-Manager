using GamelistManager.classes.core;
using GamelistManager.classes.helpers;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Text.Json;
using GamelistManager.classes.gamelist;
using System.Linq;

namespace GamelistManager.classes.services
{
    public class DataGridService
    {
        private readonly DataGrid _dataGrid;

        public DataGridService(DataGrid dataGrid)
        {
            _dataGrid = dataGrid;
        }

        private static double MeasureColumnHeaderWidth(DataGridColumn column)
        {
            string header = column.Header.ToString()!;
            var textBlock = new TextBlock { Text = header };
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return textBlock.DesiredSize.Width;
        }

        private double MeasureColumnContentWidth(DataGridColumn column)
        {
            double maxWidth = 0;

            foreach (var item in _dataGrid.Items)
            {
                if (column.GetCellContent(item) is FrameworkElement cellContent)
                {
                    cellContent.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    double width = cellContent.DesiredSize.Width;
                    if (width > maxWidth)
                    {
                        maxWidth = width;
                    }
                }
            }
            return maxWidth;
        }

        public void UpdateDataGridFontSize(double fontSize)
        {
            // Update font size for DataGrid header and cells
            foreach (var column in _dataGrid.Columns)
            {
                if (column.Header is TextBlock header)
                {
                    header.FontSize = fontSize;
                }
            }
            _dataGrid.RowHeight = fontSize + 7; // Adjust row height for better fit
        }

        public void AdjustDataGridColumnWidths(bool autoSizeColumns)
        {
            _dataGrid.UpdateLayout();

            DataGridLength size = new(1, DataGridLengthUnitType.SizeToCells);
            if (autoSizeColumns)
            {
                size = new DataGridLength(1, DataGridLengthUnitType.Star);
            }

            foreach (var column in _dataGrid.Columns)
            {
                double headerWidth = MeasureColumnHeaderWidth(column);

                if (column is DataGridTemplateColumn)
                {
                    // For boolean columns
                    column.CanUserResize = false;
                    column.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToHeader);
                }
                else
                {
                    // Measure content width
                    double contentWidth = MeasureColumnContentWidth(column);

                    // Set width to header if content width is shorter
                    if (contentWidth < headerWidth)
                    {
                        column.Width = new DataGridLength(headerWidth, DataGridLengthUnitType.SizeToHeader);
                    }
                    else
                    {
                        column.Width = size;
                    }
                    column.CanUserResize = true;
                }
            }
        }

        public void SetupMainDataGrid()
        {
            _dataGrid.ItemsSource = null;
            _dataGrid.Columns.Clear();

            foreach (DataColumn column in SharedData.DataSet.Tables[0].Columns)
            {
                if (column.DataType == typeof(bool))
                {
                    _dataGrid.Columns.Add(new DataGridTemplateColumn
                    {
                        Header = column.ColumnName,
                        CellTemplate = CheckBoxTemplateHelper.CreateCheckbox(column.ColumnName),
                        SortMemberPath = column.ColumnName
                    });
                }
                else
                {
                    _dataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = column.ColumnName,
                        Binding = new Binding(column.ColumnName),
                        SortMemberPath = column.ColumnName
                    });
                }
            }

            _dataGrid.ItemsSource = SharedData.DataSet.Tables[0].DefaultView;
        }

        public int FindRowIndexInDataGrid(int currentIndex, string searchText, string columnName)
        {
            if (string.IsNullOrWhiteSpace(searchText) || _dataGrid.Items.Count == 0)
            {
                return -1;
            }

            int startIndex = (currentIndex + 1) % _dataGrid.Items.Count;

            // Pass 1: from startIndex → end
            for (int i = startIndex; i < _dataGrid.Items.Count; i++)
            {
                if (IsMatch(_dataGrid.Items[i], searchText, columnName))
                    return i;
            }

            // Pass 2: from 0 → currentIndex
            for (int i = 0; i < startIndex; i++)
            {
                if (IsMatch(_dataGrid.Items[i], searchText, columnName))
                    return i;
            }

            return -1; // nothing found
        }

        private static bool IsMatch(object item, string searchText, string columnName)
        {
            if (item is DataRowView rowView)
            {
                if (string.IsNullOrEmpty(columnName))
                {
                    // Search all columns
                    return rowView.Row.ItemArray
                              .Cast<object?>()
                              .Any(col =>
                              {
                                  var s = col?.ToString();
                                  return !string.IsNullOrEmpty(s) &&
                                         s.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                              });
                }
                else if (rowView.Row.Table.Columns.Contains(columnName))
                {
                    var value = rowView.Row[columnName]?.ToString();
                    return !string.IsNullOrEmpty(value) &&
                           value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                }
            }
            return false;
        }

        public void ShowMediaPaths(bool showPaths)
        {
            var mediaItems = GamelistMetaData.GetMetaDataDictionary().Values
                .Where(decl => decl.DataType == MetaDataType.Image ||
                                decl.DataType == MetaDataType.Video ||
                                decl.DataType == MetaDataType.Music ||
                                decl.DataType == MetaDataType.Document)
                                .Select(decl => decl.Name)
                                .ToList();

            foreach (DataGridColumn column in _dataGrid.Columns)
            {
                if (mediaItems.Contains(column.Header.ToString()!))
                {
                    column.Visibility = (showPaths) ? Visibility.Visible : Visibility.Collapsed;
                    column.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
                }
            }
            _dataGrid.UpdateLayout();
        }


    }
}