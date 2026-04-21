using Avalonia;
using System;
using System.Threading;

namespace Gamelist_Manager;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Use a named mutex to ensure only one instance runs at a time.
        // The name is prefixed with Local\ so it works for the current user session on both Windows and Linux.
        using var mutex = new Mutex(true, @"Local\GamelistManager_SingleInstance", out bool isNewInstance);
        if (!isNewInstance)
            return;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions { OverlayPopups = false })
            .With(new X11PlatformOptions { OverlayPopups = false })
            .WithInterFont()
            .LogToTrace();
}