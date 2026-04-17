using Gamelist_Manager.Classes.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IniData = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace Gamelist_Manager.Services
{
    public partial class SettingsService
    {
        private static SettingsService? _instance;
        public static SettingsService Instance => _instance ??= new SettingsService();

        private string _settingsFilePath;
        private readonly string _iniFolder;
        private Dictionary<string, Dictionary<string, string>>? _cachedSections;

        private SettingsService()
        {
            var programDirectory = AppContext.BaseDirectory;
            _iniFolder = Path.Combine(programDirectory, "ini");

            // ProfileService handles migration and ensures the profiles folder exists
            _settingsFilePath = ProfileService.Instance.ActiveProfilePath;

            // Don't auto-create the profile file when no profiles exist.
            // The UI will prompt the user to pick a profile type first.
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

        public Dictionary<string, string>? GetSection(string sectionName)
        {
            var sections = GetCachedSections();
            return sections.TryGetValue(sectionName, out var section) ? section : null;
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


        public void SetSection(string section, Dictionary<string, string> values)
        {
            SaveAllSettings(new Dictionary<string, Dictionary<string, string>>
            {
                [section] = values
            });
        }

        public List<string> GetRecentFiles()
        {
            var recentFiles = new List<string>();
            var sections = GetCachedSections();

            if (!sections.TryGetValue(SettingKeys.RecentFilesSection, out var section))
                return recentFiles;

            // Get all keys that start with "file" and sort them numerically
            var fileKeys = section.Keys
                .Where(k => k.StartsWith("file"))
                .OrderBy(k =>
                {
                    var numStr = k.Substring(4); // Remove "file" prefix
                    return int.TryParse(numStr, out var num) ? num : int.MaxValue;
                })
                .ToList();

            foreach (var key in fileKeys)
            {
                var filePath = section[key];
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    recentFiles.Add(filePath);
                }
            }

            return recentFiles;
        }

        public void SaveRecentFiles(List<string> recentFiles)
        {
            var recentFilesDict = new Dictionary<string, string>();

            // Save files with numbered keys (file1, file2, file3, etc.)
            for (int i = 0; i < recentFiles.Count; i++)
            {
                recentFilesDict[$"file{i + 1}"] = recentFiles[i];
            }

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
            return new IniData
            {
                [SettingKeys.AppearanceSection] = new Dictionary<string, string>
                {
                    [SettingKeys.Theme] = "Light",
                    [SettingKeys.Color] = "Blue",
                    [SettingKeys.AlternatingRowColorIndex] = "1",
                    [SettingKeys.GridLinesVisibilityIndex] = "0",
                    [SettingKeys.GridLineVisibility] = "Horizontal",
                    [SettingKeys.GlobalFontSize] = "12",
                    [SettingKeys.GridFontSize] = "12"
                },
                [SettingKeys.BehaviorSection] = new Dictionary<string, string>
                {
                    [SettingKeys.ConfirmBulkChange] = "True",
                    [SettingKeys.SaveReminder] = "True",
                    [SettingKeys.VerifyDownloadedImages] = "True",
                    [SettingKeys.ShowGamelistStats] = "True",
                    [SettingKeys.VideoAutoplay] = "True",
                    [SettingKeys.RememberColumns] = "False",
                    [SettingKeys.RememberAutoSize] = "False",
                    [SettingKeys.EnableDelete] = "False",
                    [SettingKeys.IgnoreDuplicates] = "False",
                    [SettingKeys.BatchProcessing] = "True"
                },
                [SettingKeys.AdvancedSection] = new Dictionary<string, string>
                {
                    [SettingKeys.MaxUndo] = "5",
                    [SettingKeys.SearchDepth] = "2",
                    [SettingKeys.RecentFilesCount] = "15",
                    [SettingKeys.BatchProcessingMaximum] = "300",
                    [SettingKeys.LogVerbosity] = "1",
                    [SettingKeys.Volume] = "75"
                },
                [SettingKeys.ConnectionSection] = new Dictionary<string, string>
                {
                    [SettingKeys.HostName] = "batocera",
                    [SettingKeys.UserID] = "root",
                    [SettingKeys.Password] = "linux"
                },
                [SettingKeys.FolderPathsSection] = new Dictionary<string, string>
                {
                    [SettingKeys.MamePath] = "",
                    [SettingKeys.RomsFolder] = ""
                },
                [SettingKeys.MediaPathsSection] = new Dictionary<string, string>
                {
                    ["image"] = "./images",
                    ["image_enabled"] = "true",
                    ["titleshot"] = "./images",
                    ["titleshot_enabled"] = "true",
                    ["marquee"] = "./images",
                    ["marquee_enabled"] = "true",
                    ["wheel"] = "./images",
                    ["wheel_enabled"] = "false",
                    ["thumbnail"] = "./images",
                    ["thumbnail_enabled"] = "true",
                    ["cartridge"] = "./images",
                    ["cartridge_enabled"] = "true",
                    ["video"] = "./videos",
                    ["video_enabled"] = "true",
                    ["music"] = "./music",
                    ["music_enabled"] = "true",
                    ["map"] = "./images",
                    ["map_enabled"] = "false",
                    ["bezel"] = "./images",
                    ["bezel_enabled"] = "true",
                    ["manual"] = "./manuals",
                    ["manual_enabled"] = "true",
                    ["fanart"] = "./images",
                    ["fanart_enabled"] = "true",
                    ["boxart"] = "./images",
                    ["boxart_enabled"] = "true",
                    ["boxback"] = "./images",
                    ["boxback_enabled"] = "true",
                    ["magazine"] = "./images",
                    ["magazine_enabled"] = "false",
                    ["mix"] = "./images",
                    ["mix_enabled"] = "true"
                },
                [SettingKeys.EsDeSection] = BuildEsDeDefaults()
            };
        }
    }
}