using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using System.Collections.Generic;

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

        foreach (var column in MetadataService.GetToggleableColumns())
            SetColumnVisible(column.Type, column.DefaultVisible);

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

        if (!_settingsState.RememberColumns || saved == null)
            return;

        DescriptionPanelVisible = bool.TryParse(saved.GetValueOrDefault("DescriptionPanel", "true"), out var dp) ? dp : true;
        MediaPathsVisible = bool.TryParse(saved.GetValueOrDefault("MediaPaths", "false"), out var mp) ? mp : false;

        foreach (var column in toggleableColumns)
            _columnVisibility[column.Type] = bool.TryParse(saved.GetValueOrDefault(column.Type, column.DefaultVisible.ToString()), out var cv) ? cv : column.DefaultVisible;
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

            foreach (var column in MetadataService.GetToggleableColumns())
                values[column.Type] = GetColumnVisible(column.Type).ToString();
        }

        _settingsState.SaveColumnVisibility(values);
    }
    #endregion
}