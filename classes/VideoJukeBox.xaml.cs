using GamelistManager.classes;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GamelistManager
{
    public partial class JukeBoxWindow : Window
    {
        private readonly string[] _filePaths;
        private readonly Random _random;
        private LibVLC _libVLC = null!;
        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer = null!;
        private int _currentIndex;
        private bool _isPaused = false;
        private int _lastIndex;
        private bool _randomPlayback;
        private bool _isVideo;


        public JukeBoxWindow(string[] paths, bool boolValue)
        {
            InitializeComponent();
            _random = new Random();
            _filePaths = paths;
            _randomPlayback = false;
            _currentIndex = 0;
            _lastIndex = paths.Length - 1;
            _isVideo = boolValue;

            var fileNames = Array.ConvertAll(_filePaths, Path.GetFileName);
            comboBox_CurrentTrack.ItemsSource = fileNames;

            InitializeVLC();

            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            string systemName = textInfo.ToTitleCase(SharedData.CurrentSystem);

            string typeOfJukebox = _isVideo ? "Video" : "Music";
            this.Title = $"{systemName} {typeOfJukebox} Jukebox";

            PlayCurrentVideo(0);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                AnimateButton(button);

                // slight delay
                await Task.Delay(250);

                // Process the method based on button name
                switch (button.Name)
                {
                    case "button_Previous":
                        PlayNextVideo(-1);
                        break;
                    case "button_Pause":
                        PauseVideo();
                        break;
                    case "button_Stop":
                        StopVideo();
                        break;
                    case "button_Play":
                        PlayVideoOrResume();
                        break;
                    case "button_Next":
                        PlayNextVideo(1);
                        break;
                }
            }
        }


        private void InitializeVLC()
        {
            var vlcOptions = new List<string>();
            if (!_isVideo)
            {
                // string selectedVisualization = "Goom"; // Default to Goom
                vlcOptions.Add("--audio-visual=visual");
                vlcOptions.Add("--effect-width=50");
                vlcOptions.Add("--effect-height=50");
                vlcOptions.Add("--effect-list=spectrometer"); var libVlcDirectory = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vlc"));
                vlcOptions.Add($"--plugin-path={libVlcDirectory}");
            }

            vlcOptions.Add("--network-caching=3000");
            vlcOptions.Add("--file-caching=3000");
            //vlcOptions.Add("--video-title-position=8");

            var options = vlcOptions.ToArray();

            _libVLC = new LibVLC(options);
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            _mediaPlayer.EndReached += MediaPlayer_EndReached!;

            VideoView.MediaPlayer = _mediaPlayer;
        }

        private void PlayVideo(string fileName)
        {
            var media = new Media(_libVLC, new Uri(fileName));
            _mediaPlayer.Media = media;
            _mediaPlayer.Play();

            UpdateButtonStates(isPlaying: true);
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        _mediaPlayer.Stop();
                        PlayNextVideo(1);
                    }
                    catch
                    {
                        // Handle exceptions as needed
                    }
                });
            });
        }

        private void PlayNextVideo(int modifier)
        {
            if (_randomPlayback)
            {
                _currentIndex = _random.Next(0, _lastIndex + 1);
            }
            else
            {
                _currentIndex += modifier;

                if (_currentIndex > _lastIndex || _currentIndex < 0)
                {
                    _currentIndex = 0;
                }
            }
            PlayCurrentVideo(_currentIndex);
        }

        private void PlayCurrentVideo(int index)
        {
            string fileName = _filePaths[index];
            comboBox_CurrentTrack.SelectedIndex = index;
            PlayVideo(fileName);
        }

        private void PauseVideo()
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                _isPaused = true;
                UpdateButtonStates(isPlaying: false);
            }
        }

        private void StopVideo()
        {
            _mediaPlayer?.Stop();
            _isPaused = false;
            UpdateButtonStates(isPlaying: false);
        }

        private void PlayVideoOrResume()
        {
            if (_mediaPlayer.IsPlaying)
            {
                return;
            }

            if (_isPaused)
            {
                _mediaPlayer.Play();
            }
            else
            {
                PlayCurrentVideo(_currentIndex);
            }

            _isPaused = false;
            UpdateButtonStates(isPlaying: true);
        }

        private void UpdateButtonStates(bool isPlaying)
        {
            button_Play.IsEnabled = !isPlaying;
            button_Pause.IsEnabled = isPlaying;
            button_Stop.IsEnabled = isPlaying;
            button_Next.IsEnabled = isPlaying;
            button_Previous.IsEnabled = isPlaying;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_mediaPlayer.IsPlaying || _isPaused)
            {
                _mediaPlayer.Stop();
            }
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
            base.OnClosed(e);
        }

        private void checkBox_Randomize_Click(object sender, RoutedEventArgs e)
        {
            _randomPlayback = checkBox_Randomize.IsChecked == true;
        }

        private void AnimateButton(Button button)
        {
            // Create and configure the animation
            Storyboard storyboard = new Storyboard();

            // ScaleX Animation
            DoubleAnimation scaleXAnimation = new DoubleAnimation
            {
                To = 0.9,
                Duration = new Duration(TimeSpan.FromSeconds(0.08)),
                AutoReverse = true
            };
            Storyboard.SetTarget(scaleXAnimation, button);
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("(Button.RenderTransform).(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleXAnimation);

            // ScaleY Animation
            DoubleAnimation scaleYAnimation = new DoubleAnimation
            {
                To = 0.9,
                Duration = new Duration(TimeSpan.FromSeconds(0.08)),
                AutoReverse = true
            };
            Storyboard.SetTarget(scaleYAnimation, button);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("(Button.RenderTransform).(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleYAnimation);

            // Set a ScaleTransform as RenderTransform of the button
            button.RenderTransform = new ScaleTransform();

            // Start the animation
            storyboard.Begin();

        }

        private void comboBox_CurrentTrack_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = comboBox_CurrentTrack.SelectedIndex;
            PlayCurrentVideo(index);
        }

        private void button_Playlist_Click_1(object sender, RoutedEventArgs e)
        {
            // The animate button is a method because I had difficulty with it
            // in a style.  Changing the mediaplayer file would interfere with the 
            // button animations.  This way, I can do the animation and then other stuff
            // in order.  Maybe there's another solution, I tried several.
            if (sender is Button button)
            {
                AnimateButton(button);
            }

            if (stackPanel_FileSelector.Visibility == Visibility.Visible)
            {
                stackPanel_FileSelector.Visibility = Visibility.Collapsed;
            }
            else
            {
                stackPanel_FileSelector.Visibility = Visibility.Visible;
            }
        }
    }
}
