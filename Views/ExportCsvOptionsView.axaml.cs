using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace Gamelist_Manager.Views
{
    public record ExportCsvOptions(bool UseFilteredRows, bool UseVisibleColumnsOnly);

    public partial class ExportCsvOptionsView : Window
    {
        public static async Task<ExportCsvOptions?> ShowAsync(Window? owner = null)
        {
            owner ??= (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (owner == null) return null;
            return await new ExportCsvOptionsView().ShowDialog<ExportCsvOptions?>(owner);
        }

        public ExportCsvOptionsView()
        {
            InitializeComponent();
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

        private void ExportButton_Click(object? sender, RoutedEventArgs e)
        {
            var options = new ExportCsvOptions(
                UseFilteredRows: RadioFilteredRows.IsChecked == true,
                UseVisibleColumnsOnly: RadioVisibleColumns.IsChecked == true);
            Close(options);
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}
