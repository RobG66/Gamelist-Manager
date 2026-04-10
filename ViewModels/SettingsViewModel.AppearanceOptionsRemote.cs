using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Services;

namespace Gamelist_Manager.ViewModels;

public partial class SettingsViewModel
{
    #region Observable Properties — Appearance

    [ObservableProperty] private int _selectedThemeIndex;
    [ObservableProperty] private int _selectedColorIndex;
    [ObservableProperty] private int _selectedAlternatingRowColorIndex;
    [ObservableProperty] private int _selectedGridLinesVisibilityIndex;
    [ObservableProperty] private double _appFontSize = 12;
    [ObservableProperty] private double _gridFontSize = 12;

    #endregion

    #region Observable Properties — Options

    [ObservableProperty] private bool _confirmBulkChanges = true;
    [ObservableProperty] private bool _enableSaveReminder = true;
    [ObservableProperty] private bool _verifyImageDownloads = true;
    [ObservableProperty] private bool _showGamelistStats = true;
    [ObservableProperty] private bool _videoAutoplay;
    [ObservableProperty] private bool _rememberColumns;
    [ObservableProperty] private bool _rememberAutosize;
    [ObservableProperty] private bool _enableDelete;
    [ObservableProperty] private bool _ignoreDuplicates;
    [ObservableProperty] private bool _batchProcessing = true;
    [ObservableProperty] private bool _showLogTimestamp;
    [ObservableProperty] private string _maxUndo = "15";
    [ObservableProperty] private string _maxBatch = "300";
    [ObservableProperty] private string _searchDepth = "2";
    [ObservableProperty] private string _recentFilesCount = "15";
    [ObservableProperty] private double _defaultVolume = 50;
    [ObservableProperty] private int _logVerbosityIndex;

    #endregion

    #region Observable Properties — Remote

    [ObservableProperty] private string _hostname = string.Empty;
    [ObservableProperty] private string _userId = string.Empty;
    [ObservableProperty] private string _password = string.Empty;

    #endregion

    #region Property Change Callbacks

    partial void OnSelectedThemeIndexChanged(int value)
    {
        if (!_isLoading)
        {
            ThemeService.ApplyThemeVariant(value);
            SelectedAlternatingRowColorIndex = 0;
        }
    }

    partial void OnSelectedColorIndexChanged(int value)
    {
        if (!_isLoading)
            ThemeService.ApplyAccentColor(value);
    }

    #endregion
}
