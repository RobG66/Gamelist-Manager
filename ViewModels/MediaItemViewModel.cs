using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using LibVLCSharp.Shared;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace Gamelist_Manager.ViewModels;

public partial class MediaItemViewModel : ObservableObject, IDisposable
{
    #region Private Fields
    private readonly SharedDataService _sharedData = SharedDataService.Instance;
    private volatile bool _previewSeekPending;
    private GameMetadataRow? _game;
    private readonly MetaDataKeys _pathKey;
    private readonly string _mediaTypeKey;
    private bool _disposed;
    private CancellationTokenSource _changeMediaCts = new();
    #endregion

    #region Observable Properties
    [ObservableProperty] private string _mediaType = string.Empty;
    [ObservableProperty] private string? _mediaPath;
    [ObservableProperty] private bool _hasMedia;
    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsImageMedia))]
    private bool _isVideo;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsImageMedia))]
    private bool _isManual;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsImageMedia))]
    private bool _fileExists;
    [ObservableProperty] private bool _showMissingIcon;
    [ObservableProperty] private bool _showDropIcon;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private bool _isDragOver;
    [ObservableProperty] private MediaPlayer? _mediaPlayer;
    [ObservableProperty] private Media? _media;
    #endregion

    #region Public Properties
    public MetaDataKeys PathKey => _pathKey;
    public string MediaTypeKey => _mediaTypeKey;
    public bool IsImageMedia => FileExists && !IsVideo && !IsManual;
    #endregion

    #region Constructor
    public MediaItemViewModel(string mediaType, string mediaTypeKey, MetaDataKeys pathKey, bool isVideo = false, bool isManual = false)
    {
        MediaType = mediaType;
        _mediaTypeKey = mediaTypeKey;
        _pathKey = pathKey;
        IsVideo = isVideo;
        IsManual = isManual;
        UpdateFileExistence();
    }
    #endregion

    #region Game Attachment
    public void AttachGame(GameMetadataRow? game)
    {
        if (_game != null)
            _game.PropertyChanged -= OnGamePropertyChanged;

        _game = game;

        if (_game != null)
            _game.PropertyChanged += OnGamePropertyChanged;

        RefreshFromGame();
    }

    private void OnGamePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == $"Item[{_pathKey}]")
            RefreshFromGame();
    }

    private void RefreshFromGame()
    {
        if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            RefreshFromGameCore();
        else
            Avalonia.Threading.Dispatcher.UIThread.Post(RefreshFromGameCore);
    }

    private void RefreshFromGameCore()
    {
        var newPath = _game?.GetValue(_pathKey)?.ToString();
        if (string.IsNullOrEmpty(newPath))
            newPath = null;

        MediaPath = null;
        MediaPath = newPath;
        UpdateFileExistence();

        if (IsVideo && newPath == null)
            DisposeVideoPlayer();

        OnPropertyChanged(nameof(HasMedia));
    }
    #endregion

    #region Public Methods
    public void UpdateFileExistence()
    {
        HasMedia = !string.IsNullOrWhiteSpace(MediaPath);

        if (string.IsNullOrWhiteSpace(MediaPath))
        {
            FileExists = false;
            ShowMissingIcon = false;
            ShowDropIcon = !IsVideo;
            return;
        }

        FileExists = File.Exists(ResolveFullPath(MediaPath));
        ShowMissingIcon = HasMedia && !FileExists;
        ShowDropIcon = false;
    }

    public void InitializeVideoPlayer(LibVLC? libVlc, bool autoPlay = false)
    {
        if (!IsVideo || libVlc == null || string.IsNullOrWhiteSpace(MediaPath)) return;
        if (MediaPlayer != null) return;

        try
        {
            var fullPath = ResolveFullPath(MediaPath);
            if (!File.Exists(fullPath)) return;

            Media = new Media(libVlc, fullPath);
            Media.AddOption(":input-repeat=65535");
            Media.AddOption(":file-caching=300");

            MediaPlayer = new MediaPlayer(libVlc)
            {
                Media = Media,
                Volume = _sharedData.DefaultVolume,
                EnableHardwareDecoding = true
            };

            MediaPlayer.Playing += OnMediaPlayerPlaying;
            MediaPlayer.Stopped += OnMediaPlayerStopped;

            if (!autoPlay)
            {
                Media.AddOption(":start-time=5");
                _previewSeekPending = true;
                MediaPlayer.Volume = 0;
            }
        }
        catch
        {
            DisposeVideoPlayer();
        }
    }

    // Swaps the media source on the existing player without tearing down the native
    // VLC player object. Used on datagrid row changes where the slot stays in place
    // but points at a different game's file.
    public void ChangeMedia(LibVLC libVlc, string newPath, bool autoPlay)
    {
        if (MediaPlayer == null) return;

        var fullPath = ResolveFullPath(newPath);
        if (!File.Exists(fullPath))
        {
            DisposeVideoPlayer();
            return;
        }

        // Cancel any in-flight ChangeMedia for this slot
        _changeMediaCts.Cancel();
        _changeMediaCts.Dispose();
        _changeMediaCts = new CancellationTokenSource();
        var token = _changeMediaCts.Token;

        var oldMedia = Media;

        var newMedia = new Media(libVlc, fullPath);
        newMedia.AddOption(":input-repeat=65535");
        newMedia.AddOption(":file-caching=300");

        if (!autoPlay)
        {
            newMedia.AddOption(":start-time=5");
            _previewSeekPending = true;
            MediaPlayer.Volume = 0;
        }
        else
        {
            MediaPlayer.Volume = _sharedData.DefaultVolume;
        }

        // Swap the media reference on the UI thread before handing off,
        // so bindings update immediately even while the background op is pending.
        MediaPlayer.Media = newMedia;
        Media = newMedia;

        _ = System.Threading.Tasks.Task.Run(() =>
        {
            // Stop blocks until the native player is fully quiesced.
            try { MediaPlayer?.Stop(); } catch { }

            if (token.IsCancellationRequested)
            {
                // A newer ChangeMedia took over — dispose both and exit.
                try { oldMedia?.Dispose(); } catch { }
                try { newMedia?.Dispose(); } catch { }
                return;
            }

            try { oldMedia?.Dispose(); } catch { }
            try { MediaPlayer?.Play(); } catch { }
        });
    }

    public void Play()
    {
        if (MediaPlayer == null || !FileExists) return;
        try { MediaPlayer.Play(); }
        catch { }
    }

    public void Stop()
    {
        if (MediaPlayer == null) return;
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            try { MediaPlayer?.Stop(); }
            catch { }
        });
    }

    public void TogglePlayback()
    {
        if (MediaPlayer == null || !FileExists) return;
        try
        {
            if (MediaPlayer.IsPlaying)
            {
                MediaPlayer.Pause();
                Avalonia.Threading.Dispatcher.UIThread.Post(() => IsPlaying = false);
            }
            else
            {
                MediaPlayer.Volume = _sharedData.DefaultVolume;
                MediaPlayer.Play();
            }
        }
        catch { }
    }

    public void SetVolume(int volume)
    {
        if (MediaPlayer == null) return;
        try { MediaPlayer.Volume = Math.Clamp(volume, 0, 100); }
        catch { }
    }

    public void DisposeVideoPlayer()
    {
        // Cancel any in-flight ChangeMedia before tearing down.
        _changeMediaCts.Cancel();

        try
        {
            var player = MediaPlayer;
            var media = Media;

            if (player != null)
            {
                player.Playing -= OnMediaPlayerPlaying;
                player.Stopped -= OnMediaPlayerStopped;
            }

            IsPlaying = false;
            MediaPlayer = null;
            Media = null;

            // Stop and dispose on a background thread — player.Stop() is a blocking
            // native VLC call that can take 1-2 seconds and must not run on the UI thread.
            if (player != null || media != null)
            {
                _ = System.Threading.Tasks.Task.Run(() =>
                {
                    try { player?.Stop(); } catch { }
                    try { media?.Dispose(); } catch { }
                    try { player?.Dispose(); } catch { }
                });
            }
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _changeMediaCts.Cancel();
        _changeMediaCts.Dispose();
        AttachGame(null);
        DisposeVideoPlayer();
    }
    #endregion

    #region Drop Validation
    public bool IsValidDrop(string filePath)
    {
        if (IsVideo) return IsVideoFile(filePath);
        if (IsManual) return IsPdfFile(filePath);
        return IsImageFile(filePath);
    }

    public static bool IsImageFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff" or ".tif" or ".webp";
    }

    public static bool IsVideoFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm" or ".m4v";
    }

    public static bool IsPdfFile(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() == ".pdf";
    }
    #endregion

    #region Internal Methods
    internal string ResolveFullPath(string path)
    {
        if (Path.IsPathRooted(path)) return path;

        var gamelistDirectory = _sharedData.GamelistDirectory;
        return !string.IsNullOrEmpty(gamelistDirectory)
            ? FilePathHelper.GamelistPathToFullPath(path, gamelistDirectory)
            : path;
    }
    #endregion

    #region Private Methods

    // VLC raises these events on its own internal native threads, never on the UI thread.
    // Any Avalonia observable property change must be dispatched to the UI thread to avoid
    // cross-thread binding updates, which can deadlock on Linux.

    private void OnMediaPlayerPlaying(object? sender, EventArgs e)
    {
        if (_previewSeekPending)
        {
            _previewSeekPending = false;
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    Thread.Sleep(350);
                    MediaPlayer?.SetPause(true);
                }
                catch { }
            });
        }
        else
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => IsPlaying = true);
        }
    }

    private void OnMediaPlayerStopped(object? sender, EventArgs e)
        => Avalonia.Threading.Dispatcher.UIThread.Post(() => IsPlaying = false);

    #endregion
}