using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Reflection;

namespace Gamelist_Manager.ViewModels
{
    public partial class AboutWindowViewModel : ViewModelBase
    {
        public string Version { get; }

        // Event to notify view to close
        public event System.Action? CloseRequested;

        public AboutWindowViewModel()
        {
            var infoVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "Unknown";
            var plusIndex = infoVersion.IndexOf('+');
            if (plusIndex >= 0)
                infoVersion = infoVersion[..plusIndex];
            Version = $"Version {infoVersion}";
        }

        [RelayCommand]
        private void OpenGitHub()
        {
            OpenUrl("https://github.com/RobG66/Gamelist-Manager");
        }

        [RelayCommand]
        private void OpenGitHubSponsors()
        {
            OpenUrl("https://github.com/sponsors/RobG66");
        }

        [RelayCommand]
        private void OpenKofi()
        {
            OpenUrl("https://ko-fi.com/robg66");
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke();
        }

        private static void OpenUrl(string url)
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
            }
        }
    }
}
