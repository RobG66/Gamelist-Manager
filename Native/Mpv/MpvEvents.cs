using System;
using System.Runtime.InteropServices;

namespace Gamelist_Manager.Native.Mpv;

internal enum MpvFormat : int
{
    None = 0,
    String = 1,
    OsdString = 2,
    Flag = 3,
    Int64 = 4,
    Double = 5,
    Node = 6,
    NodeArray = 7,
    NodeMap = 8,
    ByteArray = 9
}

internal enum MpvEventId : int
{
    None = 0,
    Shutdown = 1,
    LogMessage = 2,
    GetPropertyReply = 3,
    SetPropertyReply = 4,
    CommandReply = 5,
    StartFile = 6,
    EndFile = 7,
    FileLoaded = 8,
    Idle = 11,
    Tick = 14,
    ClientMessage = 16,
    VideoReconfig = 17,
    AudioReconfig = 18,
    Seek = 20,
    PlaybackRestart = 21,
    PropertyChange = 22,
    QueueOverflow = 24,
    Hook = 25
}

[StructLayout(LayoutKind.Sequential)]
internal struct MpvEvent
{
    public MpvEventId EventId;
    public int Error;
    public ulong ReplyUserData;
    public IntPtr Data;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MpvEventProperty
{
    public IntPtr Name;
    public MpvFormat Format;
    public IntPtr Data;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MpvEventLogMessage
{
    public IntPtr Prefix;
    public IntPtr Level;
    public IntPtr Text;
    public int LogLevel;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MpvEventEndFile
{
    public int Reason;
    public int Error;
    public ulong PlaylistInsertId;
    public int PlaylistInsertNumEntries;
}

internal enum MpvRenderParamType : int
{
    Invalid = 0,
    ApiType = 1,
    OpenglInitParams = 2,
    OpenglFbo = 3,
    FlipY = 4,
    Depth = 5,
    IccProfile = 6,
    AmbientLight = 7,
    X11Display = 8,
    WlDisplay = 9,
    AdvancedControl = 10,
    NextFrameInfo = 11,
    BlockForTargetTime = 12,
    SkipRendering = 13
}

[StructLayout(LayoutKind.Sequential)]
internal struct MpvRenderParam
{
    public MpvRenderParamType Type;
    public IntPtr Data;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MpvOpenglInitParams
{
    public IntPtr GetProcAddress;
    public IntPtr GetProcAddressCtx;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MpvOpenglFbo
{
    public int Fbo;
    public int Width;
    public int Height;
    public int InternalFormat;
}

[Flags]
internal enum MpvRenderUpdateFlags : ulong
{
    None = 0,
    Frame = 1
}

internal static class MpvRenderApiType
{
    public const string Opengl = "opengl";
}
