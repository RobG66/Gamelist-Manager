using Gamelist_Manager.Models;
using System.Collections.Generic;
using System.Linq;

namespace Gamelist_Manager.Classes.Helpers;

public record SettingDef<T>(string Section, string Key, T Default);

public record ProfileTypeOption(string Key, string DisplayName)
{
    public override string ToString() => DisplayName;
}

public static class SettingKeys
{
    #region Sections

    public const string AppearanceSection = "Appearance";
    public const string BehaviorSection = "Behavior";
    public const string AdvancedSection = "Advanced";
    public const string ConnectionSection = "Connection";
    public const string FolderPathsSection = "FolderPaths";
    public const string MediaPathsSection = "MediaPaths";
    public const string MediaViewerSection = "MediaViewer";
    public const string RecentFilesSection = "RecentFiles";
    public const string ScraperSection = "Scraper";
    public const string EsDeSection = "EsDe";
    public const string ProfileSection = "Profile";

    #endregion

    #region ProfileType Values

    public const string ProfileTypeEs = "es";
    public const string ProfileTypeEsDe = "esde";

    public static readonly IReadOnlyList<ProfileTypeOption> AllProfileTypes =
    [
    new ProfileTypeOption(ProfileTypeEs,   "ES"),
    new ProfileTypeOption(ProfileTypeEsDe, "ES-DE"),
    ];
    #endregion

    #region Appearance Settings

    public static readonly SettingDef<string> Theme = new(AppearanceSection, "Theme", "Light");
    public static readonly SettingDef<string> Color = new(AppearanceSection, "Color", "Blue");
    public static readonly SettingDef<int> AlternatingRowColorIndex = new(AppearanceSection, "AlternatingRowColorIndex", 1);
    public static readonly SettingDef<int> GridLinesVisibilityIndex = new(AppearanceSection, "GridLinesVisibilityIndex", 0);
    public static readonly SettingDef<string> GridLineVisibility = new(AppearanceSection, "GridLineVisibility", "Horizontal");
    public static readonly SettingDef<int> GlobalFontSize = new(AppearanceSection, "GlobalFontSize", 12);
    public static readonly SettingDef<int> GridFontSize = new(AppearanceSection, "GridFontSize", 12);

    #endregion

    #region Behavior Settings

    public static readonly SettingDef<bool> ConfirmBulkChange = new(BehaviorSection, "ConfirmBulkChange", true);
    public static readonly SettingDef<bool> SaveReminder = new(BehaviorSection, "SaveReminder", true);
    public static readonly SettingDef<bool> VerifyDownloadedImages = new(BehaviorSection, "VerifyDownloadedImages", true);
    public static readonly SettingDef<bool> VideoAutoplay = new(BehaviorSection, "VideoAutoplay", true);
    public static readonly SettingDef<bool> RememberColumns = new(BehaviorSection, "RememberColumns", false);
    public static readonly SettingDef<bool> RememberAutoSize = new(BehaviorSection, "RememberAutoSize", false);
    public static readonly SettingDef<bool> EnableDelete = new(BehaviorSection, "EnableDelete", false);
    public static readonly SettingDef<bool> IgnoreDuplicates = new(BehaviorSection, "IgnoreDuplicates", false);
    public static readonly SettingDef<bool> BatchProcessing = new(BehaviorSection, "BatchProcessing", true);
    public static readonly SettingDef<bool> ShowLogTimestamp = new(BehaviorSection, "ShowLogTimestamp", false);
    public static readonly SettingDef<int> ScraperConfigSave = new(BehaviorSection, "ScraperConfigSave", 0);
    public static readonly SettingDef<bool> CheckForNewAndMissingGamesOnLoad = new(BehaviorSection, "CheckForNewAndMissingGamesOnLoad", false);

    #endregion

    #region Advanced Settings

    public static readonly SettingDef<int> MaxUndo = new(AdvancedSection, "MaxUndo", 5);
    public static readonly SettingDef<int> SearchDepth = new(AdvancedSection, "SearchDepth", 2);
    public static readonly SettingDef<int> RecentFilesCount = new(AdvancedSection, "RecentFilesCount", 15);
    public static readonly SettingDef<int> BatchProcessingMaximum = new(AdvancedSection, "BatchProcessingMaximum", 300);
    public static readonly SettingDef<int> LogVerbosity = new(AdvancedSection, "LogVerbosity", 1);
    public static readonly SettingDef<int> Volume = new(AdvancedSection, "Volume", 75);

    #endregion

    #region Connection Settings

    public static readonly SettingDef<string> HostName = new(ConnectionSection, "HostName", "");
    public static readonly SettingDef<string> UserID = new(ConnectionSection, "UserID", "");
    public static readonly SettingDef<string> Password = new(ConnectionSection, "Password", "");

    #endregion

    #region Folder Paths Settings

    public static readonly SettingDef<string> MamePath = new(FolderPathsSection, "MamePath", "");
    public static readonly SettingDef<string> RomsFolder = new(FolderPathsSection, "RomsFolder", "");

    #endregion

    #region MediaViewer Settings

    public static readonly SettingDef<bool> ScaledDisplay = new(MediaViewerSection, "ScaledDisplay", true);

    #endregion

    #region ES-DE Settings

    public static readonly SettingDef<string> EsDeRoot = new(EsDeSection, "EsDeRoot", "");
    public static readonly SettingDef<string> ProfileType = new(ProfileSection, "ProfileType", ProfileTypeEs);

    #endregion

    #region Scraper Settings

    public static readonly SettingDef<string> ScreenScraperLanguage = new(ScraperSection, $"{ScraperRegistry.ScreenScraper}_Language", "English (en)");
    public static readonly SettingDef<string> ScreenScraperPrimaryRegion = new(ScraperSection, $"{ScraperRegistry.ScreenScraper}_PrimaryRegion", "USA (us)");
    public static readonly SettingDef<bool> ScreenScraperGenreEnglish = new(ScraperSection, $"{ScraperRegistry.ScreenScraper}_GenreEnglish", false);
    public static readonly SettingDef<bool> ScreenScraperAnyMedia = new(ScraperSection, $"{ScraperRegistry.ScreenScraper}_AnyMedia", false);
    public static readonly SettingDef<bool> ScreenScraperNamesLanguageFirst = new(ScraperSection, $"{ScraperRegistry.ScreenScraper}_NamesLanguageFirst", false);
    public static readonly SettingDef<bool> ScreenScraperMediaRegionFirst = new(ScraperSection, $"{ScraperRegistry.ScreenScraper}_MediaRegionFirst", false);
    public static readonly SettingDef<string> ScreenScraperRegionFallback = new(ScraperSection, $"{ScraperRegistry.ScreenScraper}_RegionFallback", """["USA (us)", "Europe (eu)", "United Kingdom (uk)", "World (wor)", "Japan (jp)", "ScreenScraper (ss)", "Custom (cus)"]""");
    public static readonly SettingDef<bool> ScrapeAllMode = new(ScraperSection, "ScrapeAllMode", true);
    public static readonly SettingDef<bool> OverwriteName = new(ScraperSection, "OverwriteName", true);
    public static readonly SettingDef<bool> OverwriteMedia = new(ScraperSection, "OverwriteMedia", false);
    public static readonly SettingDef<bool> ScrapeHiddenItems = new(ScraperSection, "ScrapeHiddenItems", false);
    public static readonly SettingDef<string> SelectedScraper = new(ScraperSection, "SelectedScraper", "");


    #endregion


    #region All Definitions

    // Master list used by BuildDefaultSections to auto-generate INI defaults.
    // MediaPaths and ES-DE are handled separately due to special logic.
    public static readonly IReadOnlyList<object> AllDefinitions =
    [
        // Appearance
        Theme, Color, AlternatingRowColorIndex, GridLinesVisibilityIndex,
        GridLineVisibility, GlobalFontSize, GridFontSize,

        // Behavior
        ConfirmBulkChange, SaveReminder, VerifyDownloadedImages, VideoAutoplay,
        RememberColumns, RememberAutoSize, EnableDelete, IgnoreDuplicates,
        BatchProcessing, ShowLogTimestamp, ScraperConfigSave, CheckForNewAndMissingGamesOnLoad,

        // Advanced
        MaxUndo, SearchDepth, RecentFilesCount, BatchProcessingMaximum,
        LogVerbosity, Volume,

        // Connection
        HostName, UserID, Password,

        // Folder Paths
        MamePath, RomsFolder,

        // MediaViewer
        ScaledDisplay,

        // ES-DE
        EsDeRoot, ProfileType,

         // Scraper
        ScreenScraperLanguage, ScreenScraperPrimaryRegion, ScreenScraperGenreEnglish,
        ScreenScraperAnyMedia, ScreenScraperNamesLanguageFirst, ScreenScraperMediaRegionFirst, ScreenScraperRegionFallback,
        ScrapeAllMode, OverwriteName, OverwriteMedia, ScrapeHiddenItems, SelectedScraper

    ];

    #endregion

    #region Media Path Defaults

    // MediaPaths have their own structure — not driven by SettingDef
    // Derived from the model definitions
    public static readonly IReadOnlyDictionary<string, string> DefaultMediaPaths =
     GamelistMetaData.GetAllMediaFolderTypes()
         .SelectMany(d => new[]
         {
            KeyValuePair.Create(d.Type, d.DefaultPath),
            KeyValuePair.Create($"{d.Type}_enabled", d.DefaultEnabled.ToString().ToLower())
         })
         .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    #endregion

}





