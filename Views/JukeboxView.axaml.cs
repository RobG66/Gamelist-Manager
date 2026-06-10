using Avalonia.Controls;
using Gamelist_Manager.ViewModels;
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
            
            vm.RequestPlayer();
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        VideoView.MediaPlayer = null;

        if (DataContext is JukeboxViewModel vm)
        {
            vm.MediaPlayerCreated -= OnMediaPlayerCreated;
            vm.ErrorOccurred -= OnErrorOccurred;
            vm.CloseRequested -= OnCloseRequested;
            _ = vm.DisposeAsync();
        }
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    private MediaPlayer? _latestMediaPlayer;

    private void OnMediaPlayerCreated(MediaPlayer? mediaPlayer)
    {
        _latestMediaPlayer = mediaPlayer;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_latestMediaPlayer == mediaPlayer)
            {
                VideoView.MediaPlayer = mediaPlayer;
                if (DataContext is JukeboxViewModel vm)
                {
                    vm.NotifyPlayerAttached();
                }
            }
        }, Avalonia.Threading.DispatcherPriority.Background);
    }

    private async void OnErrorOccurred(string message)
    {
        await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Jukebox Error",
            Message = message,
            IconTheme = DialogIconTheme.Error,
            Button1Text = "OK",
            Button1Result = ThreeButtonResult.Button1,
        }, this);
    }
}
