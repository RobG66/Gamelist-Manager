using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class MainWindowViewModel
{
    #region Private Fields
    private readonly Dictionary<string, bool> _columnVisibility = new();
    #endregion

    #region Observable Properties
    [ObservableProperty] private bool _mediaPathsVisible;
    [ObservableProperty] private bool _descriptionPanelVisible = true;
    [ObservableProperty] private bool _sizeToFit = true;
    #endregion

    #region Public Properties
    public bool RememberColumns => _settingsState.RememberColumns;
    public bool RememberAutosize => _settingsState.RememberAutosize;
    #endregion

    #region Public Methods
    public bool GetColumnVisible(string type) =>
        _columnVisibility.TryGetValue(type, out var v) && v;

    public void SetColumnVisible(string type, bool value)
    {
        _columnVisibility[type] = value;
        OnPropertyChanged($"Column_{type}");
    }

    public event System.Action? ColumnVisibilityChanged;
    #endregion

    #region Commands
    [RelayCommand]
    private void ToggleColumn(string type)
    {
        SetColumnVisible(type, !GetColumnVisible(type));
        ColumnVisibilityChanged?.Invoke();
        SaveColumnSettings();
    }

    [RelayCommand]
    private void ToggleDescriptionPanel()
    {
        DescriptionPanelVisible = !DescriptionPanelVisible;
        SaveColumnSettings();
    }

    [RelayCommand]
    private void ToggleSizeToFit()
    {
        SizeToFit = !SizeToFit;
        SaveColumnSettings();
    }

    [RelayCommand]
    private void ToggleRememberColumns()
    {
        _settingsState.Save(SettingKeys.RememberColumns, !_settingsState.RememberColumns);
    }

    [RelayCommand]
    private void ToggleRememberAutosize()
    {
        _settingsState.Save(SettingKeys.RememberAutoSize, !_settingsState.RememberAutosize);
    }

    [RelayCommand]
    private void ToggleMediaPaths()
    {
        MediaPathsVisible = !MediaPathsVisible;

        if (MediaPathsVisible)
            SizeToFit = false;

        ColumnVisibilityChanged?.Invoke();
        SaveColumnSettings();
    }

    [RelayCommand]
    private void ResetColumnVisibility()
    {
        DescriptionPanelVisible = true;
        MediaPathsVisible = false;
        SizeToFit = true;

        // Standard columns
        foreach (var column in MetadataService.GetToggleableColumns())
            SetColumnVisible(column.Type, column.DefaultVisible);

        // Custom columns
        foreach (var decl in CustomColumnDecl.AllDeclarations)
            SetColumnVisible(decl.Type, decl.DefaultVisible);

        ColumnVisibilityChanged?.Invoke();
        SaveColumnSettings();
    }
    #endregion

    #region Private Methods
    private void LoadColumnSettings()
    {
        var saved = _settingsState.GetColumnVisibility();
        var toggleableColumns = MetadataService.GetToggleableColumns();

        if (_settingsState.RememberAutosize || _settingsState.RememberColumns)
            SizeToFit = saved != null && bool.TryParse(saved.GetValueOrDefault("SizeToFit", "true"), out var v) ? v : true;

        foreach (var column in toggleableColumns)
            _columnVisibility[column.Type] = column.DefaultVisible;

        foreach (var decl in CustomColumnDecl.AllDeclarations)
            _columnVisibility[decl.Type] = decl.DefaultVisible;

        if (!_settingsState.RememberColumns || saved == null)
            return;

        DescriptionPanelVisible = bool.TryParse(saved.GetValueOrDefault("DescriptionPanel", "true"), out var dp) ? dp : true;
        MediaPathsVisible = bool.TryParse(saved.GetValueOrDefault("MediaPaths", "false"), out var mp) ? mp : false;

        // Standard columns
        foreach (var column in toggleableColumns)
            _columnVisibility[column.Type] = bool.TryParse(saved.GetValueOrDefault(column.Type, column.DefaultVisible.ToString()), out var cv) ? cv : column.DefaultVisible;

        // Custom columns
        foreach (var decl in CustomColumnDecl.AllDeclarations)
            _columnVisibility[decl.Type] = bool.TryParse(saved.GetValueOrDefault(decl.Type, decl.DefaultVisible.ToString()), out var cv) ? cv : decl.DefaultVisible;
    }

    private void SaveColumnSettings()
    {
        if (!_settingsState.RememberAutosize && !_settingsState.RememberColumns)
            return;

        var values = new Dictionary<string, string>();

        if (_settingsState.RememberAutosize || _settingsState.RememberColumns)
            values["SizeToFit"] = SizeToFit.ToString();

        if (_settingsState.RememberColumns)
        {
            values["DescriptionPanel"] = DescriptionPanelVisible.ToString();
            values["MediaPaths"] = MediaPathsVisible.ToString();

            // Standard columns
            foreach (var column in MetadataService.GetToggleableColumns())
                values[column.Type] = GetColumnVisible(column.Type).ToString();

            // Custom columns
            foreach (var decl in CustomColumnDecl.AllDeclarations)
                values[decl.Type] = GetColumnVisible(decl.Type).ToString();
        }



        _settingsState.SaveColumnVisibility(values);
    }

    #endregion



    #region Custom Column Helpers

    private async Task PopulateCustomColumnsAsync(string? romFolder)
    {
        if (!string.IsNullOrEmpty(romFolder))
            await PopulateRomSizesAsync(_sourceCache.Items.ToList(), romFolder);

        PopulateRomExtensions();
        PopulateMissingMedia();

    }

    private void PopulateMissingMedia()
    {
        var enabledMedia = _sessionState.AvailableMedia
            .Where(m => m.MediaEnabled)
            .ToList();

        foreach (var game in _sourceCache.Items)
            game.EnabledMedia = enabledMedia;
    }

    private static async Task PopulateRomSizesAsync(IList<GameMetadataRow> games, string romFolder)
    {
        if (!Directory.Exists(romFolder)) return;

        var sizeMap = await Task.Run(() =>
            new DirectoryInfo(romFolder)
                .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                .ToDictionary(f => f.Name, f => f.Length, StringComparer.OrdinalIgnoreCase));

        foreach (var game in games)
        {
            if (string.IsNullOrEmpty(game.Path)) continue;
            var fileName = Path.GetFileName(game.Path);
            if (fileName == null) continue;
            if (!sizeMap.TryGetValue(fileName, out var bytes)) continue;

            game.RomFileSizeBytes = bytes;
            game.RomFileSize = bytes switch
            {
                >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} gb",
                >= 1_048_576 => $"{bytes / 1_048_576.0:F1} mb",
                >= 1_024 => $"{bytes / 1_024.0:F0} kb",
                _ => $"{bytes} b"
            };
        }
    }

    private void PopulateRomExtensions()
    {
        foreach (var game in _sourceCache.Items)
        {
            if (string.IsNullOrEmpty(game.Path)) continue;
            game.RomExtension = Path.GetExtension(game.Path).TrimStart('.');
        }
    }

    #endregion



}