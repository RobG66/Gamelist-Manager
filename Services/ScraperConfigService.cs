using Gamelist_Manager.Classes.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Gamelist_Manager.Services
{
    public class ScraperConfigService
    {
        #region Private Fields

        private static ScraperConfigService? _instance;

        // Cached per scraper name to avoid re-reading INI files on every access.
        private readonly ConcurrentDictionary<string, Dictionary<string, Dictionary<string, string>>> _scraperSectionsCache = new(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Public Properties

        public static ScraperConfigService Instance => _instance ??= new ScraperConfigService();

        #endregion

        #region Constructor

        private ScraperConfigService() { }

        #endregion

        #region Public Methods

        public IReadOnlyDictionary<string, string> GetScraperSources(string scraperName, string sectionName)
        {
            var sections = GetScraperIniSections(scraperName);
            return sections.TryGetValue(sectionName, out var section)
                ? section
                : [];
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
            string savedLanguage = SettingsService.Instance.GetValue(SettingKeys.ScraperSection, $"{scraperName}_Language", string.Empty);
            if (!string.IsNullOrEmpty(savedLanguage))
            {
                string code = ExtractRegionCode(savedLanguage);
                if (!string.IsNullOrEmpty(code))
                    return code;
            }

            var sections = GetScraperIniSections(scraperName);
            if (!sections.TryGetValue("Languages", out var langs)) return null;
            return langs.Keys.Select(ExtractRegionCode).FirstOrDefault(c => !string.IsNullOrEmpty(c));
        }

        public string? GetScraperPrimaryRegionCode(string scraperName)
        {
            string saved = SettingsService.Instance.GetValue(SettingKeys.ScraperSection, $"{scraperName}_PrimaryRegion", string.Empty);
            if (!string.IsNullOrEmpty(saved))
            {
                string code = ExtractRegionCode(saved);
                if (!string.IsNullOrEmpty(code))
                    return code;
            }
            return null;
        }

        public IReadOnlyList<string> GetScraperRegionCodes(string scraperName)
        {
            var sections = GetScraperIniSections(scraperName);
            if (!sections.TryGetValue("Regions", out var regs)) return [];
            return regs.Keys.Select(ExtractRegionCode).Where(c => !string.IsNullOrEmpty(c)).ToList();
        }

        public IReadOnlyList<string> GetScraperFallbackRegionCodes(string scraperName)
        {
            var json = SettingsService.Instance.GetValue(SettingKeys.ScraperSection, $"{scraperName}_RegionFallback", string.Empty);
            if (string.IsNullOrEmpty(json)) return [];
            try
            {
                var displayNames = JsonSerializer.Deserialize<List<string>>(json) ?? [];
                return displayNames.Select(ExtractRegionCode).Where(c => !string.IsNullOrEmpty(c)).ToList();
            }
            catch
            {
                return [];
            }
        }

        public IReadOnlyList<string> GetScraperElements(string scraperName)
        {
            var sections = GetScraperIniSections(scraperName);
            return sections.TryGetValue("SupportedFields", out var fields)
                ? fields.Keys.ToList()
                : [];
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

        public string GetScraperSourceSetting(string scraperName, string sectionName)
            => SettingsService.Instance.GetValue(SettingKeys.ScraperSection, $"{scraperName}_{sectionName}", "");

        public bool GetScraperBoolSetting(string scraperName, string settingName, bool defaultValue = false)
            => SettingsService.Instance.GetBool(SettingKeys.ScraperSection, $"{scraperName}_{settingName}", defaultValue);

        #endregion

        #region Private Methods

        private Dictionary<string, Dictionary<string, string>> GetScraperIniSections(string scraperName)
        {
            return _scraperSectionsCache.GetOrAdd(scraperName, name =>
            {
                var iniPath = Path.Combine(AppContext.BaseDirectory, "ini", $"{name.ToLowerInvariant()}_options.ini");
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
