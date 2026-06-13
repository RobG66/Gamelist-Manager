using Avalonia;
using Avalonia.Controls;
using Gamelist_Manager.ViewModels;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;
using System;

namespace Gamelist_Manager.Views;

public partial class JukeboxView : Window
{
    public JukeboxView()
    {
        InitializeComponent();

        Opened += OnOpened;
        Closing += OnClosing;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is JukeboxViewModel vm)
        {
            vm.MediaPlayerCreated += OnMediaPlayerCreated;
            vm.ErrorOccurred += OnErrorOccurred;
            vm.CloseRequested += OnCloseRequested;
            vm.PropertyChanged += OnViewModelPropertyChanged;

            VideoView.SizeChanged += OnVideoViewSizeChanged;

            var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
            vm.InitializeDimensions((int)(VideoView.Bounds.Width * scaling), (int)(VideoView.Bounds.Height * scaling));
            vm.RequestPlayer();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Removed dynamic window resizing for the picker as it is now an overlay
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        VideoView.SizeChanged -= OnVideoViewSizeChanged;
        VideoView.MediaPlayer = null;

        if (DataContext is JukeboxViewModel vm)
        {
            vm.MediaPlayerCreated -= OnMediaPlayerCreated;
            vm.ErrorOccurred -= OnErrorOccurred;
            vm.CloseRequested -= OnCloseRequested;
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            _ = vm.DisposeAsync();
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    private void OnMediaPlayerCreated(MediaPlayer? mediaPlayer)
    {
        // Immediately detach the UI from the player if we receive null, 
        // to prevent native crashes when the backend player is being destroyed
        VideoView.MediaPlayer = mediaPlayer;
        
        if (mediaPlayer != null && DataContext is JukeboxViewModel vm)
        {
            vm.NotifyPlayerAttached();
        }
    }

    private void OnVideoViewSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is JukeboxViewModel vm)
        {
            var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
            vm.HandleResize((int)(e.NewSize.Width * scaling), (int)(e.NewSize.Height * scaling));
        }
    }

    private async void OnErrorOccurred(string message)
    {
        try
        {
            await ThreeButtonDialogView.ShowErrorAsync(
                "Jukebox Error",
                message,
                owner: this);
        }
        catch (Exception)
        {
        }
    }

    private void PresetList_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is JukeboxViewModel vm && vm.SelectedPreset != null)
        {
            vm.ApplyPresetCommand.Execute(null);
        }
    }
}
