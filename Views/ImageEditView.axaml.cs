using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.ViewModels;

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
        // Circle 2 disabled — click and cursor handling preserved for re-enabling
        // OriginalImageControl.PointerPressed += OnOriginalImagePointerPressed;
        // viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _ = viewModel.LoadAsync();
    }

    public static async Task<string?> ShowAsync(string imagePath, Window? owner = null)
    {
        owner ??= (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null) return null;
        return await new ImageEditView(imagePath).ShowDialog<string?>(owner);
    }

    // Circle 2 disabled — cursor and click logic preserved for re-enabling
    // private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    // {
    //     if (e.PropertyName == nameof(ImageEditViewModel.NeedsClick))
    //     {
    //         var vm = (ImageEditViewModel)sender!;
    //         OriginalImageControl.Cursor = vm.NeedsClick ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
    //     }
    // }

    // private void OnOriginalImagePointerPressed(object? sender, PointerPressedEventArgs e)
    // {
    //     if (DataContext is not ImageEditViewModel vm || !vm.IsCircleFitMode) return;
    //     var pos = e.GetPosition(OriginalImageControl);
    //     int pixelX = (int)Math.Round(pos.X);
    //     int pixelY = (int)Math.Round(pos.Y);
    //     vm.SetClickPoint(pixelX, pixelY);
    // }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        (DataContext as IDisposable)?.Dispose();
    }
}
