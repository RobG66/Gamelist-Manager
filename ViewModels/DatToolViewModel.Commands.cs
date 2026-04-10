using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Views;
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
                await GetMameListXmlStreamAsync(_sharedData.MamePath, "-listxml");

            if (xmlStream == null)
            {
                await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
                {
                    Title = "Error",
                    Message = "Failed to get XML stream from MAME.",
                    IconTheme = DialogIconTheme.Error,
                    Button1Text = string.Empty,
                    Button2Text = string.Empty,
                    Button3Text = "OK"
                });
                return;
            }

            _mameProcess = mameProcess;
            await ProcessDatStreamAsync(xmlStream, "MAME -listxml output", _mameProcess);
        }
        catch (Exception ex)
        {
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Error",
                Message = $"Error streaming from MAME: {ex.Message}",
                IconTheme = DialogIconTheme.Error,
                Button1Text = string.Empty,
                Button2Text = string.Empty,
                Button3Text = "OK"
            });
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
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Error",
                Message = $"Error opening DAT file: {ex.Message}",
                IconTheme = DialogIconTheme.Error,
                Button1Text = string.Empty,
                Button2Text = string.Empty,
                Button3Text = "OK"
            });
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
        if (_datSummary.Count == 0 || _gamelistSummary.Count == 0) return;

        IsBusy = true;

        try
        {
            bool isTextFormat = !CsvOutput;
            string datFileName = DatFileName;
            bool filterParents = MissingFilterParents;
            bool filterClones = MissingFilterClones;

            string reportContent = await Task.Run(() =>
            {
                var gamelistLookup = new HashSet<string>(
                    _gamelistSummary.Select(g => g.Name),
                    FilePathHelper.PathComparer);

                var missingParents = new List<(string Name, string Description, string NonPlayable, string CHDRequired)>();
                var missingClones = new List<(string Name, string CloneOf, string Description, string NonPlayable, string CHDRequired)>();

                foreach (var datItem in _datSummary)
                {
                    if (gamelistLookup.Contains(datItem.Name)) continue;

                    if (!string.IsNullOrEmpty(datItem.NonPlayable))
                    {
                        string np = datItem.NonPlayable.ToLowerInvariant();
                        if (np.Contains("device") || np.Contains("bios") || np.Contains("software list"))
                            continue;
                    }

                    if (string.IsNullOrEmpty(datItem.CloneOf))
                        missingParents.Add((datItem.Name, datItem.Description, datItem.NonPlayable, datItem.CHDRequired));
                    else
                        missingClones.Add((datItem.Name, datItem.CloneOf, datItem.Description, datItem.NonPlayable, datItem.CHDRequired));
                }

                if (filterParents)
                    missingClones.Clear();
                else if (filterClones)
                    missingParents.Clear();

                if (missingParents.Count == 0 && missingClones.Count == 0)
                    return string.Empty;

                return isTextFormat
                    ? GenerateTextReport(missingParents, missingClones, datFileName)
                    : GenerateCsvReport(missingParents, missingClones, datFileName);
            });

            if (string.IsNullOrEmpty(reportContent))
            {
                await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
                {
                    Title = "No Missing Games",
                    Message = "All playable games from the DAT are present in your gamelist.",
                    IconTheme = DialogIconTheme.Info,
                    Button1Text = string.Empty,
                    Button2Text = string.Empty,
                    Button3Text = "OK"
                });
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
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Error",
                Message = $"Error generating report: {ex.Message}",
                IconTheme = DialogIconTheme.Error,
                Button1Text = string.Empty,
                Button2Text = string.Empty,
                Button3Text = "OK"
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Resets the Report Summary ComboBox and clears the internal report lookup data.
    // Temporary DataGrid columns are managed by the main ViewModel via
    // MainWindowViewModel.ClearReportColumnsCommand.
    [RelayCommand]
    private void ClearReport()
    {
        ReportLookup.Clear();
        ReportViewIndex = 0;
        IsClearReportEnabled = false;
    }

    // Disposes the ViewModel, which fires ReportColumnsCleared to clean up temporary
    // DataGrid columns, and raises CloseRequested so the main window can tear down the panel.
    [RelayCommand]
    private void Close()
    {
        Dispose();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
