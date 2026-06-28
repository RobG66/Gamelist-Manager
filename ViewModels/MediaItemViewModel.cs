using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Native.Mpv;
using Gamelist_Manager.Services;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class MediaItemViewModel : ObservableObject, IDisposable
{
    #region Fields & Constants
    private const double ThumbnailPositionSeconds = 1.0;

    private readonly SessionState _sessionState = SessionState.Instance;
    private readonly SettingsState _settingsState = SettingsState.Instance;
    private volatile bool _previewSeekPending;
    private GameMetadataRow? _game;
    private readonly MetaDataKeys _pathKey;
    private readonly string _mediaTypeKey;
    private bool _disposed;
    #endregion

    #region Observable Properties
    [ObservableProperty] private string _mediaType = string.Empty;
    [ObservableProperty] private string? _mediaPath;
    [ObservableProperty] private Bitmap? _imageBitmap;
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
    [ObservableProperty] private MpvContext? _mediaPlayer;
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

    #region Public Methods
    public void AttachGame(GameMetadataRow? game)
    {
        if (_game != null)
            _game.PropertyChanged -= OnGamePropertyChanged;

        _game = game;

        if (_game != null)
            _game.PropertyChanged += OnGamePropertyChanged;

        RefreshFromGame();
    }

    public void UpdateImageBitmap()
    {
        var old = ImageBitmap;
        ImageBitmap = null;
        old?.Dispose();

        if (!IsVideo && !IsManual && FileExists && !string.IsNullOrWhiteSpace(MediaPath))
        {
            var fullPath = ResolveFullPath(MediaPath);
            ImageBitmap = ImageHelper.LoadImageWithoutLock(fullPath);
        }
    }

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

    public void InitializeVideoPlayer(bool autoPlay = false)
    {
        if (!IsVideo || string.IsNullOrWhiteSpace(MediaPath)) return;
        if (MediaPlayer != null) return;

        try
        {
            var fullPath = ResolveFullPath(MediaPath);
            if (!File.Exists(fullPath)) return;

            var player = MpvService.CreateContext();
            if (player == null) return;

            player.SetPropertyString("volume", _settingsState.DefaultVolume.ToString(System.Globalization.CultureInfo.InvariantCulture));

            if (!autoPlay)
            {
                _previewSeekPending = true;
                player.SetMute(true);
                player.Pause();
                Avalonia.Threading.Dispatcher.UIThread.Post(() => IsPlaying = false);
            }
            else
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => IsPlaying = true);
            }

            player.FileLoaded += () => OnPlayerFileLoaded(player, autoPlay);
            player.EndFile += () => Avalonia.Threading.Dispatcher.UIThread.Post(() => IsPlaying = false);

            MediaPlayer = player;

            if (player.IsRenderContextAttached)
            {
                player.LoadFile(fullPath, "replace");
                if (autoPlay) player.Play();
            }
            else
            {
                player.RenderContextAttached = () =>
                {
                    player.RenderContextAttached = null;
                    player.LoadFile(fullPath, "replace");
                    if (autoPlay) player.Play();
                };
            }
        }
        catch
        {
            DisposeVideoPlayer();
        }
    }

    public void ChangeMedia(string newPath, bool autoPlay)
    {
        if (MediaPlayer == null) return;

        var fullPath = ResolveFullPath(newPath);
        if (!File.Exists(fullPath))
        {
            DisposeVideoPlayer();
            return;
        }

        var player = MediaPlayer;

        if (!autoPlay)
        {
            _previewSeekPending = true;
            player.SetMute(true);
            player.Pause();
            Avalonia.Threading.Dispatcher.UIThread.Post(() => IsPlaying = false);
        }
        else
        {
            player.SetMute(false);
            player.SetVolume(_settingsState.DefaultVolume);
            Avalonia.Threading.Dispatcher.UIThread.Post(() => IsPlaying = true);
        }

        if (player.IsRenderContextAttached)
        {
            player.LoadFile(fullPath, "replace");
            if (autoPlay) player.Play();
        }
        else
        {
            player.RenderContextAttached = () =>
            {
                player.RenderContextAttached = null;
                player.LoadFile(fullPath, "replace");
                if (autoPlay) player.Play();
            };
        }
    }

    public void Play()
    {
        if (MediaPlayer == null || !FileExists) return;
        try
        {
            MediaPlayer.SetMute(false);
            MediaPlayer.SetVolume(_settingsState.DefaultVolume);
            MediaPlayer.Play();
            Avalonia.Threading.Dispatcher.UIThread.Post(() => IsPlaying = true);
        }
        catch { }
    }

    public void Stop()
    {
        if (MediaPlayer == null) return;
        try 
        { 
            MediaPlayer.Stop(); 
            Avalonia.Threading.Dispatcher.UIThread.Post(() => IsPlaying = false);
        }
        catch { }
    }

    public void TogglePlayback()
    {
        if (MediaPlayer == null || !FileExists) return;
        try
        {
            if (MediaPlayer.IsPlaying)
            {
                MediaPlayer.Pause();
                _sessionState.VideoUserPaused = true;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => IsPlaying = false);
            }
            else
            {
                _sessionState.VideoUserPaused = false;
                MediaPlayer.SetMute(false);
                MediaPlayer.SetVolume(_settingsState.DefaultVolume);
                MediaPlayer.Play();
                Avalonia.Threading.Dispatcher.UIThread.Post(() => IsPlaying = true);
            }
        }
        catch { }
    }

    public void SetVolume(int volume)
    {
        if (MediaPlayer == null) return;
        try { MediaPlayer.SetVolume(volume); }
        catch { }
    }

    public void DisposeVideoPlayer()
    {
        try
        {
            var player = MediaPlayer;
            IsPlaying = false;
            MediaPlayer = null;

            if (player != null)
            {
                _ = Task.Run(() =>
                {
                    try { player.Stop(); } catch { }
                    try { player.Dispose(); } catch { }
                });
            }
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        AttachGame(null);
        DisposeVideoPlayer();
        var bmp = ImageBitmap;
        ImageBitmap = null;
        bmp?.Dispose();
    }

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

    internal string ResolveFullPath(string path)
    {
        var currentMediaDirectory = _sessionState.CurrentMediaFolder;

        return !string.IsNullOrEmpty(currentMediaDirectory)
            ? FilePathHelper.GamelistPathToFullPath(path, currentMediaDirectory)
            : path;
    }

    #endregion

    #region Private Methods

    private void OnPlayerFileLoaded(MpvContext player, bool autoPlay)
    {
        if (_previewSeekPending)
        {
            _previewSeekPending = false;
            try
            {
                player.Seek(ThumbnailPositionSeconds);
            }
            catch { }
        }

        if (autoPlay)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => IsPlaying = true);
        }
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
        UpdateImageBitmap();

        if (IsVideo && newPath == null)
            DisposeVideoPlayer();

        OnPropertyChanged(nameof(HasMedia));
    }

    #endregion
}
