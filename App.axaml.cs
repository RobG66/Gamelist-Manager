using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Gamelist_Manager.ViewModels;
using Gamelist_Manager.Views;

namespace Gamelist_Manager;

public partial class App : Application
{
    public override void Initialize()
    {
        if (Services.MpvService.IsNativeLibraryPresent())
        {
            try
            {
                _ = Services.MpvService.InitializationTask.Value;
            }
            catch (System.Exception ex)
            {
                Services.MpvService.MarkMpvUnavailable();
                System.Console.WriteLine("WARNING: libmpv initialization failed. Video playback will not be available.");
                System.Console.WriteLine($"Error: {ex.Message}");
            }
        }
        else
        {
            Services.MpvService.MarkMpvUnavailable();
            System.Console.WriteLine("WARNING: libmpv native library not found. Video playback will not be available.");

            if (System.OperatingSystem.IsLinux())
            {
                System.Console.WriteLine("On Linux, install libmpv via your package manager, for example:");
                System.Console.WriteLine("  sudo apt install libmpv2");
                System.Console.WriteLine("  sudo dnf install libmpv");
                System.Console.WriteLine("  sudo pacman -S libmpv");
            }
            else
            {
                System.Console.WriteLine("On Windows, place libmpv-2.dll in the lib/ folder next to the executable.");
                System.Console.WriteLine("See lib/README.md for details.");
            }
        }

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            SettingsViewModel.ApplyThemeOnStartup();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(new Services.AvaloniaWindowOwnerProvider()),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
