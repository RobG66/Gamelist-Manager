using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
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
    public bool RememberColumns => _sharedData.RememberColumns;
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
        _sharedData.RememberColumns = !_sharedData.RememberColumns;
        _settingsService.SetBool(SettingKeys.BehaviorSection, SettingKeys.RememberColumns, _sharedData.RememberColumns);
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

        foreach (var column in GamelistMetaData.GetToggleableColumns())
            SetColumnVisible(column.Type, column.DefaultVisible);

        ColumnVisibilityChanged?.Invoke();
        SaveColumnSettings();
    }
    #endregion

    #region Private Methods
    private void LoadColumnSettings()
    {
        if (_sharedData.RememberAutosize || _sharedData.RememberColumns)
            SizeToFit = _settingsService.GetBool("ColumnVisibility", "SizeToFit", true);

        foreach (var column in GamelistMetaData.GetToggleableColumns())
            _columnVisibility[column.Type] = column.DefaultVisible;

        if (!_sharedData.RememberColumns)
            return;

        DescriptionPanelVisible = _settingsService.GetBool("ColumnVisibility", "DescriptionPanel", true);
        MediaPathsVisible = _settingsService.GetBool("ColumnVisibility", "MediaPaths", false);

        foreach (var column in GamelistMetaData.GetToggleableColumns())
            _columnVisibility[column.Type] = _settingsService.GetBool("ColumnVisibility", column.Type, column.DefaultVisible);
    }

    private void SaveColumnSettings()
    {
        if (_sharedData.RememberAutosize || _sharedData.RememberColumns)
            _settingsService.SetBool("ColumnVisibility", "SizeToFit", SizeToFit);

        if (!_sharedData.RememberColumns)
            return;

        _settingsService.SetBool("ColumnVisibility", "DescriptionPanel", DescriptionPanelVisible);
        _settingsService.SetBool("ColumnVisibility", "MediaPaths", MediaPathsVisible);

        foreach (var column in GamelistMetaData.GetToggleableColumns())
            _settingsService.SetBool("ColumnVisibility", column.Type, GetColumnVisible(column.Type));
    }
    #endregion
}