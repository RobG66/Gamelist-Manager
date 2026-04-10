using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Gamelist_Manager.ViewModels;
using Gamelist_Manager.Views;
using LibVLCSharp.Shared;
using System.Linq;

namespace Gamelist_Manager;

public partial class App : Application
{
    public override void Initialize()
    {
        // Initialize LibVLC core library for video playback
        try
        {
            Core.Initialize();
            // Fire-and-forget: start the expensive LibVLC engine construction immediately
            // so it's ready (or nearly ready) by the time the user opens the media panel.
            _ = MediaPreviewViewModel.PreloadLibVLCAsync();
        }
        catch (System.Exception ex)
        {
            // Log LibVLC initialization error
            System.Console.WriteLine("WARNING: LibVLC initialization failed. Video playback will not be available.");
            System.Console.WriteLine($"Error: {ex.Message}");

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                System.Console.WriteLine("\nOn Linux, make sure you have installed:");
                System.Console.WriteLine("  sudo apt-get install vlc libvlc5 libvlccore9");
                System.Console.WriteLine("or");
                System.Console.WriteLine("  sudo dnf install vlc vlc-core");
                System.Console.WriteLine("or");
                System.Console.WriteLine("  sudo pacman -S vlc");
            }

            // Don't crash the app - continue without video support
        }

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // Load and apply saved theme settings before showing the main window
            SettingsViewModel.LoadAndApplySettingsOnStartup();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}