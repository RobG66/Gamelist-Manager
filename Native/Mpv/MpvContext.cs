using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Gamelist_Manager.Native.Mpv;

public sealed class MpvContext : IDisposable
{
    #region Fields & Constants

    private IntPtr _handle;
    private Thread? _eventThread;
    private CancellationTokenSource _cts = new();
    private readonly object _commandLock = new();
    private bool _disposed;

    #endregion

    #region Public Properties

    public IntPtr Handle => _handle;
    public bool IsInitialized => _handle != IntPtr.Zero;
    public bool IsMuted { get; private set; }
    public int Volume { get; private set; } = 100;
    public bool IsRenderContextAttached { get; set; }



    public event Action? FileLoaded;
    public event Action? EndFile;
    public event Action? Shutdown;
    public Action? RenderContextAttached { get; set; }
    public event Action? VideoReconfig;

    #endregion

    #region Public Methods

    public static MpvContext Create()
    {
        var handle = MpvNative.mpv_create();
        if (handle == IntPtr.Zero)
            throw new InvalidOperationException("mpv_create returned null handle");

        var ctx = new MpvContext { _handle = handle };
        ctx.ApplyDefaults();
        return ctx;
    }

    public void Initialize()
    {
        if (_handle == IntPtr.Zero) return;
        var err = MpvNative.mpv_initialize(_handle);
        if (err < 0)
            throw new InvalidOperationException($"mpv_initialize failed: {MpvErrorToString(err)}");

        StartEventLoop();
    }

    public int SetOptionString(string name, string value)
    {
        if (_handle == IntPtr.Zero) return -1;
        return MpvNative.mpv_set_option_string(_handle, name, value);
    }

    public int SetPropertyString(string name, string value)
    {
        if (_handle == IntPtr.Zero) return -1;
        return MpvNative.mpv_set_property_string(_handle, name, value);
    }

    public string? GetPropertyString(string name)
    {
        if (_handle == IntPtr.Zero) return null;
        var ptr = MpvNative.mpv_get_property_string(_handle, name);
        if (ptr == IntPtr.Zero) return null;
        try
        {
            return Marshal.PtrToStringUTF8(ptr);
        }
        finally
        {
            MpvNative.mpv_free(ptr);
        }
    }

    public int SetPropertyLong(string name, long value)
    {
        if (_handle == IntPtr.Zero) return -1;
        return MpvNative.mpv_set_property(_handle, name, MpvFormat.Int64, ref value);
    }

    public int SetPropertyDouble(string name, double value)
    {
        if (_handle == IntPtr.Zero) return -1;
        return MpvNative.mpv_set_property(_handle, name, MpvFormat.Double, ref value);
    }

    public int SetVolume(int volume)
    {
        Volume = Math.Clamp(volume, 0, 100);
        return SetPropertyLong("volume", Volume);
    }

    public void SetMute(bool mute)
    {
        IsMuted = mute;
        SetPropertyString("mute", mute ? "yes" : "no");
    }

    public int Command(params string[] args)
    {
        if (_handle == IntPtr.Zero || args.Length == 0) return -1;
        lock (_commandLock)
        {
            var pointers = new List<IntPtr>(args.Length + 1);
            try
            {
                foreach (var arg in args)
                    pointers.Add(Marshal.StringToCoTaskMemUTF8(arg));
                pointers.Add(IntPtr.Zero);

                return MpvNative.mpv_command(_handle, pointers.ToArray());
            }
            finally
            {
                foreach (var p in pointers)
                    if (p != IntPtr.Zero)
                        Marshal.FreeCoTaskMem(p);
            }
        }
    }

    public int CommandAsync(ulong replyId, params string[] args)
    {
        if (_handle == IntPtr.Zero || args.Length == 0) return -1;
        lock (_commandLock)
        {
            var pointers = new List<IntPtr>(args.Length + 1);
            try
            {
                foreach (var arg in args)
                    pointers.Add(Marshal.StringToCoTaskMemUTF8(arg));
                pointers.Add(IntPtr.Zero);

                return MpvNative.mpv_command_async(_handle, replyId, pointers.ToArray());
            }
            finally
            {
                foreach (var p in pointers)
                    if (p != IntPtr.Zero)
                        Marshal.FreeCoTaskMem(p);
            }
        }
    }

    public int LoadFile(string path, string mode = "replace")
    {
        return Command("loadfile", path, mode);
    }

    public void Play() => SetPropertyString("pause", "no");

    public void Pause() => SetPropertyString("pause", "yes");

    public void Stop() => Command("stop");

    public bool GetPauseFlag()
    {
        var s = GetPropertyString("pause");
        return s == "yes";
    }

    public bool IsPlaying => !GetPauseFlag();

    public void Seek(double seconds)
    {
        SetPropertyDouble("time-pos", seconds);
    }

    public void Wakeup()
    {
        if (_handle != IntPtr.Zero)
            MpvNative.mpv_wakeup(_handle);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        Wakeup();

        try
        {
            _eventThread?.Join(500);
        }
        catch { }

        _cts.Dispose();

        if (_handle != IntPtr.Zero)
        {
            try { MpvNative.mpv_terminate_destroy(_handle); } catch { }
            _handle = IntPtr.Zero;
        }

        FileLoaded = null;
        EndFile = null;
        Shutdown = null;
    }

    #endregion

    #region Private Methods

    private void ApplyDefaults()
    {
        SetOptionString("hwdec", "auto");
        SetOptionString("vo", "libmpv");
        SetOptionString("terminal", "no");
        SetOptionString("msg-level", "all=warn");
        SetOptionString("keep-open", "yes");
        SetOptionString("loop-file", "inf");
        SetOptionString("input-default-bindings", "no");
        SetOptionString("input-vo-keyboard", "no");
        SetOptionString("osc", "no");
        SetOptionString("osd-level", "0");
        SetOptionString("force-window", "no");
    }

    private void StartEventLoop()
    {
        _eventThread = new Thread(EventLoopProc)
        {
            Name = "mpv-event-loop",
            IsBackground = true
        };
        _eventThread.Start();
    }

    private void EventLoopProc()
    {
        var token = _cts.Token;
        while (!token.IsCancellationRequested && _handle != IntPtr.Zero)
        {
            var evtPtr = MpvNative.mpv_wait_event(_handle, 0.1);
            if (evtPtr == IntPtr.Zero) break;

            var evt = Marshal.PtrToStructure<MpvEvent>(evtPtr);
            if (evt.EventId == MpvEventId.None) continue;

            switch (evt.EventId)
            {
                case MpvEventId.FileLoaded:
                    FileLoaded?.Invoke();
                    break;
                case MpvEventId.EndFile:
                    EndFile?.Invoke();
                    break;
                case MpvEventId.Shutdown:
                    Shutdown?.Invoke();
                    return;
                case MpvEventId.VideoReconfig:
                    VideoReconfig?.Invoke();
                    break;
            }
        }
    }

    private static string MpvErrorToString(int error)
    {
        var ptr = MpvNative.mpv_error_string(error);
        return ptr == IntPtr.Zero ? $"error {error}" : Marshal.PtrToStringUTF8(ptr) ?? $"error {error}";
    }

    #endregion

}
