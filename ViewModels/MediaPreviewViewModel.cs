using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class MediaPreviewViewModel : ViewModelBase, IDisposable
{
    #region Private Fields

    private static readonly Lazy<Task<LibVLC?>> _libVlcInit = new(
        () => Task.Run(CreateLibVLC), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly SessionState _sessionState = SessionState.Instance;
    private readonly SettingsState _settingsState = SettingsState.Instance;
    private CancellationTokenSource _videoInitCts = new();
    private bool _disposed;

    #endregion

    #region Observable Properties

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScrapeGameCommand))]
    private GameMetadataRow? _selectedGame;
    [ObservableProperty] private bool _scaledDisplay = true;
    [ObservableProperty] private bool _showAllMedia;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private bool _statusIconVisible;
    [ObservableProperty] private bool _statusIsOk;
    [ObservableProperty] private bool _statusIsError;
    [ObservableProperty] private string _scraperStatusText = string.Empty;
    [ObservableProperty] private bool _scraperStatusIconVisible;
    [ObservableProperty] private bool _scraperStatusIsOk;
    [ObservableProperty] private bool _scraperStatusIsError;
    [ObservableProperty] private bool _isLibVLCMissing;
    [ObservableProperty] private bool _isLibVLCInitialized;
    [ObservableProperty] private bool _overwriteMedia;
    [ObservableProperty] private bool _overwriteMetadata;
    [ObservableProperty] private bool _showNoMediaLogo;

    #endregion

    #region Public Properties

    public bool IsMediaPreviewEnabled => !_sessionState.IsScraping && !_sessionState.IsBusy;
    public bool IsScraping => _sessionState.IsScraping;
    public bool VideoAutoplay => _settingsState.VideoAutoplay;

    public ObservableCollection<MediaItemViewModel> MediaItems { get; } = new();
    public LibVLC? LibVLC { get; private set; }
    public static bool IsLibVLCInstalled { get; private set; } = true;

    public static void MarkLibVLCUnavailable() => IsLibVLCInstalled = false;

    #endregion

    #region Private Properties

    private bool EffectiveAutoPlay => _settingsState.VideoAutoplay && !_sessionState.VideoUserPaused;

    #endregion

    #region Constructor

    public MediaPreviewViewModel()
    {
        _sessionState.PropertyChanged += OnSessionStatePropertyChanged;
        InitializeMediaItems();
    }

    #endregion

    #region Property Change Callbacks

    partial void OnSelectedGameChanged(GameMetadataRow? value)
    {
        SetScraperStatus(string.Empty, null);
        UpdateMediaItems(value);
        UpdateSelectionStatus(value);
    }

    partial void OnShowAllMediaChanged(bool value)
    {
        _ = value;
        UpdateVisibility();
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void ToggleScaleMode() => ScaledDisplay = !ScaledDisplay;

    [RelayCommand]
    private void ToggleShowAll() => ShowAllMedia = !ShowAllMedia;

    #endregion

    #region Public Methods

    public void OnViewReady() => InitializeLibVLC();

    public static Task PreloadLibVLCAsync() => _libVlcInit.Value;

    public void RefreshMedia(string mediaType)
    {
        if (SelectedGame == null) return;
        var mediaItem = MediaItems.FirstOrDefault(m => m.MediaType == mediaType);
        if (mediaItem == null) return;
        var currentPath = SelectedGame.GetValue(mediaItem.PathKey)?.ToString();
        SelectedGame.SetValue(mediaItem.PathKey, null);
        SelectedGame.SetValue(mediaItem.PathKey, currentPath);
    }

    public void UpdateMediaPath(string mediaType, string fullPath)
    {
        if (SelectedGame == null) return;
        var mediaItem = MediaItems.FirstOrDefault(m => m.MediaType == mediaType);
        if (mediaItem == null) return;
        var romFolder = FilePathHelper.CurrentRomFolder(_settingsState.RomsFolder, _sessionState.CurrentSystem);
        var relativePath = FilePathHelper.PathToRelativePathWithDotSlashPrefix(fullPath, romFolder!);
        SelectedGame.SetValue(mediaItem.PathKey, null);
        SelectedGame.SetValue(mediaItem.PathKey, relativePath);
        _sessionState.IsDataChanged = true;
    }

    public async void InitializeLibVLC()
    {
        if (IsLibVLCInitialized && LibVLC != null)
        {
            InitializeVideosForCurrentGame();
            return;
        }

        var token = _videoInitCts.Token;
        var libVlc = await _libVlcInit.Value.ConfigureAwait(false);

        if (_disposed || token.IsCancellationRequested) return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            LibVLC = libVlc;
            IsLibVLCInitialized = LibVLC != null;
            IsLibVLCMissing = LibVLC == null;

            if (IsLibVLCInitialized)
                InitializeVideosForCurrentGame(token);
        });
    }

    public void SuspendVideo()
    {
        foreach (var item in MediaItems.Where(m => m.IsVideo))
            item.DisposeVideoPlayer();
    }

    public async Task UpdateGameMedia(string mediaType, string? newPath)
    {
        if (SelectedGame == null) return;

        var mediaItem = MediaItems.FirstOrDefault(m => m.MediaType == mediaType);
        if (mediaItem == null) return;

        if (newPath == null)
        {
            if (mediaItem.IsVideo)
                mediaItem.DisposeVideoPlayer();
            SelectedGame.SetValue(mediaItem.PathKey, string.Empty);
            _sessionState.IsDataChanged = true;
            SetStatus($"Successfully cleared {mediaType}", "ok");
            return;
        }

        var mediaFolder = _sessionState.AvailableMedia.FirstOrDefault(m => m.Type == mediaItem.MediaTypeKey);
        var romFolder = FilePathHelper.CurrentRomFolder(_settingsState.RomsFolder, _sessionState.CurrentSystem);

        if (mediaFolder == null || string.IsNullOrEmpty(romFolder))
        {
            SetStatus($"Media path not configured for {mediaType}", "error");
            return;
        }

        try
        {
            var destFolder = mediaFolder.FolderPath;
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);

            var romPath = SelectedGame.GetValue(MetaDataKeys.path)?.ToString() ?? string.Empty;
            var romName = FilePathHelper.NormalizeRomName(romPath);
            var extension = Path.GetExtension(newPath);
            var suffix = mediaFolder.SfxEnabled && !string.IsNullOrEmpty(mediaFolder.Suffix)
                ? $"-{mediaFolder.Suffix}"
                : string.Empty;
            var destFileName = $"{romName}{suffix}{extension}";
            var destFullPath = Path.Combine(destFolder, destFileName);
            var tempPath = destFullPath + ".tmp";

            await Task.Run(() =>
            {
                File.Copy(newPath, tempPath, overwrite: true);
                File.Move(tempPath, destFullPath, overwrite: true);
            });

            var relativePath = FilePathHelper.PathToRelativePathWithDotSlashPrefix(destFullPath, romFolder);
            SelectedGame.SetValue(mediaItem.PathKey, null);
            SelectedGame.SetValue(mediaItem.PathKey, relativePath);
            _sessionState.IsDataChanged = true;

            if (mediaItem.IsVideo)
            {
                mediaItem.DisposeVideoPlayer();
                if (mediaItem.FileExists)
                    mediaItem.InitializeVideoPlayer(LibVLC, EffectiveAutoPlay);
            }

            SetStatus($"Successfully added {mediaType}", "ok");
        }
        catch (Exception ex)
        {
            SetStatus($"Error adding {mediaType}: {ex.Message}", "error");
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _videoInitCts.Cancel();
        _videoInitCts.Dispose();
        _sessionState.PropertyChanged -= OnSessionStatePropertyChanged;

        foreach (var item in MediaItems)
        {
            item.AttachGame(null);
            if (item.IsVideo)
                item.DisposeVideoPlayer();
        }

        LibVLC?.Dispose();
        LibVLC = null;
    }

    #endregion

    #region Private Methods

    private static LibVLC? CreateLibVLC()
    {
        try
        {
            var options = new List<string>
            {
                "--no-video-title-show",
                "--no-stats",
                "--no-snapshot-preview",
                "--no-sub-autodetect-file"
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                options.Add("--aout=pulse");

            return new LibVLC(options.ToArray());
        }
        catch
        {
            IsLibVLCInstalled = false;
            return null;
        }
    }

    private void InitializeVideosForCurrentGame(CancellationToken token = default)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => InitializeVideosForCurrentGame(token));
            return;
        }

        if (token.IsCancellationRequested) return;
        if (!IsLibVLCInitialized || LibVLC == null) return;

        var autoPlay = EffectiveAutoPlay;
        foreach (var item in MediaItems.Where(m => m.IsVideo && m.FileExists && m.MediaPlayer == null))
        {
            if (token.IsCancellationRequested) return;
            item.InitializeVideoPlayer(LibVLC, autoPlay);
        }
    }

    private void InitializeMediaItems()
    {
        MediaItems.Clear();
        foreach (var metadata in MetadataService.GetMediaMetadata())
        {
            var isVideo = metadata.DataType == MetaDataType.Video;
            var isManual = metadata.DataType == MetaDataType.Document;
            var item = new MediaItemViewModel(metadata.Name, metadata.Type, metadata.Key, isVideo, isManual);
            item.PropertyChanged += OnMediaItemPropertyChanged;
            MediaItems.Add(item);
        }
        UpdateVisibility();
    }

    private void OnMediaItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MediaItemViewModel.HasMedia))
        {
            Dispatcher.UIThread.Post(() =>
            {
                UpdateVisibility();
                UpdateSelectionStatus(SelectedGame);
            });
        }
    }

    private void UpdateMediaItems(GameMetadataRow? game)
    {
        _videoInitCts.Cancel();
        _videoInitCts.Dispose();
        _videoInitCts = new CancellationTokenSource();
        var token = _videoInitCts.Token;
        var autoPlay = EffectiveAutoPlay;

        foreach (var item in MediaItems.Where(m => m.IsVideo))
        {
            var newPath = game?.GetValue(item.PathKey)?.ToString();
            var hasNewPath = !string.IsNullOrEmpty(newPath);

            if (!hasNewPath)
                item.DisposeVideoPlayer();
            else if (item.MediaPlayer != null && LibVLC != null)
                item.ChangeMedia(LibVLC, newPath!, autoPlay);
        }

        foreach (var item in MediaItems)
            item.AttachGame(game);

        UpdateVisibility();

        if (game != null && IsLibVLCInitialized && LibVLC != null)
            InitializeVideosForCurrentGame(token);
    }

    private void UpdateVisibility()
    {
        foreach (var item in MediaItems)
        {
            item.IsVisible = item.HasMedia || (ShowAllMedia && _sessionState.AvailableMedia.Any(m => m.Type == item.MediaTypeKey));
        }

        ShowNoMediaLogo = SelectedGame != null
            && !ShowAllMedia
            && MediaItems.All(m => !m.HasMedia);
    }

    private void OnSessionStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SessionState.IsScraping):
                OnPropertyChanged(nameof(IsScraping));
                OnPropertyChanged(nameof(IsMediaPreviewEnabled));
                ScrapeGameCommand.NotifyCanExecuteChanged();
                break;
            case nameof(SessionState.IsBusy):
                OnPropertyChanged(nameof(IsMediaPreviewEnabled));
                ScrapeGameCommand.NotifyCanExecuteChanged();
                break;
        }
    }

    private void UpdateSelectionStatus(GameMetadataRow? game)
    {
        if (game == null)
        {
            SetStatus("No item selected", null);
            return;
        }

        int total = MediaItems.Count(m => m.HasMedia);
        int found = MediaItems.Count(m => m.HasMedia && m.FileExists);
        int missing = total - found;

        var gameName = game.GetValue(MetaDataKeys.name)?.ToString() ?? string.Empty;
        var icon = missing > 0 ? "error" : "ok";
        var text = missing > 0
            ? $"{gameName}  —  {found}/{total} media files found,  {missing} missing"
            : total > 0
                ? $"{gameName}  —  {total} media files found"
                : $"{gameName}  —  no media";

        SetStatus(text, icon);
    }

    private void SetStatus(string message, string? icon)
    {
        StatusText = message;
        StatusIconVisible = icon != null;
        StatusIsOk = icon == "ok";
        StatusIsError = icon == "error";
    }

    private void SetScraperStatus(string message, string? icon)
    {
        ScraperStatusText = message;
        ScraperStatusIconVisible = icon != null;
        ScraperStatusIsOk = icon == "ok";
        ScraperStatusIsError = icon == "error";
    }

    #endregion
}