using Avalonia;
using Avalonia.Controls;
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

    private bool _isNavigating = false;

    private bool TryBeginNavigation()
    {
        if (_isNavigating) return false;
        _isNavigating = true;
        return true;
    }

    private void EndNavigation() => _isNavigating = false;

    #region Properties & Events

    public bool HasRecentFiles => _sessionState.RecentFiles.Count > 0;
    public event EventHandler? RequestSelectFirstItem;
    public event EventHandler? RequestClearSelection;
    public event EventHandler<List<GameMetadataRow>>? RequestRestoreSelection;

    [ObservableProperty] private string _fileStatusText = "No file loaded";
    [ObservableProperty] private string _lastModifiedText = string.Empty;

    #endregion

    #region Commands

    [RelayCommand]
    private async Task ExportToCSV()
    {
        if (Games.Count == 0) return;

        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var options = await ExportCsvOptionsView.ShowAsync(mainWindow);
        if (options == null) return;

        var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Gamelist to CSV",
            DefaultExtension = "csv",
            FileTypeChoices =
            [
                new FilePickerFileType("CSV Files") { Patterns = ["*.csv"] },
                new FilePickerFileType("All Files") { Patterns = ["*"] }
            ]
        });

        if (file == null) return;

        var filePath = file.Path.LocalPath;

        try
        {
            await Task.Run(() => ExportDataToCsv(filePath, Games.ToList(), options));
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Export Successful",
                Message = $"Gamelist exported to CSV successfully.\nFile: {filePath}",
                IconTheme = DialogIconTheme.Info,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            }, mainWindow);
        }
        catch (Exception ex)
        {
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Export Failed",
                Message = $"Failed to export gamelist to CSV.\nError: {ex.Message}",
                IconTheme = DialogIconTheme.Error,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            }, mainWindow);
        }
    }

    [RelayCommand]
    private void ClearRecentFiles()
    {
        _sessionState.RecentFiles.Clear();
        ProfileService.Instance.SaveRecentFiles([]);
    }

    [RelayCommand]
    private async Task LoadGamelistBackupAsync()
    {
        if (!TryBeginNavigation()) return;
        try
        {
            if (!await CheckUnsavedChangesAsync()) return;

            var topLevel = GetMainWindow();
            if (topLevel == null) return;

            var backupFolder = Path.Combine(AppContext.BaseDirectory, BackupFolderName);
            Directory.CreateDirectory(backupFolder);

            IStorageFolder? suggestedStart = null;
            if (Directory.Exists(backupFolder))
                suggestedStart = await topLevel.StorageProvider.TryGetFolderFromPathAsync(backupFolder);

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load Gamelist Backup",
                AllowMultiple = false,
                SuggestedStartLocation = suggestedStart,
                FileTypeFilter =
                [
                    new FilePickerFileType("Gamelist XML") { Patterns = ["*.xml"] },
                    new FilePickerFileType("All Files")    { Patterns = ["*.*"] }
                ]
            });

            if (files.Count == 0) return;

            var backupPath = files[0].Path.LocalPath;

            if (!backupPath.StartsWith(backupFolder, FilePathHelper.PathComparison))
            {
                await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
                {
                    Title = "Invalid Backup",
                    Message = "The selected file is not from the Gamelist Backups folder.",
                    DetailMessage = $"Please select a backup from:\n{backupFolder}",
                    IconTheme = DialogIconTheme.Warning,
                    Button1Text = "",
                    Button2Text = "",
                    Button3Text = "OK"
                });
                return;
            }

            var systemName = Path.GetFileName(Path.GetDirectoryName(backupPath));
            if (string.IsNullOrEmpty(systemName))
            {
                await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
                {
                    Title = "Restore Failed",
                    Message = "Could not determine the system name from the backup path.",
                    IconTheme = DialogIconTheme.Error,
                    Button1Text = "",
                    Button2Text = "",
                    Button3Text = "OK"
                });
                return;
            }

            if (!await LoadGamelistFromFileAsync(backupPath)) return;

            // Null guard
            if (string.IsNullOrEmpty(_settingsState.RootGamelistFolder)) return;

            // Point session at the real gamelist destination, show no path (treated as unsaved new gamelist)
            var realGamelistPath = Path.Combine(_settingsState.RootGamelistFolder, systemName, "gamelist.xml");
            _sessionState.XmlFilename = realGamelistPath;
            FileStatusText = string.Empty;
            LastModifiedText = string.Empty;

            CalculateStatistics();
            if (Games.Count > 0) RequestSelectFirstItem?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            EndNavigation();
        }
    }

    [RelayCommand]
    private async Task SaveGamelistAsync()
    {
        if (string.IsNullOrEmpty(_sessionState.XmlFilename) || _sessionState.CurrentSystem == null)
            return;

        _sessionState.IsBusy = true;
        try
        {
            var allGames = new ObservableCollection<GameMetadataRow>(_sourceCache.Items);
            var filename = _sessionState.XmlFilename;
            var system = _sessionState.CurrentSystem;

            bool success = await Task.Run(() =>
            {
                var backupPath = CreateBackupBeforeSave(filename, system);
                if (backupPath != null)
                    System.Diagnostics.Debug.WriteLine($"Backup created: {backupPath}");
                return GamelistService.SaveGamelist(filename, allGames);
            });

            if (success)
            {
                _sessionState.IsDataChanged = false;
                IsSaveEnabled = false;
                UpdateStatusBar(_sessionState.XmlFilename!);
                AddRecentFile(_sessionState.XmlFilename!);

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
            _sessionState.IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BrowseForGamelistAsync()
    {
        if (!TryBeginNavigation()) return;
        try
        {
            if (!await CheckUnsavedChangesAsync()) return;

            var topLevel = GetMainWindow();
            if (topLevel == null) return;

            IStorageFolder? suggestedStart = null;
            try
            {
                var startPath = _settingsState.RootGamelistFolder;

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
                    new FilePickerFileType("All Files")    { Patterns = ["*.*"] }
                ]
            });

            if (files.Count == 0) return;

            var filePath = files[0].Path.LocalPath;
            if (!await LoadGamelistFromFileAsync(filePath)) return;
            await PostLoadAsync(filePath);
        }
        finally
        {
            EndNavigation();
        }
    }

    [RelayCommand]
    private async Task ReloadCurrentGamelistAsync()
    {
        if (!TryBeginNavigation()) return;
        try
        {
            if (!await CheckUnsavedChangesAsync()) return;
            if (string.IsNullOrEmpty(_sessionState.XmlFilename)) return;
            var filePath = _sessionState.XmlFilename;
            if (!await LoadGamelistFromFileAsync(filePath)) return;
            await PostLoadAsync(filePath, addRecentFile: false);
        }
        finally
        {
            EndNavigation();
        }
    }

    [RelayCommand]
    private async Task OpenRecentGamelistAsync(string? filePath)
    {
        if (!TryBeginNavigation()) return;
        try
        {
            if (string.IsNullOrEmpty(filePath)) return;
            if (!await CheckUnsavedChangesAsync()) return;
            if (!await LoadGamelistFromFileAsync(filePath)) return;
            await PostLoadAsync(filePath);
        }
        finally
        {
            EndNavigation();
        }
    }

    [RelayCommand]
    private async Task OpenSystemGamelistAsync(string? gamelistPath)
    {
        if (!TryBeginNavigation()) return;
        try
        {
            if (string.IsNullOrEmpty(gamelistPath)) return;
            if (string.Equals(gamelistPath, _sessionState.XmlFilename, FilePathHelper.PathComparison)) return;
            if (!await CheckUnsavedChangesAsync()) return;
            if (!await LoadGamelistFromFileAsync(gamelistPath)) return;
            await PostLoadAsync(gamelistPath);
        }
        finally
        {
            EndNavigation();
        }
    }

    [RelayCommand]
    private async Task NewGamelistAsync()
    {
        if (!TryBeginNavigation()) return;
        try
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

            pickerVm.Cleanup();

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

            UnloadGamelist();

            var systemName = selected.Name;

            var gamelistFolder = Path.Combine(SettingsState.Instance.RootGamelistFolder!, systemName);

            var gamelistPath = Path.Combine(gamelistFolder, "gamelist.xml");

            if (!Directory.Exists(gamelistFolder))
                Directory.CreateDirectory(gamelistFolder);

            _sessionState.SetGamelist(gamelistPath, systemName, new ObservableCollection<GameMetadataRow>());

            _sessionState.RefreshAvailableMedia();

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
                var logo = TryLoadSystemLogo(systemName, 130);
                matchedSystem = new SystemItem { Name = systemName, GamelistPath = gamelistPath, Logo = logo };
            }

            if (SelectedSystem != null && !Systems.Contains(SelectedSystem))
                SelectedSystem.Logo?.Dispose();

            SelectedSystem = matchedSystem;
            OnPropertyChanged(nameof(SystemLogo));
            CalculateStatistics();

            await FindNewItems();
            await PopulateCustomColumnsAsync(_sessionState.CurrentRomFolder);
        }
        finally
        {
            EndNavigation();
        }
    }

    [RelayCommand]
    private async Task OpenSystemPickerAsync()
    {
        if (!TryBeginNavigation()) return;
        try
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

            pickerVm.Cleanup();

            if (selected == null) return;

            var gamelistPath = Path.Combine(SettingsState.Instance.RootGamelistFolder!, selected.Name, "gamelist.xml");
            if (!await LoadGamelistFromFileAsync(gamelistPath)) return;
            await PostLoadAsync(gamelistPath);
        }
        finally
        {
            EndNavigation();
        }
    }

    #endregion

    #region Private Methods

    private async Task<List<SystemPickerItem>> BuildSystemCandidatesAsync()
    {
        var scanFolder = _settingsState.RootRomFolder;

        if (string.IsNullOrWhiteSpace(scanFolder) || !Directory.Exists(scanFolder))
            return [];

        var fileTypes = SettingsService.Instance.GetFileTypes();

        return await Task.Run(() =>
            Directory.EnumerateDirectories(scanFolder)
                .Select(dir => Path.GetFileName(dir)!)
                .Where(name => fileTypes.ContainsKey(name))
                .OrderBy(name => name)
                .Select(name => new SystemPickerItem
                {
                    Name = name,
                    FolderPath = Path.Combine(scanFolder, name),
                    Logo = TryLoadSystemLogo(name, 90),
                    HasGamelist = File.Exists(Path.Combine(SettingsState.Instance.RootGamelistFolder!, name, "gamelist.xml"))
                })
                .ToList());
    }

    private Window? GetMainWindow() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

    private async Task LoadSystemsAsync()
    {
        var profile = SettingKeys.GetProfileTypeOption(_settingsState.ProfileType);
        var scanFolder = _settingsState.RootGamelistFolder;

        if (string.IsNullOrWhiteSpace(scanFolder) || !Directory.Exists(scanFolder))
        {
            foreach (var sys in Systems)
                sys.Logo?.Dispose();
            Systems.Clear();
            StatusText = profile.ShowsEsDePathsSection
                ? "Set ES-DE root folder in Settings"
                : "Set ROMs folder in Settings";
            IsSystemsMenuEnabled = false;
            return;
        }

        foreach (var sys in Systems)
            sys.Logo?.Dispose();
        Systems.Clear();
        IsSystemsMenuEnabled = false;

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
            Systems.Add(new SystemItem { Name = name, GamelistPath = path, Logo = TryLoadSystemLogo(name, 90) });

        IsSystemsMenuEnabled = Systems.Count > 0;

        StatusText = Systems.Count == 0 ? "No gamelists found" : string.Empty;
    }

    private void LoadRecentFilesFromSettings()
    {
        _sessionState.RecentFiles.Clear();
        foreach (var file in _settingsService.GetRecentFiles())
            _sessionState.RecentFiles.Add(new RecentFileItem(file));
    }

    private void AddRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        var existing = _sessionState.RecentFiles.FirstOrDefault(r => r.FilePath.Equals(filePath, FilePathHelper.PathComparison));
        if (existing != null)
            _sessionState.RecentFiles.Remove(existing);

        _sessionState.RecentFiles.Insert(0, new RecentFileItem(filePath));

        var maxRecentFiles = _settingsState.RecentFilesCount;
        while (_sessionState.RecentFiles.Count > maxRecentFiles)
            _sessionState.RecentFiles.RemoveAt(_sessionState.RecentFiles.Count - 1);

        ProfileService.Instance.SaveRecentFiles(_sessionState.RecentFiles.Select(r => r.FilePath).ToList());
    }

    internal async Task<bool> CheckUnsavedChangesAsync()
    {
        if (!_sessionState.IsDataChanged || !_settingsState.EnableSaveReminder)
            return true;

        var mainWindow = GetMainWindow();
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

    private async Task<bool> LoadGamelistFromFileAsync(string filePath)
    {

        if (!File.Exists(filePath))
        {
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "File Not Found",
                Message = "The gamelist file could not be found.",
                DetailMessage = filePath,
                IconTheme = DialogIconTheme.Error,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            });
            return false;
        }

        if (!await EnsureMatchingProfileAsync(filePath)) return false;

        _sessionState.IsBusy = true;
        try
        {
            ObservableCollection<GameMetadataRow> loadedGames;
            List<string> duplicates;
            try
            {
                (loadedGames, duplicates) = await Task.Run(() => GamelistService.LoadGamelist(filePath, _settingsState.IgnoreDuplicates));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load error: {ex.Message}");
                FileStatusText = $"Load failed: {filePath}";
                LastModifiedText = string.Empty;
                return false;
            }

            if (duplicates.Count > 0)
            {
                var detail = string.Join("\n", duplicates.Take(10).Select(Path.GetFileName));
                if (duplicates.Count > 10)
                    detail += $"\n...and {duplicates.Count - 10} more";

                await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
                {
                    Title = "Duplicate Entries Detected",
                    Message = "Duplicate entries were found in the gamelist. ROM paths should be unique.",
                    DetailMessage = detail,
                    IconTheme = DialogIconTheme.Warning,
                    Button1Text = "",
                    Button2Text = "",
                    Button3Text = "OK"
                });
            }

            var systemName = Path.GetFileName(Path.GetDirectoryName(filePath)) ?? "unknown";

            _sessionState.SetGamelist(filePath, systemName, loadedGames);

            _sessionState.RefreshAvailableMedia();

            if (!_sessionState.GamelistHasMediaPaths &&
                _sessionState.CurrentMediaFolder is { } mediaDir)
                await Task.Run(() => GamelistService.PopulateMediaPaths(loadedGames, mediaDir));

            _isLoadingData = true;
            _sourceCache.Clear();
            _sourceCache.AddOrUpdate(loadedGames);
            PopulateAvailableGenres();

            IsSaveEnabled = false;
            IsGamelistLoaded = true;
            IsPersistentSelectionEnabled = false;

            var matchedSystem = Systems.FirstOrDefault(s => string.Equals(s.GamelistPath, filePath, FilePathHelper.PathComparison));
            if (matchedSystem == null)
            {
                var logo = TryLoadSystemLogo(systemName, 130);
                matchedSystem = new SystemItem { Name = systemName, GamelistPath = filePath, Logo = logo };
            }

            if (SelectedSystem != null && !Systems.Contains(SelectedSystem))
                SelectedSystem.Logo?.Dispose();

            SelectedSystem = matchedSystem;
            OnPropertyChanged(nameof(SystemLogo));

            foreach (var game in loadedGames)
                game.PropertyChanged += GameItem_PropertyChanged;

            await PopulateCustomColumnsAsync(_sessionState.CurrentRomFolder);

            _isLoadingData = false;
            return true;
        }
        finally
        {
            _sessionState.IsBusy = false;
        }
    }

    private async Task PostLoadAsync(string filePath, bool addRecentFile = true)
    {
        ClearFilters();
        ClearReportColumns();
        if (addRecentFile) AddRecentFile(filePath);
        UpdateStatusBar(filePath);
        CalculateStatistics();
        if (Games.Count > 0) RequestSelectFirstItem?.Invoke(this, EventArgs.Empty);
        if (_settingsState.CheckForNewAndMissingGamesOnLoad)
        {
            await FindNewItemsCore(silent: true);
            await FindMissingItemsCore(silent: true);
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

    private static Bitmap? TryLoadSystemLogo(string systemName, int width)
    {
        try
        {
            var uri = new Uri($"avares://Gamelist_Manager/Assets/Systems/{systemName}.png");
            using var stream = AssetLoader.Open(uri);
            return Bitmap.DecodeToWidth(stream, width);
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

    
    private void ExportDataToCsv(string filePath, IList<GameMetadataRow> filteredRows, ExportCsvOptions options)
    {
        var rows = options.UseFilteredRows
            ? filteredRows
            : _sourceCache.Items.ToList();

        var columns = MetadataService.GetColumnDeclarations()
            .Where(d => d.Key != MetaDataKeys.music && d.Key != MetaDataKeys.desc)
            .Where(d =>
            {
                if (!options.UseVisibleColumnsOnly) return true;
                if (d.AlwaysVisible) return true;
                return d.IsMedia ? MediaPathsVisible : GetColumnVisible(d.Type);
            })
            .ToList();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(string.Join(",", columns.Select(d => CsvHelper.EscapeCsv(d.Name))));

        foreach (var game in rows)
        {
            sb.AppendLine(string.Join(",", columns.Select(d =>
            {
                var value = game.GetValue(d.Key);
                return CsvHelper.EscapeCsv(value?.ToString() ?? string.Empty);
            })));
        }

        File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);
    }

    #endregion
}