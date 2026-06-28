using Gamelist_Manager.Native.Mpv;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.Services;

public static class MpvService
{
    #region Fields & Constants

    public static readonly Lazy<Task<bool>> InitializationTask = new(
        VerifyMpvAsync, LazyThreadSafetyMode.ExecutionAndPublication);

    #endregion

    #region Public Properties

    public static bool IsMpvAvailable { get; private set; } = true;

    #endregion

    #region Public Methods

    public static void MarkMpvUnavailable() => IsMpvAvailable = false;

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

    public static bool IsNativeLibraryPresent() => MpvNative.IsLibraryPresent();

    #endregion

    #region Private Methods

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
}
