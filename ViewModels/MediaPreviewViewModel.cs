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
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class MediaPreviewViewModel : ViewModelBase, IDisposable
{
    #region Private Fields
    private static readonly Lazy<Task<LibVLC?>> _libVlcInit = new(
        () => Task.Run(CreateLibVLC), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly SharedDataService _sharedData = SharedDataService.Instance;
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
    #endregion

    #region Public Properties
    public bool IsMediaPreviewEnabled => !_sharedData.IsScraping && !_sharedData.IsBusy;
    public bool IsScraping => _sharedData.IsScraping;
    public bool VideoAutoplay => _sharedData.VideoAutoplay;
    public ObservableCollection<MediaItemViewModel> MediaItems { get; } = new();
    public LibVLC? LibVLC { get; private set; }
    public static bool IsLibVLCInstalled { get; private set; } = true;
    #endregion

    #region Constructor
    public MediaPreviewViewModel()
    {
        _sharedData.PropertyChanged += OnSharedDataPropertyChanged;
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

    // Forces the image panel for the given media type to re-read its file from
    // disk — used after an in-place file operation such as background removal.
    public void RefreshMedia(string mediaType)
    {
        if (SelectedGame == null) return;
        var mediaItem = MediaItems.FirstOrDefault(m => m.MediaType == mediaType);
        if (mediaItem == null) return;
        var currentPath = SelectedGame.GetValue(mediaItem.PathKey)?.ToString();
        SelectedGame.SetValue(mediaItem.PathKey, null);
        SelectedGame.SetValue(mediaItem.PathKey, currentPath);
    }

    // Updates the game metadata path for a media item to a new full path.
    // Used when an operation changes the file extension (e.g. JPEG background
    // removal saves as PNG). Marks data as changed so the user is prompted to save.
    public void UpdateMediaPath(string mediaType, string fullPath)
    {
        if (SelectedGame == null) return;
        var mediaItem = MediaItems.FirstOrDefault(m => m.MediaType == mediaType);
        if (mediaItem == null) return;
        var relativePath = FilePathHelper.PathToRelativePathWithDotSlashPrefix(fullPath, _sharedData.GamelistDirectory);
        SelectedGame.SetValue(mediaItem.PathKey, null);
        SelectedGame.SetValue(mediaItem.PathKey, relativePath);
        _sharedData.IsDataChanged = true;
    }

    public void InitializeLibVLC()
    {
        if (IsLibVLCInitialized && LibVLC != null)
        {
            InitializeVideosForCurrentGame();
            return;
        }

        _ = _libVlcInit.Value.ContinueWith(t =>
        {
            if (_disposed) return;

            LibVLC = t.Result;
            IsLibVLCInitialized = LibVLC != null;
            IsLibVLCMissing = LibVLC == null;

            if (IsLibVLCInitialized)
                InitializeVideosForCurrentGame();
        }, TaskScheduler.FromCurrentSynchronizationContext());
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

        // Clearing media
        if (newPath == null)
        {
            if (mediaItem.IsVideo)
                mediaItem.DisposeVideoPlayer();
            SelectedGame.SetValue(mediaItem.PathKey, string.Empty);
            _sharedData.IsDataChanged = true;
            SetStatus($"Successfully cleared {mediaType}", "ok");
            return;
        }

        var mediaSettings = _sharedData.MediaSettings.GetValueOrDefault(mediaItem.MediaTypeKey);
        var gamelistDir = _sharedData.GamelistDirectory;

        if (mediaSettings == null || string.IsNullOrEmpty(gamelistDir))
        {
            SetStatus($"Media path not configured for {mediaType}", "error");
            return;
        }

        try
        {
            // Resolve destination folder
            var destFolder = FilePathHelper.GamelistPathToFullPath(mediaSettings.Path, gamelistDir);
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);

            // Build destination filename
            var romPath = SelectedGame.GetValue(MetaDataKeys.path)?.ToString() ?? string.Empty;
            var romName = FilePathHelper.NormalizeRomName(romPath);
            var extension = Path.GetExtension(newPath);
            var suffix = mediaSettings.SfxEnabled && !string.IsNullOrEmpty(mediaSettings.Suffix)
                ? $"-{mediaSettings.Suffix}"
                : string.Empty;
            var destFileName = $"{romName}{suffix}{extension}";
            var destFullPath = Path.Combine(destFolder, destFileName);

            // Copy via temp to avoid corrupting existing file if copy fails
            var tempPath = destFullPath + ".tmp";
            await Task.Run(() =>
            {
                File.Copy(newPath, tempPath, overwrite: true);
                File.Move(tempPath, destFullPath, overwrite: true);
            });

            // Convert to gamelist-relative path and store
            var relativePath = FilePathHelper.PathToRelativePathWithDotSlashPrefix(destFullPath, gamelistDir);

            SelectedGame.SetValue(mediaItem.PathKey, null);
            SelectedGame.SetValue(mediaItem.PathKey, relativePath);
            _sharedData.IsDataChanged = true;


            // Refresh video player if needed
            if (mediaItem.IsVideo)
            {
                mediaItem.DisposeVideoPlayer();
                if (mediaItem.FileExists)
                    mediaItem.InitializeVideoPlayer(LibVLC, _sharedData.VideoAutoplay);
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
        _sharedData.PropertyChanged -= OnSharedDataPropertyChanged;

        foreach (var item in MediaItems)
        {
            item.AttachGame(null);
            if (item.IsVideo)
                item.DisposeVideoPlayer();
        }

        LibVLC = null;
    }
    #endregion

    #region Private Methods
    private static LibVLC? CreateLibVLC()
    {
        try
        {
            return new LibVLC(
                "--no-video-title-show",
                "--no-stats",
                "--no-snapshot-preview",
                "--no-sub-autodetect-file"
            );
        }
        catch (Exception ex)
        {
            IsLibVLCInstalled = false;
            System.Diagnostics.Debug.WriteLine($"[MediaPreview] LibVLC init failed: {ex.Message}");
            return null;
        }
    }

    private void InitializeVideosForCurrentGame()
    {
        if (!IsLibVLCInitialized || LibVLC == null) return;

        var autoPlay = _sharedData.VideoAutoplay;
        foreach (var item in MediaItems.Where(m => m.IsVideo && m.FileExists && m.MediaPlayer == null))
            item.InitializeVideoPlayer(LibVLC, autoPlay);
    }

    private void InitializeMediaItems()
    {
        MediaItems.Clear();
        foreach (var metadata in GamelistMetaData.GetMediaMetadata())
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
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateVisibility();
                UpdateSelectionStatus(SelectedGame);
            });
        }
    }

    private void UpdateMediaItems(GameMetadataRow? game)
    {
        foreach (var item in MediaItems.Where(m => m.IsVideo))
            item.DisposeVideoPlayer();

        foreach (var item in MediaItems)
            item.AttachGame(game);

        UpdateVisibility();

        if (game != null && IsLibVLCInitialized && LibVLC != null)
            InitializeVideosForCurrentGame();
    }

    private void UpdateVisibility()
    {
        foreach (var item in MediaItems)
        {
            bool newVisible;
            if (item.HasMedia)
                newVisible = true;
            else if (ShowAllMedia)
            {
                newVisible = _sharedData.MediaSettings.TryGetValue(item.MediaTypeKey, out var ms) && ms.Enabled;
            }
            else
                newVisible = false;

            item.IsVisible = newVisible;
        }
    }

    private void OnSharedDataPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SharedDataService.IsScraping):
            case nameof(SharedDataService.IsBusy):
                OnPropertyChanged(nameof(IsScraping));
                OnPropertyChanged(nameof(IsMediaPreviewEnabled));
                ScrapeGameCommand.NotifyCanExecuteChanged();
                break;
        }
    }

    private void UpdateSelectionStatus(GameMetadataRow? game)
    {
        if (game == null)
        {
            SetStatus("No game selected", null);
            return;
        }

        int total = MediaItems.Count(m => m.HasMedia);
        int found = MediaItems.Count(m => m.HasMedia && m.FileExists);
        int missing = total - found;

        var gameName = game.GetValue(MetaDataKeys.name)?.ToString() ?? string.Empty;
        string icon = missing > 0 ? "error" : "ok";
        string text = missing > 0
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