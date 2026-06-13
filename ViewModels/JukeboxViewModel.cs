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
using System.Runtime.InteropServices;

namespace Gamelist_Manager.ViewModels;


public partial class JukeboxViewModel : ViewModelBase, IAsyncDisposable
{
    private LibVLC? _videoLibVLC;
    private MediaPlayer? _videoMediaPlayer;

    private LibVLC? _audioLibVLC;
    private MediaPlayer? _audioMediaPlayer;

    private MediaPlayer? ActiveMediaPlayer => VisualizationsEnabled ? _audioMediaPlayer : _videoMediaPlayer;
    private LibVLC? ActiveLibVLC => VisualizationsEnabled ? _audioLibVLC : _videoLibVLC;

    private Media? _currentMedia;

    private string[] _mediaFiles = Array.Empty<string>();
    private readonly Random _random = new();
    private bool _isDisposed;
    [ObservableProperty]
    private bool _visualizationsEnabled;
    
    private CancellationTokenSource? _scannerCts;
    private Task? _scannerTask;
    private CancellationTokenSource? _resizeDebounceCts;
    private int _lastWidth = 512;
    private int _lastHeight = 384;

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
        _initTask = Task.Run(() => 
        {
            EnsureProjectMPluginsCopied();
            InitializeVideoVLC();
        });
        LoadPresets();
    }








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
        }
        catch (Exception ex)
        {
            RaiseError($"VLC Init Error: {ex.Message}");
        }
    }

    private void CreateAudioVLC(int width, int height)
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

            var baseOptions = new[]
            {
                "--no-video-title-show",
                "--no-stats",
                "--no-snapshot-preview",
                "--no-sub-autodetect-file",
                "--network-caching=300",
                "--file-caching=300"
            };

            var audioOptions = baseOptions.ToList();
            var vlcDir = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vlc"));
            audioOptions.Add($"--plugin-path={vlcDir.FullName}");

            var projectMDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectM");
            var presetsDir = Path.Combine(projectMDir, "presets");
            
            bool useProjectM = false;

            if (Directory.Exists(projectMDir) && Directory.Exists(presetsDir))
            {
                string pluginFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "libprojectm_plugin.dll" : "libprojectm_plugin.so";
                string destPluginDir = Path.Combine(vlcDir.FullName, "plugins", "visualization");
                string destPluginPath = Path.Combine(destPluginDir, pluginFileName);

                if (File.Exists(destPluginPath))
                {
                    useProjectM = true;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // On Linux, the plugin might be installed system-wide via apt-get.
                    useProjectM = true;
                }
            }

            _currentAudioWidth = width;
            _currentAudioHeight = height;

            if (useProjectM)
            {
                var tempPresetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectM", "temp_preset");
                
                if (!Directory.Exists(tempPresetDir)) Directory.CreateDirectory(tempPresetDir);
                
                // If it's empty, grab a random file from anywhere so we don't start with a black screen
                if (Directory.GetFiles(tempPresetDir, "*.milk").Length == 0)
                {
                    try
                    {
                        var allMilk = Directory.GetFiles(presetsDir, "*.milk", SearchOption.AllDirectories);
                        if (allMilk.Length > 0)
                        {
                            var randomFile = allMilk[new Random().Next(allMilk.Length)];
                            File.Copy(randomFile, Path.Combine(tempPresetDir, Path.GetFileName(randomFile)));
                        }
                    }
                    catch { }
                }

                audioOptions.Add("--audio-visual=projectm");
                audioOptions.Add($"--projectm-preset-path={tempPresetDir}");
                audioOptions.Add($"--projectm-width={width}");
                audioOptions.Add($"--projectm-height={height}");
            }
            else
            {
                audioOptions.Add("--audio-visual=visual");
                audioOptions.Add($"--effect-width={width}");
                audioOptions.Add($"--effect-height={height}");
                audioOptions.Add("--effect-list=spectrometer");
            }

            audioOptions.Add("--verbose=2");

            _audioLibVLC = new LibVLC(audioOptions.ToArray());
            _audioMediaPlayer = new MediaPlayer(_audioLibVLC);
            _audioMediaPlayer.Volume = (int)Volume;
            _audioMediaPlayer.Scale = 0;
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

        if (ActiveMediaPlayer.IsPlaying || IsPaused)
        {
            var player = ActiveMediaPlayer;
            Task.Run(() => { try { player.Stop(); } catch { } });
            _currentMedia?.Dispose();
            _currentMedia = null;
            IsPlaying = false;
            IsPaused = false;
            IsStopped = true;
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

    public void InitializeDimensions(int width, int height)
    {
        _lastWidth = Math.Max(1, width);
        _lastHeight = Math.Max(1, height);
    }

    public void NotifyPlayerAttached()
    {
        _isPlayerAttached = true;
        if (_playPending)
        {
            _playPending = false;
            PlayCurrentTrack();
        }
    }

    private int _currentAudioWidth;
    private int _currentAudioHeight;

    public void HandleResize(int width, int height)
    {
        if (width <= 0 || height <= 0 || (_lastWidth == width && _lastHeight == height))
            return;

        _lastWidth = width;
        _lastHeight = height;

        if (_audioMediaPlayer != null && VisualizationsEnabled)
        {
            _resizeDebounceCts?.Cancel();
            _resizeDebounceCts?.Dispose();
            _resizeDebounceCts = new CancellationTokenSource();
            var token = _resizeDebounceCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500, token);
                    if (!token.IsCancellationRequested)
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() => RecreateAudioPlayer(width, height));
                    }
                }
                catch (TaskCanceledException) { }
            });
        }
    }

    private void RecreateAudioPlayer(int width, int height)
    {
        if (_isDisposed) return;

        // Prevent recreating the player if the dimensions haven't actually changed!
        // This stops transient layout jitters (like opening the sidebar) from dropping the audio.
        if (_currentAudioWidth == width && _currentAudioHeight == height)
            return;

        bool wasPlaying = false;
        long currentTime = 0;
        string? currentMediaUrl = _currentMedia?.Mrl;

        if (_audioMediaPlayer != null)
        {
            try
            {
                wasPlaying = _audioMediaPlayer.IsPlaying || IsPaused;
                if (wasPlaying) currentTime = _audioMediaPlayer.Time;
            }
            catch { }
            
            // Do NOT call Stop() or Dispose() here! 
            // CreateAudioVLC will safely detach and dispose the old players on a background thread.
        }

        CreateAudioVLC(width, height);

        MediaPlayerCreated?.Invoke(_audioMediaPlayer);

        if (currentMediaUrl != null && wasPlaying)
        {
            _currentMedia?.Dispose();
            // Use FromLocation because Mrl is a URI
            _currentMedia = new Media(_audioLibVLC!, currentMediaUrl, FromType.FromLocation);
            
            var player = _audioMediaPlayer!;
            var media = _currentMedia;
            bool shouldPause = IsPaused;

            Task.Run(async () => 
            {
                try 
                { 
                    player.Play(media);
                    if (currentTime > 0)
                    {
                        // Wait for player to start playing before seeking
                        while (!player.IsPlaying && !_isDisposed)
                        {
                            await Task.Delay(50);
                        }
                        if (!_isDisposed)
                        {
                            player.Time = currentTime;
                            if (shouldPause)
                                player.Pause();
                        }
                    }
                } catch { }
            });
        }
    }

    private void PlayTrack(string fileName)
    {
        if (_isDisposed) return;

        try
        {
            bool isAudio = IsAudioFile(fileName);

            // Swap players if needed
            if (VisualizationsEnabled != isAudio || ActiveMediaPlayer == null)
            {
                ActiveMediaPlayer?.Stop();
                VisualizationsEnabled = isAudio;
                if (!isAudio) IsPickerVisible = false;
                _isPlayerAttached = false;

                if (isAudio && _audioLibVLC == null)
                {
                    CreateAudioVLC(_lastWidth, _lastHeight);
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

        if (_resizeDebounceCts != null)
        {
            try { _resizeDebounceCts.Cancel(); } catch { }
            try { _resizeDebounceCts.Dispose(); } catch { }
            _resizeDebounceCts = null;
        }

        MediaPlayerCreated?.Invoke(null);
        
        var videoPlayer = _videoMediaPlayer;
        var videoVlc = _videoLibVLC;
        
        var audioPlayer = _audioMediaPlayer;
        var audioVlc = _audioLibVLC;

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

