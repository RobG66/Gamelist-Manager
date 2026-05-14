using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Gamelist_Manager.ViewModels;
using Gamelist_Manager.Views;
using LibVLCSharp.Shared;
using System.Net.Http;

namespace Gamelist_Manager;

public partial class App : Application
{
    public static HttpClient HttpClient { get; internal set; }

    public override void Initialize()
    {
        // Probe for the native libvlc library before calling any LibVLC API.
        // Core.Initialize() triggers native code; if the library is missing or
        // incompatible it causes a native crash (SIGSEGV/SIGABRT) that a
        // try/catch cannot intercept.
        if (IsLibVLCNativeLibraryPresent())
        {
            try
            {
                Core.Initialize();
                // Fire-and-forget: start the expensive LibVLC engine construction immediately
                // so it's ready (or nearly ready) by the time the user opens the media panel.
                _ = MediaPreviewViewModel.PreloadLibVLCAsync();
            }
            catch (System.Exception ex)
            {
                MediaPreviewViewModel.MarkLibVLCUnavailable();
                System.Console.WriteLine("WARNING: LibVLC initialization failed. Video playback will not be available.");
                System.Console.WriteLine($"Error: {ex.Message}");
            }
        }
        else
        {
            MediaPreviewViewModel.MarkLibVLCUnavailable();
            System.Console.WriteLine("WARNING: libvlc native library not found. Video playback will not be available.");

            if (System.OperatingSystem.IsLinux())
            {
                System.Console.WriteLine("On Linux, install VLC via your package manager, for example:");
                System.Console.WriteLine("  sudo apt-get install vlc libvlc5 libvlccore9");
                System.Console.WriteLine("  sudo dnf install vlc vlc-core");
                System.Console.WriteLine("  sudo pacman -S vlc");
            }
        }

        AvaloniaXamlLoader.Load(this);
    }

    private static bool IsLibVLCNativeLibraryPresent()
    {
        if (System.OperatingSystem.IsLinux())
        {
            return System.IO.File.Exists("/usr/lib/x86_64-linux-gnu/libvlc.so.5")
                || System.IO.File.Exists("/usr/lib64/libvlc.so.5")
                || System.IO.File.Exists("/usr/lib/libvlc.so.5");
        }

        // On Windows, libvlc.dll is bundled alongside the executable
        return System.IO.File.Exists(
            System.IO.Path.Combine(System.AppContext.BaseDirectory, "libvlc.dll"));
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Load and apply saved theme settings before showing the main window
            SettingsViewModel.ApplyThemeOnStartup();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}