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

    [ObservableProperty] private bool _confirmBulkChanges;
    [ObservableProperty] private bool _enableSaveReminder;
    [ObservableProperty] private bool _verifyImageDownloads;
    [ObservableProperty] private bool _videoAutoplay;
    [ObservableProperty] private bool _rememberColumns;
    [ObservableProperty] private bool _rememberAutosize;
    [ObservableProperty] private bool _enableDelete;
    [ObservableProperty] private bool _ignoreDuplicates;
    [ObservableProperty] private bool _batchProcessing;
    [ObservableProperty] private bool _showLogTimestamp;
    [ObservableProperty] private bool _checkForNewAndMissingGamesOnLoad;
    [ObservableProperty] private int _scraperConfigSaveIndex;
    [ObservableProperty] private int _maxUndo;
    [ObservableProperty] private int _maxBatch;
    [ObservableProperty] private int _searchDepth;
    [ObservableProperty] private int _recentFilesCount;
    [ObservableProperty] private double _defaultVolume;
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
