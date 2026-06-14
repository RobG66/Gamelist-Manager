using Avalonia;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Gamelist_Manager.Classes.ProjectM;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using static Avalonia.OpenGL.GlConsts;

namespace Gamelist_Manager.Controls;

public class ProjectMControl : OpenGlControlBase
{
    private IntPtr _projectMHandle;
    private readonly ConcurrentQueue<short[]> _pcmQueue = new();
    private readonly ConcurrentQueue<string> _presetQueue = new();

    private long _frameCount = 0;
    private int _lastWidth = 0;
    private int _lastHeight = 0;

    public static readonly StyledProperty<string> PresetPathProperty =
        AvaloniaProperty.Register<ProjectMControl, string>(nameof(PresetPath), "");

    public string PresetPath
    {
        get => GetValue(PresetPathProperty);
        set => SetValue(PresetPathProperty, value);
    }

    private string _lastLog = "";

    private ProjectMNative.projectm_log_callback _logCallback;

    public ProjectMControl()
    {
        _logCallback = (IntPtr userData, int level, string message) =>
        {
            if (level <= 2) // Warning or Error
            {
                _lastLog = message;
            }
            Console.WriteLine($"[ProjectM NATIVE] {message}");
        };
    }

    private bool _engineRequested = false;

    public void StartEngine()
    {
        _engineRequested = true;
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        base.OnOpenGlInit(gl);
        RequestNextFrameRendering();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // We handle resizing in OnOpenGlRender now to ensure it happens on the GL thread
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        try
        {
            var renderScaling = Avalonia.Controls.TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
            int width = (int)Math.Max(1, Bounds.Width * renderScaling);
            int height = (int)Math.Max(1, Bounds.Height * renderScaling);

            // Clear to black
            gl.BindFramebuffer(GL_FRAMEBUFFER, fb);
            gl.Viewport(0, 0, width, height);
            gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

            if (!_engineRequested)
            {
                RequestNextFrameRendering();
                return;
            }

            if (_projectMHandle == IntPtr.Zero) 
            {
                try
                {
                    if (ProjectMNative.projectm_set_log_level != null)
                    {
                        ProjectMNative.projectm_set_log_level(4); // DEBUG
                        ProjectMNative.projectm_set_log_callback(_logCallback, IntPtr.Zero);
                    }

                    if (ProjectMNative.projectm_create != null)
                    {
                        Console.WriteLine("[ProjectM] Calling projectm_create()...");
                        _projectMHandle = ProjectMNative.projectm_create();
                        if (_projectMHandle != IntPtr.Zero)
                        {
                            ProjectMNative.projectm_set_fps(_projectMHandle, 60);
                            ProjectMNative.projectm_set_window_size(_projectMHandle, (nuint)Math.Max(1, Bounds.Width), (nuint)Math.Max(1, Bounds.Height));
                            
                            if (ProjectMNative.projectm_set_texture_search_paths != null)
                            {
                                var texturesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectM", "textures");
                                if (System.IO.Directory.Exists(texturesPath))
                                {
                                    IntPtr ptr = Marshal.StringToHGlobalAnsi(texturesPath);
                                    ProjectMNative.projectm_set_texture_search_paths(_projectMHandle, new IntPtr[] { ptr }, 1);
                                    Marshal.FreeHGlobal(ptr);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ProjectM Init Error: {ex}");
                }

                if (_projectMHandle == IntPtr.Zero)
                {
                    RequestNextFrameRendering();
                    return;
                }
            }

            if (_lastWidth != width || _lastHeight != height)
            {
                _lastWidth = width;
                _lastHeight = height;
                ProjectMNative.projectm_set_window_size(_projectMHandle, (nuint)width, (nuint)height);
            }

            // Load any pending presets on the GL thread
            while (_presetQueue.TryDequeue(out var presetPath))
            {
                if (System.IO.File.Exists(presetPath))
                {
                    if (ProjectMNative.projectm_load_preset_file != null)
                    {
                        var normalizedPath = presetPath.Replace('\\', '/');
                        ProjectMNative.projectm_load_preset_file(_projectMHandle, normalizedPath, 0);
                    }
                    else
                    {
                        Console.WriteLine($"[ProjectM] ERROR: load_preset_file delegate is NULL!");
                    }
                }
                else
                {
                    Console.WriteLine($"[ProjectM] PRESET FILE NOT FOUND: {presetPath}");
                }
            }

            // Drain PCM queue and feed to projectM
            while (_pcmQueue.TryDequeue(out var samples))
            {
                ProjectMNative.projectm_pcm_add_int16(_projectMHandle, samples, (uint)samples.Length / 2, ProjectMChannels.Stereo);
            }

            ProjectMNative.projectm_opengl_render_frame(_projectMHandle);
            
            _frameCount++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ProjectM Render Error: {ex}");
        }

        RequestNextFrameRendering();
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        if (_projectMHandle != IntPtr.Zero)
        {
            ProjectMNative.projectm_destroy(_projectMHandle);
            _projectMHandle = IntPtr.Zero;
        }
        base.OnOpenGlDeinit(gl);
    }

    public void FeedPcm(short[] samples)
    {
        _pcmQueue.Enqueue(samples);
        while (_pcmQueue.Count > 10)
        {
            _pcmQueue.TryDequeue(out _);
        }
    }

    public void LoadPreset(string path, bool smooth = true)
    {
        try
        {
            var tempDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectM", "temp_preset");
            System.IO.Directory.CreateDirectory(tempDir);
            
            // Clear old temp files
            foreach (var file in System.IO.Directory.GetFiles(tempDir))
            {
                try { System.IO.File.Delete(file); } catch { }
            }

            string destPath = System.IO.Path.Combine(tempDir, System.IO.Path.GetFileName(path));
            System.IO.File.Copy(path, destPath, true);

            // Copy associated textures
            string sourceDir = System.IO.Path.GetDirectoryName(path) ?? "";
            string content = System.IO.File.ReadAllText(path);
            var regex = new System.Text.RegularExpressions.Regex(@"[a-zA-Z0-9_-]+\.(?:jpg|png|bmp|tga)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            foreach (System.Text.RegularExpressions.Match match in regex.Matches(content))
            {
                string textureName = match.Value;
                string sourceTex = System.IO.Path.Combine(sourceDir, textureName);
                if (System.IO.File.Exists(sourceTex))
                {
                    string destTex = System.IO.Path.Combine(tempDir, textureName);
                    System.IO.File.Copy(sourceTex, destTex, true);
                }
            }

            _presetQueue.Enqueue(destPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProjectM] Failed to copy preset to temp folder: {ex.Message}");
            // Fallback to original path if copy fails
            _presetQueue.Enqueue(path);
        }

        while (_presetQueue.Count > 5)
        {
            _presetQueue.TryDequeue(out _);
        }
    }
    
    public bool IsHandleValid => _projectMHandle != IntPtr.Zero;
}
