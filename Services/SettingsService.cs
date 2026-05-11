using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using IniData = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace Gamelist_Manager.Services
{
    public class SettingsService
    {
        private static SettingsService? _instance;
        public static SettingsService Instance => _instance ??= new SettingsService();

        private string _settingsFilePath;
        private readonly string _iniFolder;
        private Dictionary<string, Dictionary<string, string>>? _cachedSections;
        private Dictionary<string, string>? _fileTypesCache;

        private SettingsService()
        {
            var programDirectory = AppContext.BaseDirectory;
            _iniFolder = Path.Combine(programDirectory, "ini");

            _settingsFilePath = ProfileService.Instance.ActiveProfilePath;

            if (!ProfileService.Instance.NoProfilesExist && !File.Exists(_settingsFilePath))
            {
                CreateDefaultSettings();
            }
        }

        public void SwitchProfile(string profilePath)
        {
            InvalidateCache();
            _settingsFilePath = profilePath;
            if (!File.Exists(_settingsFilePath))
                CreateDefaultSettings();
        }

        private void CreateDefaultSettings()
        {
            IniFileService.WriteIniFile(_settingsFilePath, BuildDefaultSections());
            InvalidateCache();
        }

        private Dictionary<string, Dictionary<string, string>> GetCachedSections()
            => _cachedSections ??= IniFileService.ReadIniFile(_settingsFilePath);

        private void InvalidateCache()
            => _cachedSections = null;

        public string GetValue(string section, string key, string defaultValue = "")
        {
            try
            {
                var sections = GetCachedSections();
                if (sections.TryGetValue(section, out var sectionData) && sectionData.TryGetValue(key, out var value))
                    return value;
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public bool GetBool(string section, string key, bool defaultValue = false)
        {
            var value = GetValue(section, key, defaultValue.ToString());
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        public int GetInt(string section, string key, int defaultValue = 0)
        {
            var value = GetValue(section, key, defaultValue.ToString());
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        // Convenience overloads that pull section, key, and default from a SettingDef.
        public string GetValue(SettingDef<string> def) => GetValue(def.Section, def.Key, def.Default);
        public bool GetBool(SettingDef<bool> def) => GetBool(def.Section, def.Key, def.Default);
        public int GetInt(SettingDef<int> def) => GetInt(def.Section, def.Key, def.Default);

        public Dictionary<string, string>? GetSection(string sectionName)
        {
            var sections = GetCachedSections();
            return sections.TryGetValue(sectionName, out var section) ? section : null;
        }

        // Reads the standalone filetypes.ini data file (not profile-specific).
        public IReadOnlyDictionary<string, string> GetFileTypes()
        {
            if (_fileTypesCache != null)
                return _fileTypesCache;

            var iniPath = Path.Combine(_iniFolder, "filetypes.ini");
            var sections = IniFileService.ReadIniFile(iniPath);
            _fileTypesCache = sections.TryGetValue("Filetypes", out var filetypes)
                ? filetypes
                : [];

            return _fileTypesCache;
        }

        public void SetValue(string section, string key, string value)
        {
            var sections = GetCachedSections();

            if (!sections.ContainsKey(section))
                sections[section] = new Dictionary<string, string>();

            sections[section][key] = value;
            IniFileService.WriteIniFile(_settingsFilePath, sections);
        }

        public void SetBool(string section, string key, bool value)
        {
            SetValue(section, key, value.ToString());
        }

        public List<string> GetRecentFiles()
        {
            var recentFiles = new List<string>();
            var sections = GetCachedSections();

            if (!sections.TryGetValue(SettingKeys.RecentFilesSection, out var section))
                return recentFiles;

            var fileKeys = section.Keys
                .Where(k => k.StartsWith("file"))
                .OrderBy(k =>
                {
                    var numStr = k.Substring(4);
                    return int.TryParse(numStr, out var num) ? num : int.MaxValue;
                })
                .ToList();

            foreach (var key in fileKeys)
            {
                var filePath = section[key];
                if (!string.IsNullOrWhiteSpace(filePath))
                    recentFiles.Add(filePath);
            }

            return recentFiles;
        }

        public void SaveRecentFiles(List<string> recentFiles)
        {
            var recentFilesDict = new Dictionary<string, string>();

            var resolvedFiles = recentFiles
                .Where(f => !string.IsNullOrEmpty(f))
                .ToList();

            for (int i = 0; i < resolvedFiles.Count; i++)
                recentFilesDict[$"file{i + 1}"] = resolvedFiles[i]!;

            var allSettings = GetCachedSections();
            allSettings[SettingKeys.RecentFilesSection] = recentFilesDict;
            IniFileService.WriteIniFile(_settingsFilePath, allSettings);
        }

        public void SaveAllSettings(Dictionary<string, Dictionary<string, string>> allSettings)
        {
            var existingSettings = GetCachedSections();

            foreach (var section in allSettings)
            {
                if (!existingSettings.ContainsKey(section.Key))
                    existingSettings[section.Key] = new Dictionary<string, string>();

                foreach (var kvp in section.Value)
                    existingSettings[section.Key][kvp.Key] = kvp.Value;
            }

            IniFileService.WriteIniFile(_settingsFilePath, existingSettings);
            ProfileService.MigrateProfile(_settingsFilePath);
            InvalidateCache();
        }

        public void ResetToDefaults()
        {
            InvalidateCache();
            if (File.Exists(_settingsFilePath))
                File.Delete(_settingsFilePath);
            CreateDefaultSettings();
        }

        public IniData BuildDefaultSections()
        {
            var result = new IniData();

            foreach (var def in SettingKeys.AllDefinitions)
            {
                var (section, key, defaultStr) = def switch
                {
                    SettingDef<string> s => (s.Section, s.Key, s.Default),
                    SettingDef<bool> b => (b.Section, b.Key, b.Default.ToString()),
                    SettingDef<int> i => (i.Section, i.Key, i.Default.ToString()),
                    _ => throw new InvalidOperationException($"Unsupported SettingDef type: {def.GetType()}")
                };

                if (!result.TryGetValue(section, out var dict))
                {
                    dict = new Dictionary<string, string>();
                    result[section] = dict;
                }
                dict[key] = defaultStr;
            }

            result[SettingKeys.MediaPathsSection] = new Dictionary<string, string>(SettingKeys.DefaultMediaPaths);

            return result;
        }

        #region Calculated Properties

        public string EsDeMediaDirectory(string esDeMediaBase, string? currentSystem)
            => !string.IsNullOrEmpty(esDeMediaBase) && !string.IsNullOrEmpty(currentSystem)
                ? Path.Combine(esDeMediaBase, currentSystem)
                : string.Empty;

        public string GamelistsRootFolder(string profileType, string esDeRoot, string romsFolder)
            => profileType == SettingKeys.ProfileTypeEsDe && !string.IsNullOrEmpty(esDeRoot)
                ? Path.Combine(esDeRoot, "gamelists")
                : romsFolder;

        public string? CurrentGamelistFolder(string gamelistsRootFolder, string? currentSystem)
            => !string.IsNullOrEmpty(gamelistsRootFolder) && !string.IsNullOrEmpty(currentSystem)
                ? Path.Combine(gamelistsRootFolder, currentSystem)
                : null;

        public string? CurrentRomFolder(string romsFolder, string? currentSystem)
            => !string.IsNullOrEmpty(romsFolder) && !string.IsNullOrEmpty(currentSystem)
                ? Path.Combine(romsFolder, currentSystem)
                : null;

        // Resolves all enabled media folders for the current profile into a flat list.
        // Callers receive ready-to-use FolderPath values — no profile knowledge required.
        public IReadOnlyList<AvailableMediaFolder> BuildAvailableMedia(
            string profileType,
            string? mediaBaseFolder,
            Dictionary<string, string> mediaPaths)
        {
            bool isEsDe = profileType == SettingKeys.ProfileTypeEsDe;
            var result = new List<AvailableMediaFolder>();

            if (string.IsNullOrEmpty(mediaBaseFolder))
                return result;

            foreach (var decl in GamelistMetaData.GetAllMediaFolderTypes())
            {
                bool isEnabled = mediaPaths.TryGetValue($"{decl.Type}_enabled", out var enabled)
                    ? bool.TryParse(enabled, out var eb) && eb
                    : decl.DefaultEnabled;

                // ES-DE types without a dedicated folder name are not supported
                if (isEsDe && string.IsNullOrEmpty(decl.EsDeFolderName))
                    continue;

                if (!isEnabled)
                    continue;

                string folderPath;
                if (isEsDe)
                {
                    folderPath = Path.Combine(mediaBaseFolder, decl.EsDeFolderName);
                }
                else
                {
                    var relativePath = mediaPaths.TryGetValue(decl.Type, out var path) ? path : decl.DefaultPath;
                    // Strip leading "./" or "." before combining
                    var cleanRelative = relativePath.TrimStart('.').TrimStart('/').TrimStart('\\');
                    folderPath = Path.Combine(mediaBaseFolder, cleanRelative);
                }

                var suffix = isEsDe ? string.Empty : (mediaPaths.TryGetValue($"{decl.Type}_suffix", out var sfx) ? sfx : decl.DefaultSuffix);
                var sfxEnabled = !isEsDe && (mediaPaths.TryGetValue($"{decl.Type}_sfx_enabled", out var sfxEnabledStr)
                    ? bool.TryParse(sfxEnabledStr, out var seb) && seb
                    : !string.IsNullOrEmpty(decl.DefaultSuffix));

                result.Add(new AvailableMediaFolder(decl.Type, decl.Name, folderPath, suffix, sfxEnabled));
            }

            return result;
        }

        #endregion

        public static EsDeDetectedPaths ReadPathsFromEsDeSettings(string esDeRoot)
        {
            if (string.IsNullOrWhiteSpace(esDeRoot))
                return new EsDeDetectedPaths(null, null);

            string? romDirectory = null;
            string? mediaDirectory = null;

            var settingsPath = Path.Combine(esDeRoot, "settings", "es_settings.xml");
            if (File.Exists(settingsPath))
            {
                try
                {
                    var doc = XDocument.Load(settingsPath);

                    var romDir = doc.Descendants("string")
                        .FirstOrDefault(e => string.Equals(e.Attribute("name")?.Value, "ROMDirectory", StringComparison.Ordinal))
                        ?.Attribute("value")?.Value;

                    if (!string.IsNullOrWhiteSpace(romDir))
                    {
                        var expanded = ExpandTilde(romDir);
                        var trimmed = Path.TrimEndingDirectorySeparator(expanded);
                        if (Directory.Exists(trimmed))
                            romDirectory = trimmed;
                    }

                    var mediaDir = doc.Descendants("string")
                        .FirstOrDefault(e => string.Equals(e.Attribute("name")?.Value, "MediaDirectory", StringComparison.Ordinal))
                        ?.Attribute("value")?.Value;

                    if (!string.IsNullOrWhiteSpace(mediaDir))
                    {
                        var expanded = ExpandTilde(mediaDir);
                        var trimmed = Path.TrimEndingDirectorySeparator(expanded);
                        if (Directory.Exists(trimmed))
                            mediaDirectory = trimmed;
                    }
                }
                catch { }
            }

            if (romDirectory == null)
            {
                var fallback = Path.Combine(esDeRoot, "..", "ROMs");
                fallback = Path.GetFullPath(fallback);
                if (Directory.Exists(fallback))
                    romDirectory = fallback;
            }

            if (mediaDirectory == null)
            {
                var fallback = Path.Combine(esDeRoot, "downloaded_media");
                if (Directory.Exists(fallback))
                    mediaDirectory = fallback;
            }

            return new EsDeDetectedPaths(romDirectory, mediaDirectory);
        }

        // Expands a leading ~ to the current user's home directory, matching shell behaviour on Linux/macOS.
        private static string ExpandTilde(string path)
        {
            if (path.StartsWith("~/", StringComparison.Ordinal) || path == "~")
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return home + path[1..];
            }
            return path;
        }

        public record EsDeDetectedPaths(string? RomDirectory, string? MediaDirectory);
    }
}