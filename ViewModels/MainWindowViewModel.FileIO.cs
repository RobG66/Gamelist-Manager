using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using Gamelist_Manager.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class MainWindowViewModel
{
    private const string BackupFolderName = "Gamelist Backups";

    #region Properties & Events
    public ObservableCollection<RecentFileItem> RecentFiles { get; } = new();
    public bool HasRecentFiles => RecentFiles.Count > 0;
    public event EventHandler? RequestSelectFirstItem;

    [ObservableProperty] private string _fileStatusText = "No file loaded";
    [ObservableProperty] private string _lastModifiedText = string.Empty;
    #endregion

    #region Commands
    [RelayCommand]
    private void ClearRecentFiles()
    {
        RecentFiles.Clear();
        _settingsService.SaveRecentFiles(new List<string>());
    }

    [RelayCommand]
    private async Task SaveGamelistAsync()
    {
        if (string.IsNullOrEmpty(_sharedData.XmlFilename) || _sharedData.CurrentSystem == null)
            return;

        _sharedData.IsBusy = true;
        try
        {
            var allGames = new ObservableCollection<GameMetadataRow>(_sourceCache.Items);
            var filename = _sharedData.XmlFilename;
            var system = _sharedData.CurrentSystem;

            // Run backup + save together on a background thread so the UI thread
            // is never blocked by file I/O (also avoids the Avalonia PopupRoot
            // "PlatformImpl is null" trace that fires when the MenuFlyout closes
            // and the UI loop processes stale events during an await suspension)
            bool success = await Task.Run(() =>
            {
                var backupPath = CreateBackupBeforeSave(filename, system);
                if (backupPath != null)
                    System.Diagnostics.Debug.WriteLine($"Backup created: {backupPath}");
                return GamelistService.SaveGamelist(filename, allGames);
            });

            if (success)
            {
                _sharedData.IsDataChanged = false;
                IsSaveEnabled = false;
                UpdateStatusBar(_sharedData.XmlFilename);

                ShowSaveConfirmation = true;
                _ = Task.Delay(2000).ContinueWith(_ =>
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => ShowSaveConfirmation = false));
            }
            else
            {
                await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
                {
                    Title = "Save Failed",
                    Message = "Failed to save the gamelist. Please check file permissions and disk space.",
                    IconTheme = DialogIconTheme.Error,
                    Button1Text = "",
                    Button2Text = "",
                    Button3Text = "OK"
                });
            }
        }
        catch (Exception ex)
        {
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Save Error",
                Message = $"An error occurred while saving the gamelist.\nError: {ex.Message}",
                IconTheme = DialogIconTheme.Error,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            });
        }
        finally
        {
            _sharedData.IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BrowseForGamelistAsync()
    {
        if (!await CheckUnsavedChangesAsync()) return;

        var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load Gamelist XML",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Gamelist XML") { Patterns = ["gamelist.xml"] },
                new FilePickerFileType("All Files") { Patterns = ["*.*"] }
            ]
        });

        if (files.Count == 0) return;

        await LoadGamelistFromFileAsync(files[0].Path.LocalPath);
    }

    [RelayCommand]
    private async Task ReloadCurrentGamelistAsync()
    {
        if (!await CheckUnsavedChangesAsync()) return;
        if (string.IsNullOrEmpty(_sharedData.XmlFilename)) return;
        await LoadGamelistFromFileAsync(_sharedData.XmlFilename);
    }

    [RelayCommand]
    private async Task OpenRecentGamelistAsync(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        if (!System.IO.File.Exists(filePath)) return;
        if (!await CheckUnsavedChangesAsync()) return;
        await LoadGamelistFromFileAsync(filePath);
    }

    [RelayCommand]
    private async Task OpenSystemGamelistAsync(string? gamelistPath)
    {
        if (string.IsNullOrEmpty(gamelistPath)) return;
        if (string.Equals(gamelistPath, _sharedData.XmlFilename, FilePathHelper.PathComparison)) return;
        if (!await CheckUnsavedChangesAsync()) return;
        await LoadGamelistFromFileAsync(gamelistPath);
    }

    [RelayCommand]
    private async Task NewGamelistAsync()
    {
        if (!await CheckUnsavedChangesAsync()) return;

        var romsFolder = Path.TrimEndingDirectorySeparator(_sharedData.RomsFolder);

        var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel == null) return;

        IStorageFolder? suggestedStart = null;
        try { suggestedStart = await topLevel.StorageProvider.TryGetFolderFromPathAsync(new Uri(romsFolder)); } catch { }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select System Folder for New Gamelist",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStart
        });

        if (folders.Count == 0) return;

        var selectedFolder = Path.TrimEndingDirectorySeparator(folders[0].Path.LocalPath);

        // Must be exactly one level inside the ROMs folder
        var parent = Path.GetDirectoryName(selectedFolder);
        if (!string.Equals(parent, romsFolder, FilePathHelper.PathComparison))
        {
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "New Gamelist",
                Message = "The selected folder must be directly inside the ROMs folder.",
                DetailMessage = $"Expected a folder one level inside: {romsFolder}",
                IconTheme = DialogIconTheme.Warning,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            });
            return;
        }

        var systemName = Path.GetFileName(selectedFolder)!;

        // System must have file types defined in filetypes.ini
        if (!_sharedData.GetFileTypes().ContainsKey(systemName))
        {
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "New Gamelist",
                Message = $"'{systemName}' is not a recognised system.",
                DetailMessage = "No file types are configured for this system in filetypes.ini.",
                IconTheme = DialogIconTheme.Warning,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            });
            return;
        }

        // Set up an empty gamelist in memory — nothing is written to disk until the user saves
        var gamelistPath = Path.Combine(selectedFolder, "gamelist.xml");
        _sharedData.SetGamelist(gamelistPath, systemName, new ObservableCollection<GameMetadataRow>());

        ClearFilters();
        ClearReportColumns();

        _isLoadingData = true;
        _sourceCache.Clear();
        PopulateAvailableGenres();
        _isLoadingData = false;

        IsSaveEnabled = false;
        IsGamelistLoaded = true;
        FileStatusText = gamelistPath;
        LastModifiedText = string.Empty;

        var matchedSystem = Systems.FirstOrDefault(s => string.Equals(s.Name, systemName, FilePathHelper.PathComparison));
        if (matchedSystem == null)
        {
            var logo = TryLoadSystemLogo(systemName);
            matchedSystem = new SystemItem { Name = systemName, GamelistPath = gamelistPath, Logo = logo };
        }

        SelectedSystem = matchedSystem;
        OnPropertyChanged(nameof(SystemLogo));
        CalculateStatistics();

        await FindNewItems();
    }

    #endregion

    #region Private Methods
    private void LoadSystems()
    {
        Systems.Clear();

        var romsFolder = _sharedData.RomsFolder;
        if (string.IsNullOrWhiteSpace(romsFolder) || !Directory.Exists(romsFolder))
        {
            StatusText = "Set ROMs folder in Settings";
            IsSystemsComboBoxEnabled = false;
            return;
        }

        foreach (var dir in Directory.EnumerateDirectories(romsFolder).OrderBy(Path.GetFileName))
        {
            var gamelistPath = Path.Combine(dir, "gamelist.xml");
            if (!File.Exists(gamelistPath)) continue;

            var systemName = Path.GetFileName(dir) ?? dir;
            var logo = TryLoadSystemLogo(systemName);

            Systems.Add(new SystemItem { Name = systemName, GamelistPath = gamelistPath, Logo = logo });
        }

        IsSystemsComboBoxEnabled = Systems.Count > 0;
        StatusText = Systems.Count == 0 ? "No systems found in ROMs folder" : string.Empty;
    }

    private void LoadRecentFilesFromSettings()
    {
        RecentFiles.Clear();
        foreach (var file in _settingsService.GetRecentFiles())
            RecentFiles.Add(new RecentFileItem(file));
    }

    private void AddRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        var existing = RecentFiles.FirstOrDefault(r => r.FilePath.Equals(filePath, FilePathHelper.PathComparison));
        if (existing != null)
            RecentFiles.Remove(existing);

        RecentFiles.Insert(0, new RecentFileItem(filePath));

        var maxRecentFiles = _settingsService.GetInt(SettingKeys.AdvancedSection, SettingKeys.RecentFilesCount, 15);
        while (RecentFiles.Count > maxRecentFiles)
            RecentFiles.RemoveAt(RecentFiles.Count - 1);

        _settingsService.SaveRecentFiles(RecentFiles.Select(r => r.FilePath).ToList());
    }

    private async Task<bool> CheckUnsavedChangesAsync()
    {
        if (!_sharedData.IsDataChanged || !_sharedData.EnableSaveReminder)
            return true;

        var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (mainWindow == null) return true;

        var dialog = new ThreeButtonDialogView(new ThreeButtonDialogConfig
        {
            Title = "Unsaved Changes",
            Message = "You have unsaved changes to the current gamelist.",
            DetailMessage = "Do you want to save your changes before continuing?",
            IconTheme = DialogIconTheme.Warning,
            Button1Text = "Cancel",
            Button2Text = "Don't Save",
            Button3Text = "Save"
        });

        var result = await dialog.ShowDialog<ThreeButtonResult>(mainWindow);

        return result switch
        {
            ThreeButtonResult.Button1 => false,
            ThreeButtonResult.Button3 => await SaveGamelistAsync().ContinueWith(_ => true),
            _ => true
        };
    }

    private async Task LoadGamelistFromFileAsync(string filePath)
    {
        ClearFilters();
        ClearReportColumns();

        _sharedData.IsBusy = true;
        try
        {
            ObservableCollection<GameMetadataRow> loadedGames;
            List<string> duplicates;
            try
            {
                (loadedGames, duplicates) = await Task.Run(() => GamelistService.LoadGamelist(filePath, _sharedData.IgnoreDuplicates));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load error: {ex.Message}");
                FileStatusText = $"Load failed: {filePath}";
                LastModifiedText = string.Empty;
                return;
            }

            if (duplicates.Count > 0)
            {
                var detail = string.Join("\n", duplicates.Take(10).Select(Path.GetFileName));
                if (duplicates.Count > 10)
                    detail += $"\n...and {duplicates.Count - 10} more";
                await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
                {
                    Title = "Duplicate Entries Detected",
                    Message = "Duplicate entries were found in the gamelist. ROM path must be unique.",
                    DetailMessage = detail,
                    IconTheme = DialogIconTheme.Warning,
                    Button1Text = "",
                    Button2Text = "",
                    Button3Text = "OK"
                });
            }

            var systemName = Path.GetFileName(Path.GetDirectoryName(filePath)) ?? "unknown";

            _sharedData.SetGamelist(filePath, systemName, loadedGames);

            _isLoadingData = true;
            _sourceCache.Clear();
            _sourceCache.AddOrUpdate(loadedGames);
            PopulateAvailableGenres();
            _isLoadingData = false;

            foreach (var game in loadedGames)
                game.PropertyChanged += GameItem_PropertyChanged;

            IsSaveEnabled = false;
            IsGamelistLoaded = true;

            var matchedSystem = Systems.FirstOrDefault(s => string.Equals(s.GamelistPath, filePath, FilePathHelper.PathComparison));
            if (matchedSystem == null)
            {
                var logo = TryLoadSystemLogo(systemName);
                matchedSystem = new SystemItem { Name = systemName, GamelistPath = filePath, Logo = logo };
            }

            SelectedSystem = matchedSystem;
            OnPropertyChanged(nameof(SystemLogo));

            AddRecentFile(filePath);
            CalculateStatistics();
            UpdateStatusBar(filePath);

            if (Games.Count > 0)
                RequestSelectFirstItem?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _sharedData.IsBusy = false;
        }
    }

    private void UpdateStatusBar(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            FileStatusText = filePath;
            LastModifiedText = $"Modified: {fileInfo.LastWriteTime:MMM dd, yyyy HH:mm}";
        }
        catch
        {
            FileStatusText = filePath;
            LastModifiedText = string.Empty;
        }
    }

    private static Bitmap? TryLoadSystemLogo(string systemName)
    {
        try
        {
            var uri = new Uri($"avares://Gamelist_Manager/Assets/Systems/{systemName}.png");
            using var stream = AssetLoader.Open(uri);
            return new Bitmap(stream);
        }
        catch { return null; }
    }

    private static string? CreateBackupBeforeSave(string gamelistPath, string systemName)
    {
        try
        {
            if (!File.Exists(gamelistPath))
                return null;

            var backupDirectory = Path.Combine(AppContext.BaseDirectory, BackupFolderName, systemName);
            Directory.CreateDirectory(backupDirectory);

            var timestamp = DateTime.Now.ToString("ddMMMyyyy_HHmm");
            var backupPath = Path.Combine(backupDirectory, $"gamelist_{timestamp}.xml");

            File.Copy(gamelistPath, backupPath, overwrite: false);
            return backupPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Backup failed: {ex.Message}");
            return null;
        }
    }

    #endregion
}