using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class JukeboxViewModel : ViewModelBase, IAsyncDisposable
{
    private LibVLC? _videoLibVLC;
    private MediaPlayer? _videoMediaPlayer;

    private LibVLC? _audioLibVLC;
    private MediaPlayer? _audioMediaPlayer;

    private MediaPlayer? ActiveMediaPlayer => _visualizationsEnabled ? _audioMediaPlayer : _videoMediaPlayer;
    private LibVLC? ActiveLibVLC => _visualizationsEnabled ? _audioLibVLC : _videoLibVLC;

    private Media? _currentMedia;

    private string[] _mediaFiles = Array.Empty<string>();
    private readonly Random _random = new();
    private bool _isDisposed;
    private bool _visualizationsEnabled;
    
    private CancellationTokenSource? _scannerCts;
    private Task? _scannerTask;

    [ObservableProperty]
    private int _currentIndex;

    [ObservableProperty]
    private ObservableCollection<JukeboxTrack> _tracks = new();

    [ObservableProperty]
    private ObservableCollection<JukeboxTrack> _filteredTracks = new();

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private JukeboxTrack? _selectedTrack;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private bool _isStopped = true;

    [ObservableProperty]
    private bool _isRandomPlayback;

    [ObservableProperty]
    private double _volume = 75;

    [ObservableProperty]
    private bool _isPlaylistVisible;

    [ObservableProperty]
    private bool _hasMultipleTracks;

    [ObservableProperty]
    private bool _canPlay = true;

    [ObservableProperty]
    private bool _canPause;

    [ObservableProperty]
    private bool _canStop;

    public event Action<MediaPlayer?>? MediaPlayerCreated;
    public event Action<string>? ErrorOccurred;
    public event EventHandler? CloseRequested;

    public JukeboxViewModel()
    {
        InitializeVLC();
    }

    private void InitializeVLC()
    {
        try
        {
            var baseOptions = new[]
            {
                "--no-video-title-show",
                "--no-stats",
                "--no-snapshot-preview",
                "--no-sub-autodetect-file",
                "--network-caching=300",
                "--file-caching=300"
            };

            // 1. Initialize Video Player
            _videoLibVLC = new LibVLC(baseOptions);
            _videoMediaPlayer = new MediaPlayer(_videoLibVLC);
            _videoMediaPlayer.Volume = (int)Volume;
            _videoMediaPlayer.EndReached += OnEndReached;
            _videoMediaPlayer.TimeChanged += OnTimeChanged;

            // 2. Initialize Audio Player with Visualizations
            var audioOptions = baseOptions.ToList();
            audioOptions.Add("--audio-visual=visual");
            audioOptions.Add("--effect-width=50");
            audioOptions.Add("--effect-height=50");
            audioOptions.Add("--effect-list=spectrometer");

            var vlcDir = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vlc"));
            audioOptions.Add($"--plugin-path={vlcDir.FullName}");

            _audioLibVLC = new LibVLC(audioOptions.ToArray());
            _audioMediaPlayer = new MediaPlayer(_audioLibVLC);
            _audioMediaPlayer.Volume = (int)Volume;
            _audioMediaPlayer.EndReached += OnEndReached;
            _audioMediaPlayer.TimeChanged += OnTimeChanged;
        }
        catch (Exception ex)
        {
            RaiseError($"VLC Init Error: {ex.Message}");
        }
    }

    public void RequestPlayer()
    {
        MediaPlayerCreated?.Invoke(ActiveMediaPlayer);
    }

    [RelayCommand]
    private async Task Close()
    {
        Stop();
        CloseRequested?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void Play()
    {
        if (_isDisposed) return;

        if (ActiveMediaPlayer != null && IsPaused)
            Resume();
        else
            PlayCurrentTrack();
    }

    [RelayCommand]
    private void Pause()
    {
        if (_isDisposed || ActiveMediaPlayer == null) return;

        if (ActiveMediaPlayer.IsPlaying)
        {
            ActiveMediaPlayer.Pause();
            IsPaused = true;
            IsPlaying = false;
            IsStopped = false;
            UpdateButtons();
        }
    }

    [RelayCommand]
    private void Stop()
    {
        if (_isDisposed || ActiveMediaPlayer == null) return;

        if (ActiveMediaPlayer.IsPlaying || IsPaused)
        {
            ActiveMediaPlayer.Stop();
            _currentMedia?.Dispose();
            _currentMedia = null;
            IsPlaying = false;
            IsPaused = false;
            IsStopped = true;
            UpdateButtons();
        }
    }

    [RelayCommand]
    private void Previous() => GotoPreviousTrack();

    [RelayCommand]
    private void Next() => GotoNextTrack();

    [RelayCommand]
    private void TogglePlaylist()
    {
        if (_isDisposed) return;
        IsPlaylistVisible = !IsPlaylistVisible;
    }

    public async Task PlayMediaFilesAsync(string[] fileList, bool autoPlay)
    {
        if (_isDisposed) return;
        try
        {
            LoadPlaylist(fileList, singleFile: fileList.Length == 1);
            if (autoPlay)
                PlayCurrentTrack();
            await Task.CompletedTask;
        }
        catch (Exception ex) { RaiseError($"Error playing media files: {ex.Message}"); }
    }

    public async Task PlayMediaAsync(string fileName, bool autoPlay)
    {
        if (_isDisposed) return;
        try
        {
            LoadPlaylist(new[] { fileName }, singleFile: true);
            if (autoPlay)
                PlayCurrentTrack();
            else
            {
                PlayCurrentTrack();
                await Task.Delay(100);
                Pause();
            }
        }
        catch (Exception ex) { RaiseError($"Error playing media: {ex.Message}"); }
    }

    partial void OnVolumeChanged(double value)
    {
        if (_videoMediaPlayer != null) _videoMediaPlayer.Volume = (int)value;
        if (_audioMediaPlayer != null) _audioMediaPlayer.Volume = (int)value;
    }

    partial void OnSelectedTrackChanged(JukeboxTrack? value)
    {
        if (value == null || _isDisposed) return;
        if (value.Index == CurrentIndex && (IsPlaying || IsPaused)) return;

        CurrentIndex = value.Index;
        PlayTrack(value.FilePath);
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnCurrentIndexChanged(int value)
    {
        if (Tracks == null || value < 0 || value >= Tracks.Count) return;

        for (int i = 0; i < Tracks.Count; i++)
        {
            Tracks[i].IsPlaying = (i == value);
        }

        if (SelectedTrack != Tracks[value])
        {
            SelectedTrack = Tracks[value];
        }
    }

    private void LoadPlaylist(string[] files, bool singleFile)
    {
        _mediaFiles = files;
        var list = new List<JukeboxTrack>();
        for (int i = 0; i < files.Length; i++)
        {
            list.Add(new JukeboxTrack(i, files[i], Path.GetFileNameWithoutExtension(files[i])));
        }
        Tracks = new ObservableCollection<JukeboxTrack>(list);

        CurrentIndex = 0;
        IsPlaying = false;
        IsPaused = false;
        IsStopped = true;

        ApplyFilter();

        var firstTrack = Tracks.FirstOrDefault();
        if (firstTrack != null)
        {
            SelectedTrack = firstTrack;
            firstTrack.IsPlaying = false;
        }

        HasMultipleTracks = !singleFile;
        IsPlaylistVisible = false;
        UpdateButtons();
        
        // Temporarily disable the metadata scanner to isolate the crash
        StartMetadataScanner();
    }

    private void ApplyFilter()
    {
        if (Tracks == null) return;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredTracks = new ObservableCollection<JukeboxTrack>(Tracks);
        }
        else
        {
            var filtered = Tracks.Where(t => t.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            FilteredTracks = new ObservableCollection<JukeboxTrack>(filtered);
        }
    }

    private void StartMetadataScanner()
    {
        _scannerCts?.Cancel();
        _scannerCts?.Dispose();
        _scannerCts = new CancellationTokenSource();
        var token = _scannerCts.Token;

        _scannerTask = Task.Run(() => RunScannerAsync(token), token);
    }

    private async Task RunScannerAsync(CancellationToken token)
    {
        var tracksToParse = Tracks.ToList();

        foreach (var track in tracksToParse)
        {
            if (token.IsCancellationRequested || _isDisposed) break;

            try
            {
                using var file = TagLib.File.Create(track.FilePath);
                var duration = file.Properties.Duration;
                
                string durationStr = duration.TotalMilliseconds > 0 
                    ? duration.ToString(duration.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss")
                    : "--:--";

                string resolutionStr = "";
                string bitrateStr = "";

                if (file.Properties.VideoWidth > 0 && file.Properties.VideoHeight > 0)
                {
                    resolutionStr = $"{file.Properties.VideoWidth}x{file.Properties.VideoHeight}";
                }

                if (file.Properties.AudioBitrate > 0)
                {
                    bitrateStr = $"{file.Properties.AudioBitrate} kbps";
                }

                if (token.IsCancellationRequested || _isDisposed) break;

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (token.IsCancellationRequested || _isDisposed) return;
                    track.Duration = durationStr;
                    track.Resolution = resolutionStr;
                    if (!string.IsNullOrEmpty(bitrateStr))
                        track.Bitrate = bitrateStr;
                });
            }
            catch { }
        }
        
        await Task.CompletedTask;
    }

    private void PlayCurrentTrack()
    {
        if (_mediaFiles.Length == 0 || CurrentIndex < 0 || CurrentIndex >= _mediaFiles.Length || _isDisposed)
            return;

        PlayTrack(_mediaFiles[CurrentIndex]);
    }

    private bool _isPlayerAttached;
    private bool _playPending;

    public void NotifyPlayerAttached()
    {
        _isPlayerAttached = true;
        if (_playPending)
        {
            _playPending = false;
            PlayCurrentTrack();
        }
    }

    private void PlayTrack(string fileName)
    {
        if (_isDisposed) return;

        try
        {
            bool isAudio = IsAudioFile(fileName);

            // Swap players if needed
            if (_visualizationsEnabled != isAudio || ActiveMediaPlayer == null)
            {
                ActiveMediaPlayer?.Stop();
                _visualizationsEnabled = isAudio;
                _isPlayerAttached = false;

                var playerToAttach = ActiveMediaPlayer;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => MediaPlayerCreated?.Invoke(playerToAttach));
            }

            if (!_isPlayerAttached)
            {
                _playPending = true;
                return;
            }

            var activePlayer = ActiveMediaPlayer;
            var activeLibVlc = ActiveLibVLC;

            if (activePlayer == null || activeLibVlc == null) return;

            _currentMedia?.Dispose();
            _currentMedia = new Media(activeLibVlc, fileName, FromType.FromPath);
            activePlayer.Play(_currentMedia);

            IsPlaying = true;
            IsPaused = false;
            IsStopped = false;
            UpdateButtons();
        }
        catch (Exception ex)
        {
            RaiseError($"Playback error: {ex.Message}");
        }
    }

    private void Resume()
    {
        if (_isDisposed || ActiveMediaPlayer == null) return;

        if (IsPaused)
        {
            ActiveMediaPlayer.Play();
            IsPlaying = true;
            IsPaused = false;
            IsStopped = false;
            UpdateButtons();
        }
    }

    private void GotoNextTrack()
    {
        if (_mediaFiles.Length == 0 || _isDisposed) return;

        CurrentIndex = IsRandomPlayback
            ? _random.Next(0, _mediaFiles.Length)
            : (CurrentIndex + 1) % _mediaFiles.Length;

        if (CurrentIndex >= 0 && CurrentIndex < Tracks.Count)
        {
            SelectedTrack = Tracks[CurrentIndex];
            PlayCurrentTrack();
        }
    }

    private void GotoPreviousTrack()
    {
        if (_mediaFiles.Length == 0 || _isDisposed) return;

        CurrentIndex = IsRandomPlayback
            ? _random.Next(0, _mediaFiles.Length)
            : CurrentIndex - 1;

        if (CurrentIndex < 0) CurrentIndex = _mediaFiles.Length - 1;

        if (CurrentIndex >= 0 && CurrentIndex < Tracks.Count)
        {
            SelectedTrack = Tracks[CurrentIndex];
            PlayCurrentTrack();
        }
    }

    private void UpdateButtons()
    {
        CanPlay = !IsPlaying || IsPaused;
        CanPause = IsPlaying && !IsPaused;
        CanStop = IsPlaying || IsPaused;
    }

    private static bool IsAudioFile(string fileName)
    {
        string[] audioExtensions = { ".mp3", ".wav", ".ogg", ".flac", ".aac", ".m4a" };
        return audioExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());
    }

    private void RaiseError(string message) =>
        Avalonia.Threading.Dispatcher.UIThread.Post(() => ErrorOccurred?.Invoke(message));

    private void OnEndReached(object? sender, EventArgs e) => 
        Avalonia.Threading.Dispatcher.UIThread.Post(GotoNextTrack);
        
    private void OnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
    {
        var player = sender as MediaPlayer;
        if (player != null && player.Length > 0 && CurrentIndex >= 0 && CurrentIndex < Tracks.Count)
        {
            var track = Tracks[CurrentIndex];
            if (track.Duration == "--:--")
            {
                long durationMs = player.Length;
                string durationStr = durationMs > 0 
                    ? TimeSpan.FromMilliseconds(durationMs).ToString(durationMs >= 3600000 ? @"h\:mm\:ss" : @"m\:ss")
                    : "--:--";
                Avalonia.Threading.Dispatcher.UIThread.Post(() => track.Duration = durationStr);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_scannerCts != null)
        {
            try { _scannerCts.Cancel(); } catch { }
        }

        if (_scannerTask != null)
        {
            try { await _scannerTask; } catch { }
        }

        if (_scannerCts != null)
        {
            try { _scannerCts.Dispose(); } catch { }
            _scannerCts = null;
        }

        MediaPlayerCreated?.Invoke(null);
        
        var videoPlayer = _videoMediaPlayer;
        var videoVlc = _videoLibVLC;
        
        var audioPlayer = _audioMediaPlayer;
        var audioVlc = _audioLibVLC;

        var currentMedia = _currentMedia;

        _videoMediaPlayer = null;
        _videoLibVLC = null;
        
        _audioMediaPlayer = null;
        _audioLibVLC = null;
        
        _currentMedia = null;

        await Task.Run(() =>
        {
            try
            {
                if (videoPlayer != null)
                {
                    if (videoPlayer.IsPlaying) videoPlayer.Stop();
                    videoPlayer.EndReached -= OnEndReached;
                    videoPlayer.TimeChanged -= OnTimeChanged;
                    videoPlayer.Dispose();
                }
                
                if (audioPlayer != null)
                {
                    if (audioPlayer.IsPlaying) audioPlayer.Stop();
                    audioPlayer.EndReached -= OnEndReached;
                    audioPlayer.TimeChanged -= OnTimeChanged;
                    audioPlayer.Dispose();
                }
                
                currentMedia?.Dispose();
                
                videoVlc?.Dispose();
                audioVlc?.Dispose();
            }
            catch { }
        });
    }
}

public partial class JukeboxTrack : ObservableObject
{
    public int Index { get; }
    public string FilePath { get; }
    public string DisplayName { get; }

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private string _duration = "--:--";

    [ObservableProperty]
    private string _resolution = "";

    [ObservableProperty]
    private string _bitrate = "";

    public JukeboxTrack(int index, string filePath, string displayName)
    {
        Index = index;
        FilePath = filePath;
        DisplayName = displayName;
    }
}
