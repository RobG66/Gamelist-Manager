using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Gamelist_Manager.ViewModels;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;
using System;
using System.Runtime.InteropServices;

namespace Gamelist_Manager.Views;

public partial class JukeboxView : Window
{
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    private readonly DispatcherTimer _mousePoller = new();
    private POINT _lastMousePos;

    public JukeboxView()
    {
        InitializeComponent();

        Opened += OnOpened;
        Closing += OnClosing;

        _mousePoller.Interval = TimeSpan.FromMilliseconds(100);
        _mousePoller.Tick += OnMousePollerTick;
        _mousePoller.Start();
    }

    private void OnMousePollerTick(object? sender, EventArgs e)
    {
        if (!IsVisible) return;

        if (GetCursorPos(out var p))
        {
            if (p.X != _lastMousePos.X || p.Y != _lastMousePos.Y)
            {
                _lastMousePos = p;
                
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;
                
                var clientPos = topLevel.PointToClient(new PixelPoint(p.X, p.Y));
                if (clientPos.X >= 0 && clientPos.X <= Bounds.Width &&
                    clientPos.Y >= 0 && clientPos.Y <= Bounds.Height)
                {
                    if (DataContext is JukeboxViewModel vm)
                    {
                        vm.ResetAutoHideTimer();
                    }
                }
            }
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is JukeboxViewModel vm)
        {
            vm.SetProjectMControl(ProjectMView);
            vm.SetStorageProvider(TopLevel.GetTopLevel(this)?.StorageProvider);
            vm.MediaPlayerCreated += OnMediaPlayerCreated;
            vm.ErrorOccurred += OnErrorOccurred;
            vm.CloseRequested += OnCloseRequested;
            vm.PropertyChanged += OnViewModelPropertyChanged;

            vm.RequestPlayer();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Removed dynamic window resizing for the picker as it is now an overlay
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
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
        if (DataContext is JukeboxViewModel vm)
        {
            // Only attach the player to the VideoView if we are playing a video, OR if we are playing audio but ProjectM is missing (fallback).
            // If we are playing audio (VisualizationsEnabled == true) and ProjectM is installed, we leave the VideoView detached
            // so its native HWND doesn't pop up and cover the visualizer.
            if (vm.VisualizationsEnabled && vm.HasProjectM)
            {
                VideoView.MediaPlayer = null;
                VideoView.IsVisible = false;
                ProjectMWrapper.IsVisible = true;
            }
            else
            {
                VideoView.MediaPlayer = mediaPlayer;
                VideoView.IsVisible = true;
                ProjectMWrapper.IsVisible = false;
            }
            
            if (mediaPlayer != null)
            {
                vm.NotifyPlayerAttached();
            }
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
