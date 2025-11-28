using System.Windows;
using GamelistManager.controls;

namespace GamelistManager
{
    public partial class MediaPlayerWindow : Window
    {
        private MediaPlayerControl _mediaPlayerControl;

        public MediaPlayerWindow(string[] fileNames)
        {
            InitializeComponent();

            // Create MediaPlayerControl in code-behind
            _mediaPlayerControl = new MediaPlayerControl();
            _mediaPlayerControl.SetVolume((int)Properties.Settings.Default.Volume);

            // Add it to the grid
            grid_MainGrid.Children.Add(_mediaPlayerControl);

            // Start playback
            _mediaPlayerControl.PlayMedia(fileNames, true);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_mediaPlayerControl != null)
            {
                _mediaPlayerControl.StopPlaying();
                _mediaPlayerControl.MediaPlayerControlDispose();
                grid_MainGrid.Children.Clear();
                _mediaPlayerControl = null!;
            }
        }
    }
}
