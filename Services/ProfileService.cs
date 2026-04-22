using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IniData = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace Gamelist_Manager.Services
{
    public class ProfileService
    {
        private static ProfileService? _instance;
        public static ProfileService Instance => _instance ??= new ProfileService();

        public const string DefaultProfileName = "Default";
        private const string AppIniSection = "App";
        private const string ActiveProfileKey = "ActiveProfile";

        private readonly string _iniFolder;
        private readonly string _profilesFolder;
        private readonly string _appIniPath;

        public string ActiveProfile { get; private set; }

        // True when no profile INI files exist at startup.
        // The UI should prompt the user to pick a profile type before continuing.
        public bool NoProfilesExist { get; private set; }

        private ProfileService()
        {
            //_instance = this; an old band aid, I can't remember?  for informational reference

            _iniFolder = Path.Combine(AppContext.BaseDirectory, "ini");
            _profilesFolder = Path.Combine(_iniFolder, "Profiles");
            _appIniPath = Path.Combine(_iniFolder, "app.ini");

            Directory.CreateDirectory(_iniFolder);
            Directory.CreateDirectory(_profilesFolder);

            NoProfilesExist = !Directory.EnumerateFiles(_profilesFolder, "*.ini").Any();
            ActiveProfile = ReadActiveProfile();

            if (!NoProfilesExist)
                MigrateProfile(ActiveProfilePath);
        }

        public string ActiveProfilePath => GetProfilePath(ActiveProfile);

        public string GetProfilePath(string profileName) =>
            Path.Combine(_profilesFolder, profileName + ".ini");

        public List<string> GetProfiles()
        {
            return Directory.EnumerateFiles(_profilesFolder, "*.ini")
                .Select(f => Path.GetFileNameWithoutExtension(f)!)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public void SetActiveProfile(string profileName)
        {
            MigrateProfile(GetProfilePath(profileName));
            ActiveProfile = profileName;
            WriteActiveProfile(profileName);
        }

        public bool CreateProfile(string name, bool copyFromActive)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            var newPath = GetProfilePath(name);
            if (File.Exists(newPath)) return false;

            if (copyFromActive)
            {
                var sourcePath = ActiveProfilePath;
                if (File.Exists(sourcePath))
                    File.Copy(sourcePath, newPath);
                else
                    IniFileService.WriteIniFile(newPath, SettingsService.Instance.BuildDefaultSections());
            }
            else
            {
                IniFileService.WriteIniFile(newPath, SettingsService.Instance.BuildDefaultSections());
            }

            return true;
        }

        public bool RenameProfile(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return false;

            var oldPath = GetProfilePath(oldName);
            var newPath = GetProfilePath(newName);

            if (!File.Exists(oldPath) || File.Exists(newPath)) return false;

            File.Move(oldPath, newPath);

            if (string.Equals(ActiveProfile, oldName, StringComparison.OrdinalIgnoreCase))
            {
                ActiveProfile = newName;
                WriteActiveProfile(newName);
            }

            return true;
        }

        public bool CreateProfileFromTemplate(string name, Dictionary<string, string> connectionOverrides, bool overwrite = false)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            var newPath = GetProfilePath(name);
            if (File.Exists(newPath))
            {
                if (!overwrite) return false;
                File.Delete(newPath);
            }

            var sections = SettingsService.Instance.BuildDefaultSections();

            foreach (var kvp in connectionOverrides)
                sections[SettingKeys.ConnectionSection][kvp.Key] = kvp.Value;

            IniFileService.WriteIniFile(newPath, sections);
            return true;
        }

        // Creates a new profile with default settings and sets its ProfileType in the [Profile]
        // section. Appends a numeric suffix to the name if it is already taken.
        // Returns the final profile name, or null if creation failed.
        public string? CreateTypedProfile(string name, string profileType)
        {
            var candidate = name.Trim();
            if (string.IsNullOrWhiteSpace(candidate)) return null;

            var finalName = candidate;
            var counter = 2;
            while (File.Exists(GetProfilePath(finalName)))
                finalName = $"{candidate} {counter++}";

            var sections = SettingsService.Instance.BuildDefaultSections();
            sections[SettingKeys.ProfileSection][SettingKeys.ProfileType.Key] = profileType;

            if (profileType == SettingKeys.ProfileTypeEsDe)
                ApplyEsDeDefaults(sections);

            IniFileService.WriteIniFile(GetProfilePath(finalName), sections);
            return finalName;
        }

        // Reads the ProfileType from the given profile's INI file.
        // Reads the ProfileType from the given profile's [Profile] section.
        public string GetProfileType(string profileName)
        {
            var path = GetProfilePath(profileName);
            var profileSection = IniFileService.GetSection(path, SettingKeys.ProfileSection);
            if (profileSection != null && profileSection.TryGetValue(SettingKeys.ProfileType.Key, out var value))
                return value;
            return SettingKeys.ProfileTypeEs;
        }

        // Returns true when the profile is ES-DE type AND has a valid, existing root folder configured.
        public bool IsEsDeRootConfigured(string profileName)
        {
            if (GetProfileType(profileName) != SettingKeys.ProfileTypeEsDe)
                return true;

            var section = IniFileService.GetSection(GetProfilePath(profileName), SettingKeys.EsDeSection);
            return section != null
                && section.TryGetValue(SettingKeys.EsDeRoot.Key, out var root)
                && !string.IsNullOrWhiteSpace(root)
                && Directory.Exists(root);
        }

        public bool DeleteProfile(string name)
        {
            if (string.Equals(name, ActiveProfile, StringComparison.OrdinalIgnoreCase))
                return false;

            if (GetProfiles().Count <= 1)
                return false;

            var path = GetProfilePath(name);
            if (!File.Exists(path)) return false;

            File.Delete(path);
            return true;
        }

        // Creates (or recreates) the Default profile with the given type.
        public void CreateDefaultProfile(string profileType)
        {
            var path = GetProfilePath(DefaultProfileName);
            if (File.Exists(path))
                File.Delete(path);

            var sections = SettingsService.Instance.BuildDefaultSections();
            sections[SettingKeys.ProfileSection][SettingKeys.ProfileType.Key] = profileType;

            if (profileType == SettingKeys.ProfileTypeEsDe)
                ApplyEsDeDefaults(sections);

            IniFileService.WriteIniFile(path, sections);

            // The file was written directly — force SettingsService to drop any
            // stale cache so subsequent reads (e.g. SaveEsDeRoot during the
            // ES-DE root prompt) pick up the correct ProfileType.
            SettingsService.Instance.SwitchProfile(path);
        }

        public void ClearNoProfilesFlag()
        {
            NoProfilesExist = false;
        }

        #region Private Methods

        // ES-DE profiles don't use batocera connection defaults or media paths.
        private static void ApplyEsDeDefaults(IniData sections)
        {
            sections[SettingKeys.ConnectionSection][SettingKeys.HostName.Key] = "";
            sections[SettingKeys.ConnectionSection][SettingKeys.UserID.Key] = "";
            sections[SettingKeys.ConnectionSection][SettingKeys.Password.Key] = "";
            sections[SettingKeys.MediaPathsSection].Clear();
        }

        private string ReadActiveProfile()
        {
            var sections = IniFileService.ReadIniFile(_appIniPath);
            if (sections.TryGetValue(AppIniSection, out var section) &&
                section.TryGetValue(ActiveProfileKey, out var name) &&
                !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
            return DefaultProfileName;
        }

        private void WriteActiveProfile(string profileName)
        {
            IniFileService.WriteIniFile(_appIniPath, new IniData
            {
                [AppIniSection] = new Dictionary<string, string>
                {
                    [ActiveProfileKey] = profileName
                }
            });
        }

        // Normalises a single profile INI file against the current set of known settings.
        // For sections driven by AllDefinitions: adds missing keys with defaults, removes obsolete keys.
        // For the Scraper section: adds missing known keys only — never removes (source keys are dynamic).
        // MediaPaths and RecentFiles are left entirely untouched.
        // Writes the file back only if at least one change was made.
        private static void MigrateProfile(string profilePath)
        {
            if (!File.Exists(profilePath)) return;

            var sections = IniFileService.ReadIniFile(profilePath);
            var changed = false;

            // Build the expected key/default map grouped by section from AllDefinitions.
            var expectedBySection = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var def in SettingKeys.AllDefinitions)
            {
                var (section, key, defaultStr) = def switch
                {
                    SettingDef<string> s => (s.Section, s.Key, s.Default),
                    SettingDef<bool> b => (b.Section, b.Key, b.Default.ToString()),
                    SettingDef<int> i => (i.Section, i.Key, i.Default.ToString()),
                    _ => throw new InvalidOperationException($"Unsupported SettingDef type: {def.GetType()}")
                };

                if (!expectedBySection.TryGetValue(section, out var sectionMap))
                {
                    sectionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    expectedBySection[section] = sectionMap;
                }
                sectionMap[key] = defaultStr;
            }

            // Add missing keys and prune obsolete keys for each managed section.
            foreach (var (section, expectedKeys) in expectedBySection)
            {
                if (!sections.TryGetValue(section, out var existing))
                {
                    existing = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    sections[section] = existing;
                }

                // Add any key that is missing.
                foreach (var (key, defaultValue) in expectedKeys)
                {
                    if (!existing.ContainsKey(key))
                    {
                        existing[key] = defaultValue;
                        changed = true;
                    }
                }

                // Remove any key that is no longer in the definition set.
                var obsolete = existing.Keys
                    .Where(k => !expectedKeys.ContainsKey(k))
                    .ToList();
                foreach (var key in obsolete)
                {
                    existing.Remove(key);
                    changed = true;
                }
            }

            // Scraper section: add missing known keys only, never remove.
            var knownScraperKeys = BuildKnownScraperKeys();
            if (!sections.TryGetValue(SettingKeys.ScraperSection, out var scraperSection))
            {
                scraperSection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                sections[SettingKeys.ScraperSection] = scraperSection;
            }
            foreach (var (key, defaultValue) in knownScraperKeys)
            {
                if (!scraperSection.ContainsKey(key))
                {
                    scraperSection[key] = defaultValue;
                    changed = true;
                }
            }

            if (changed)
                IniFileService.WriteIniFile(profilePath, sections);
        }

        // Builds the expected key/default pairs for the Scraper section, derived from
        // ScraperRegistry so the list stays in sync if scrapers are added or removed.
        private static Dictionary<string, string> BuildKnownScraperKeys()
        {
            var keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var scraper in ScraperRegistry.All)
            {
                keys[$"{scraper.Name}_Language"] = "";
                keys[$"{scraper.Name}_PrimaryRegion"] = "";
                keys[$"{scraper.Name}_GenreEnglish"] = "False";
                keys[$"{scraper.Name}_AnyMedia"] = "False";
                keys[$"{scraper.Name}_NamesLanguageFirst"] = "False";
                keys[$"{scraper.Name}_MediaRegionFirst"] = "False";
                keys[$"{scraper.Name}_RegionFallback"] = "";
            }
            return keys;
        }

        #endregion
    }
}