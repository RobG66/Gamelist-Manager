using GamelistManager.controls;
using System.ComponentModel;
using System.Windows;

namespace GamelistManager
{
    public partial class MediaPlayerWindow : Window
    {
        private MediaPlayerControl _mediaPlayerControl;
        private bool _isClosing;


        public MediaPlayerWindow(string[] fileNames)
        {
            InitializeComponent();

            // Create MediaPlayerControl in code-behind
            _mediaPlayerControl = new MediaPlayerControl();
            _mediaPlayerControl.SetVolume((int)Properties.Settings.Default.Volume);

            // Add it to the grid
            grid_MainGrid.Children.Add(_mediaPlayerControl);

            // Start playback
            _ = _mediaPlayerControl.PlayMediaFilesAsync(fileNames, true);
        }


        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_isClosing)
                return;

            e.Cancel = true; 
            _isClosing = true;

            if (_mediaPlayerControl != null)
            {
                await _mediaPlayerControl.StopPlayingAsync(); 
                _mediaPlayerControl.MediaPlayerControlDispose();
                grid_MainGrid.Children.Clear();
                _mediaPlayerControl = null!;
            }

            Close();
        }


    }
}
