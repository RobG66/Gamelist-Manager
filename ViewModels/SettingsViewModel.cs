using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Services;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Gamelist_Manager.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    #region Fields

    private readonly SharedDataService _sharedData = SharedDataService.Instance;
    private bool _isLoading;

    private static readonly string[] ThemeNames = ["Light", "Dark"];
    private static readonly string[] ColorNames =
    [
        "Blue", "Red", "Orange", "Green", "Yellow",
        "Magenta", "Purple", "Teal", "Lime", "Light Blue", "Indigo"
    ];

    #endregion

    #region Observable Properties

    [ObservableProperty] private bool _isDirty;

    #endregion

    #region Constructor

    public SettingsViewModel()
    {
        InitializeMediaFolderItems();
        LoadSettings();
        LoadTemplates();

        PropertyChanged += (_, e) =>
        {
            if (!_isLoading && e.PropertyName != nameof(IsDirty))
                IsDirty = true;
        };
    }

    #endregion

    #region Load / Save / Reset

    public void LoadSettings()
    {
        _isLoading = true;

        var settings = SettingsService.Instance;

        SelectedThemeIndex = NameToIndex(ThemeNames, settings.GetValue(SettingKeys.AppearanceSection, SettingKeys.Theme, "Light"));
        SelectedColorIndex = NameToIndex(ColorNames, settings.GetValue(SettingKeys.AppearanceSection, SettingKeys.Color, "Blue"));

        SelectedAlternatingRowColorIndex = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.AlternatingRowColorIndex, 1);
        SelectedGridLinesVisibilityIndex = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GridLinesVisibilityIndex);
        AppFontSize = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GlobalFontSize, 12);
        GridFontSize = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GridFontSize, 12);

        ConfirmBulkChanges = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.ConfirmBulkChange, true);
        EnableSaveReminder = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.SaveReminder, true);
        VerifyImageDownloads = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.VerifyDownloadedImages, true);
        ShowGamelistStats = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.ShowGamelistStats, true);
        VideoAutoplay = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.VideoAutoplay, true);
        RememberColumns = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.RememberColumns);
        RememberAutosize = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.RememberAutoSize);
        EnableDelete = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.EnableDelete);
        IgnoreDuplicates = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.IgnoreDuplicates);
        BatchProcessing = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.BatchProcessing, true);
        ShowLogTimestamp = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.ShowLogTimestamp, false);

        MaxUndo = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.MaxUndo, 5).ToString();
        SearchDepth = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.SearchDepth, 2).ToString();
        RecentFilesCount = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.RecentFilesCount, 15).ToString();
        MaxBatch = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.BatchProcessingMaximum, 300).ToString();
        DefaultVolume = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.Volume, 75);
        LogVerbosityIndex = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.LogVerbosity, 1);

        Hostname = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.HostName, string.Empty);
        UserId = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.UserID, string.Empty);
        Password = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.Password, string.Empty);

        // Folder paths live in their own section; fall back to Connection for old profiles.
        MamePath = settings.GetValue(SettingKeys.FolderPathsSection, SettingKeys.MamePath,
                   settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.MamePath));
        RomsPath = settings.GetValue(SettingKeys.FolderPathsSection, SettingKeys.RomsFolder,
                   settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.RomsFolder));

        var isEsDe = _sharedData.IsEsDeMode;
        foreach (var item in MediaFolderItems)
        {
            item.Path = LoadMediaPath(settings.GetValue(SettingKeys.MediaPathsSection, item.Key, item.DefaultPath), item.DefaultPath);
            item.Suffix = settings.GetValue(SettingKeys.MediaPathsSection, $"{item.Key}_suffix", item.DefaultSuffix);
            item.SfxEnabled = settings.GetBool(SettingKeys.MediaPathsSection, $"{item.Key}_sfx_enabled", item.DefaultSfxEnabled);

            // Unsupported ES-DE types are always disabled regardless of what the INI says.
            item.Enabled = (!isEsDe || item.IsEsDeSupported) &&
                           settings.GetBool(SettingKeys.MediaPathsSection, $"{item.Key}_enabled", item.DefaultEnabled);
        }

        LoadScraperCredentials();
        RefreshProfileList();
        LoadEsDeSettings();

        IsDirty = false;
        _isLoading = false;
    }

    public void SaveSettings()
    {
        var settings = new Dictionary<string, Dictionary<string, string>>
        {
            [SettingKeys.AppearanceSection] = new()
            {
                [SettingKeys.Theme] = IndexToName(ThemeNames, SelectedThemeIndex),
                [SettingKeys.Color] = IndexToName(ColorNames, SelectedColorIndex),
                [SettingKeys.AlternatingRowColorIndex] = SelectedAlternatingRowColorIndex.ToString(),
                [SettingKeys.GridLinesVisibilityIndex] = SelectedGridLinesVisibilityIndex.ToString(),
                [SettingKeys.GridLineVisibility] = SelectedGridLinesVisibilityIndex switch
                {
                    0 => "Horizontal",
                    1 => "Vertical",
                    2 => "All",
                    3 => "None",
                    _ => "Horizontal"
                },
                [SettingKeys.GlobalFontSize] = AppFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [SettingKeys.GridFontSize] = GridFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            [SettingKeys.BehaviorSection] = new()
            {
                [SettingKeys.ConfirmBulkChange] = ConfirmBulkChanges.ToString(),
                [SettingKeys.SaveReminder] = EnableSaveReminder.ToString(),
                [SettingKeys.VerifyDownloadedImages] = VerifyImageDownloads.ToString(),
                [SettingKeys.ShowGamelistStats] = ShowGamelistStats.ToString(),
                [SettingKeys.VideoAutoplay] = VideoAutoplay.ToString(),
                [SettingKeys.RememberColumns] = RememberColumns.ToString(),
                [SettingKeys.RememberAutoSize] = RememberAutosize.ToString(),
                [SettingKeys.EnableDelete] = EnableDelete.ToString(),
                [SettingKeys.IgnoreDuplicates] = IgnoreDuplicates.ToString(),
                [SettingKeys.BatchProcessing] = BatchProcessing.ToString(),
                [SettingKeys.ShowLogTimestamp] = ShowLogTimestamp.ToString()
            },
            [SettingKeys.AdvancedSection] = new()
            {
                [SettingKeys.MaxUndo] = MaxUndo,
                [SettingKeys.SearchDepth] = SearchDepth,
                [SettingKeys.RecentFilesCount] = RecentFilesCount,
                [SettingKeys.BatchProcessingMaximum] = MaxBatch,
                [SettingKeys.LogVerbosity] = LogVerbosityIndex.ToString(),
                [SettingKeys.Volume] = DefaultVolume.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            [SettingKeys.ConnectionSection] = new()
            {
                [SettingKeys.HostName] = Hostname,
                [SettingKeys.UserID] = UserId,
                [SettingKeys.Password] = Password
            },
            [SettingKeys.FolderPathsSection] = new()
            {
                [SettingKeys.MamePath] = MamePath,
                [SettingKeys.RomsFolder] = RomsPath
            },
            [SettingKeys.MediaPathsSection] = _sharedData.IsEsDeMode
                ? MediaFolderItems
                    .Select(item => new KeyValuePair<string, string>($"{item.Key}_enabled", item.Enabled.ToString()))
                    .ToDictionary(kv => kv.Key, kv => kv.Value)
                : MediaFolderItems
                    .SelectMany(item => new[]
                    {
                        new KeyValuePair<string, string>(item.Key,                      item.Path),
                        new KeyValuePair<string, string>($"{item.Key}_enabled",         item.Enabled.ToString()),
                        new KeyValuePair<string, string>($"{item.Key}_suffix",          item.Suffix),
                        new KeyValuePair<string, string>($"{item.Key}_sfx_enabled",     item.SfxEnabled.ToString()),
                    })
                    .ToDictionary(kv => kv.Key, kv => kv.Value)
        };

        SettingsService.Instance.SaveAllSettings(settings);

        SaveScraperSetup();
        SaveEsDeSettings();
        ThemeService.ApplyTheme(SelectedThemeIndex, SelectedColorIndex);
        _sharedData.LoadFromSettings();

        IsDirty = false;
    }

    public void ResetAllSettings()
    {
        SettingsService.Instance.ResetToDefaults();
        LoadSettings();
    }

    public static void ApplyThemeOnStartup()
    {
        var shared = SharedDataService.Instance;

        var themeIndex = NameToIndex(ThemeNames, shared.Theme);
        var colorIndex = NameToIndex(ColorNames, shared.Color);

        ThemeService.ApplyTheme(themeIndex, colorIndex);
        ThemeService.ApplyFontSizes(shared.AppFontSize, shared.GridFontSize);
    }

    #endregion

    #region Private Methods

    private static int NameToIndex(string[] names, string name)
    {
        var index = Array.IndexOf(names, name);
        return index >= 0 ? index : 0;
    }

    private static string IndexToName(string[] names, int index)
    {
        return (uint)index < (uint)names.Length ? names[index] : names[0];
    }

    #endregion
}
