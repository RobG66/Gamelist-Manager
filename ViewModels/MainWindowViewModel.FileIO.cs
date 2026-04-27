using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Xaml.Interactions.Custom;
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
    public bool HasRecentFiles => _sharedData.RecentFiles.Count > 0;
    public event EventHandler? RequestSelectFirstItem;

    [ObservableProperty] private string _fileStatusText = "No file loaded";
    [ObservableProperty] private string _lastModifiedText = string.Empty;
    #endregion

    #region Commands

    [RelayCommand]
    private void ClearRecentFiles()
    {
        _sharedData.RecentFiles.Clear();
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
                AddRecentFile(_sharedData.XmlFilename);

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

        IStorageFolder? suggestedStart = null;
        try
        {
            var profileType = _sharedData.ProfileType;
            var startPath = profileType switch
            {
                SettingKeys.ProfileTypeEsDe when !string.IsNullOrEmpty(_sharedData.EsDeRoot)
                    => Path.Combine(_sharedData.EsDeRoot, "gamelists"),
                _ when !string.IsNullOrEmpty(_sharedData.RomsFolder)
                    => _sharedData.RomsFolder,
                _ => null
            };

            if (startPath != null && Directory.Exists(startPath))
                suggestedStart = await topLevel.StorageProvider.TryGetFolderFromPathAsync(new Uri("file://" + startPath));
        }
        catch { }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load Gamelist XML",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStart,
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

        var candidates = await BuildSystemCandidatesAsync();

        if (candidates.Count == 0)
        {
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "New Gamelist",
                Message = "No recognised system folders found.",
                DetailMessage = "Make sure your ROMs folder contains system subfolders that match known systems.",
                IconTheme = DialogIconTheme.Warning,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            });
            return;
        }

        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var pickerVm = new GamelistPickerViewModel(candidates, showAllSystems: true);
        var picker = new GamelistPickerView(pickerVm);
        var selected = await picker.ShowDialog<SystemPickerItem?>(mainWindow);

        if (selected == null) return;

        if (selected.HasGamelist)
        {
            var confirm = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Gamelist Already Exists",
                Message = $"A gamelist already exists for '{selected.Name}'.",
                DetailMessage = "Creating a new one will replace it. Do you want to continue?",
                IconTheme = DialogIconTheme.Warning,
                Button1Text = "Cancel",
                Button2Text = "",
                Button3Text = "Continue"
            });
            if (confirm != ThreeButtonResult.Button3) return;
        }

        var systemName = selected.Name;
        var gamelistFolder = selected.FolderPath;
        var gamelistPath = Path.Combine(gamelistFolder, "gamelist.xml");
        // TODO: This is probably wrong and needs to be fixed for ES-DE since gamelists are in a separate folder from ROMs. Need to rethink how we build the system list and picker.

        // Create the gamelist directory if it doesn't exist
        Directory.CreateDirectory(gamelistFolder);

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

    [RelayCommand]
    private async Task OpenSystemPickerAsync()
    {
        if (!await CheckUnsavedChangesAsync()) return;

        var candidates = await BuildSystemCandidatesAsync();

        if (candidates.Count == 0 || !candidates.Any(c => c.HasGamelist))
        {
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Open Gamelist",
                Message = "No gamelists found.",
                DetailMessage = "No recognised system folders with a gamelist were found.",
                IconTheme = DialogIconTheme.Warning,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            });
            return;
        }

        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var pickerVm = new GamelistPickerViewModel(candidates, showAllSystems: false);
        var picker = new GamelistPickerView(pickerVm);
        var selected = await picker.ShowDialog<SystemPickerItem?>(mainWindow);

        if (selected == null) return;

        await LoadGamelistFromFileAsync(Path.Combine(selected.FolderPath, "gamelist.xml"));
    }

    #endregion

    #region Private Methods

    private async Task<List<SystemPickerItem>> BuildSystemCandidatesAsync()
    {
        bool isEsDe = _sharedData.ProfileType == SettingKeys.ProfileTypeEsDe;

        // Always scan ROMs folder for system names.
        // For ES-DE, gamelists live in a separate folder under EsDeRoot.
        var scanFolder = Path.TrimEndingDirectorySeparator(_sharedData.RomsFolder);

        var gamelistsFolder = isEsDe
            ? Path.Combine(Path.TrimEndingDirectorySeparator(_sharedData.EsDeRoot), "gamelists")
            : scanFolder;

        if (string.IsNullOrWhiteSpace(scanFolder) || !Directory.Exists(scanFolder))
            return [];

        var fileTypes = _sharedData.GetFileTypes();

        return await Task.Run(() =>
            Directory.EnumerateDirectories(scanFolder)
                .Select(dir => Path.GetFileName(dir)!)
                .Where(name => fileTypes.ContainsKey(name))
                .OrderBy(name => name)
                .Select(name => new SystemPickerItem
                {
                    Name = name,
                    FolderPath = Path.Combine(gamelistsFolder, name),
                    Logo = TryLoadSystemLogo(name),
                    HasGamelist = File.Exists(Path.Combine(gamelistsFolder, name, "gamelist.xml"))
                })
                .ToList());
    }

    private Window? GetMainWindow() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

    private async Task LoadSystemsAsync()
    {
        var profileType = _sharedData.ProfileType;
        var scanFolder = profileType switch
        {
            SettingKeys.ProfileTypeEsDe => Path.Combine(_sharedData.EsDeRoot, "gamelists"),
            _ => _sharedData.RomsFolder
        };

        if (string.IsNullOrWhiteSpace(scanFolder) || !Directory.Exists(scanFolder))
        {
            Systems.Clear();
            StatusText = profileType == SettingKeys.ProfileTypeEsDe ? "Set ES-DE root folder in Settings" : "Set ROMs folder in Settings";
            IsSystemsComboBoxEnabled = false;
            return;
        }

        Systems.Clear();
        IsSystemsComboBoxEnabled = false;

        var found = await Task.Run(() =>
        {
            var items = new List<(string name, string path)>();
            foreach (var dir in Directory.EnumerateDirectories(scanFolder).OrderBy(Path.GetFileName))
            {
                var gamelistPath = Path.Combine(dir, "gamelist.xml");
                if (!File.Exists(gamelistPath)) continue;

                var systemName = Path.GetFileName(dir) ?? dir;
                items.Add((systemName, gamelistPath));
            }
            return items;
        });

        foreach (var (name, path) in found)
        {
            var logo = TryLoadSystemLogo(name);
            Systems.Add(new SystemItem { Name = name, GamelistPath = path, Logo = logo });
        }

        IsSystemsComboBoxEnabled = Systems.Count > 0;
        StatusText = Systems.Count == 0
            ? (profileType == SettingKeys.ProfileTypeEsDe ? "No systems found in ES-DE gamelists folder" : "No systems found in ROMs folder")
            : string.Empty;
    }

    private void LoadRecentFilesFromSettings()
    {
        _sharedData.RecentFiles.Clear();
        foreach (var file in _settingsService.GetRecentFiles())
            _sharedData.RecentFiles.Add(new RecentFileItem(file));
    }

    private void AddRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        var existing = _sharedData.RecentFiles.FirstOrDefault(r => r.FilePath.Equals(filePath, FilePathHelper.PathComparison));
        if (existing != null)
            _sharedData.RecentFiles.Remove(existing);

        _sharedData.RecentFiles.Insert(0, new RecentFileItem(filePath));

        var maxRecentFiles = _settingsService.GetInt(SettingKeys.RecentFilesCount);
        while (_sharedData.RecentFiles.Count > maxRecentFiles)
            _sharedData.RecentFiles.RemoveAt(_sharedData.RecentFiles.Count - 1);

        _settingsService.SaveRecentFiles(_sharedData.RecentFiles.Select(r => r.FilePath).ToList());
    }

    internal async Task<bool> CheckUnsavedChangesAsync()
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

        if (!await EnsureMatchingProfileAsync(filePath)) return;

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

            if (_sharedData.ProfileType == SettingKeys.ProfileTypeEsDe)
            {
                var mediaDir = SettingsService.Instance.EsDeMediaDirectory(_sharedData.EsDeMediaBase, _sharedData.CurrentSystem);
                await Task.Run(() => PopulateMediaPaths(loadedGames, mediaDir));
            }

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

            if (_sharedData.CheckForNewAndMissingGamesOnLoad)
            {
                await FindNewItemsCore(silent: true);
                await FindMissingItemsCore(silent: true);
            }
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

    #region ES-DE Media Helpers

    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg"];
    private static readonly string[] VideoExtensions = [".mp4", ".avi", ".mkv"];
    private static readonly string[] ManualExtensions = [".pdf"];

    private static void PopulateMediaPaths(IList<GameMetadataRow> games, string mediaDirectory)
    {
        if (string.IsNullOrEmpty(mediaDirectory)) return;

        var mediaDecls = GamelistMetaData.GetAllMediaFolderTypes();

        foreach (var game in games)
        {
            var romPath = game.Path;
            if (string.IsNullOrEmpty(romPath)) continue;

            var romName = FilePathHelper.NormalizeRomName(romPath);
            if (string.IsNullOrEmpty(romName)) continue;

            foreach (var decl in mediaDecls)
            {
                if (!decl.IsEsDeSupported) continue;

                var folder = Path.Combine(mediaDirectory, decl.EsDeFolderName);

                var extensions = decl.DataType switch
                {
                    MetaDataType.Video => VideoExtensions,
                    MetaDataType.Document => ManualExtensions,
                    _ => ImageExtensions
                };

                string? resolved = null;
                foreach (var ext in extensions)
                {
                    var candidate = Path.Combine(folder, romName + ext);
                    if (File.Exists(candidate))
                    {
                        resolved = candidate;
                        break;
                    }
                }

                game.SetValue(decl.Key, resolved ?? string.Empty);
            }
        }
    }

    #endregion
}