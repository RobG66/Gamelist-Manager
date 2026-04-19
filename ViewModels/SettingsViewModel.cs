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

        SelectedThemeIndex = NameToIndex(ThemeNames, settings.GetValue(SettingKeys.Theme));
        SelectedColorIndex = NameToIndex(ColorNames, settings.GetValue(SettingKeys.Color));

        SelectedAlternatingRowColorIndex = settings.GetInt(SettingKeys.AlternatingRowColorIndex);
        SelectedGridLinesVisibilityIndex = settings.GetInt(SettingKeys.GridLinesVisibilityIndex);
        AppFontSize = settings.GetInt(SettingKeys.GlobalFontSize);
        GridFontSize = settings.GetInt(SettingKeys.GridFontSize);

        ConfirmBulkChanges = settings.GetBool(SettingKeys.ConfirmBulkChange);
        EnableSaveReminder = settings.GetBool(SettingKeys.SaveReminder);
        VerifyImageDownloads = settings.GetBool(SettingKeys.VerifyDownloadedImages);
        VideoAutoplay = settings.GetBool(SettingKeys.VideoAutoplay);
        RememberColumns = settings.GetBool(SettingKeys.RememberColumns);
        RememberAutosize = settings.GetBool(SettingKeys.RememberAutoSize);
        EnableDelete = settings.GetBool(SettingKeys.EnableDelete);
        IgnoreDuplicates = settings.GetBool(SettingKeys.IgnoreDuplicates);
        BatchProcessing = settings.GetBool(SettingKeys.BatchProcessing);
        ShowLogTimestamp = settings.GetBool(SettingKeys.ShowLogTimestamp);
        ScraperConfigSaveIndex = settings.GetInt(SettingKeys.ScraperConfigSave);

        MaxUndo = settings.GetInt(SettingKeys.MaxUndo).ToString();
        SearchDepth = settings.GetInt(SettingKeys.SearchDepth).ToString();
        RecentFilesCount = settings.GetInt(SettingKeys.RecentFilesCount).ToString();
        MaxBatch = settings.GetInt(SettingKeys.BatchProcessingMaximum).ToString();
        DefaultVolume = settings.GetInt(SettingKeys.Volume);
        LogVerbosityIndex = settings.GetInt(SettingKeys.LogVerbosity);

        Hostname = settings.GetValue(SettingKeys.HostName);
        UserId = settings.GetValue(SettingKeys.UserID);
        Password = settings.GetValue(SettingKeys.Password);

        // Folder paths live in their own section; fall back to Connection for old profiles.
        MamePath = settings.GetValue(SettingKeys.FolderPathsSection, SettingKeys.MamePath.Key,
                   settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.MamePath.Key));
        RomsPath = settings.GetValue(SettingKeys.FolderPathsSection, SettingKeys.RomsFolder.Key,
                   settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.RomsFolder.Key));

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
                [SettingKeys.Theme.Key] = IndexToName(ThemeNames, SelectedThemeIndex),
                [SettingKeys.Color.Key] = IndexToName(ColorNames, SelectedColorIndex),
                [SettingKeys.AlternatingRowColorIndex.Key] = SelectedAlternatingRowColorIndex.ToString(),
                [SettingKeys.GridLinesVisibilityIndex.Key] = SelectedGridLinesVisibilityIndex.ToString(),
                [SettingKeys.GridLineVisibility.Key] = SelectedGridLinesVisibilityIndex switch
                {
                    0 => "Horizontal",
                    1 => "Vertical",
                    2 => "All",
                    3 => "None",
                    _ => "Horizontal"
                },
                [SettingKeys.GlobalFontSize.Key] = AppFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [SettingKeys.GridFontSize.Key] = GridFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            [SettingKeys.BehaviorSection] = new()
            {
                [SettingKeys.ConfirmBulkChange.Key] = ConfirmBulkChanges.ToString(),
                [SettingKeys.SaveReminder.Key] = EnableSaveReminder.ToString(),
                [SettingKeys.VerifyDownloadedImages.Key] = VerifyImageDownloads.ToString(),
                [SettingKeys.VideoAutoplay.Key] = VideoAutoplay.ToString(),
                [SettingKeys.RememberColumns.Key] = RememberColumns.ToString(),
                [SettingKeys.RememberAutoSize.Key] = RememberAutosize.ToString(),
                [SettingKeys.EnableDelete.Key] = EnableDelete.ToString(),
                [SettingKeys.IgnoreDuplicates.Key] = IgnoreDuplicates.ToString(),
                [SettingKeys.BatchProcessing.Key] = BatchProcessing.ToString(),
                [SettingKeys.ShowLogTimestamp.Key] = ShowLogTimestamp.ToString(),
                [SettingKeys.ScraperConfigSave.Key] = ScraperConfigSaveIndex.ToString()
            },
            [SettingKeys.AdvancedSection] = new()
            {
                [SettingKeys.MaxUndo.Key] = MaxUndo,
                [SettingKeys.SearchDepth.Key] = SearchDepth,
                [SettingKeys.RecentFilesCount.Key] = RecentFilesCount,
                [SettingKeys.BatchProcessingMaximum.Key] = MaxBatch,
                [SettingKeys.LogVerbosity.Key] = LogVerbosityIndex.ToString(),
                [SettingKeys.Volume.Key] = DefaultVolume.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            [SettingKeys.ConnectionSection] = new()
            {
                [SettingKeys.HostName.Key] = Hostname,
                [SettingKeys.UserID.Key] = UserId,
                [SettingKeys.Password.Key] = Password
            },
            [SettingKeys.FolderPathsSection] = new()
            {
                [SettingKeys.MamePath.Key] = MamePath,
                [SettingKeys.RomsFolder.Key] = RomsPath
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
