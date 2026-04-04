using CommunityToolkit.Mvvm.ComponentModel;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Gamelist_Manager.Services
{
    public partial class SharedDataService : ObservableObject
    {
        #region Private Fields

        private static SharedDataService? _instance;

        // Cached per scraper name to avoid re-reading INI files on every access.
        private readonly ConcurrentDictionary<string, Dictionary<string, Dictionary<string, string>>> _scraperSectionsCache = new(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Observable Properties

        [ObservableProperty] private string? _xmlFilename;
        [ObservableProperty] private string? _currentSystem;
        [ObservableProperty] private bool _isDataChanged = false;
        [ObservableProperty] private bool _isScraping = false;
        [ObservableProperty] private bool _isBusy = false;

        [ObservableProperty] private string _romsFolder = string.Empty;
        [ObservableProperty] private string _hostname = "batocera";
        [ObservableProperty] private string _mamePath = string.Empty;
        [ObservableProperty] private bool _enableEdit = false;
        [ObservableProperty] private bool _videoAutoplay = false;
        [ObservableProperty] private bool _confirmBulkChanges = true;
        [ObservableProperty] private bool _enableSaveReminder = true;
        [ObservableProperty] private bool _verifyImageDownloads = true;
        [ObservableProperty] private bool _showGamelistStats = true;
        [ObservableProperty] private bool _rememberColumns = false;
        [ObservableProperty] private bool _rememberAutosize = false;
        [ObservableProperty] private bool _enableDelete = false;
        [ObservableProperty] private bool _ignoreDuplicates = false;
        [ObservableProperty] private bool _batchProcessing = true;
        [ObservableProperty] private bool _mediaViewerScaledDisplay = true;
        [ObservableProperty] private int _defaultVolume = 75;
        [ObservableProperty] private int _maxUndo = 5;
        [ObservableProperty] private int _searchDepth = 2;
        [ObservableProperty] private int _maxBatch = 300;
        [ObservableProperty] private int _logVerbosity = 1;
        [ObservableProperty] private string _theme = "Default";
        [ObservableProperty] private double _appFontSize = 12;
        [ObservableProperty] private double _gridFontSize = 12;

        #endregion

        #region Public Properties

        public static SharedDataService Instance => _instance ??= new SharedDataService();

        // The raw gamelist rows — single source of truth for all controls and ViewModels.
        public ObservableCollection<GameMetadataRow>? GamelistData { get; set; }

        // Set by MainWindowViewModel after the DynamicData pipeline is bound.
        public ReadOnlyObservableCollection<GameMetadataRow>? FilteredGamelistData { get; set; }

        // Updated by MainWindowViewModel whenever selection changes.
        public IList? SelectedItems { get; set; }

        public string? GamelistDirectory =>
            !string.IsNullOrEmpty(XmlFilename)
                ? Path.GetDirectoryName((string?)XmlFilename)
                : null;

        // Keyed by media type string (e.g. "image", "video").
        public IReadOnlyDictionary<string, MetaDataDecl> MediaSettings { get; private set; }
            = new Dictionary<string, MetaDataDecl>();

        public event EventHandler? SettingsApplied;

        #endregion

        #region Constructor

        private SharedDataService()
        {
            LoadFromSettings();
        }

        #endregion

        #region Public Methods

        public void LoadFromSettings()
        {
            var settings = SettingsService.Instance;

            RomsFolder = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.RomsFolder, "");
            Hostname = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.HostName, "batocera");
            MamePath = settings.GetValue(SettingKeys.ConnectionSection, SettingKeys.MamePath, "");

            VideoAutoplay = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.VideoAutoplay, false);
            ConfirmBulkChanges = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.ConfirmBulkChange, true);
            EnableSaveReminder = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.SaveReminder, true);
            VerifyImageDownloads = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.VerifyDownloadedImages, true);
            ShowGamelistStats = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.ShowGamelistStats, true);
            RememberColumns = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.RememberColumns, false);
            RememberAutosize = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.RememberAutoSize, false);
            EnableDelete = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.EnableDelete, false);
            IgnoreDuplicates = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.IgnoreDuplicates, false);
            BatchProcessing = settings.GetBool(SettingKeys.BehaviorSection, SettingKeys.BatchProcessing, true);

            MediaViewerScaledDisplay = settings.GetBool(SettingKeys.MediaViewerSection, SettingKeys.ScaledDisplay, true);

            DefaultVolume = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.Volume, 75);
            MaxUndo = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.MaxUndo, 5);
            SearchDepth = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.SearchDepth, 2);
            MaxBatch = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.BatchProcessingMaximum, 300);
            LogVerbosity = settings.GetInt(SettingKeys.AdvancedSection, SettingKeys.LogVerbosity, 1);

            Theme = settings.GetValue(SettingKeys.AppearanceSection, SettingKeys.Theme, "Default");
            AppFontSize = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GlobalFontSize, 12);
            GridFontSize = settings.GetInt(SettingKeys.AppearanceSection, SettingKeys.GridFontSize, 12);

            // Populate runtime properties directly on MetaDataDecl objects from the MediaPaths section.
            var mediaPaths = settings.GetSection("MediaPaths")
                             ?? new Dictionary<string, string>();

            var mediaSettingsDict = new Dictionary<string, MetaDataDecl>(StringComparer.OrdinalIgnoreCase);
            foreach (var decl in GamelistMetaData.GetAllMediaFolderTypes())
            {
                decl.Enabled = mediaPaths.TryGetValue($"{decl.Type}_enabled", out var enabled)
                    ? bool.TryParse(enabled, out var eb) && eb
                    : decl.DefaultEnabled;

                decl.MediaFolderPath = mediaPaths.TryGetValue(decl.Type, out var path)
                    ? path : decl.DefaultPath;

                decl.Suffix = mediaPaths.TryGetValue($"{decl.Type}_suffix", out var suffix)
                    ? suffix : decl.DefaultSuffix;

                decl.SfxEnabled = mediaPaths.TryGetValue($"{decl.Type}_sfx_enabled", out var sfxEnabled)
                    ? bool.TryParse(sfxEnabled, out var seb) && seb
                    : !string.IsNullOrEmpty(decl.DefaultSuffix);

                mediaSettingsDict[decl.Type] = decl;
            }
            MediaSettings = mediaSettingsDict;

            SettingsApplied?.Invoke(this, EventArgs.Empty);
        }

        public void SetGamelist(string xmlPath, string systemName, ObservableCollection<GameMetadataRow> data)
        {
            XmlFilename = xmlPath;
            CurrentSystem = systemName;
            GamelistData = data;
            IsDataChanged = false;
        }

      
        public void Clear()
        {
            XmlFilename = null;
            CurrentSystem = null;
            GamelistData = null;
            IsDataChanged = false;
        }

        public void SaveMediaViewerPreferences()
        {
            var settings = SettingsService.Instance;
            settings.SetValue(SettingKeys.MediaViewerSection, SettingKeys.ScaledDisplay, MediaViewerScaledDisplay.ToString());
        }

        public IReadOnlyDictionary<string, string> GetScraperSources(string scraperName, string sectionName)
        {
            var sections = GetScraperIniSections(scraperName);
            return sections.TryGetValue(sectionName, out var section)
                ? section
                : new Dictionary<string, string>();
        }

        public string? GetScraperSystemId(string scraperName, string systemName)
        {
            var sections = GetScraperIniSections(scraperName);
            if (sections.TryGetValue("Systems", out var systems) && systems.TryGetValue(systemName, out var id))
                return id;
            return null;
        }

        public string? GetScraperLanguageCode(string scraperName)
        {
            var sections = GetScraperIniSections(scraperName);
            if (!sections.TryGetValue("Languages", out var langs)) return null;
            return langs.Keys.Select(ExtractRegionCode).FirstOrDefault(c => !string.IsNullOrEmpty(c));
        }

        public IReadOnlyList<string> GetScraperRegionCodes(string scraperName)
        {
            var sections = GetScraperIniSections(scraperName);
            if (!sections.TryGetValue("Regions", out var regs)) return [];
            return regs.Keys.Select(ExtractRegionCode).Where(c => !string.IsNullOrEmpty(c)).ToList();
        }

        public IReadOnlyList<string> GetScraperRegions(string scraperName)
        {
            var sections = GetScraperIniSections(scraperName);
            return sections.TryGetValue("Regions", out var regs)
                ? regs.Keys.ToList()
                : [];
        }

        public IReadOnlyList<string> GetScraperLanguages(string scraperName)
        {
            var sections = GetScraperIniSections(scraperName);
            return sections.TryGetValue("Languages", out var langs)
                ? langs.Keys.ToList()
                : [];
        }

        public string GetScraperDefaultSource(string scraperName, string sectionName)
        {
            var sections = GetScraperIniSections(scraperName);
            return FirstSectionValue(sections, sectionName);
        }

        #endregion

        #region Private Methods

        private Dictionary<string, Dictionary<string, string>> GetScraperIniSections(string scraperName)
        {
            return _scraperSectionsCache.GetOrAdd(scraperName, name =>
            {
                var iniPath = Path.Combine(AppContext.BaseDirectory, "Ini", $"{name.ToLowerInvariant()}_options.ini");
                return IniFileService.ReadIniFile(iniPath);
            });
        }

        private static string ExtractRegionCode(string entry)
        {
            int open = entry.LastIndexOf('(');
            int close = entry.LastIndexOf(')');
            return open >= 0 && close > open ? entry[(open + 1)..close].Trim() : string.Empty;
        }

        private static string FirstSectionValue(Dictionary<string, Dictionary<string, string>> sections, string sectionName)
            => sections.TryGetValue(sectionName, out var s) ? (s.Values.FirstOrDefault() ?? string.Empty) : string.Empty;

        #endregion
    }
}