using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Reflection;

namespace Gamelist_Manager.ViewModels;

public partial class AboutWindowViewModel : ViewModelBase
{
    #region Public Properties

    public string Version { get; }

    #endregion

    #region Events

    public event System.Action? CloseRequested;

    #endregion

    #region Constructor

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

    #endregion

    #region Commands

    [RelayCommand]
    private void OpenGitHub() => OpenUrl("https://github.com/RobG66/Gamelist-Manager");

    [RelayCommand]
    private void OpenGitHubSponsors() => OpenUrl("https://github.com/sponsors/RobG66");

    [RelayCommand]
    private void OpenKofi() => OpenUrl("https://ko-fi.com/robg66");

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    #endregion

    #region Private Methods

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

    #endregion
}
