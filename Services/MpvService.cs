using Gamelist_Manager.Native.Mpv;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.Services;

public static class MpvService
{
    #region Public Properties

    public static bool IsMpvAvailable { get; private set; } = true;
    public static void MarkMpvUnavailable() => IsMpvAvailable = false;

    #endregion

    #region Initialization

    public static readonly Lazy<Task<bool>> InitializationTask = new(
        VerifyMpvAsync, LazyThreadSafetyMode.ExecutionAndPublication);

    private static async Task<bool> VerifyMpvAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                using var ctx = MpvContext.Create();
                ctx.Initialize();
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"WARNING: libmpv initialization failed: {ex.Message}");
                MarkMpvUnavailable();
                return false;
            }
        });
    }

    #endregion

    #region Factory

    public static MpvContext? CreateContext()
    {
        if (!IsMpvAvailable) return null;

        try
        {
            var ctx = MpvContext.Create();
            ctx.Initialize();
            return ctx;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"WARNING: failed to create MpvContext: {ex.Message}");
            MarkMpvUnavailable();
            return null;
        }
    }

    #endregion

    #region Native Library Probe

    public static bool IsNativeLibraryPresent() => MpvNative.IsLibraryPresent();

    #endregion
}
