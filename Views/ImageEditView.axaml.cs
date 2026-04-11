using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.ViewModels;
using System;
using System.Threading.Tasks;

namespace Gamelist_Manager.Views;

public partial class ImageEditView : Window
{
    public ImageEditView()
    {
        InitializeComponent();
    }

    public ImageEditView(string imagePath)
    {
        InitializeComponent();
        var viewModel = new ImageEditViewModel(imagePath);
        viewModel.CloseRequested += result => Close(result);
        DataContext = viewModel;
        OriginalImageBorder.Background = ImageHelper.CreateCheckerboardBrush();
        PreviewImageBorder.Background = ImageHelper.CreateCheckerboardBrush();
        _ = viewModel.LoadAsync();
    }

    public static async Task<string?> ShowAsync(string imagePath, Window? owner = null)
    {
        owner ??= (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null) return null;
        return await new ImageEditView(imagePath).ShowDialog<string?>(owner);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        (DataContext as IDisposable)?.Dispose();
    }
}