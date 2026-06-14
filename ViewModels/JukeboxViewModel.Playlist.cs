using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using System.Runtime.InteropServices;

namespace Gamelist_Manager.ViewModels;

public partial class JukeboxViewModel
{
    [ObservableProperty]
    private int _currentIndex;

    [ObservableProperty]
    private ObservableCollection<JukeboxTrack> _tracks = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMultipleTracks))]
    private ObservableCollection<JukeboxTrack> _filteredTracks = new();

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private JukeboxTrack? _selectedTrack;

    [ObservableProperty]
    private bool _isPlaylistVisible;

    [ObservableProperty]
    private bool _hasMultipleTracks;

    [RelayCommand]
    private void Previous() => GotoPreviousTrack();

    [RelayCommand]
    private void Next() => GotoNextTrack();

    [RelayCommand]
    private void TogglePlaylist()
    {
        if (_isDisposed) return;
        IsPlaylistVisible = !IsPlaylistVisible;
    }

    [RelayCommand]
    private async Task AddFiles()
    {
        if (_storageProvider == null) return;
        var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Media Files",
            AllowMultiple = true
        });
        
        if (files == null || files.Count == 0) return;
        var paths = files.Select(f => f.Path.LocalPath).ToArray();
        AppendToPlaylist(paths);
    }

    [RelayCommand]
    private async Task AddFolder()
    {
        if (_storageProvider == null) return;
        var folders = await _storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Media Folder",
            AllowMultiple = false
        });
        
        if (folders == null || folders.Count == 0) return;
        var folderPath = folders[0].Path.LocalPath;
        if (!Directory.Exists(folderPath)) return;
        
        var ext = new[] { ".mp3", ".wav", ".flac", ".mp4", ".mkv", ".avi", ".webm", ".ogg" };
        var paths = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(f => ext.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToArray();
            
        AppendToPlaylist(paths);
    }

    [RelayCommand]
    private void ClearPlaylist()
    {
        Stop();
        _mediaFiles = Array.Empty<string>();
        Tracks.Clear();
        ApplyFilter();
        HasVideoInPlaylist = false;
        HasMultipleTracks = false;
        SelectedTrack = null;
    }

    private void AppendToPlaylist(string[] newFiles)
    {
        if (newFiles.Length == 0) return;
        
        var currentList = _mediaFiles.ToList();
        int startIndex = currentList.Count;
        currentList.AddRange(newFiles);
        _mediaFiles = currentList.ToArray();
        
        bool hasVideo = HasVideoInPlaylist;
        
        for (int i = 0; i < newFiles.Length; i++)
        {
            var f = newFiles[i];
            bool isVideo = !IsAudioFile(f);
            if (isVideo) hasVideo = true;
            
            Tracks.Add(new JukeboxTrack(startIndex + i, f, Path.GetFileNameWithoutExtension(f)));
        }
        
        HasVideoInPlaylist = hasVideo;
        HasMultipleTracks = _mediaFiles.Length > 1;
        ApplyFilter();
        StartMetadataScanner();
    }

    public async Task PlayMediaFilesAsync(string[] fileList, bool autoPlay)
    {
        if (_isDisposed) return;
        
        try
        {
            if (_initTask != null && !_initTask.IsCompleted)
            {
                IsInitializing = true;
                await _initTask;
                IsInitializing = false;
            }
            
            LoadPlaylist(fileList, singleFile: fileList.Length == 1);
            if (autoPlay)
                PlayCurrentTrack();
            await Task.CompletedTask;
        }
        catch (Exception ex) { RaiseError($"Error playing media files: {ex.Message}"); }
    }

    public async Task PlayMediaAsync(string fileName, bool autoPlay)
    {
        if (_isDisposed) return;
        
        try
        {
            if (_initTask != null && !_initTask.IsCompleted)
            {
                IsInitializing = true;
                await _initTask;
                IsInitializing = false;
            }
            
            LoadPlaylist(new[] { fileName }, singleFile: true);
            if (autoPlay)
                PlayCurrentTrack();
            else
            {
                PlayCurrentTrack();
                await Task.Delay(100);
                Pause();
            }
        }
        catch (Exception ex) { RaiseError($"Error playing media: {ex.Message}"); }
    }
    partial void OnSelectedTrackChanged(JukeboxTrack? value)
    {
        if (value == null || _isDisposed) return;
        if (value.Index == CurrentIndex && (IsPlaying || IsPaused)) return;

        CurrentIndex = value.Index;
        PlayTrack(value.FilePath);
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnCurrentIndexChanged(int value)
    {
        if (Tracks == null || value < 0 || value >= Tracks.Count) return;

        for (int i = 0; i < Tracks.Count; i++)
        {
            Tracks[i].IsPlaying = (i == value);
        }

        if (SelectedTrack != Tracks[value])
        {
            SelectedTrack = Tracks[value];
        }
    }
    private void LoadPlaylist(string[] files, bool singleFile)
    {
        _mediaFiles = files;
        var list = new List<JukeboxTrack>();
        bool hasVideo = false;
        
        for (int i = 0; i < files.Length; i++)
        {
            if (!IsAudioFile(files[i])) hasVideo = true;
            list.Add(new JukeboxTrack(i, files[i], Path.GetFileNameWithoutExtension(files[i])));
        }
        Tracks = new ObservableCollection<JukeboxTrack>(list);

        CurrentIndex = 0;
        IsPlaying = false;
        IsPaused = false;
        IsStopped = true;

        HasVideoInPlaylist = hasVideo;

        ApplyFilter();

        var firstTrack = Tracks.FirstOrDefault();
        if (firstTrack != null)
        {
            SelectedTrack = firstTrack;
            firstTrack.IsPlaying = false;
        }

        HasMultipleTracks = !singleFile;
        IsPlaylistVisible = false;
        UpdateButtons();
        
        StartMetadataScanner();
    }

    private void ApplyFilter()
    {
        if (Tracks == null) return;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredTracks = new ObservableCollection<JukeboxTrack>(Tracks);
        }
        else
        {
            var filtered = Tracks.Where(t => t.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            FilteredTracks = new ObservableCollection<JukeboxTrack>(filtered);
        }
    }

    private void StartMetadataScanner()
    {
        _scannerCts?.Cancel();
        _scannerCts?.Dispose();
        _scannerCts = new CancellationTokenSource();
        var token = _scannerCts.Token;

        _scannerTask = Task.Run(() => RunScannerAsync(token), token);
    }

    private async Task RunScannerAsync(CancellationToken token)
    {
        var tracksToParse = Tracks.ToList();

        foreach (var track in tracksToParse)
        {
            if (token.IsCancellationRequested || _isDisposed) break;

            try
            {
                using var file = TagLib.File.Create(track.FilePath);
                var duration = file.Properties.Duration;
                
                string durationStr = duration.TotalMilliseconds > 0 
                    ? duration.ToString(duration.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss")
                    : "--:--";

                string resolutionStr = "";
                string bitrateStr = "";

                if (file.Properties.VideoWidth > 0 && file.Properties.VideoHeight > 0)
                {
                    resolutionStr = $"{file.Properties.VideoWidth}x{file.Properties.VideoHeight}";
                }

                if (file.Properties.AudioBitrate > 0)
                {
                    bitrateStr = $"{file.Properties.AudioBitrate} kbps";
                }

                if (token.IsCancellationRequested || _isDisposed) break;

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (token.IsCancellationRequested || _isDisposed) return;
                    track.Duration = durationStr;
                    track.Resolution = resolutionStr;
                    if (!string.IsNullOrEmpty(bitrateStr))
                        track.Bitrate = bitrateStr;
                });
            }
            catch { }
        }
        
        await Task.CompletedTask;
    }
    private void GotoNextTrack()
    {
        if (_mediaFiles.Length == 0 || _isDisposed) return;

        CurrentIndex = IsRandomPlayback
            ? _random.Next(0, _mediaFiles.Length)
            : (CurrentIndex + 1) % _mediaFiles.Length;

        if (CurrentIndex >= 0 && CurrentIndex < Tracks.Count)
        {
            SelectedTrack = Tracks[CurrentIndex];
            PlayCurrentTrack();
        }
    }

    private void GotoPreviousTrack()
    {
        if (_mediaFiles.Length == 0 || _isDisposed) return;

        CurrentIndex = IsRandomPlayback
            ? _random.Next(0, _mediaFiles.Length)
            : CurrentIndex - 1;

        if (CurrentIndex < 0) CurrentIndex = _mediaFiles.Length - 1;

        if (CurrentIndex >= 0 && CurrentIndex < Tracks.Count)
        {
            SelectedTrack = Tracks[CurrentIndex];
            PlayCurrentTrack();
        }
    }
}

public partial class JukeboxTrack : ObservableObject
{
    public int Index { get; }
    public string FilePath { get; }
    public string DisplayName { get; }

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private string _duration = "--:--";

    [ObservableProperty]
    private string _resolution = "";

    [ObservableProperty]
    private string _bitrate = "";

    public JukeboxTrack(int index, string filePath, string displayName)
    {
        Index = index;
        FilePath = filePath;
        DisplayName = displayName;
    }
}
