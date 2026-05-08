using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Gamelist_Manager.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Gamelist_Manager.Views
{
    public record SetColumnValueOptions(bool UseAllItems, MetaDataKeys Key, object? Value);

    public partial class SetColumnValueView : Window
    {
        public static async Task<SetColumnValueOptions?> ShowAsync(bool hasSelection, Window? owner = null)
        {
            owner ??= (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (owner == null) return null;
            return await new SetColumnValueView(hasSelection).ShowDialog<SetColumnValueOptions?>(owner);
        }

        private MetaDataDecl? _selectedDecl;

        public SetColumnValueView() : this(false) { }

        public SetColumnValueView(bool hasSelection)
        {
            InitializeComponent();

            RadioSelectedItems.IsEnabled = hasSelection;

            // Populate the column combobox — everything except path, using display names
            var columns = GamelistMetaData.GetColumnDeclarations()
                .Where(d => d.Key != MetaDataKeys.path)
                .ToList();

            ColumnComboBox.ItemsSource = columns;
            ColumnComboBox.DisplayMemberBinding = new Avalonia.Data.Binding(nameof(MetaDataDecl.Name));

            KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close(null);
                e.Handled = true;
            }
        }

        private void ColumnComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            _selectedDecl = ColumnComboBox.SelectedItem as MetaDataDecl;

            var isBool = _selectedDecl?.DataType == MetaDataType.Bool;

            BoolValuePanel.IsVisible = isBool;
            StringValuePanel.IsVisible = !isBool && _selectedDecl != null;
            ValueSection.IsVisible = _selectedDecl != null;

            // Reset value panels to their defaults
            RadioBoolEmpty.IsChecked = true;
            RadioStringEmpty.IsChecked = true;
            CustomValueTextBox.Text = string.Empty;
            CustomValueTextBox.IsEnabled = false;

            UpdateApplyEnabled();
        }

        private void StringValueRadio_Changed(object? sender, RoutedEventArgs e)
        {
            CustomValueTextBox.IsEnabled = RadioStringCustom.IsChecked == true;
            UpdateApplyEnabled();
        }

        private void CustomValueTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            UpdateApplyEnabled();
        }

        private void UpdateApplyEnabled()
        {
            if (_selectedDecl == null)
            {
                ApplyButton.IsEnabled = false;
                return;
            }

            if (_selectedDecl.DataType == MetaDataType.Bool)
            {
                ApplyButton.IsEnabled = true;
                return;
            }

            // For string/media: always valid (empty is a legal choice, custom requires non-empty text)
            ApplyButton.IsEnabled = RadioStringEmpty.IsChecked == true
                || !string.IsNullOrEmpty(CustomValueTextBox.Text);
        }

        private void ApplyButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedDecl == null) return;

            object? value;

            if (_selectedDecl.DataType == MetaDataType.Bool)
            {
                value = RadioBoolTrue.IsChecked == true ? true
                      : RadioBoolFalse.IsChecked == true ? false
                      : null; // Empty maps to null; BulkOperations will write false
            }
            else
            {
                value = RadioStringEmpty.IsChecked == true
                    ? null
                    : CustomValueTextBox.Text;
            }

            var options = new SetColumnValueOptions(
                UseAllItems: RadioAllItems.IsChecked == true,
                Key: _selectedDecl.Key,
                Value: value);

            Close(options);
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}
