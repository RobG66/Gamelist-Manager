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
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private readonly Random _random;
        private int _currentIndex;
        private int _volume;
        private bool _isPaused;
        private bool _randomPlayback;
        private string[] _mediaFiles;
        private bool _visualizationsEnabled;

        public MediaPlayerControl()
        {
            InitializeComponent();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            VideoView.MediaPlayer = _mediaPlayer;
            _currentIndex = 0;
            _randomPlayback = false;
            _random = new Random();
            _mediaFiles = Array.Empty<string>();
            _visualizationsEnabled = false;
            comboBox_CurrentTrack.Visibility = Visibility.Collapsed;
            button_Playlist.Visibility = Visibility.Collapsed;
            checkBox_Randomize.Visibility = Visibility.Collapsed;
            button_Next.IsEnabled = false;
            button_Previous.IsEnabled = false;
            _mediaPlayer.EndReached += MediaPlayer_EndReached!;
        }

        public void PlayMedia(string fileName, bool autoPlay)
        {
            // 1 element array
            _mediaFiles = new[] { fileName };

            _isPaused = false;
            button_Playlist.Visibility = Visibility.Collapsed;
            checkBox_Randomize.Visibility = Visibility.Collapsed;

            var fileNames = Array.ConvertAll(_mediaFiles, Path.GetFileName);

            comboBox_CurrentTrack.ItemsSource = fileNames;
            comboBox_CurrentTrack.SelectedIndex = 0;

            comboBox_CurrentTrack.Visibility = Visibility.Collapsed;
            button_Playlist.Visibility = Visibility.Collapsed;
            checkBox_Randomize.Visibility = Visibility.Collapsed;
            button_Next.IsEnabled = false;
            button_Previous.IsEnabled = false;
            button_Stop.IsEnabled = false;
            button_Pause.IsEnabled = false;
            button_Play.IsEnabled = true;

            if (autoPlay)
            {
                Play(_mediaFiles[_currentIndex]);
            }
        }

        public void PlayMedia(string[] fileList, bool autoPlay)
        {
            _mediaFiles = fileList;

            _isPaused = false;

            button_Playlist.Visibility = Visibility.Visible;
            checkBox_Randomize.Visibility = Visibility.Visible;

            var fileNames = Array.ConvertAll(fileList, Path.GetFileName);

            comboBox_CurrentTrack.ItemsSource = fileNames;
            comboBox_CurrentTrack.SelectedIndex = _currentIndex;
            comboBox_CurrentTrack.Visibility = Visibility.Collapsed;
            button_Playlist.Visibility = Visibility.Visible;
            checkBox_Randomize.Visibility = Visibility.Visible;
            button_Next.IsEnabled = true;
            button_Previous.IsEnabled = true;
            button_Stop.IsEnabled = false;
            button_Pause.IsEnabled = false;
            button_Play.IsEnabled = true;

            if (autoPlay)
            {
                Play(_mediaFiles[_currentIndex]);
            }

            comboBox_CurrentTrack.SelectionChanged += comboBox_CurrentTrack_SelectionChanged;

        }

        private void Play(string fileName)
        {
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.IsPlaying || _isPaused == true)
                {
                    _mediaPlayer.Stop();
                    _isPaused = false;
                }
            }

            _isPaused = false;
            button_Play.IsEnabled = false;
            button_Stop.IsEnabled = true;
            button_Pause.IsEnabled = true;

            bool isAudioFile = new[] { ".mp3", ".wav", ".ogg", ".flac", ".aac", ".m4a" }.Contains(Path.GetExtension(fileName).ToLower());

            if (isAudioFile && _visualizationsEnabled == false)
            {
                MediaPlayerControlDispose();
                _visualizationsEnabled = true;
            }

            else if (!isAudioFile && _visualizationsEnabled)
            {
                MediaPlayerControlDispose();
                _visualizationsEnabled = false;
            }

            if (_mediaPlayer == null)
            {
                InitializeVLC(_visualizationsEnabled);
            }

            if (_mediaPlayer == null || _libVLC == null)
            {
                return;
            }

            var media = new Media(_libVLC, fileName, FromType.FromPath);

            sliderVolume.Value = _volume;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _mediaPlayer.Play(media);
            }), DispatcherPriority.Loaded);
        }


        private void MediaPlayer_EndReached(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                GotoNext();
            });
        }

        public void PausePlaying()
        {
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.SetPause(true);
                    _isPaused = true;
                }
            }
        }

        public void StopPlaying()
        {
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Stop();
                    _isPaused = false;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            if (button == button_Play)
            {
                if (_mediaPlayer != null && _isPaused)
                {
                    _mediaPlayer.Play();
                }
                else
                {
                    Play(_mediaFiles[_currentIndex]);
                }
                button_Play.IsEnabled = false;
                button_Pause.IsEnabled = true;
                button_Stop.IsEnabled = true;
                _isPaused = false;
            }
            else if (button == button_Pause)
            {
                _mediaPlayer?.Pause();
                button_Pause.IsEnabled = false;
                button_Stop.IsEnabled = true;
                button_Play.IsEnabled = true;
                _isPaused = true;
            }
            else if (sender == button_Stop)
            {
                _mediaPlayer?.Stop();
                button_Stop.IsEnabled = false;
                button_Play.IsEnabled = true;
                button_Pause.IsEnabled = false;
            }
            else if (button == button_Previous)
            {
                GotoPrevious();
            }
            else if (button == button_Next)
            {
                GotoNext();
            }

        }

        private void GotoNext()
        {
            if (_randomPlayback)
            {
                _currentIndex = _random.Next(0, (_mediaFiles.Length - 1));
            }
            else
            {
                _currentIndex++;
                if (_currentIndex >= _mediaFiles.Length)
                {
                    _currentIndex = 0;
                }
            }

            comboBox_CurrentTrack.SelectedIndex = _currentIndex;
            Play(_mediaFiles[_currentIndex]);
        }

        private void GotoPrevious()
        {
            if (_randomPlayback)
            {
                _currentIndex = _random.Next(0, (_mediaFiles.Length - 1));
            }
            else
            {
                // Update the current index in a thread-safe way
                _currentIndex--;
                if (_currentIndex == -1)
                {
                    _currentIndex = _mediaFiles.Length - 1;
                }
            }
            comboBox_CurrentTrack.SelectedIndex = _currentIndex;

            Play(_mediaFiles[_currentIndex]);
        }


        private void button_Playlist_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox_CurrentTrack.Visibility == Visibility.Visible)
            {
                comboBox_CurrentTrack.Visibility = Visibility.Collapsed;
            }
            else
            {
                comboBox_CurrentTrack.Visibility = Visibility.Visible;
            }
        }

        private void comboBox_CurrentTrack_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox? comboBox = sender as ComboBox;
            if (comboBox == null || comboBox.SelectedIndex == -1)
            {
                return;
            }

            int index = comboBox.SelectedIndex;
            string fileName = _mediaFiles[index];
            Play(fileName);
        }

        private void checkBox_Randomize_Click(object sender, RoutedEventArgs e)
        {
            _randomPlayback = false;
            if (checkBox_Randomize.IsChecked == true)
            {
                _randomPlayback = true;
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            MediaPlayerControlDispose();
        }

        private void MediaPlayerControlDispose()
        {
            this.Unloaded -= UserControl_Unloaded;
            comboBox_CurrentTrack.SelectionChanged -= comboBox_CurrentTrack_SelectionChanged;
                    
            comboBox_CurrentTrack.ItemsSource = null;

            if (_mediaPlayer != null)
            {
                try
                {
                    if (_mediaPlayer.IsPlaying || _isPaused)
                    {
                        _mediaPlayer.Stop();
                    }

                    _mediaPlayer.EndReached -= MediaPlayer_EndReached;
                    _mediaPlayer.Dispose();
                }
                catch
                {
                    // Log or handle cleanup errors if needed
                }
                finally
                {
                    _mediaPlayer = null!;
                }
            }

            if (_libVLC != null)
            {
                try
                {
                    _libVLC.Dispose();
                }
                catch
                {
                    // Log or handle cleanup errors
                }
                finally
                {
                    _libVLC = null!;
                }
            }
        }


        public void InitializeVLC(bool enableVisualizations)
        {
            try
            {
                if (_mediaPlayer != null)
                {
                    MediaPlayerControlDispose();
                }

                var vlcOptions = new List<string>();
                if (enableVisualizations)
                {
                    vlcOptions.Add("--audio-visual=visual");
                    vlcOptions.Add("--effect-width=50");
                    vlcOptions.Add("--effect-height=50");
                    vlcOptions.Add("--effect-list=spectrometer"); var libVlcDirectory = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vlc"));
                    vlcOptions.Add($"--plugin-path={libVlcDirectory}");
                }
                vlcOptions.Add("--network-caching=3000");
                vlcOptions.Add("--file-caching=3000");
                var options = vlcOptions.ToArray();
                _libVLC = new LibVLC(options);
                _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
                VideoView.MediaPlayer = _mediaPlayer;
                sliderVolume.Value = _volume;
                _mediaPlayer.EndReached += MediaPlayer_EndReached!;
            }
            catch
            {
                // later
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer == null)
            {
                return;
            }
            _volume = (int)sliderVolume.Value;
            _mediaPlayer.Volume = _volume;
        }

        public void SetVolume(int volume)
        {
            _volume = volume;
            if (_mediaPlayer != null)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                sliderVolume.Value = _volume;
            });
        }

        private void sliderVolume_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                // Get the mouse position relative to the slider
                var position = e.GetPosition(slider);

                // Calculate the ratio of the mouse position to the slider width
                var relativePosition = position.X / slider.ActualWidth;

                // Determine the new Value
                var newValue = relativePosition * (slider.Maximum - slider.Minimum) + slider.Minimum;

                // Set the slider Value
                slider.Value = newValue;
            }
        }
    }
}

