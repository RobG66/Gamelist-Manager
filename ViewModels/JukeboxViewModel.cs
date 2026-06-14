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
using System.Text.RegularExpressions;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using System.Runtime.InteropServices;

namespace Gamelist_Manager.ViewModels;


public partial class JukeboxViewModel : ViewModelBase, IAsyncDisposable
{
    private LibVLC? _videoLibVLC;
    private MediaPlayer? _videoMediaPlayer;

    private LibVLC? _audioLibVLC;
    private MediaPlayer? _audioMediaPlayer;
    
    private Controls.ProjectMControl? _projectMControl;
    private IStorageProvider? _storageProvider;
    private Classes.Audio.AudioOutputHandler? _audioOutput;

    private MediaPlayer? ActiveMediaPlayer => VisualizationsEnabled ? _audioMediaPlayer : _videoMediaPlayer;
    private LibVLC? ActiveLibVLC => VisualizationsEnabled ? _audioLibVLC : _videoLibVLC;

    private Media? _currentMedia;

    private string[] _mediaFiles = Array.Empty<string>();
    private readonly Random _random = new();
    private bool _isDisposed;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VideoEnabled))]
    [NotifyPropertyChangedFor(nameof(PickerEnabled))]
    private bool _visualizationsEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PickerEnabled))]
    private bool _hasProjectM;

    public bool VideoEnabled => !VisualizationsEnabled;
    public bool PickerEnabled => VisualizationsEnabled && HasProjectM;
    
    private CancellationTokenSource? _scannerCts;
    private Task? _scannerTask;

    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? _systemLogo;

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
    private double _playbackPosition;

    [ObservableProperty]
    private double _playbackLength = 100;

    [ObservableProperty]
    private string _currentTimeString = "0:00";

    [ObservableProperty]
    private string _totalTimeString = "0:00";

    [ObservableProperty]
    private bool _isAutoHideEnabled = true;

    [ObservableProperty]
    private bool _isControlBarVisible = true;

    [ObservableProperty]
    private double _controlBarHeight = 65;

    partial void OnIsControlBarVisibleChanged(bool value)
    {
        ControlBarHeight = value ? 65 : 0;
    }

    private bool _isUpdatingTimeFromPlayer = false;
    private readonly Avalonia.Threading.DispatcherTimer _autoHideTimer = new();

    [ObservableProperty]
    private bool _hasVideoInPlaylist = true;

    [ObservableProperty]
    private bool _canPlay = true;

    [ObservableProperty]
    private bool _canPause;

    [ObservableProperty]
    private bool _canStop;

    [ObservableProperty]
    private bool _isInitializing;


    public event Action<MediaPlayer?>? MediaPlayerCreated;
    public event Action<string>? ErrorOccurred;
    public event EventHandler? CloseRequested;

    private Task _initTask;

    public JukeboxViewModel()
    {
        IsInitializing = true;
        
        _autoHideTimer.Interval = TimeSpan.FromSeconds(3);
        _autoHideTimer.Tick += (s, e) => 
        {
            if (IsAutoHideEnabled)
            {
                IsControlBarVisible = false;
            }
            _autoHideTimer.Stop();
        };

        _initTask = Task.Run(() => 
        {
            InitializeVideoVLC();
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                IsInitializing = false;
            });
        });
        LoadPresets();
    }

    public void SetProjectMControl(Controls.ProjectMControl control)
    {
        Console.WriteLine($"[ViewModel] SetProjectMControl called. Control is null? {control == null}");
        _projectMControl = control;
    }

    public void SetStorageProvider(IStorageProvider? provider)
    {
        _storageProvider = provider;
    }

    // OnAudioSetup and OnAudioCleanup removed because SetAudioFormat is used instead

    private void OnAudioPlay(IntPtr data, IntPtr samples, uint count, long pts)
    {
        int sampleCount = (int)count * 2; // stereo
        
        // Feed 16-bit PCM directly to OpenAL and ProjectM
        var shortBuffer = new short[sampleCount];
        Marshal.Copy(samples, shortBuffer, 0, sampleCount);
        _audioOutput?.Enqueue(shortBuffer);
        _projectMControl?.FeedPcm(shortBuffer);
    }

    private void OnAudioPause(IntPtr data, long pts) { }
    private void OnAudioResume(IntPtr data, long pts) { }
    private void OnAudioFlush(IntPtr data, long pts) { }
    private void OnAudioDrain(IntPtr data) { }

    public void LoadSystemLogo(string systemName)
    {
        try
        {
            var uri = new Uri($"avares://Gamelist_Manager/Assets/Systems/{systemName}.png");
            using var stream = AssetLoader.Open(uri);
            SystemLogo = new Avalonia.Media.Imaging.Bitmap(stream);
        }
        catch { }
    }

    private void InitializeVideoVLC()
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
            _videoMediaPlayer.LengthChanged += OnLengthChanged;
            _videoMediaPlayer.Buffering += OnBuffering;
            _videoMediaPlayer.Playing += OnPlaying;
              
            Avalonia.Threading.Dispatcher.UIThread.Post(() => RequestPlayer());
        }
        catch (Exception ex)
        {
            RaiseError($"VLC Init Error: {ex.Message}");
        }
    }

    private void CreateAudioVLC()
    {
        try
        {
            var oldPlayer = _audioMediaPlayer;
            var oldLibVlc = _audioLibVLC;

            _audioMediaPlayer = null;
            _audioLibVLC = null;

            if (oldPlayer != null || oldLibVlc != null)
            {
                // Unbind UI immediately to prevent native crash during destruction
                MediaPlayerCreated?.Invoke(null);
                
                Task.Run(() => 
                {
                    try
                    {
                        if (oldPlayer != null)
                        {
                            try { oldPlayer.Stop(); } catch { }
                            oldPlayer.EndReached -= OnEndReached;
                            oldPlayer.TimeChanged -= OnTimeChanged;
                            oldPlayer.LengthChanged -= OnLengthChanged;
                            oldPlayer.Buffering -= OnBuffering;
                            oldPlayer.Playing -= OnPlaying;
                            try { oldPlayer.Dispose(); } catch { }
                        }
                        if (oldLibVlc != null)
                        {
                            try { oldLibVlc.Dispose(); } catch { }
                        }
                    }
                    catch { }
                });
            }

            var baseOptions = new List<string>
            {
                "--no-video-title-show",
                "--no-stats",
                "--no-snapshot-preview",
                "--no-sub-autodetect-file",
                "--network-caching=300",
                "--file-caching=300"
            };

            HasProjectM = Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectM"));

            if (!HasProjectM)
            {
                baseOptions.Add("--audio-visual=visual");
                baseOptions.Add("--effect-width=50");
                baseOptions.Add("--effect-height=50");
                baseOptions.Add("--effect-list=spectrometer");

                var vlcDir = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vlc"));
                baseOptions.Add($"--plugin-path={vlcDir.FullName}");
            }

            _audioLibVLC = new LibVLC(baseOptions.ToArray());
            _audioMediaPlayer = new MediaPlayer(_audioLibVLC);
            _audioMediaPlayer.Volume = (int)Volume;
            _audioMediaPlayer.Scale = 0;
            _audioMediaPlayer.EndReached += OnEndReached;
            _audioMediaPlayer.TimeChanged += OnTimeChanged;
            _audioMediaPlayer.LengthChanged += OnLengthChanged;
            _audioMediaPlayer.Buffering += OnBuffering;
            _audioMediaPlayer.Playing += OnPlaying;
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
            var player = ActiveMediaPlayer;
            Task.Run(() => { try { player.Pause(); } catch { } });
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

        if (IsPlaying || IsPaused)
        {
            var player = ActiveMediaPlayer;
            Task.Run(() => { try { player.Stop(); } catch { } });
            _currentMedia?.Dispose();
            _currentMedia = null;
            IsPlaying = false;
            IsPaused = false;
            IsStopped = true;
            PlaybackPosition = 0;
            CurrentTimeString = "0:00";
            UpdateButtons();
        }
    }



    partial void OnVolumeChanged(double value)
    {
        if (_videoMediaPlayer != null) _videoMediaPlayer.Volume = (int)value;
        if (_audioMediaPlayer != null) _audioMediaPlayer.Volume = (int)value;
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

        IsInitializing = true;
        try
        {
            bool isAudio = IsAudioFile(fileName);

            // Swap players if needed
            if (VisualizationsEnabled != isAudio || ActiveMediaPlayer == null)
            {
                ActiveMediaPlayer?.Stop();
                VisualizationsEnabled = isAudio;
                if (!isAudio) 
                {
                    IsPickerVisible = false;
                }
                else
                {
                    _projectMControl?.StartEngine();
                }
                _isPlayerAttached = false;

                if (isAudio && _audioLibVLC == null)
                {
                    CreateAudioVLC();
                }

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

            if (isAudio)
            {
                if (HasProjectM)
                {
                    activePlayer.SetAudioFormat("S16N", 44100, 2);
                    activePlayer.SetAudioCallbacks(OnAudioPlay, OnAudioPause, OnAudioResume, OnAudioFlush, OnAudioDrain);
                    _audioOutput ??= new Classes.Audio.AudioOutputHandler();
                }

                // If no preset is loaded, pick a random one so we don't just see a black screen
                if (SelectedPreset == null && HasProjectM)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => PickRandomPresetCommand.Execute(null));
                }
                else
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => ApplyPresetCommand.Execute(null));
                }
            }

            _currentMedia?.Dispose();
            var mediaToPlay = new Media(activeLibVlc, fileName, FromType.FromPath);
            _currentMedia = mediaToPlay;
            
            Task.Run(() => 
            {
                try { activePlayer.Play(mediaToPlay); } catch { }
            });

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
            var player = ActiveMediaPlayer;
            Task.Run(() => { try { player.Play(); } catch { } });
            IsPlaying = true;
            IsPaused = false;
            IsStopped = false;
            UpdateButtons();
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

    private void OnEndReached(object? sender, EventArgs e) 
    {
        Task.Run(async () => 
        {
            await Task.Delay(50);
            Avalonia.Threading.Dispatcher.UIThread.Post(GotoNextTrack);
        });
    }
        
    private void OnPlaying(object? sender, EventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => IsInitializing = false);
    }

    private void OnBuffering(object? sender, MediaPlayerBufferingEventArgs e)
    {
        if (e.Cache == 100)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => IsInitializing = false);
        }
    }

    private void OnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
    {
        var player = sender as MediaPlayer;
        long time = e.Time;
        long durationMs = player?.Length ?? 0;
        
        Avalonia.Threading.Dispatcher.UIThread.Post(() => {
            _isUpdatingTimeFromPlayer = true;
            PlaybackPosition = time;
            CurrentTimeString = TimeSpan.FromMilliseconds(time).ToString(time >= 3600000 ? @"h\:mm\:ss" : @"m\:ss");
            _isUpdatingTimeFromPlayer = false;
            
            if (player != null && durationMs > 0 && CurrentIndex >= 0 && CurrentIndex < Tracks.Count)
            {
                var track = Tracks[CurrentIndex];
                if (track.Duration == "--:--")
                {
                    string durationStr = TimeSpan.FromMilliseconds(durationMs).ToString(durationMs >= 3600000 ? @"h\:mm\:ss" : @"m\:ss");
                    track.Duration = durationStr;
                }
            }
        });
    }

    private void OnLengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => {
            PlaybackLength = e.Length;
            TotalTimeString = e.Length > 0 ? TimeSpan.FromMilliseconds(e.Length).ToString(e.Length >= 3600000 ? @"h\:mm\:ss" : @"m\:ss") : "0:00";
        });
    }

    partial void OnIsAutoHideEnabledChanged(bool value)
    {
        if (value)
        {
            ResetAutoHideTimer();
        }
        else
        {
            IsControlBarVisible = true;
            _autoHideTimer.Stop();
        }
    }

    partial void OnIsInitializingChanged(bool value)
    {
        if (value)
        {
            IsControlBarVisible = true;
            _autoHideTimer.Stop();
        }
        else
        {
            ResetAutoHideTimer();
        }
    }

    public void ResetAutoHideTimer()
    {
        if (_isDisposed || IsInitializing) return;
        IsControlBarVisible = true;
        if (IsAutoHideEnabled)
        {
            _autoHideTimer.Stop();
            _autoHideTimer.Start();
        }
    }

    partial void OnPlaybackPositionChanged(double value)
    {
        if (_isUpdatingTimeFromPlayer) return;
        var player = ActiveMediaPlayer;
        if (player != null && player.IsPlaying)
        {
            Task.Run(() => { try { player.Time = (long)value; } catch { } });
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
        var audioOutput = _audioOutput;

        var currentMedia = _currentMedia;
        var logo = SystemLogo;

        _videoMediaPlayer = null;
        _videoLibVLC = null;
        
        _audioMediaPlayer = null;
        _audioLibVLC = null;
        
        _currentMedia = null;
        SystemLogo = null;

        await Task.Run(() =>
        {
            try
            {
                logo?.Dispose();
                if (videoPlayer != null)
                {
                    if (videoPlayer.IsPlaying) videoPlayer.Stop();
                    videoPlayer.EndReached -= OnEndReached;
                    videoPlayer.TimeChanged -= OnTimeChanged;
                    videoPlayer.LengthChanged -= OnLengthChanged;
                    videoPlayer.Buffering -= OnBuffering;
                    videoPlayer.Playing -= OnPlaying;
                    videoPlayer.Dispose();
                }
                
                if (audioPlayer != null)
                {
                    if (audioPlayer.IsPlaying) audioPlayer.Stop();
                    audioPlayer.EndReached -= OnEndReached;
                    audioPlayer.TimeChanged -= OnTimeChanged;
                    audioPlayer.LengthChanged -= OnLengthChanged;
                    audioPlayer.Buffering -= OnBuffering;
                    audioPlayer.Playing -= OnPlaying;
                    audioPlayer.Dispose();
                }
                
                currentMedia?.Dispose();
                
                videoVlc?.Dispose();
                audioVlc?.Dispose();
                
                audioOutput?.Dispose();
            }
            catch { }
        });
    }
}

