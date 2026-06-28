using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Gamelist_Manager.Native.Mpv;
using System;
using System.Runtime.InteropServices;

namespace Gamelist_Manager.Views.Controls;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate IntPtr MpvGetProcAddressFn(IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string name);

public sealed class MpvVideoView : OpenGlControlBase
{
    #region Fields & Constants

    private MpvContext? _mediaPlayer;
    private MpvRenderContext? _renderContext;
    private GlInterface? _glInterface;
    private MpvGetProcAddressFn? _getProcAddressDelegate;
    private bool _playerDirty;
    private double _videoAspectRatio;

    public static readonly StyledProperty<MpvContext?> MediaPlayerProperty =
        AvaloniaProperty.Register<MpvVideoView, MpvContext?>(nameof(MediaPlayer));

    #endregion

    #region Public Properties

    public MpvContext? MediaPlayer
    {
        get => GetValue(MediaPlayerProperty);
        set => SetValue(MediaPlayerProperty, value);
    }

    #endregion

    #region Constructor

    static MpvVideoView()
    {
        MediaPlayerProperty.Changed.AddClassHandler<MpvVideoView>(OnMediaPlayerChanged);
        AffectsRender<MpvVideoView>(MediaPlayerProperty);
    }

    public MpvVideoView()
    {
    }

    #endregion

    #region Property Change Callbacks

    private static void OnMediaPlayerChanged(MpvVideoView view, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is MpvContext oldPlayer)
        {
            oldPlayer.VideoReconfig -= view.OnVideoReconfig;
            if (view._renderContext != null)
            {
                view.DetachRenderContext(oldPlayer);
            }
        }

        view._mediaPlayer = e.NewValue as MpvContext;
        
        if (view._mediaPlayer != null)
        {
            view._mediaPlayer.VideoReconfig += view.OnVideoReconfig;
            view.OnVideoReconfig(); // Fetch immediately
            
            view._playerDirty = true;
            view.RequestNextFrameRendering();
        }
    }

    #endregion

    #region OpenGlControlBase Overrides

    protected override void OnOpenGlInit(GlInterface gl)
    {
        _glInterface = gl;
        _getProcAddressDelegate = GetProcAddress;
        
        // Only mark dirty if we actually have a player but no render context yet
        if (_renderContext == null && _mediaPlayer != null)
        {
            _playerDirty = true;
        }
        
        RequestNextFrameRendering();
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        // Do not destroy the render context here. Avalonia calls this when re-parenting 
        // the control (e.g. Fit to View toggle). Destroying the render context while 
        // mpv is playing breaks the video output track.
        // The context will be destroyed in DetachRenderContext when MediaPlayer changes or becomes null.
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (_mediaPlayer == null)
        {
            if (_renderContext != null)
                DetachRenderContext();
            return;
        }

        if (_playerDirty)
        {
            DetachRenderContext(_mediaPlayer);
            AttachRenderContext(_mediaPlayer);
            _playerDirty = false;
        }

        if (_renderContext == null) return;

        _renderContext.Update();

        var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        var width = Math.Max(1, (int)(Bounds.Width * scaling));
        var height = Math.Max(1, (int)(Bounds.Height * scaling));

        _renderContext.Render(fb, width, height);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_videoAspectRatio > 0)
        {
            double width = availableSize.Width;
            double height = availableSize.Height;
            
            if (double.IsInfinity(width) && double.IsInfinity(height))
            {
                width = 640;
                height = width / _videoAspectRatio;
            }
            else if (double.IsInfinity(width))
            {
                width = height * _videoAspectRatio;
            }
            else if (double.IsInfinity(height))
            {
                height = width / _videoAspectRatio;
            }
            else
            {
                double availRatio = width / height;
                if (availRatio > _videoAspectRatio)
                    width = height * _videoAspectRatio;
                else
                    height = width / _videoAspectRatio;
            }
            
            return new Size(width, height);
        }
        return base.MeasureOverride(availableSize);
    }

    #endregion

    #region Private Methods

    private IntPtr GetProcAddress(IntPtr ctx, string name)
    {
        if (_glInterface == null) return IntPtr.Zero;
        try
        {
            return _glInterface.GetProcAddress(name);
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    private void AttachRenderContext(MpvContext player)
    {
        if (_glInterface == null || _getProcAddressDelegate == null) return;

        try
        {
            var procFnPtr = Marshal.GetFunctionPointerForDelegate(_getProcAddressDelegate);
            _renderContext = MpvRenderContext.Create(player, procFnPtr, IntPtr.Zero);
            _renderContext.SetUpdateCallback(OnRenderUpdate);
            
            player.IsRenderContextAttached = true;
            player.RenderContextAttached?.Invoke();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"WARNING: failed to create mpv render context: {ex.Message}");
            _renderContext = null;
        }
    }

    private void DetachRenderContext(MpvContext? player = null)
    {
        if (_renderContext == null) return;
        try
        {
            if (player != null) player.IsRenderContextAttached = false;
            else if (_mediaPlayer != null) _mediaPlayer.IsRenderContextAttached = false;

            _renderContext.SetUpdateCallback(null);
            _renderContext.Dispose();
        }
        catch { }
        _renderContext = null;
    }

    private void OnRenderUpdate()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(RequestNextFrameRendering);
    }

    private void OnVideoReconfig()
    {
        if (_mediaPlayer == null) return;
        
        var dw = _mediaPlayer.GetPropertyString("video-params/dw") ?? _mediaPlayer.GetPropertyString("video-params/w");
        var dh = _mediaPlayer.GetPropertyString("video-params/dh") ?? _mediaPlayer.GetPropertyString("video-params/h");
        
        if (double.TryParse(dw, out var w) && double.TryParse(dh, out var h) && h > 0)
        {
            var aspect = w / h;
            if (Math.Abs(_videoAspectRatio - aspect) > 0.001)
            {
                _videoAspectRatio = aspect;
                Avalonia.Threading.Dispatcher.UIThread.Post(InvalidateMeasure);
            }
        }
    }

    #endregion
}
