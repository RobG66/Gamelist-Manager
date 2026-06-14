using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Gamelist_Manager.Classes.ProjectM;

public enum ProjectMChannels
{
    Mono = 1,
    Stereo = 2
}

public static class ProjectMNative
{
    private static IntPtr _libraryHandle = IntPtr.Zero;

    static ProjectMNative()
    {
        try
        {
            string libraryPath = GetLibraryPath();
            if (File.Exists(libraryPath))
            {
                // Crucial: we MUST tell the loader to look in the same directory as the DLL for dependencies!
                _libraryHandle = NativeLibrary.Load(libraryPath, typeof(ProjectMNative).Assembly, DllImportSearchPath.UseDllDirectoryForDependencies | DllImportSearchPath.SafeDirectories);
                LoadDelegates();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load libprojectM: {ex}");
        }
    }

    private static string GetLibraryPath()
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectM");
        
        string ridOS = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                       RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" : "linux";
                       
        string ridArch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";
        
        string fileName = ridOS switch
        {
            "win" => "libprojectM.dll",
            "osx" => "libprojectM.dylib",
            _ => "libprojectM.so.4"
        };

        return Path.Combine(dir, $"{ridOS}-{ridArch}", fileName);
    }

    private static T GetDelegate<T>(string name, bool throwOnError = true) where T : Delegate
    {
        if (_libraryHandle == IntPtr.Zero) return null!;
        if (NativeLibrary.TryGetExport(_libraryHandle, name, out IntPtr address))
        {
            try
            {
                return Marshal.GetDelegateForFunctionPointer<T>(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProjectM] ERROR parsing delegate {name}: {ex}");
                return null!;
            }
        }
        Console.WriteLine($"[ProjectM] TryGetExport failed for {name}");
        if (throwOnError)
            throw new Exception($"Export {name} not found in libprojectM.");
        return null!;
    }

    private static void LoadDelegates()
    {
        projectm_create = GetDelegate<projectm_create_delegate>("projectm_create", false);
        projectm_destroy = GetDelegate<projectm_destroy_delegate>("projectm_destroy", false);
        projectm_set_window_size = GetDelegate<projectm_set_window_size_delegate>("projectm_set_window_size", false);
        projectm_set_fps = GetDelegate<projectm_set_fps_delegate>("projectm_set_fps", false);
        projectm_pcm_add_float = GetDelegate<projectm_pcm_add_float_delegate>("projectm_pcm_add_float", false);
        projectm_pcm_add_int16 = GetDelegate<projectm_pcm_add_int16_delegate>("projectm_pcm_add_int16", false);
        projectm_opengl_render_frame = GetDelegate<projectm_opengl_render_frame_delegate>("projectm_opengl_render_frame", false);
        projectm_set_texture_search_paths = GetDelegate<projectm_set_texture_search_paths_delegate>("projectm_set_texture_search_paths", false);
        projectm_set_log_callback = GetDelegate<projectm_set_log_callback_delegate>("projectm_set_log_callback", false);
        projectm_set_log_level = GetDelegate<projectm_set_log_level_delegate>("projectm_set_log_level", false);
        projectm_load_preset_file = GetDelegate<projectm_load_preset_file_delegate>("projectm_load_preset_file", false);
        projectm_load_preset_data = GetDelegate<projectm_load_preset_data_delegate>("projectm_load_preset_data", false);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr projectm_create_delegate();
    public static projectm_create_delegate projectm_create { get; private set; } = null!;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void projectm_destroy_delegate(IntPtr instance);
    public static projectm_destroy_delegate projectm_destroy { get; private set; } = null!;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void projectm_set_window_size_delegate(IntPtr handle, nuint width, nuint height);
    public static projectm_set_window_size_delegate projectm_set_window_size { get; private set; } = null!;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void projectm_set_texture_search_paths_delegate(
        IntPtr handle, 
        IntPtr[] paths, 
        nuint count);
    public static projectm_set_texture_search_paths_delegate projectm_set_texture_search_paths { get; private set; } = null!;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void projectm_log_callback(IntPtr user_data, int level, [MarshalAs(UnmanagedType.LPUTF8Str)] string message);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void projectm_set_log_callback_delegate(projectm_log_callback callback, IntPtr user_data);
    public static projectm_set_log_callback_delegate projectm_set_log_callback { get; private set; } = null!;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void projectm_set_log_level_delegate(int level);
    public static projectm_set_log_level_delegate projectm_set_log_level { get; private set; } = null!;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void projectm_set_fps_delegate(IntPtr instance, int fps);
    public static projectm_set_fps_delegate projectm_set_fps { get; private set; } = null!;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void projectm_pcm_add_float_delegate(IntPtr instance, float[] samples, uint count, ProjectMChannels channels);
    public static projectm_pcm_add_float_delegate projectm_pcm_add_float { get; private set; } = null!;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void projectm_pcm_add_int16_delegate(IntPtr instance, short[] samples, uint count, ProjectMChannels channels);
    public static projectm_pcm_add_int16_delegate projectm_pcm_add_int16 { get; private set; } = null!;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void projectm_opengl_render_frame_delegate(IntPtr instance);
    public static projectm_opengl_render_frame_delegate projectm_opengl_render_frame { get; private set; } = null!;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void projectm_load_preset_file_delegate(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string filename, byte smooth_transition);
    public static projectm_load_preset_file_delegate projectm_load_preset_file { get; private set; } = null!;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void projectm_load_preset_data_delegate(IntPtr handle, IntPtr data, nuint length, byte smooth_transition);
    public static projectm_load_preset_data_delegate projectm_load_preset_data { get; private set; } = null!;
}
