using System;
using System.Runtime.InteropServices;

namespace Gamelist_Manager.Native.Mpv;

internal static class MpvNative
{
    #region Constants

    private const string ResolverName = "mpv";
    internal const string WindowsLibrary = "libmpv-2.dll";
    internal const string LinuxLibrary = "libmpv.so.2";

    #endregion

    #region Library Probe

    public static bool IsLibraryPresent()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return NativeLibrary.TryLoad(LinuxLibrary, out var handle1)
                ? RetainAndFree(handle1)
                : NativeLibrary.TryLoad("libmpv.so.1", out var handle2) && RetainAndFree(handle2);
        }

        var baseDir = AppContext.BaseDirectory;
        var libFolder = System.IO.Path.Combine(baseDir, "lib");
        var libPath = System.IO.Path.Combine(libFolder, WindowsLibrary);
        if (System.IO.File.Exists(libPath) && NativeLibrary.TryLoad(libPath, out var winHandle))
            return RetainAndFree(winHandle);

        return NativeLibrary.TryLoad(WindowsLibrary, out var fallback) && RetainAndFree(fallback);
    }

    private static bool RetainAndFree(IntPtr handle)
    {
        // TryLoad returns a ref-counted handle. Freeing once balances the ref.
        // The DllImportResolver will acquire its own ref later when needed.
        try { NativeLibrary.Free(handle); } catch { }
        return true;
    }

    #endregion

    #region Static Constructor — DllImport Resolver

    static MpvNative()
    {
        NativeLibrary.SetDllImportResolver(typeof(MpvNative).Assembly, ResolveMpvLibrary);
    }

    private static IntPtr ResolveMpvLibrary(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != ResolverName) return IntPtr.Zero;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (NativeLibrary.TryLoad(LinuxLibrary, out var handle)) return handle;
            if (NativeLibrary.TryLoad("libmpv.so.1", out handle)) return handle;
            return IntPtr.Zero;
        }

        var baseDir = AppContext.BaseDirectory;
        var libFolder = System.IO.Path.Combine(baseDir, "lib");
        var libPath = System.IO.Path.Combine(libFolder, WindowsLibrary);

        if (System.IO.File.Exists(libPath) && NativeLibrary.TryLoad(libPath, out var winHandle))
            return winHandle;

        return NativeLibrary.TryLoad(WindowsLibrary, out var fallback) ? fallback : IntPtr.Zero;
    }

    #endregion

    #region P/Invoke — Core

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_create();

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_initialize(IntPtr ctx);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_terminate_destroy(IntPtr ctx);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_client_name(IntPtr ctx);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong mpv_client_api_version();

    #endregion

    #region P/Invoke — Options & Properties

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_option_string(IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_property_string(IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_get_property_string(IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_property(IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        MpvFormat format, ref long data);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_property(IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        MpvFormat format, ref double data);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_get_property(IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        MpvFormat format, ref double data);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_observe_property(IntPtr ctx, ulong replyId,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name, MpvFormat format);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_unobserve_property(IntPtr ctx, ulong replyId);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_request_log_messages(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string minLevel);

    #endregion

    #region P/Invoke — Commands

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_command(IntPtr ctx, IntPtr[] args);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_command_async(IntPtr ctx, ulong replyId, IntPtr[] args);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_command_string(IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string args);

    #endregion

    #region P/Invoke — Events

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_wait_event(IntPtr ctx, double timeout);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_wakeup(IntPtr ctx);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_set_wakeup_callback(IntPtr ctx, MpvWakeupCallback? callback, IntPtr userData);

    #endregion

    #region P/Invoke — Render

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_render_context_create(out IntPtr ctx, IntPtr mpvHandle, IntPtr paramsPtr);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_render_context_set_parameter(IntPtr ctx, MpvRenderParam param);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_render_context_render(IntPtr ctx, IntPtr paramsPtr);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_render_context_report_swap(IntPtr ctx);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong mpv_render_context_update(IntPtr ctx);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_render_context_set_update_callback(IntPtr ctx,
        MpvRenderUpdateCallback? callback, IntPtr userData);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_render_context_free(IntPtr ctx);

    #endregion

    #region P/Invoke — Utilities

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_free(IntPtr ptr);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_error_string(int error);

    [DllImport(ResolverName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_event_name(MpvEventId eventId);

    #endregion
}

internal delegate void MpvWakeupCallback(IntPtr userData);

internal delegate void MpvRenderUpdateCallback(IntPtr userData);
