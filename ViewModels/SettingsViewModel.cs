using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;

namespace Gamelist_Manager.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    #region Fields

    private readonly SharedDataService _sharedData = SharedDataService.Instance;
    private bool _isLoading;

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

        SelectedThemeIndex = settings.GetValue(SettingKeys.AppearanceSection, SettingKeys.Theme, "Light") switch
        {
            "Light" => 0,
            "Dark"  => 1,
            _       => 0
        };

        SelectedColorIndex = settings.GetValue(SettingKeys.AppearanceSection, SettingKeys.Color, "Blue") switch
        {
            "Blue"       => 0,
            "Red"        => 1,
            "Orange"     => 2,
            "Green"      => 3,
            "Yellow"     => 4,
            "Magenta"    => 5,
            "Purple"     => 6,
            "Teal"       => 7,
            "Lime"       => 8,
            "Light Blue" => 9,
            "Indigo"     => 10,
            _            => 0
        };

        SelectedAlternatingRowColorIndex  = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.AlternatingRowColorIndex, 1);
        SelectedGridLinesVisibilityIndex  = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GridLinesVisibilityIndex);
        AppFontSize  = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GlobalFontSize, 12);
        GridFontSize = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GridFontSize,   12);

        ConfirmBulkChanges    = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.ConfirmBulkChange,       true);
        EnableSaveReminder    = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.SaveReminder,            true);
        VerifyImageDownloads  = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.VerifyDownloadedImages,  true);
        ShowGamelistStats     = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.ShowGamelistStats,       true);
        VideoAutoplay         = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.VideoAutoplay,           true);
        RememberColumns       = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.RememberColumns);
        RememberAutosize      = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.RememberAutoSize);
        EnableDelete          = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.EnableDelete);
        IgnoreDuplicates      = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.IgnoreDuplicates);
        BatchProcessing       = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.BatchProcessing,         true);
        ShowLogTimestamp      = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.ShowLogTimestamp,        false);

        MaxUndo         = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.MaxUndo,                5).ToString();
        SearchDepth     = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.SearchDepth,            2).ToString();
        RecentFilesCount= settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.RecentFilesCount,       15).ToString();
        MaxBatch        = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.BatchProcessingMaximum, 300).ToString();
        DefaultVolume   = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.Volume,                 75);
        LogVerbosityIndex = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.LogVerbosity,         1);

        Hostname = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.HostName, "batocera");
        UserId   = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.UserID,           "root");
        Password = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.Password,         "linux");
        MamePath = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.MamePath);
        RomsPath = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.RomsFolder);

        foreach (var item in MediaFolderItems)
        {
            item.Path       = LoadMediaPath(settings.GetValue("MediaPaths", item.Key, item.DefaultPath), item.DefaultPath);
            item.Enabled    = settings.GetBool("MediaPaths", $"{item.Key}_enabled",     item.DefaultEnabled);
            item.Suffix     = settings.GetValue("MediaPaths", $"{item.Key}_suffix",     item.DefaultSuffix);
            item.SfxEnabled = settings.GetBool("MediaPaths", $"{item.Key}_sfx_enabled", item.DefaultSfxEnabled);
        }

        LoadScraperCredentials();
        RefreshProfileList();

        IsDirty    = false;
        _isLoading = false;
    }

    public void SaveSettings()
    {
        var settings = new Dictionary<string, Dictionary<string, string>>
        {
            [SettingKeys.AppearanceSection] = new()
            {
                [SettingKeys.Theme] = SelectedThemeIndex switch { 0 => "Light", 1 => "Dark", _ => "Light" },
                [SettingKeys.Color] = SelectedColorIndex switch
                {
                    0 => "Blue", 1 => "Red",   2 => "Orange",     3 => "Green",
                    4 => "Yellow", 5 => "Magenta", 6 => "Purple", 7 => "Teal",
                    8 => "Lime", 9 => "Light Blue", 10 => "Indigo", _ => "Blue"
                },
                [SettingKeys.AlternatingRowColorIndex] = SelectedAlternatingRowColorIndex.ToString(),
                [SettingKeys.GridLinesVisibilityIndex] = SelectedGridLinesVisibilityIndex.ToString(),
                [SettingKeys.GridLineVisibility] = SelectedGridLinesVisibilityIndex switch
                {
                    0 => "Horizontal", 1 => "Vertical", 2 => "All", 3 => "None", _ => "Horizontal"
                },
                [SettingKeys.GlobalFontSize] = AppFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [SettingKeys.GridFontSize]   = GridFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            [SettingKeys.BehaviorSection] = new()
            {
                [SettingKeys.ConfirmBulkChange]      = ConfirmBulkChanges.ToString(),
                [SettingKeys.SaveReminder]            = EnableSaveReminder.ToString(),
                [SettingKeys.VerifyDownloadedImages]  = VerifyImageDownloads.ToString(),
                [SettingKeys.ShowGamelistStats]       = ShowGamelistStats.ToString(),
                [SettingKeys.VideoAutoplay]           = VideoAutoplay.ToString(),
                [SettingKeys.RememberColumns]         = RememberColumns.ToString(),
                [SettingKeys.RememberAutoSize]        = RememberAutosize.ToString(),
                [SettingKeys.EnableDelete]            = EnableDelete.ToString(),
                [SettingKeys.IgnoreDuplicates]        = IgnoreDuplicates.ToString(),
                [SettingKeys.BatchProcessing]         = BatchProcessing.ToString(),
                [SettingKeys.ShowLogTimestamp]        = ShowLogTimestamp.ToString()
            },
            [SettingKeys.AdvancedSection] = new()
            {
                [SettingKeys.MaxUndo]                 = MaxUndo,
                [SettingKeys.SearchDepth]             = SearchDepth,
                [SettingKeys.RecentFilesCount]        = RecentFilesCount,
                [SettingKeys.BatchProcessingMaximum]  = MaxBatch,
                [SettingKeys.LogVerbosity]            = LogVerbosityIndex.ToString(),
                [SettingKeys.Volume]                  = DefaultVolume.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            [SettingKeys.ConnectionSection] = new()
            {
                [SettingKeys.HostName] = Hostname,
                [SettingKeys.UserID]           = UserId,
                [SettingKeys.Password]         = Password,
                [SettingKeys.MamePath]         = MamePath,
                [SettingKeys.RomsFolder]       = RomsPath
            },
            [SettingKeys.MediaPathsSection] = MediaFolderItems
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
        ThemeService.ApplyTheme(SelectedThemeIndex, SelectedColorIndex);

        IsDirty = false;
    }

    public void ResetAllSettings()
    {
        SettingsService.Instance.ResetToDefaults();
        LoadSettings();
    }

    public static void LoadAndApplySettingsOnStartup()
    {
        var settings = new SettingsViewModel();
        ThemeService.ApplyTheme(settings.SelectedThemeIndex, settings.SelectedColorIndex);
        ThemeService.ApplyFontSizes(settings.AppFontSize, settings.GridFontSize);
    }

    #endregion
}
