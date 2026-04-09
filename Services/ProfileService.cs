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

        private ProfileService()
        {
            _iniFolder = Path.Combine(AppContext.BaseDirectory, "ini");
            _profilesFolder = Path.Combine(_iniFolder, "Profiles");
            _appIniPath = Path.Combine(_iniFolder, "app.ini");

            Directory.CreateDirectory(_iniFolder);
            Directory.CreateDirectory(_profilesFolder);

            ActiveProfile = ReadActiveProfile();
            EnsureActiveProfileExists();
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
                    IniFileService.WriteIniFile(newPath, SettingsService.BuildDefaultSections());
            }
            else
            {
                IniFileService.WriteIniFile(newPath, SettingsService.BuildDefaultSections());
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

            var sections = SettingsService.BuildDefaultSections();

            foreach (var kvp in connectionOverrides)
                sections["Connection"][kvp.Key] = kvp.Value;

            IniFileService.WriteIniFile(newPath, sections);
            return true;
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

        #region Private Methods

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

        private void EnsureActiveProfileExists()
        {
            var path = ActiveProfilePath;
            if (!File.Exists(path))
            {
                IniFileService.WriteIniFile(path, SettingsService.BuildDefaultSections());
                WriteActiveProfile(ActiveProfile);
            }
        }

        #endregion
    }
}