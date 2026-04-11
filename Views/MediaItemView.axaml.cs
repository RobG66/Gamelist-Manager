using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Gamelist_Manager.Classes.IO;
using Gamelist_Manager.ViewModels;
using LibVLCSharp.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gamelist_Manager.Views;

public partial class MediaItemView : UserControl
{
    public static readonly StyledProperty<bool> IsScaledProperty =
        AvaloniaProperty.Register<MediaItemView, bool>(nameof(IsScaled));

    public bool IsScaled
    {
        get => GetValue(IsScaledProperty);
        set => SetValue(IsScaledProperty, value);
    }

    private VideoView? _videoView;
    private Panel? _videoViewContainer;
    private Viewbox? _viewbox;
    private Size _lastViewboxSize;
    private IDisposable? _boundsSubscription;
    private LibVLCSharp.Shared.MediaPlayer? _pendingMediaPlayer;
    private MediaItemViewModel? _subscribedViewModel;

    public MediaItemView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    #region Video View Lifecycle

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_pendingMediaPlayer != null)
        {
            CreateVideoView(_pendingMediaPlayer);
            _pendingMediaPlayer = null;
            return;
        }

        if (DataContext is MediaItemViewModel viewModel && viewModel.IsVideo)
        {
            if (_subscribedViewModel != viewModel)
            {
                viewModel.PropertyChanged += OnViewModelPropertyChanged;
                _subscribedViewModel = viewModel;
            }
            if (viewModel.MediaPlayer != null && _videoView == null)
                CreateVideoView(viewModel.MediaPlayer);
        }
    }

    private void OnUnloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        UnsubscribeFromViewModel();
        DisposeVideoView();
        _pendingMediaPlayer = null;
    }

    private void UnsubscribeFromViewModel()
    {
        _subscribedViewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _subscribedViewModel = null;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        UnsubscribeFromViewModel();
        DisposeVideoView();
        _pendingMediaPlayer = null;

        _videoViewContainer = this.FindControl<Panel>("VideoViewContainer");

        if (DataContext is MediaItemViewModel viewModel && viewModel.IsVideo)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _subscribedViewModel = viewModel;

            if (viewModel.MediaPlayer != null)
            {
                if (IsLoaded)
                    CreateVideoView(viewModel.MediaPlayer);
                else
                    _pendingMediaPlayer = viewModel.MediaPlayer;
            }
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MediaItemViewModel.MediaPlayer))
            return;

        if (DataContext is not MediaItemViewModel viewModel)
            return;

        if (viewModel.MediaPlayer != null)
        {
            if (IsLoaded)
                CreateVideoView(viewModel.MediaPlayer);
            else
                _pendingMediaPlayer = viewModel.MediaPlayer;
        }
        else
        {
            DisposeVideoView();
            _pendingMediaPlayer = null;
        }
    }

    private void CreateVideoView(LibVLCSharp.Shared.MediaPlayer mediaPlayer)
    {
        if (_videoViewContainer == null)
            return;

        DisposeVideoView();

        _videoView = new VideoView
        {
            Width = 400,
            Height = 300,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
        };

        _viewbox = new Viewbox
        {
            Stretch = Avalonia.Media.Stretch.Uniform,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            Child = _videoView
        };

        _boundsSubscription = _viewbox.GetObservable(Viewbox.BoundsProperty)
            .Subscribe(newBounds => OnViewboxBoundsChanged(newBounds));

        _videoViewContainer.Children.Add(_viewbox);

        // Defer MediaPlayer assignment until after the layout pass so the
        // native window handle exists.  Without this, VLC can occasionally
        // open a detached standalone window.  Play() is called here rather
        // than in InitializeVideoPlayer to guarantee the handle is wired up.
        var videoView = _videoView;
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (videoView != _videoView) return;
            videoView.MediaPlayer = mediaPlayer;
            if (DataContext is MediaItemViewModel vm)
                vm.Play();
        }, Avalonia.Threading.DispatcherPriority.Loaded);
    }

    private void OnViewboxBoundsChanged(Rect newBounds)
    {
        if (_viewbox == null || _videoView == null)
            return;

        var newSize = newBounds.Size;
        if (newSize == _lastViewboxSize || newSize.Width <= 0 || newSize.Height <= 0)
            return;

        _lastViewboxSize = newSize;

        const double videoWidth = 400;
        const double videoHeight = 300;
        var videoAspect = videoWidth / videoHeight;
        var viewboxAspect = newSize.Width / newSize.Height;

        double scaledWidth, scaledHeight;

        if (viewboxAspect > videoAspect)
        {
            scaledHeight = newSize.Height;
            scaledWidth = scaledHeight * videoAspect;
        }
        else
        {
            scaledWidth = newSize.Width;
            scaledHeight = scaledWidth / videoAspect;
        }

        _videoView.Width = Math.Max(1, scaledWidth);
        _videoView.Height = Math.Max(1, scaledHeight);
        _videoView.InvalidateMeasure();
        _videoView.InvalidateArrange();
    }

    public void DisposeVideoView()
    {
        try
        {
            _boundsSubscription?.Dispose();
            _boundsSubscription = null;

            if (_videoView != null)
            {
                _videoView.MediaPlayer = null;
                _videoViewContainer?.Children.Clear();
                _videoView = null;
                _viewbox = null;
                _lastViewboxSize = default;
            }
        }
        catch { }
    }

    #endregion

    #region Drag and Drop

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (DataContext is not MediaItemViewModel mediaItem)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        if (IsValidDrop(e, mediaItem))
        {
            e.DragEffects = DragDropEffects.Copy;
            mediaItem.IsDragOver = true;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (DataContext is not MediaItemViewModel mediaItem)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = IsValidDrop(e, mediaItem)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (DataContext is MediaItemViewModel mediaItem)
            mediaItem.IsDragOver = false;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MediaItemViewModel mediaItem)
            return;

        mediaItem.IsDragOver = false;

        if (!IsValidDrop(e, mediaItem))
            return;

        var parentViewModel = FindMediaPreviewViewModel();
        if (parentViewModel?.SelectedGame == null)
            return;

        var (filePath, isTemp) = await GetDroppedFile(e, mediaItem);
        if (string.IsNullOrEmpty(filePath))
            return;

        try
        {
            await parentViewModel.UpdateGameMedia(mediaItem.MediaType, filePath);
        }
        finally
        {
            if (isTemp && File.Exists(filePath))
                try { File.Delete(filePath); } catch { }
        }
    }

#pragma warning disable CS0618
    private static bool IsValidDrop(DragEventArgs e, MediaItemViewModel mediaItem)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles()?.ToList();
            if (files?.Count == 1)
                return mediaItem.IsValidDrop(files[0].Path.LocalPath);
            return false;
        }

        if (mediaItem.IsVideo || mediaItem.IsManual)
            return false;

        return e.Data.Contains("text/uri-list") ||
               e.Data.Contains("text/html") ||
               e.Data.Contains(DataFormats.Text);
    }

    private static async Task<(string? FilePath, bool IsTemp)> GetDroppedFile(DragEventArgs e, MediaItemViewModel mediaItem)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles()?.ToList();
            if (files?.Count == 1)
                return (files[0].Path.LocalPath, false);
            return (null, false);
        }

        if (mediaItem.IsVideo || mediaItem.IsManual)
            return (null, false);

        if (e.Data.Contains("text/uri-list"))
        {
            var url = e.Data.GetText()?.Trim();
            if (!string.IsNullOrWhiteSpace(url))
                return (await DownloadImageFromUrl(url), true);
        }

        if (e.Data.Contains(DataFormats.Text))
        {
            var text = e.Data.GetText()?.Trim();
            if (!string.IsNullOrEmpty(text) &&
                Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https"))
                return (await DownloadImageFromUrl(text), true);
        }

        if (e.Data.Contains("text/html"))
        {
            var html = e.Data.GetText() ?? string.Empty;
            var imageUrl = ExtractImageUrlFromHtml(html);
            if (!string.IsNullOrEmpty(imageUrl))
                return (await DownloadImageFromUrl(imageUrl), true);
        }

        return (null, false);
    }
#pragma warning restore CS0618

    private static async Task<string?> DownloadImageFromUrl(string url)
    {
        try
        {
            var factory = Startup.Services.GetRequiredService<System.Net.Http.IHttpClientFactory>();
            using var httpClient = factory.CreateClient("MediaDropClient");

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var extension = "png";
            if (response.Content.Headers.ContentType?.MediaType is { } mediaType)
            {
                extension = mediaType.ToLower() switch
                {
                    "image/jpeg" => "jpg",
                    "image/png" => "png",
                    "image/gif" => "gif",
                    "image/bmp" => "bmp",
                    "image/webp" => "webp",
                    _ => Path.GetExtension(url).TrimStart('.').ToLower()
                };
            }
            else
            {
                extension = Path.GetExtension(url).TrimStart('.').ToLower();
            }

            if (string.IsNullOrEmpty(extension))
                extension = "png";

            var tempPath = Path.Combine(Path.GetTempPath(), $"dropped_image_{Guid.NewGuid()}.{extension}");
            await using var fileStream = File.Create(tempPath);
            await response.Content.CopyToAsync(fileStream);
            return tempPath;
        }
        catch { }
        return null;
    }

    private static string? ExtractImageUrlFromHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return null;
        var match = System.Text.RegularExpressions.Regex.Match(
            html, @"<img[^>]+src=[""']([^""']+)[""']",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    #endregion

    #region Visual Tree

    private MediaPreviewViewModel? FindMediaPreviewViewModel()
    {
        Control? current = this.Parent as Control;
        while (current != null)
        {
            if (current.DataContext is MediaPreviewViewModel vm)
                return vm;
            current = current.Parent as Control;
        }
        return null;
    }

    #endregion
}