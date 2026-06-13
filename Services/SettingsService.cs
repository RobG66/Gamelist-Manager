using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gamelist_Manager.Services
{
    public class SettingsService
    {
        #region Fields & Constants

        private static SettingsService? _instance;

        private string _settingsFilePath;
        private readonly string _iniFolder;
        private Dictionary<string, Dictionary<string, string>>? _cache;
        private Dictionary<string, string>? _fileTypesCache;

        #endregion

        #region Public Properties

        public static SettingsService Instance => _instance ??= new SettingsService();

        #endregion

        #region Constructor

        private SettingsService()
        {
            var programDirectory = AppContext.BaseDirectory;
            _iniFolder = Path.Combine(programDirectory, "ini");
            _settingsFilePath = ProfileService.Instance.ActiveProfilePath;

            if (!ProfileService.Instance.NoProfilesExist && !File.Exists(_settingsFilePath))
                CreateDefaultSettings();
        }

        #endregion

        #region Public Methods


        public void SwitchProfile(string profilePath)
        {
            _settingsFilePath = profilePath;
            InvalidateCache();
            if (!File.Exists(_settingsFilePath))
                CreateDefaultSettings();
        }

        // --- Core read ---

        public string GetValue(string section, string key, string defaultValue = "")
        {
            try
            {
                var sections = Cache();
                return sections.TryGetValue(section, out var sectionDict) && sectionDict.TryGetValue(key, out var value)
                    ? value : defaultValue;
            }
            catch { return defaultValue; }
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

        // SettingDef overloads — preferred call style
        public string GetValue(SettingDef<string> def) => GetValue(def.Section, def.Key, def.Default);
        public bool GetBool(SettingDef<bool> def) => GetBool(def.Section, def.Key, def.Default);
        public int GetInt(SettingDef<int> def) => GetInt(def.Section, def.Key, def.Default);

        public Dictionary<string, string>? GetSection(string sectionName)
        {
            var sections = Cache();
            return sections.TryGetValue(sectionName, out var section) ? section : null;
        }

        // --- File types (separate data file, not profile-specific) ---

        public IReadOnlyDictionary<string, string> GetFileTypes()
        {
            if (_fileTypesCache != null)
                return _fileTypesCache;

            var iniPath = Path.Combine(_iniFolder, "filetypes.ini");
            var sections = IniFileService.ReadIniFile(iniPath);
            _fileTypesCache = sections.TryGetValue("Filetypes", out var ft) ? ft : [];
            return _fileTypesCache;
        }

        // --- Core write ---

        // --- Recent files ---

        public List<string> GetRecentFiles()
        {
            var sections = Cache();
            if (!sections.TryGetValue(SettingKeys.RecentFilesSection, out var section))
                return [];

            return section.Keys
                .Where(k => k.StartsWith("file"))
                .OrderBy(k => int.TryParse(k[4..], out var n) ? n : int.MaxValue)
                .Select(k => section[k])
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .ToList();
        }

        // --- Reset ---

        public void ResetToDefaults()
        {
            InvalidateCache();
            if (File.Exists(_settingsFilePath))
                File.Delete(_settingsFilePath);
            CreateDefaultSettings();
        }

        // --- Default INI structure ---

        public Dictionary<string, Dictionary<string, string>> BuildDefaultSections()
        {
            var result = new Dictionary<string, Dictionary<string, string>>();

            foreach (var def in SettingKeys.AllDefinitions)
            {
                if (!result.TryGetValue(def.Section, out var dict))
                {
                    dict = [];
                    result[def.Section] = dict;
                }
                dict[def.Key] = def.DefaultStr;
            }

            result[SettingKeys.MediaPathsSection] = new Dictionary<string, string>(SettingKeys.DefaultMediaPaths);
            return result;
        }

        public void ClearSystemMediaOverrides(string system)
        {
            IniFileService.DeleteKeysWithPrefix(
                _settingsFilePath,
                SettingKeys.MediaPathOverridesSection,
                $"{system}_");
            InvalidateCache();
        }

        #endregion

        #region Private Methods

        private Dictionary<string, Dictionary<string, string>> Cache() =>
            _cache ??= IniFileService.ReadIniFile(_settingsFilePath);

        internal void InvalidateCache() => _cache = null;

        private void CreateDefaultSettings()
        {
            IniFileService.WriteIniFile(_settingsFilePath, BuildDefaultSections());
            InvalidateCache();
        }

        #endregion
    }
}
