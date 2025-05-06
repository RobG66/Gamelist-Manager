using System.Windows;

// This is just a window wrapper for the mediaplayercontrol

namespace GamelistManager
{
    public partial class MediaPlayerWindow : Window
    {
        public MediaPlayerWindow(string[] fileNames)
        {
            InitializeComponent();
            mediaPlayerControl.SetVolume((int)Properties.Settings.Default.Volume);
            mediaPlayerControl.PlayMedia(fileNames, true);
        }
    }
}
