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
using System.Runtime.InteropServices;

namespace Gamelist_Manager.ViewModels;

public partial class JukeboxViewModel
{
    [ObservableProperty]
    private bool _isPickerVisible;

    [ObservableProperty]
    private ObservableCollection<PresetCategory> _presetCategories = new();

    [ObservableProperty]
    private PresetCategory? _selectedCategory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddToFavoritesCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveFromFavoritesCommand))]
    [NotifyCanExecuteChangedFor(nameof(RenamePresetCommand))]
    private PresetItem? _selectedPreset;

    private bool CanAddToFavorites => SelectedPreset != null && !IsPresetInFavorites(SelectedPreset);
    private bool CanRemoveFromFavorites => SelectedPreset != null && IsPresetInFavorites(SelectedPreset);
    private bool CanRenamePreset => SelectedPreset != null;

    private bool IsPresetInFavorites(PresetItem preset)
    {
        var projectMDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectM");
        var favoritesDirectory = Path.Combine(projectMDir, "presets", "Favorites");
        if (!Directory.Exists(favoritesDirectory)) return false;
        
        if (preset.FullPath.StartsWith(favoritesDirectory, StringComparison.OrdinalIgnoreCase))
            return true;

        string destinationPath = Path.Combine(favoritesDirectory, Path.GetFileName(preset.FullPath));
        if (File.Exists(destinationPath))
        {
            try
            {
                var presetInfo = new FileInfo(preset.FullPath);
                var destInfo = new FileInfo(destinationPath);
                if (presetInfo.Length == destInfo.Length)
                    return true;
            }
            catch { }
        }
        
        return false;
    }
    private void EnsureProjectMPluginsCopied()
    {
        try
        {
            var projectMDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectM");
            var vlcDir = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vlc"));
            
            if (!Directory.Exists(projectMDir)) return;

            string pluginFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "libprojectm_plugin.dll" : "libprojectm_plugin.so";
            string osFolder = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win-x64" : "linux-x64";
            
            string sourcePluginPath = Path.Combine(projectMDir, osFolder, pluginFileName);
            string destinationPluginDirectory = Path.Combine(vlcDir.FullName, "plugins", "visualization");
            string destinationPluginPath = Path.Combine(destinationPluginDirectory, pluginFileName);

            if (File.Exists(sourcePluginPath))
            {
                if (!Directory.Exists(destinationPluginDirectory))
                    Directory.CreateDirectory(destinationPluginDirectory);

                // Copy plugin if it doesn't exist or if source timestamp differs
                if (!File.Exists(destinationPluginPath) || File.GetLastWriteTimeUtc(sourcePluginPath) != File.GetLastWriteTimeUtc(destinationPluginPath))
                {
                    File.Copy(sourcePluginPath, destinationPluginPath, true);
                }
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string sourceCoreDll = Path.Combine(projectMDir, osFolder, "libprojectM.dll");
                    string destinationCoreDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libprojectM.dll");
                    if (File.Exists(sourceCoreDll))
                    {
                        if (!File.Exists(destinationCoreDll) || File.GetLastWriteTimeUtc(sourceCoreDll) != File.GetLastWriteTimeUtc(destinationCoreDll))
                        {
                            File.Copy(sourceCoreDll, destinationCoreDll, true);
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
        }
    }
    private void LoadPresets()
    {
        var projectMDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectM");
        var presetsDir = Path.Combine(projectMDir, "presets");

        if (!Directory.Exists(presetsDir)) return;

        Task.Run(() =>
        {
            var categories = new List<PresetCategory>();
            var allPresetsCategory = new PresetCategory { Name = "All Presets" };
            
            // Scan directories efficiently
            foreach (var dir in Directory.EnumerateDirectories(presetsDir))
            {
                var dirName = Path.GetFileName(dir);
                var category = new PresetCategory { Name = dirName };
                
                var files = Directory.EnumerateFiles(dir, "*.milk", SearchOption.AllDirectories)
                                     .Select(f => new PresetItem { Name = Path.GetFileNameWithoutExtension(f), FullPath = f })
                                     .OrderBy(p => p.Name)
                                     .ToList();
                                     
                foreach (var item in files)
                {
                    category.Presets.Add(item);
                    allPresetsCategory.Presets.Add(item);
                }
                
                if (category.Presets.Count > 0)
                {
                    categories.Add(category);
                }
            }

            // Also get files in the root preset directory
            var rootFiles = Directory.EnumerateFiles(presetsDir, "*.milk")
                                     .Select(f => new PresetItem { Name = Path.GetFileNameWithoutExtension(f), FullPath = f })
                                     .OrderBy(p => p.Name)
                                     .ToList();
                                     
            foreach (var item in rootFiles)
            {
                allPresetsCategory.Presets.Add(item);
            }

            // Sort categories alphabetically
            categories = categories.OrderBy(c => c.Name).ToList();
            
            // Sort the All Presets category
            var sortedAll = allPresetsCategory.Presets.OrderBy(p => p.Name).ToList();
            allPresetsCategory.Presets.Clear();
            foreach (var p in sortedAll) allPresetsCategory.Presets.Add(p);

            categories.Insert(0, allPresetsCategory);

            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                PresetCategories = new ObservableCollection<PresetCategory>(categories.Where(c => c.Presets.Count > 0));
                SelectedCategory = PresetCategories.FirstOrDefault();
            });
        });
    }
    [RelayCommand]
    private void TogglePicker()
    {
        IsPickerVisible = !IsPickerVisible;
    }
    [RelayCommand]
    private void PickRandomPreset()
    {
        if (PresetCategories.Count == 0) return;
        var allPresets = PresetCategories.FirstOrDefault(c => c.Name == "All Presets");
        if (allPresets != null && allPresets.Presets.Count > 0)
        {
            SelectedCategory = allPresets;
            int idx = _random.Next(allPresets.Presets.Count);
            SelectedPreset = allPresets.Presets[idx];
            ApplyPreset();
        }
    }
    [RelayCommand]
    private void ApplyPreset()
    {
        if (SelectedPreset == null || _isDisposed) return;
        
        var presetToApply = SelectedPreset;
        
        Task.Run(async () =>
        {
            try
            {
                var tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectM", "temp_preset");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                
                // Clean temp dir
                foreach (var f in Directory.GetFiles(tempDir, "*.milk"))
                {
                    try { File.Delete(f); } catch { }
                }
                
                // Copy selected preset
                var destPath = Path.Combine(tempDir, Path.GetFileName(presetToApply.FullPath));
                File.Copy(presetToApply.FullPath, destPath, true);

                bool wasPlaying = false;
                long currentTime = 0;
                
                if (_audioMediaPlayer != null)
                {
                    try
                    {
                        wasPlaying = _audioMediaPlayer.IsPlaying;
                        if (wasPlaying) currentTime = _audioMediaPlayer.Time;
                    }
                    catch { }
                }

                if (wasPlaying && _currentMedia != null)
                {
                    // Stop the player so the visualizer unloads
                    _audioMediaPlayer?.Stop();
                    
                    // Small delay to ensure resources are freed
                    await Task.Delay(50);
                    
                    // Restart playback to spin up the visualizer with the new preset
                    _audioMediaPlayer?.Play(_currentMedia);
                    
                    // Poll for up to 2.5 seconds to allow media to parse and start before seeking
                    for (int i = 0; i < 25; i++)
                    {
                        if (_audioMediaPlayer != null && _audioMediaPlayer.IsPlaying)
                            break;
                        await Task.Delay(100);
                    }

                    try
                    {
                        if (_audioMediaPlayer != null && _audioMediaPlayer.IsPlaying)
                            _audioMediaPlayer.Time = currentTime;
                    }
                    catch { }
                }
            }
            catch (Exception)
            {
            }
        });
    }
    [RelayCommand(CanExecute = nameof(CanAddToFavorites))]
    private void AddToFavorites()
    {
        if (SelectedPreset == null || _isDisposed) return;
        
        try
        {
            var projectMDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectM");
            var favoritesDirectory = Path.Combine(projectMDir, "presets", "Favorites");
            if (!Directory.Exists(favoritesDirectory)) Directory.CreateDirectory(favoritesDirectory);
            
            string baseName = Path.GetFileNameWithoutExtension(SelectedPreset.FullPath);
            string ext = Path.GetExtension(SelectedPreset.FullPath);
            string destinationPath = Path.Combine(favoritesDirectory, $"{baseName}{ext}");
            string finalName = baseName;

            int counter = 1;
            while (File.Exists(destinationPath))
            {
                try
                {
                    if (new FileInfo(SelectedPreset.FullPath).Length == new FileInfo(destinationPath).Length)
                    {
                        break;
                    }
                }
                catch { }

                finalName = $"{baseName} ({counter})";
                destinationPath = Path.Combine(favoritesDirectory, $"{finalName}{ext}");
                counter++;
            }

            File.Copy(SelectedPreset.FullPath, destinationPath, true);
            
            var favoritesCategory = PresetCategories.FirstOrDefault(c => c.Name.Equals("Favorites", StringComparison.OrdinalIgnoreCase));
            if (favoritesCategory == null)
            {
                favoritesCategory = new PresetCategory { Name = "Favorites" };
                PresetCategories.Insert(1, favoritesCategory); // Insert right after All Presets
            }
            
            if (!favoritesCategory.Presets.Any(p => p.FullPath.Equals(destinationPath, StringComparison.OrdinalIgnoreCase)))
            {
                var newItem = new PresetItem { Name = finalName, FullPath = destinationPath };
                favoritesCategory.Presets.Add(newItem);
                
                var sorted = favoritesCategory.Presets.OrderBy(p => p.Name).ToList();
                favoritesCategory.Presets.Clear();
                foreach (var p in sorted) favoritesCategory.Presets.Add(p);
            }

            var allCat = PresetCategories.FirstOrDefault(c => c.Name == "All Presets");
            if (allCat != null && !allCat.Presets.Any(p => p.FullPath.Equals(destinationPath, StringComparison.OrdinalIgnoreCase)))
            {
                var newItem = new PresetItem { Name = finalName, FullPath = destinationPath };
                allCat.Presets.Add(newItem);
                var sorted = allCat.Presets.OrderBy(p => p.Name).ToList();
                allCat.Presets.Clear();
                foreach (var p in sorted) allCat.Presets.Add(p);
            }

            AddToFavoritesCommand.NotifyCanExecuteChanged();
            RemoveFromFavoritesCommand.NotifyCanExecuteChanged();
        }
        catch (Exception)
        {
        }
    }
    [RelayCommand(CanExecute = nameof(CanRemoveFromFavorites))]
    private void RemoveFromFavorites()
    {
        if (SelectedPreset == null || _isDisposed) return;
        
        try
        {
            var projectMDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectM");
            var favoritesDirectory = Path.Combine(projectMDir, "presets", "Favorites");
            if (!Directory.Exists(favoritesDirectory)) return;

            string destinationPath = Path.Combine(favoritesDirectory, Path.GetFileName(SelectedPreset.FullPath));
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            var favoritesCategory = PresetCategories.FirstOrDefault(c => c.Name.Equals("Favorites", StringComparison.OrdinalIgnoreCase));
            if (favoritesCategory != null)
            {
                var item = favoritesCategory.Presets.FirstOrDefault(p => p.FullPath.Equals(destinationPath, StringComparison.OrdinalIgnoreCase));
                if (item != null) favoritesCategory.Presets.Remove(item);
                
                if (favoritesCategory.Presets.Count == 0)
                {
                    PresetCategories.Remove(favoritesCategory);
                }
            }

            var allCat = PresetCategories.FirstOrDefault(c => c.Name == "All Presets");
            if (allCat != null)
            {
                var item = allCat.Presets.FirstOrDefault(p => p.FullPath.Equals(destinationPath, StringComparison.OrdinalIgnoreCase));
                if (item != null) allCat.Presets.Remove(item);
            }

            AddToFavoritesCommand.NotifyCanExecuteChanged();
            RemoveFromFavoritesCommand.NotifyCanExecuteChanged();
        }
        catch (Exception)
        {
        }
    }

    [RelayCommand(CanExecute = nameof(CanRenamePreset))]
    private async Task RenamePreset()
    {
        if (SelectedPreset == null || _isDisposed) return;
        var preset = SelectedPreset;

        string currentDir = Path.GetDirectoryName(preset.FullPath) ?? string.Empty;

        var newName = await Views.RenameDialogView.ShowAsync(preset.Name, null, (name) =>
        {
            string newPath = Path.Combine(currentDir, name + ".milk");
            if (File.Exists(newPath))
            {
                return (false, $"A preset named '{name}' already exists in this folder.");
            }
            return (true, string.Empty);
        });

        if (string.IsNullOrWhiteSpace(newName) || newName == preset.Name) return;

        try
        {
            string newPath = Path.Combine(currentDir, newName + ".milk");
            File.Move(preset.FullPath, newPath);

            // Update the preset item so the UI reflects the change
            preset.Name = newName;
            preset.FullPath = newPath;
        }
        catch (Exception ex)
        {
            RaiseError($"Failed to rename preset: {ex.Message}");
        }
    }
}

public partial class PresetItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _fullPath = string.Empty;
}

public class PresetCategory
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<PresetItem> Presets { get; set; } = new();
}
