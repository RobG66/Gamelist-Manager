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

    private void LoadPresets()
    {
        var projectMDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectM");
        var presetsDir = Path.Combine(projectMDir, "presets");

        if (!Directory.Exists(presetsDir)) return;

        Task.Run(() =>
        {
            var categories = new List<PresetCategory>();
            var allPresetsCategory = new PresetCategory { Name = "All Presets" };
            
            // Get all subdirectories recursively, plus the root presets dir
            var allDirs = new List<string> { presetsDir };
            try
            {
                allDirs.AddRange(Directory.GetDirectories(presetsDir, "*", SearchOption.AllDirectories));
            }
            catch { }

            foreach (var dir in allDirs)
            {
                var files = Directory.EnumerateFiles(dir, "*.milk", SearchOption.TopDirectoryOnly)
                                     .Select(f => new PresetItem { Name = Path.GetFileNameWithoutExtension(f), FullPath = f })
                                     .OrderBy(p => p.Name)
                                     .ToList();
                                     
                if (files.Count > 0)
                {
                    // Create a readable category name relative to the presets folder
                    var relativePath = Path.GetRelativePath(presetsDir, dir);
                    var categoryName = relativePath == "." ? "Root" : relativePath.Replace(Path.DirectorySeparatorChar, '/');
                    
                    var category = new PresetCategory { Name = categoryName };
                    foreach (var item in files)
                    {
                        category.Presets.Add(item);
                        allPresetsCategory.Presets.Add(item);
                    }
                    categories.Add(category);
                }
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
                PresetCategories = new ObservableCollection<PresetCategory>(categories);
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
        if (PresetCategories.Count == 0) 
        {
            Console.WriteLine("[ViewModel] PickRandomPreset aborted: PresetCategories is empty!");
            return;
        }
        var allPresets = PresetCategories.FirstOrDefault(c => c.Name == "All Presets");
        if (allPresets != null && allPresets.Presets.Count > 0)
        {
            SelectedCategory = allPresets;
            int idx = _random.Next(allPresets.Presets.Count);
            SelectedPreset = allPresets.Presets[idx];
            Console.WriteLine($"[ViewModel] PickRandomPreset picked: {SelectedPreset.FullPath}");
            ApplyPreset();
        }
        else
        {
            Console.WriteLine("[ViewModel] PickRandomPreset aborted: No presets found in All Presets category.");
        }
    }
    [RelayCommand]
    private void ApplyPreset()
    {
        Console.WriteLine($"[ViewModel] ApplyPreset called! SelectedPreset: {SelectedPreset?.FullPath ?? "NULL"}");
        if (SelectedPreset == null || _projectMControl == null || _isDisposed) 
        {
            Console.WriteLine($"[ViewModel] ApplyPreset ABORTED. _projectMControl is null? {_projectMControl == null}. _isDisposed? {_isDisposed}");
            return;
        }
        Console.WriteLine($"[ViewModel] Calling _projectMControl.LoadPreset with {SelectedPreset.FullPath}");
        _projectMControl.LoadPreset(SelectedPreset.FullPath, smooth: true);
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
            
            // Also copy any locally associated textures
            try
            {
                string sourceDir = Path.GetDirectoryName(SelectedPreset.FullPath) ?? "";
                string content = File.ReadAllText(SelectedPreset.FullPath);
                var regex = new System.Text.RegularExpressions.Regex(@"[a-zA-Z0-9_-]+\.(?:jpg|png|bmp|tga)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                foreach (System.Text.RegularExpressions.Match match in regex.Matches(content))
                {
                    string textureName = match.Value;
                    string sourceTex = Path.Combine(sourceDir, textureName);
                    if (File.Exists(sourceTex))
                    {
                        string destTex = Path.Combine(favoritesDirectory, textureName);
                        File.Copy(sourceTex, destTex, true);
                    }
                }
            }
            catch { }

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

            string oldPath = preset.FullPath;
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
