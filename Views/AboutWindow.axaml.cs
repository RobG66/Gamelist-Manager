using Avalonia.Controls;
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
    }
}
