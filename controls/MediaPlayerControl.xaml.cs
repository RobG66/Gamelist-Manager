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

            InitializeUI();
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

        public async void PlayMedia(string fileName, bool autoPlay)
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

        public async void PlayMediaFiles(string[] fileList, bool autoPlay)
        {
            LoadPlaylist(fileList, singleFile: false);
            if (autoPlay) await PlayCurrentTrackAsync();
        }

        public void ResumePlaying()
        {
            if (_mediaPlayer != null && _isPaused)
            {
                _isPaused = false;
                _isPlaying = true;
                _isStopped = false;

                UpdatePlaybackButtons(playing: true, paused: false);

                Dispatcher.InvokeAsync(() => _mediaPlayer?.Play());
            }
        }

        public void PausePlaying()
        {
            if (_mediaPlayer?.IsPlaying == true)
            {
                _isPaused = true;
                _isStopped = false;
                _isPlaying = false;

                UpdatePlaybackButtons(playing: true, paused: true);
                Dispatcher.InvokeAsync(() => _mediaPlayer?.Pause());
            }
        }

        public void StopPlaying()
        {
            if (_mediaPlayer?.IsPlaying == true || _isPaused)
            {
                _isPaused = false;
                _isPlaying = false;
                _isStopped = true;

                UpdatePlaybackButtons(playing: false, paused: false);

                Dispatcher.InvokeAsync(() =>
                {
                    _mediaPlayer?.Stop();
                    _currentMedia?.Dispose();
                    _currentMedia = null;
                });
            }
        }

        public void SetVolume(int volume)
        {
            _volume = volume;
            if (_mediaPlayer != null)
                _mediaPlayer.Volume = _volume;

            Dispatcher.InvokeAsync(() => sliderVolume.Value = _volume, DispatcherPriority.Background);
        }

        private async Task LoadPausedAsync(string videoPath)
        {
            if (_mediaFiles.Length == 0)
                return;

            if (_mediaPlayer == null || _libVLC == null)
            {
                await InitializeVLCAsync(_visualizationsEnabled);
            }

            // Double-check after initialization
            if (_mediaPlayer == null || _libVLC == null)
                return;

            EventHandler<EventArgs>? onPlaying = null;
            var playingEventFired = false;

            try
            {
                Media? newMedia = null;

                await Dispatcher.InvokeAsync(() =>
                {
                    // Check again inside dispatcher - could be null due to race condition
                    if (_libVLC == null)
                        return;

                    _currentMedia?.Dispose();
                    newMedia = new Media(_libVLC, videoPath, FromType.FromPath);
                    _currentMedia = newMedia;
                });

                // If media creation failed, exit
                if (newMedia == null)
                    return;

                onPlaying = (s, e) =>
                {
                    playingEventFired = true;

                    if (_mediaPlayer != null)
                        _mediaPlayer.Playing -= onPlaying;

                    Dispatcher.BeginInvoke(new Action(async () =>
                    {
                        try
                        {
                            if (_mediaPlayer == null || !_mediaPlayer.IsPlaying)
                                return;

                            long targetTime = 3000;
                            if (_mediaPlayer.Length > 0 && _mediaPlayer.Length < 3000)
                                targetTime = (long)(_mediaPlayer.Length * 0.2);

                            _mediaPlayer.Time = targetTime;

                            await Task.Delay(50);

                            _mediaPlayer.Pause();

                            _isPaused = true;
                            _isStopped = false;
                            _isPlaying = false;
                            UpdatePlaybackButtons(playing: true, paused: true);
                        }
                        catch
                        {
                            _isPaused = false;
                            _isStopped = true;
                            _isPlaying = false;
                            UpdatePlaybackButtons(playing: false, paused: false);
                        }
                    }));
                };

                _mediaPlayer.Playing += onPlaying;

                await Dispatcher.InvokeAsync(() =>
                {
                    _mediaPlayer.Volume = _volume;
                    _mediaPlayer.Play(_currentMedia);
                });

                var timeout = Task.Delay(3000);
                while (!playingEventFired && !timeout.IsCompleted)
                {
                    await Task.Delay(50);
                }

                if (!playingEventFired)
                {
                    if (onPlaying != null && _mediaPlayer != null)
                    {
                        try
                        {
                            _mediaPlayer.Playing -= onPlaying;
                        }
                        catch { }
                    }

                    _isPaused = false;
                    _isStopped = true;
                    _isPlaying = false;
                    UpdatePlaybackButtons(playing: false, paused: false);
                }
            }
            catch
            {
                if (onPlaying != null && _mediaPlayer != null)
                {
                    try
                    {
                        _mediaPlayer.Playing -= onPlaying;
                    }
                    catch { }
                }

                try
                {
                    _currentMedia?.Dispose();
                    _currentMedia = null;
                }
                catch { }

                _isPaused = false;
                _isStopped = true;
                _isPlaying = false;
                UpdatePlaybackButtons(playing: false, paused: false);
            }
        }

        private void LoadPlaylist(string[] files, bool singleFile)
        {
            _mediaFiles = files;
            _currentIndex = 0;
            _isPaused = false;
            _isStopped = true;
            _isPlaying = false;

            var fileNames = Array.ConvertAll(files, Path.GetFileName);
            comboBox_CurrentTrack.SelectionChanged -= comboBox_CurrentTrack_SelectionChanged;
            comboBox_CurrentTrack.ItemsSource = fileNames;
            comboBox_CurrentTrack.SelectedIndex = 0;
            comboBox_CurrentTrack.SelectionChanged += comboBox_CurrentTrack_SelectionChanged;

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
            if (_mediaFiles.Length == 0 || _currentIndex < 0 || _currentIndex >= _mediaFiles.Length)
                return;

            await PlayAsync(_mediaFiles[_currentIndex]);
        }

        private async Task PlayAsync(string fileName)
        {
            if (_mediaPlayer == null || _libVLC == null)
            {
                await InitializeVLCAsync(_visualizationsEnabled);
            }

            // Double-check after initialization
            if (_mediaPlayer == null || _libVLC == null)
                return;

            if (_mediaFiles.Length == 0) return;

            bool isAudioFile = IsAudioFile(fileName);
            bool needsReinit = (isAudioFile && !_visualizationsEnabled) || (!isAudioFile && _visualizationsEnabled);

            if (needsReinit)
            {
                _visualizationsEnabled = isAudioFile;
                await InitializeVLCAsync(_visualizationsEnabled);

                // Check again after reinit
                if (_mediaPlayer == null || _libVLC == null)
                    return;
            }

            await Dispatcher.InvokeAsync(() =>
            {
                // Check again inside dispatcher - could be null due to race condition
                if (_libVLC == null || _mediaPlayer == null)
                    return;

                _currentMedia?.Dispose();
                _currentMedia = new Media(_libVLC, fileName, FromType.FromPath);

                _mediaPlayer.Play(_currentMedia);
                _mediaPlayer.Volume = _volume;
            });

            _isPaused = false;
            _isStopped = false;
            _isPlaying = true;
            UpdatePlaybackButtons(playing: true, paused: false);
        }

        private bool IsAudioFile(string fileName)
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

        private void GotoNext()
        {
            if (_mediaFiles.Length == 0) return;

            _currentIndex = _randomPlayback
                ? _random.Next(0, _mediaFiles.Length)
                : (_currentIndex + 1) % _mediaFiles.Length;

            Dispatcher.InvokeAsync(() => comboBox_CurrentTrack.SelectedIndex = _currentIndex);
            _ = PlayCurrentTrackAsync();
        }

        private void GotoPrevious()
        {
            if (_mediaFiles.Length == 0) return;

            _currentIndex = _randomPlayback
                ? _random.Next(0, _mediaFiles.Length)
                : _currentIndex - 1;

            if (_currentIndex < 0) _currentIndex = _mediaFiles.Length - 1;

            Dispatcher.InvokeAsync(() => comboBox_CurrentTrack.SelectedIndex = _currentIndex);
            _ = PlayCurrentTrackAsync();
        }

        private async Task InitializeVLCAsync(bool enableVisualizations)
        {
            if (_isInitializing)
                return;

            if (_libVLC != null || _mediaPlayer != null)
            {
                await DisposeVLCAsync();
            }

            _isInitializing = true;

            try
            {
                await Dispatcher.InvokeAsync(() =>
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

                    _libVLC = new LibVLC(vlcOptions.ToArray());
                    _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
                    VideoView.MediaPlayer = _mediaPlayer;
                    _mediaPlayer.Volume = _volume;
                    _mediaPlayer.EndReached += MediaPlayer_EndReached;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"VLC Init Error: {ex.Message}");
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private async Task DisposeVLCAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (_mediaPlayer != null)
                    {
                        if (_mediaPlayer.IsPlaying || _isPaused)
                            _mediaPlayer.Stop();

                        _mediaPlayer.EndReached -= MediaPlayer_EndReached;
                        _mediaPlayer.Dispose();
                        _mediaPlayer = null;
                    }

                    _libVLC?.Dispose();
                    _libVLC = null;

                    _currentMedia?.Dispose();
                    _currentMedia = null;
                }
                catch { }
            });
        }

        public void MediaPlayerControlDispose()
        {
            this.Unloaded -= UserControl_Unloaded;
            _mediaFiles = Array.Empty<string>();

            comboBox_CurrentTrack.SelectionChanged -= comboBox_CurrentTrack_SelectionChanged;
            comboBox_CurrentTrack.ItemsSource = null;

            _ = DisposeVLCAsync();
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e) => Dispatcher.InvokeAsync(GotoNext);

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            if (button == button_Play)
            {
                if (_mediaPlayer != null && _isPaused)
                {
                    ResumePlaying();
                }
                else
                {
                    _ = PlayCurrentTrackAsync();
                }
            }
            else if (button == button_Pause) PausePlaying();
            else if (button == button_Stop) StopPlaying();
            else if (button == button_Previous) GotoPrevious();
            else if (button == button_Next) GotoNext();
        }

        private void button_Playlist_Click(object sender, RoutedEventArgs e)
        {
            comboBox_CurrentTrack.Visibility = comboBox_CurrentTrack.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void comboBox_CurrentTrack_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox || comboBox.SelectedIndex == -1) return;

            _currentIndex = comboBox.SelectedIndex;
            if (_currentIndex >= 0 && _currentIndex < _mediaFiles.Length)
                _ = PlayAsync(_mediaFiles[_currentIndex]);
        }

        private void checkBox_Randomize_Click(object sender, RoutedEventArgs e) =>
            _randomPlayback = checkBox_Randomize.IsChecked == true;

        private void UserControl_Unloaded(object sender, RoutedEventArgs e) => MediaPlayerControlDispose();

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _volume = (int)sliderVolume.Value;
            if (_mediaPlayer != null) _mediaPlayer.Volume = _volume;
        }

        private void sliderVolume_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                var pos = e.GetPosition(slider);
                slider.Value = pos.X / slider.ActualWidth * (slider.Maximum - slider.Minimum) + slider.Minimum;
            }
        }
    }
}