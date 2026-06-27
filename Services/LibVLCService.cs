using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.Services;

public static class LibVLCService
{
    public static bool IsLibVLCInstalled { get; private set; } = true;
    public static void MarkLibVLCUnavailable() => IsLibVLCInstalled = false;

    public static readonly Lazy<Task<LibVLC?>> InitializationTask = new(
        () => Task.Run(CreateLibVLC), LazyThreadSafetyMode.ExecutionAndPublication);

    private static LibVLC? CreateLibVLC()
    {
        try
        {
            var options = new List<string>
            {
                "--no-video-title-show",
                "--no-stats",
                "--no-snapshot-preview",
                "--no-sub-autodetect-file"
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                options.Add("--aout=pulse");

            return new LibVLC(options.ToArray());
        }
        catch
        {
            IsLibVLCInstalled = false;
            return null;
        }
    }
}
