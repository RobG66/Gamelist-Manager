using Gamelist_Manager.Classes.Helpers;
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
            _instance = this; // Break the circular dependency early

            _iniFolder = Path.Combine(AppContext.BaseDirectory, "ini");
            _profilesFolder = Path.Combine(_iniFolder, "Profiles");
            _appIniPath = Path.Combine(_iniFolder, "app.ini");

            Directory.CreateDirectory(_iniFolder);
            Directory.CreateDirectory(_profilesFolder);

            NoProfilesExist = !Directory.EnumerateFiles(_profilesFolder, "*.ini").Any();
            ActiveProfile = ReadActiveProfile();
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

        // Creates a new profile with default settings and sets its ProfileType in the [EsDe]
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
            sections[SettingKeys.EsDeSection][SettingKeys.ProfileType] = profileType;

            if (profileType == SettingKeys.ProfileTypeEsDe)
                ApplyEsDeDefaults(sections);

            IniFileService.WriteIniFile(GetProfilePath(finalName), sections);
            return finalName;
        }

        // Reads the ProfileType from the given profile's INI file,
        // defaulting to Standard if not set.
        public string GetProfileType(string profileName)
        {
            var path = GetProfilePath(profileName);
            var section = IniFileService.GetSection(path, SettingKeys.EsDeSection);
            if (section != null && section.TryGetValue(SettingKeys.ProfileType, out var value))
                return value;
            return SettingKeys.ProfileTypeStandard;
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
            sections[SettingKeys.EsDeSection][SettingKeys.ProfileType] = profileType;

            if (profileType == SettingKeys.ProfileTypeEsDe)
                ApplyEsDeDefaults(sections);

            IniFileService.WriteIniFile(path, sections);

            // The file was written directly — force SettingsService to drop any
            // stale cache so subsequent reads (e.g. SaveEsDeRoot during the
            // ES-DE root prompt) pick up the correct ProfileType.
            SettingsService.Instance.SwitchProfile(path);
        }

        #region Private Methods

        // ES-DE profiles don't use batocera connection defaults or media paths.
        private static void ApplyEsDeDefaults(IniData sections)
        {
            sections[SettingKeys.ConnectionSection][SettingKeys.HostName] = "";
            sections[SettingKeys.ConnectionSection][SettingKeys.UserID] = "";
            sections[SettingKeys.ConnectionSection][SettingKeys.Password] = "";
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

        public void ClearNoProfilesFlag()
        {
            NoProfilesExist = false;
        }

        #endregion
    }
}