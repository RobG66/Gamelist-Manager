using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class DatToolViewModel
{
    #region Commands
    [RelayCommand]
    private async Task StreamFromMame()
    {
        Reset();
        IsBusy = true;
        IsStreamFromMameEnabled = false;

        try
        {
            (Stream? xmlStream, System.Diagnostics.Process? mameProcess) =
                await GetMameListXmlStreamAsync(_settingsState.MamePath, "-listxml");

            if (xmlStream == null)
            {
                await _dialogService.ShowErrorAsync("Error", "Failed to get XML stream from MAME.");
                return;
            }

            _mameProcess = mameProcess;
            await ProcessDatStreamAsync(xmlStream, "MAME -listxml output", _mameProcess);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Error streaming from MAME: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            IsStreamFromMameEnabled = _allowStreamingFromMame;
        }
    }

    // Opens a file picker for .dat/.xml files and processes the selected file
    // through the DAT pipeline.
    [RelayCommand]
    private async Task OpenDatFile()
    {
        var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select a DAT File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("DAT files") { Patterns = ["*.dat"] },
                new FilePickerFileType("XML files") { Patterns = ["*.xml"] },
                new FilePickerFileType("All files") { Patterns = ["*.*"] }
            ]
        });

        if (files.Count == 0) return;

        Reset();
        IsBusy = true;
        IsStreamFromMameEnabled = false;

        try
        {
            string datFileName = Path.GetFileName(files[0].Path.LocalPath);
            using Stream xmlStream = File.OpenRead(files[0].Path.LocalPath);
            await ProcessDatStreamAsync(xmlStream, datFileName);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Error opening DAT file: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            IsStreamFromMameEnabled = _allowStreamingFromMame;
        }
    }

    // Compares the DAT summary against the gamelist to find missing parents and clones,
    // generates a text or CSV report, writes it to a temp file, and opens it in the
    // default application. Devices, BIOS entries, and software-list items are excluded.
    [RelayCommand]
    private async Task FindMissing()
    {
        if (_datSummary.Count == 0) return;

        IsBusy = true;

        try
        {
            bool isTextFormat = !CsvOutput;
            string datFileName = DatFileName;
            bool filterParents = MissingFilterParents;
            bool filterClones = MissingFilterClones;

            var datSnapshot = _datSummary.ToList();
            var gamelistSnapshot = _gamelistSummary.ToList();
            string reportContent = await Task.Run(() => BuildMissingReport(
                datSnapshot, gamelistSnapshot, filterParents, filterClones, isTextFormat, datFileName));

            if (string.IsNullOrEmpty(reportContent))
            {
                await _dialogService.ShowInfoAsync("No Missing Games", "All playable games from the DAT are present in your gamelist.");
                return;
            }

            string extension = isTextFormat ? "txt" : "csv";
            string tempFilePath = Path.Combine(
                Path.GetTempPath(),
                $"MissingGames_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}");

            await File.WriteAllTextAsync(tempFilePath, reportContent);

            Process.Start(new ProcessStartInfo(tempFilePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Error generating report: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }


    [RelayCommand]
    private void Close()
    {
        Dispose();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
