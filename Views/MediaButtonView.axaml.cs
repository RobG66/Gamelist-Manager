using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Gamelist_Manager.Views;

public partial class MediaButtonView : UserControl
{
    public static bool IsWindows { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    private const double PREVIEW_WINDOW_WIDTH = 768;
    private const double PREVIEW_WINDOW_HEIGHT = 576;

    private Window? _previewWindow;

    public MediaButtonView()
    {
        InitializeComponent();
        MenuButton.Click += MenuButton_Click;
        ExpandButton.Click += ExpandButton_Click;
        PlayPauseButton.Click += PlayPauseButton_Click;
    }

    private void PlayPauseButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MediaItemViewModel mediaItem) return;
        mediaItem.TogglePlayback();
    }

    private void MenuButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MediaItemViewModel mediaItem || sender is not Button btn)
            return;

        if (IsWindows)
            PopulateOpenWithSubmenu(OpenWithItem, GetFullPath(mediaItem) ?? string.Empty);

        PopulateScrapeSubmenu(mediaItem);
        btn.ContextMenu?.Open(btn);
    }

    private void OpenItem_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MediaItemViewModel mediaItem) return;
        OpenFile(GetFullPath(mediaItem) ?? string.Empty);
    }

    private void OpenLocationItem_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MediaItemViewModel mediaItem) return;
        OpenFileLocation(GetFullPath(mediaItem) ?? string.Empty);
    }

    private void PropertiesItem_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MediaItemViewModel mediaItem) return;
        var fullPath = GetFullPath(mediaItem);
        if (fullPath != null) PropertiesHelper.Show(fullPath);
    }

    private async void CopyPathItem_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MediaItemViewModel mediaItem) return;
        var fullPath = GetFullPath(mediaItem);
        if (fullPath == null) return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard != null)
            await topLevel.Clipboard.SetTextAsync(fullPath);
    }

    private async void ClearItem_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MediaItemViewModel mediaItem) return;
        var vm = FindMediaPreviewViewModel();
        if (vm != null)
            await vm.UpdateGameMedia(mediaItem.MediaType, null);
    }

    private async void DeleteItem_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MediaItemViewModel mediaItem) return;
        var fullPath = GetFullPath(mediaItem);
        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath)) return;
        try { await Task.Run(() => File.Delete(fullPath)); }
        catch { return; }
        var vm = FindMediaPreviewViewModel();
        if (vm != null)
            await vm.UpdateGameMedia(mediaItem.MediaType, null);
    }

    private async void EditImageItem_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MediaItemViewModel mediaItem) return;
        var fullPath = GetFullPath(mediaItem);
        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath)) return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var savedPath = await ImageEditView.ShowAsync(fullPath, owner);

        if (savedPath == null) return;

        var vm = FindMediaPreviewViewModel();
        if (vm == null) return;

        if (string.Equals(savedPath, fullPath, FilePathHelper.PathComparison))
            vm.RefreshMedia(mediaItem.MediaType);
        else
            vm.UpdateMediaPath(mediaItem.MediaType, savedPath);
    }

    private void PopulateScrapeSubmenu(MediaItemViewModel mediaItem)
    {
        var items = new List<object>();
        foreach (var scraper in ScraperRegistry.All)
        {
            var preview = FindMediaPreviewViewModel();
            bool available = preview?.IsScraperAvailable(scraper) ?? true;
            var scraperName = scraper.Name;
            var scraperItem = new MenuItem
            {
                Header = scraper.Name,
                IsEnabled = available,
                Opacity = available ? 1.0 : 0.4
            };
            scraperItem.Click += (s, e) =>
            {
                var vm = FindMediaPreviewViewModel();
                if (vm != null)
                    _ = vm.ReScrapeGameAsync(scraperName, [mediaItem.MediaTypeKey]);
            };
            items.Add(scraperItem);
        }
        ScrapeItem.ItemsSource = items;
    }

    private string? GetFullPath(MediaItemViewModel mediaItem) =>
        string.IsNullOrWhiteSpace(mediaItem.MediaPath) ? null : mediaItem.ResolveFullPath(mediaItem.MediaPath!);

    private static void OpenFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open file: {ex.Message}");
        }
    }

    private static void OpenFileLocation(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return;
        try
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return;

            if (IsWindows)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{filePath}\"",
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (TryOpenWithCommand("nautilus", $"--select \"{filePath}\"")) return;
                if (TryOpenWithCommand("dolphin", $"--select \"{filePath}\"")) return;
                if (TryOpenWithCommand("thunar", filePath)) return;
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{directory}\"",
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"-R \"{filePath}\"",
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open file location: {ex.Message}");
        }
    }

    private static bool TryOpenWithCommand(string command, string arguments)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            return process != null;
        }
        catch
        {
            return false;
        }
    }

    private static void PopulateOpenWithSubmenu(MenuItem openWithMenu, string filePath)
    {
        var items = new List<object>();
        string extension = Path.GetExtension(filePath);
        var apps = AppAssociationHelper.GetAssociatedApps(extension);

        if (apps.Count == 0)
        {
            items.Add(new MenuItem { Header = "No associated apps", IsEnabled = false });
            openWithMenu.ItemsSource = items;
            return;
        }

        foreach (var app in apps)
        {
            var item = new MenuItem
            {
                Header = app.Name,
                Tag = new Tuple<string, string>(filePath, app.Command)
            };
            item.Click += async (s, e) =>
            {
                if (s is not MenuItem menuItem || menuItem.Tag is not Tuple<string, string> data)
                    return;
                var (targetFile, command) = data;
                try
                {
                    if (command.StartsWith("shell:AppsFolder\\", StringComparison.OrdinalIgnoreCase) ||
                        (!command.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && command.Contains('!')))
                    {
                        var appId = command.Replace("shell:AppsFolder\\", "");
                        await UwpAppHelper.LaunchAppWithFileAsync(appId, targetFile, null);
                    }
                    else
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = command,
                            Arguments = $"\"{targetFile}\"",
                            UseShellExecute = true,
                            CreateNoWindow = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
                    {
                        Title = "Error",
                        Message = $"Failed to open with {app.Name}.\n\n{ex.Message}",
                        IconTheme = DialogIconTheme.Error,
                        Button1Text = "",
                        Button2Text = "",
                        Button3Text = "OK"
                    });
                }
            };
            items.Add(item);
        }

        openWithMenu.ItemsSource = items;
    }

    private void ExpandButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MediaItemViewModel mediaItem)
            return;

        var path = mediaItem.MediaPath;
        if (string.IsNullOrWhiteSpace(path))
            return;

        var fullPath = mediaItem.ResolveFullPath(path);
        if (!File.Exists(fullPath))
            return;

        if (mediaItem.IsVideo || mediaItem.IsManual)
        {
            if (mediaItem.IsVideo && mediaItem.IsPlaying)
                mediaItem.TogglePlayback();

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = fullPath,
                    UseShellExecute = true
                });
            }
            catch { }
            return;
        }

        OpenPreviewWindow(fullPath);
    }

    private void OpenPreviewWindow(string path)
    {
        try
        {
            var bitmap = new Bitmap(path);

            _previewWindow?.Close();

            var image = new Image
            {
                Source = bitmap,
                Stretch = Avalonia.Media.Stretch.Uniform
            };

            var viewbox = new Viewbox
            {
                Child = image,
                Stretch = Avalonia.Media.Stretch.Uniform
            };

            var host = new Border
            {
                Background = Avalonia.Media.Brushes.Black,
                Child = viewbox,
                Padding = new Thickness(6)
            };

            _previewWindow = new Window
            {
                Content = host,
                Title = Path.GetFileName(path),
                Width = PREVIEW_WINDOW_WIDTH,
                Height = PREVIEW_WINDOW_HEIGHT,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = true
            };

            _previewWindow.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Escape)
                    _previewWindow?.Close();
            };

            var owner = TopLevel.GetTopLevel(this);
            if (owner is Window ownerWindow)
                _previewWindow.Show(ownerWindow);
            else
                _previewWindow.Show();
        }
        catch { }
    }

    private MediaPreviewViewModel? FindMediaPreviewViewModel() =>
        VisualTreeHelper.FindAncestorViewModel<MediaPreviewViewModel>(this);

    public void CleanupPreviewWindow()
    {
        _previewWindow?.Close();
        _previewWindow = null;
    }
}