using Avalonia.Controls;
using Avalonia.Input;
using Gamelist_Manager.ViewModels;

namespace Gamelist_Manager.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        var viewModel = new AboutWindowViewModel();
        viewModel.CloseRequested += () => Close();
        DataContext = viewModel;

        KeyDown += AboutWindow_KeyDown;
    }

    private void AboutWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
