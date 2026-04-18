namespace Gamelist_Manager.Classes.Helpers;

public static class SettingKeys
{
    // Sections
    public const string AppearanceSection = "Appearance";
    public const string BehaviorSection = "Behavior";
    public const string AdvancedSection = "Advanced";
    public const string ConnectionSection = "Connection";
    public const string FolderPathsSection = "FolderPaths";
    public const string MediaPathsSection = "MediaPaths";
    public const string MediaViewerSection = "MediaViewer";
    public const string RecentFilesSection = "RecentFiles";
    public const string ScraperSection = "Scraper";

    // Appearance keys
    public const string Theme = "Theme";
    public const string Color = "Color";
    public const string AlternatingRowColorIndex = "AlternatingRowColorIndex";
    public const string GridLinesVisibilityIndex = "GridLinesVisibilityIndex";
    public const string GridLineVisibility = "GridLineVisibility";
    public const string GlobalFontSize = "GlobalFontSize";
    public const string GridFontSize = "GridFontSize";

    // Behavior keys
    public const string ConfirmBulkChange = "ConfirmBulkChange";
    public const string SaveReminder = "SaveReminder";
    public const string VerifyDownloadedImages = "VerifyDownloadedImages";
    public const string VideoAutoplay = "VideoAutoplay";
    public const string ShowGamelistStats = "ShowGamelistStats";
    public const string RememberColumns = "RememberColumns";
    public const string RememberAutoSize = "RememberAutoSize";
    public const string EnableDelete = "EnableDelete";
    public const string IgnoreDuplicates = "IgnoreDuplicates";
    public const string BatchProcessing = "BatchProcessing";
    public const string ShowLogTimestamp = "ShowLogTimestamp";

    // Advanced keys
    public const string MaxUndo = "MaxUndo";
    public const string SearchDepth = "SearchDepth";
    public const string RecentFilesCount = "RecentFilesCount";
    public const string BatchProcessingMaximum = "BatchProcessingMaximum";
    public const string LogVerbosity = "LogVerbosity";
    public const string Volume = "Volume";

    // Connection keys
    public const string HostName = "HostName";
    public const string UserID = "UserID";
    public const string Password = "Password";

    // Folder paths keys
    public const string MamePath = "MamePath";
    public const string RomsFolder = "RomsFolder";

    // MediaViewer keys
    public const string ScaledDisplay = "ScaledDisplay";

    // ES-DE keys
    public const string EsDeSection = "EsDe";
    public const string EsDeRoot = "EsDeRoot";
    public const string ProfileType = "ProfileType";

    // ProfileType values
    public const string ProfileTypeStandard = "standard";
    public const string ProfileTypeEsDe = "esde";

    // Default values (shared between BuildDefaultSections and LoadFromSettings)
    public const string DefaultTheme = "Light";
    public const string DefaultColor = "Blue";
    public const int DefaultAlternatingRowColorIndex = 1;
    public const int DefaultGridLinesVisibilityIndex = 0;
    public const string DefaultGridLineVisibility = "Horizontal";
    public const int DefaultGlobalFontSize = 12;
    public const int DefaultGridFontSize = 12;

    public const bool DefaultConfirmBulkChange = true;
    public const bool DefaultSaveReminder = true;
    public const bool DefaultVerifyDownloadedImages = true;
    public const bool DefaultShowGamelistStats = true;
    public const bool DefaultVideoAutoplay = true;
    public const bool DefaultRememberColumns = false;
    public const bool DefaultRememberAutoSize = false;
    public const bool DefaultEnableDelete = false;
    public const bool DefaultIgnoreDuplicates = false;
    public const bool DefaultBatchProcessing = true;

    public const int DefaultMaxUndo = 5;
    public const int DefaultSearchDepth = 2;
    public const int DefaultRecentFilesCount = 15;
    public const int DefaultBatchProcessingMaximum = 300;
    public const int DefaultLogVerbosity = 1;
    public const int DefaultVolume = 75;

    public const string DefaultHostName = "";
    public const string DefaultUserID = "";
    public const string DefaultPassword = "";
}

