using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GamelistManager.controls
{
    public partial class MediaPlayerControl : UserControl
    {
        private LibVLC? _libVLC;
        private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
        private readonly Random _random;

        private string[] _mediaFiles;
        private int _currentIndex;
        private bool _randomPlayback;

        private bool _isPaused;
        private bool _isStopped;
        private bool _isPlaying;
        private int _volume;
        private bool _visualizationsEnabled;
        private bool _isInitializing;
        private Media? _currentMedia;

        private readonly SemaphoreSlim _operationLock;
        private bool _isDisposed;

        public bool IsPaused => _isPaused;
        public bool IsStopped => _isStopped;
        public bool IsPlaying => _isPlaying;
        public int Volume => _volume;

        public MediaPlayerControl()
        {
            InitializeComponent();

            _random = new Random();
            _mediaFiles = Array.Empty<string>();
            _currentIndex = 0;
            _randomPlayback = false;
            _isPaused = false;
            _volume = 50;
            _visualizationsEnabled = false;
            _isInitializing = false;
            _operationLock = new SemaphoreSlim(1, 1);
            _isDisposed = false;

            InitializeUI();

            this.Unloaded += UserControl_Unloaded;
        }

        private void InitializeUI()
        {
            comboBox_CurrentTrack.Visibility = Visibility.Collapsed;
            button_Playlist.Visibility = Visibility.Collapsed;
            checkBox_Randomize.Visibility = Visibility.Collapsed;
            button_Next.IsEnabled = false;
            button_Previous.IsEnabled = false;
            button_Stop.IsEnabled = false;
            button_Pause.IsEnabled = false;
            button_Play.IsEnabled = false;
        }

        public async Task PlayMediaAsync(string fileName, bool autoPlay)
        {
            if (_isDisposed) return;

            try
            {
                LoadPlaylist(new[] { fileName }, singleFile: true);

                if (autoPlay)
                {
                    await PlayCurrentTrackAsync();
                }
                else
                {
                    await LoadPausedAsync(fileName);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Error playing media: {ex.Message}");
                await ResetPlaybackStateAsync();
            }
        }

        public async Task PlayMediaFilesAsync(string[] fileList, bool autoPlay)
        {
            if (_isDisposed) return;

            try
            {
                LoadPlaylist(fileList, singleFile: false);
                if (autoPlay)
                {
                    await PlayCurrentTrackAsync();
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Error playing media files: {ex.Message}");
                await ResetPlaybackStateAsync();
            }
        }

        public async Task ResumePlayingAsync()
        {
            if (_isDisposed) return;

            await _operationLock.WaitAsync();
            try
            {
                if (_mediaPlayer != null && _isPaused)
                {
                    _mediaPlayer.Play();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        _isPaused = false;
                        _isPlaying = true;
                        _isStopped = false;
                        UpdatePlaybackButtons(playing: true, paused: false);
                    });
                }
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public async Task PausePlayingAsync()
        {
            if (_isDisposed) return;

            await _operationLock.WaitAsync();
            try
            {
                if (_mediaPlayer?.IsPlaying == true)
                {
                    _mediaPlayer.Pause();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        _isPaused = true;
                        _isStopped = false;
                        _isPlaying = false;
                        UpdatePlaybackButtons(playing: true, paused: true);
                    });
                }
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public async Task StopPlayingAsync()
        {
            if (_isDisposed) return;

            await _operationLock.WaitAsync();
            try
            {
                if (_mediaPlayer?.IsPlaying == true || _isPaused)
                {
                    _mediaPlayer?.Stop();
                    _currentMedia?.Dispose();
                    _currentMedia = null;

                    await Dispatcher.InvokeAsync(() =>
                    {
                        _isPaused = false;
                        _isPlaying = false;
                        _isStopped = true;
                        UpdatePlaybackButtons(playing: false, paused: false);
                    });
                }
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public void SetVolume(int volume)
        {
            if (_isDisposed) return;

            _volume = volume;
            if (_mediaPlayer != null)
                _mediaPlayer.Volume = _volume;

            Dispatcher.BeginInvoke(() => sliderVolume.Value = _volume, DispatcherPriority.Background);
        }

        public async Task DisposeMediaAsync()
        {
            if (_isDisposed) return;

            await _operationLock.WaitAsync();
            try
            {
                if (_isPlaying || _isPaused)
                {
                    _mediaPlayer?.Stop();
                }

                _currentMedia?.Dispose();
                _currentMedia = null;
                _mediaFiles = Array.Empty<string>();
                _currentIndex = 0;

                await Dispatcher.InvokeAsync(() =>
                {
                    _isPaused = false;
                    _isStopped = true;
                    _isPlaying = false;
                    button_Play.IsEnabled = false;

                    comboBox_CurrentTrack.SelectionChanged -= ComboBox_CurrentTrack_SelectionChanged;
                    comboBox_CurrentTrack.ItemsSource = null;

                    if (_mediaPlayer != null)
                        _mediaPlayer.EndReached -= MediaPlayer_EndReached;
                });
            }
            finally
            {
                _operationLock.Release();
            }
        }

        private async Task LoadPausedAsync(string videoPath)
        {
            if (_mediaFiles.Length == 0)
                return;

            if (_mediaPlayer == null || _libVLC == null)
                await InitializeVLCAsync(_visualizationsEnabled);

            if (_mediaPlayer == null || _libVLC == null)
                return;

            EventHandler<EventArgs>? onPlaying = null;
            var playingEventFired = false;

            const int previewSeekMs = 3000;    // How far into the video to pause
            const int playingTimeoutMs = 3000; // Maximum wait for Playing event

            void ResetPlaybackState()
            {
                _isPaused = false;
                _isStopped = true;
                _isPlaying = false;
                UpdatePlaybackButtons(playing: false, paused: false);

                if (_mediaPlayer != null && onPlaying != null)
                {
                    try { _mediaPlayer.Playing -= onPlaying; } catch { }
                }
            }

            try
            {
                Media? newMedia = null;

                await Dispatcher.InvokeAsync(() =>
                {
                    if (_libVLC == null)
                        return;

                    _currentMedia?.Dispose();
                    newMedia = new Media(_libVLC, videoPath, FromType.FromPath);
                    // Eliminate 'tick' noise from startup
                    newMedia.AddOption(":no-audio");
                    _currentMedia = newMedia;
                });

                if (newMedia == null)
                    return;

                onPlaying = (s, e) =>
                {
                    playingEventFired = true;

                    if (_mediaPlayer != null)
                        _mediaPlayer.Playing -= onPlaying;

                    Dispatcher.InvokeAsync(async () =>
                    {
                        try
                        {
                            if (_mediaPlayer == null)
                                return;

                            long targetTime = previewSeekMs;
                            if (_mediaPlayer.Length > 0 && _mediaPlayer.Length < previewSeekMs)
                                targetTime = (long)(_mediaPlayer.Length * 0.2);

                            _mediaPlayer.Time = targetTime;

                            await Task.Delay(50); // small buffer for decoder

                            _mediaPlayer.Pause();
                                
                            // The trick is setting paused as false so media reloads when play is pushed
                            // That is how we get the video and audio
                            _isPaused = false;
                            _isStopped = false;
                            _isPlaying = false;

                            UpdatePlaybackButtons(playing: _isPlaying, paused: _isPaused);
                        }
                        catch
                        {
                            ResetPlaybackState();
                        }
                    });
                };

                _mediaPlayer.Playing += onPlaying;

                await Dispatcher.InvokeAsync(() =>
                {
                    _mediaPlayer.Play(_currentMedia);
                });

                // Wait for Playing event or timeout
                var timeoutTask = Task.Delay(playingTimeoutMs);
                while (!playingEventFired && !timeoutTask.IsCompleted)
                {
                    await Task.Delay(50);
                }

                if (!playingEventFired)
                    ResetPlaybackState();
            }
            catch
            {
                ResetPlaybackState();
                try
                {
                    _currentMedia?.Dispose();
                    _currentMedia = null;
                }
                catch { }
            }
        }


        private void LoadPlaylist(string[] files, bool singleFile)
        {
            if (_isDisposed) return;

            _mediaFiles = files;
            _currentIndex = 0;
            _isPaused = false;
            _isStopped = true;
            _isPlaying = false;

            var fileNames = Array.ConvertAll(files, Path.GetFileName);

            comboBox_CurrentTrack.SelectionChanged -= ComboBox_CurrentTrack_SelectionChanged;
            comboBox_CurrentTrack.ItemsSource = fileNames;
            comboBox_CurrentTrack.SelectedIndex = 0;
            comboBox_CurrentTrack.SelectionChanged += ComboBox_CurrentTrack_SelectionChanged;

            if (singleFile)
            {
                comboBox_CurrentTrack.Visibility = Visibility.Collapsed;
                button_Playlist.Visibility = Visibility.Collapsed;
                checkBox_Randomize.Visibility = Visibility.Collapsed;
                button_Next.IsEnabled = false;
                button_Previous.IsEnabled = false;
            }
            else
            {
                comboBox_CurrentTrack.Visibility = Visibility.Collapsed;
                button_Playlist.Visibility = Visibility.Visible;
                checkBox_Randomize.Visibility = Visibility.Visible;
                button_Next.IsEnabled = true;
                button_Previous.IsEnabled = true;
            }

            UpdatePlaybackButtons(playing: false, paused: false);
        }

        private async Task PlayCurrentTrackAsync()
        {
            if (_mediaFiles.Length == 0 || _currentIndex < 0 || _currentIndex >= _mediaFiles.Length || _isDisposed)
                return;

            await PlayAsync(_mediaFiles[_currentIndex]);
        }

        private async Task PlayAsync(string fileName)
        {
            if (_isDisposed) return;

            await _operationLock.WaitAsync();
            try
            {
                if (_mediaPlayer == null || _libVLC == null)
                {
                    await InitializeVLCAsync(_visualizationsEnabled);
                }

                if (_mediaPlayer == null || _libVLC == null || _mediaFiles.Length == 0 || _isDisposed)
                    return;

                bool isAudioFile = IsAudioFile(fileName);
                bool needsReinit = (isAudioFile && !_visualizationsEnabled) || (!isAudioFile && _visualizationsEnabled);

                if (needsReinit)
                {
                    _visualizationsEnabled = isAudioFile;
                    await InitializeVLCAsync(_visualizationsEnabled);

                    if (_mediaPlayer == null || _libVLC == null || _isDisposed)
                        return;
                }

                var libVLC = _libVLC;
                var mediaPlayer = _mediaPlayer;

                Media? newMedia = null;
                await Dispatcher.InvokeAsync(() =>
                {
                    _currentMedia?.Dispose();
                    newMedia = new Media(libVLC, fileName, FromType.FromPath);
                    _currentMedia = newMedia;
                });

                if (newMedia == null || _isDisposed)
                    return;

                mediaPlayer.Play(newMedia);
              
                await Dispatcher.InvokeAsync(() =>
                {
                    _isPaused = false;
                    _isStopped = false;
                    _isPlaying = true;
                    UpdatePlaybackButtons(playing: true, paused: false);
                });
            }
            finally
            {
                _operationLock.Release();
            }
        }

        private void GotoNextTrack()
        {
            if (_mediaFiles.Length == 0 || _isDisposed) return;

            _currentIndex = _randomPlayback
                ? _random.Next(0, _mediaFiles.Length)
                : (_currentIndex + 1) % _mediaFiles.Length;

            Dispatcher.InvokeAsync(() => comboBox_CurrentTrack.SelectedIndex = _currentIndex);

            SafeFireAndForget(PlayCurrentTrackAsync());
        }

        private void GotoPreviousTrack()
        {
            if (_mediaFiles.Length == 0 || _isDisposed) return;

            _currentIndex = _randomPlayback
                ? _random.Next(0, _mediaFiles.Length)
                : _currentIndex - 1;

            if (_currentIndex < 0)
                _currentIndex = _mediaFiles.Length - 1;

            Dispatcher.InvokeAsync(() => comboBox_CurrentTrack.SelectedIndex = _currentIndex);

            SafeFireAndForget(PlayCurrentTrackAsync());
        }

        private async Task InitializeVLCAsync(bool enableVisualizations)
        {
            if (_isInitializing || _isDisposed)
                return;

            if (_libVLC != null || _mediaPlayer != null)
            {
                await DisposeVLCAsync();
            }

            _isInitializing = true;

            try
            {
                await Task.Run(async () =>
                {
                    try
                    {
                        var vlcOptions = new List<string>
                        {
                            "--network-caching=300",
                            "--file-caching=300"
                        };

                        if (enableVisualizations)
                        {
                            vlcOptions.Add("--audio-visual=visual");
                            vlcOptions.Add("--effect-width=50");
                            vlcOptions.Add("--effect-height=50");
                            vlcOptions.Add("--effect-list=spectrometer");

                            var libVlcDirectory = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vlc"));
                            vlcOptions.Add($"--plugin-path={libVlcDirectory.FullName}");
                        }

                        var libVLC = new LibVLC(vlcOptions.ToArray());
                        var mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(libVLC);
                        mediaPlayer.Volume = _volume;

                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (_isDisposed) return;

                            _libVLC = libVLC;
                            _mediaPlayer = mediaPlayer;
                            VideoView.MediaPlayer = mediaPlayer;
                            mediaPlayer.EndReached += MediaPlayer_EndReached;
                        });
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorAsync($"VLC Init Error: {ex.Message}");
                    }
                });
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private async Task DisposeVLCAsync()
        {
            await Task.Run(async () =>
            {
                var mediaPlayer = _mediaPlayer;
                var libVLC = _libVLC;
                var currentMedia = _currentMedia;

                await Dispatcher.InvokeAsync(() =>
                {
                    _mediaPlayer = null;
                    _libVLC = null;
                    _currentMedia = null;
                });

                try
                {
                    if (mediaPlayer != null)
                    {
                        if (mediaPlayer.IsPlaying)
                            mediaPlayer.Stop();

                        await Dispatcher.InvokeAsync(() =>
                        {
                            mediaPlayer.EndReached -= MediaPlayer_EndReached;
                        });

                        mediaPlayer.Dispose();
                    }

                    libVLC?.Dispose();
                    currentMedia?.Dispose();
                }
                catch { }
            });
        }

        private static bool IsAudioFile(string fileName)
        {
            string[] audioExtensions = { ".mp3", ".wav", ".ogg", ".flac", ".aac", ".m4a" };
            return audioExtensions.Contains(Path.GetExtension(fileName).ToLower());
        }

        private void UpdatePlaybackButtons(bool playing, bool paused)
        {
            button_Play.IsEnabled = !playing || paused;
            button_Pause.IsEnabled = playing && !paused;
            button_Stop.IsEnabled = playing || paused;
        }

        private async Task ResetPlaybackStateAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                _isPaused = false;
                _isStopped = true;
                _isPlaying = false;
                UpdatePlaybackButtons(playing: false, paused: false);
            });
        }

        private async Task ShowErrorAsync(string message)
        {
            await Dispatcher.InvokeAsync(() => MessageBox.Show(message));
        }

        private async void SafeFireAndForget(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Playback error: {ex.Message}");
                await ResetPlaybackStateAsync();
            }
        }

        private void MediaPlayer_EndReached(object? sender, EventArgs e)
        {
            GotoNextTrack();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_isDisposed) return;

            try
            {
                if (sender is not Button button) return;

                if (button == button_Play)
                {
                    if (_mediaPlayer != null && _isPaused)
                    {
                        await ResumePlayingAsync();
                    }
                    else
                    {
                        await PlayCurrentTrackAsync();
                    }
                }
                else if (button == button_Pause)
                {
                    await PausePlayingAsync();
                }
                else if (button == button_Stop)
                {
                    await StopPlayingAsync();
                }
                else if (button == button_Previous)
                {
                    GotoPreviousTrack();
                }
                else if (button == button_Next)
                {
                    GotoNextTrack();
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Playback error: {ex.Message}");
                await ResetPlaybackStateAsync();
            }
        }

        private void Button_Playlist_Click(object sender, RoutedEventArgs e)
        {
            if (_isDisposed) return;

            comboBox_CurrentTrack.Visibility = comboBox_CurrentTrack.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private async void ComboBox_CurrentTrack_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isDisposed) return;

            try
            {
                if (sender is not ComboBox comboBox || comboBox.SelectedIndex == -1) return;

                _currentIndex = comboBox.SelectedIndex;
                if (_currentIndex >= 0 && _currentIndex < _mediaFiles.Length)
                    await PlayAsync(_mediaFiles[_currentIndex]);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Error changing track: {ex.Message}");
                await ResetPlaybackStateAsync();
            }
        }

        private void CheckBox_Randomize_Click(object sender, RoutedEventArgs e)
        {
            if (_isDisposed) return;
            _randomPlayback = checkBox_Randomize.IsChecked == true;
        }

        private async void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            await DisposeAsync();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isDisposed) return;

            _volume = (int)sliderVolume.Value;
            if (_mediaPlayer != null)
                _mediaPlayer.Volume = _volume;
        }

        private void SliderVolume_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isDisposed) return;

            if (sender is Slider slider)
            {
                var pos = e.GetPosition(slider);
                slider.Value = pos.X / slider.ActualWidth * (slider.Maximum - slider.Minimum) + slider.Minimum;
            }
        }

        public async Task DisposeAsync()
        {
            if (_isDisposed) return;

            _isDisposed = true;

            await _operationLock.WaitAsync();
            try
            {
                this.Unloaded -= UserControl_Unloaded;

                await DisposeMediaAsync();
                await DisposeVLCAsync();

                _mediaFiles = Array.Empty<string>();

                comboBox_CurrentTrack.SelectionChanged -= ComboBox_CurrentTrack_SelectionChanged;
                comboBox_CurrentTrack.ItemsSource = null;
            }
            finally
            {
                _operationLock.Release();
                _operationLock.Dispose();
            }
        }

        public void MediaPlayerControlDispose()
        {
            _ = DisposeAsync();
        }
    }
}