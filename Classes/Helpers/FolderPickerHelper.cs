using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System;
using System.Threading.Tasks;

namespace Gamelist_Manager.Classes.Helpers;

// Shared folder-picker logic so callers don't duplicate the Avalonia storage API boilerplate.
public static class FolderPickerHelper
{
    public static async Task<string?> BrowseForFolderAsync(string title, string? suggestedPath = null, Window? owner = null)
    {
        var topLevel = owner
            ?? (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null);

        if (topLevel == null) return null;

        IStorageFolder? suggestedStart = null;
        try
        {
            if (!string.IsNullOrEmpty(suggestedPath))
                suggestedStart = await topLevel.StorageProvider.TryGetFolderFromPathAsync(new Uri(suggestedPath));
        }
        catch { }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStart
        });

        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }
}
