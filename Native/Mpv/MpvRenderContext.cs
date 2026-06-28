using System;
using System.Runtime.InteropServices;

namespace Gamelist_Manager.Native.Mpv;

internal sealed class MpvRenderContext : IDisposable
{
    #region Fields & Constants

    private IntPtr _handle;
    private MpvRenderUpdateCallback? _updateCallback;
    private Action? _updateHandler;
    private bool _disposed;

    #endregion

    #region Public Properties

    public IntPtr Handle => _handle;
    public bool IsCreated => _handle != IntPtr.Zero;

    #endregion

    #region Public Methods

    public static MpvRenderContext Create(MpvContext context, IntPtr getProcAddressFn, IntPtr getProcAddressCtx)
    {
        if (context == null || !context.IsInitialized)
            throw new InvalidOperationException("MpvContext is not initialized");

        var apiTypePtr = Marshal.StringToHGlobalAnsi(MpvRenderApiType.Opengl);
        var initParams = new MpvOpenglInitParams
        {
            GetProcAddress = getProcAddressFn,
            GetProcAddressCtx = getProcAddressCtx
        };
        var initParamsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<MpvOpenglInitParams>());
        Marshal.StructureToPtr(initParams, initParamsPtr, false);

        var paramsArray = new[]
        {
            new MpvRenderParam { Type = MpvRenderParamType.ApiType, Data = apiTypePtr },
            new MpvRenderParam { Type = MpvRenderParamType.OpenglInitParams, Data = initParamsPtr },
            new MpvRenderParam { Type = MpvRenderParamType.Invalid, Data = IntPtr.Zero }
        };

        var paramsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<MpvRenderParam>() * paramsArray.Length);
        try
        {
            var structureSize = Marshal.SizeOf<MpvRenderParam>();
            for (var i = 0; i < paramsArray.Length; i++)
            {
                Marshal.StructureToPtr(paramsArray[i], paramsPtr + i * structureSize, false);
            }

            var err = MpvNative.mpv_render_context_create(out var renderCtx, context.Handle, paramsPtr);
            if (err < 0)
            {
                var msg = Marshal.PtrToStringUTF8(MpvNative.mpv_error_string(err)) ?? $"error {err}";
                throw new InvalidOperationException($"mpv_render_context_create failed: {msg}");
            }

            return new MpvRenderContext { _handle = renderCtx };
        }
        finally
        {
            Marshal.FreeHGlobal(paramsPtr);
            Marshal.FreeHGlobal(initParamsPtr);
            Marshal.FreeHGlobal(apiTypePtr);
        }
    }

    public void SetUpdateCallback(Action? handler)
    {
        _updateHandler = handler;
        if (handler == null)
        {
            MpvNative.mpv_render_context_set_update_callback(_handle, null, IntPtr.Zero);
            _updateCallback = null;
        }
        else
        {
            _updateCallback = _ => _updateHandler?.Invoke();
            MpvNative.mpv_render_context_set_update_callback(_handle, _updateCallback, IntPtr.Zero);
        }
    }

    public MpvRenderUpdateFlags Update()
    {
        if (_handle == IntPtr.Zero) return MpvRenderUpdateFlags.None;
        return (MpvRenderUpdateFlags)MpvNative.mpv_render_context_update(_handle);
    }

    public int Render(int fbo, int width, int height)
    {
        if (_handle == IntPtr.Zero) return -1;

        var openglFbo = new MpvOpenglFbo
        {
            Fbo = fbo,
            Width = width,
            Height = height,
            InternalFormat = 0
        };
        var fboPtr = Marshal.AllocHGlobal(Marshal.SizeOf<MpvOpenglFbo>());
        Marshal.StructureToPtr(openglFbo, fboPtr, false);

        var flipY = 1;
        var flipYPtr = Marshal.AllocHGlobal(sizeof(int));
        Marshal.WriteInt32(flipYPtr, flipY);

        var paramsArray = new[]
        {
            new MpvRenderParam { Type = MpvRenderParamType.OpenglFbo, Data = fboPtr },
            new MpvRenderParam { Type = MpvRenderParamType.FlipY, Data = flipYPtr },
            new MpvRenderParam { Type = MpvRenderParamType.Invalid, Data = IntPtr.Zero }
        };

        var paramsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<MpvRenderParam>() * paramsArray.Length);
        try
        {
            var structureSize = Marshal.SizeOf<MpvRenderParam>();
            for (var i = 0; i < paramsArray.Length; i++)
            {
                Marshal.StructureToPtr(paramsArray[i], paramsPtr + i * structureSize, false);
            }

            return MpvNative.mpv_render_context_render(_handle, paramsPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(paramsPtr);
            Marshal.FreeHGlobal(fboPtr);
            Marshal.FreeHGlobal(flipYPtr);
        }
    }

    public void ReportSwap()
    {
        if (_handle != IntPtr.Zero)
            MpvNative.mpv_render_context_report_swap(_handle);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_handle != IntPtr.Zero)
        {
            try
            {
                MpvNative.mpv_render_context_set_update_callback(_handle, null, IntPtr.Zero);
                MpvNative.mpv_render_context_free(_handle);
            }
            catch { }
            _handle = IntPtr.Zero;
        }

        _updateCallback = null;
        _updateHandler = null;
    }

    #endregion
}
