using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace GamelistManager
{
    public partial class AboutWindow : Window
    {
        public string Version { get; set; }

        public AboutWindow()
        {
            InitializeComponent();
            string filePath = Assembly.GetExecutingAssembly().Location;
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
            string fileVersion = fileVersionInfo.FileVersion!;
            Version = $"Version {fileVersion}";
            DataContext = this;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void GitHubLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenLink("https://github.com/RobG66/Gamelist-Manager");
        }

        private void PaypalLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenLink("https://paypal.me/RobG");
        }

        private void BMAC_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenLink("https://buymeacoffee.com/silverballb");
        }

        private void OpenLink(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show($"Unable to open link: {url}");
            }
        }
    }
}
