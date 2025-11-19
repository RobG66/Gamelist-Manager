using GamelistManager.classes.core;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace GamelistManager.classes.helpers
{
    public static class RegionLanguageHelper
    {
        private static readonly HashSet<string> ArcadeSystems;
        private static readonly HashSet<string> JapanDefaults;
        private static readonly List<LanguageData> languages;
        private static readonly Dictionary<string, LanguageData> LanguageLookup;

        static RegionLanguageHelper()
        {
            // Load ArcadeSystems
            ArcadeSystems = [];
            string arcadeIniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ini", "arcadesystems.ini");
            if (File.Exists(arcadeIniPath))
            {
                foreach (var line in File.ReadAllLines(arcadeIniPath))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("[") || string.IsNullOrWhiteSpace(trimmed)) continue;
                    var parts = trimmed.Split('=');
                    if (parts.Length == 2)
                    {
                        ArcadeSystems.Add(parts[1].Trim().ToLowerInvariant());
                    }
                }
            }
            else
            {
                MessageBox.Show("arcadesystems.ini could not be loaded. Arcade system detection will be disabled.", "Configuration Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Load JapanDefaults
            JapanDefaults = [];
            string japanIniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ini", "japan_systems.ini");
            if (File.Exists(japanIniPath))
            {
                foreach (var line in File.ReadAllLines(japanIniPath))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("[") || string.IsNullOrWhiteSpace(trimmed)) continue;
                    JapanDefaults.Add(trimmed.ToLowerInvariant());
                }
            }
            else
            {
                MessageBox.Show("japan_systems.ini could not be loaded. Japanese system detection will be disabled.", "Configuration Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Load LanguageData from INI file
            languages = [];
            string languageDataIniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ini", "language_data.ini");
            if (File.Exists(languageDataIniPath))
            {
                languages.AddRange(ParseLangDataIni(languageDataIniPath));
            }
            else
            {
                MessageBox.Show("language_data.ini could not be loaded. Language/Region detection will be disabled.", "Configuration Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Build LanguageLookup as before
            LanguageLookup = languages
                .SelectMany(ld => ld.Value.Select(v => (v, ld)))
                .ToDictionary(x => x.v, x => x.ld);
        }

        public class LanguageData
        {
            public List<string> Value { get; set; } = [];
            public string Language { get; set; } = string.Empty;
            public string Region { get; set; } = string.Empty;
        }


        // INI parser for language_data.ini
        private static IEnumerable<LanguageData> ParseLangDataIni(string path)
        {
            var result = new List<LanguageData>();
            LanguageData current = null;

            foreach (var line in File.ReadLines(path))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    if (current != null)
                        result.Add(current);
                    current = new LanguageData();
                }
                else if (current != null)
                {
                    var idx = trimmed.IndexOf('=');
                    if (idx > 0)
                    {
                        var key = trimmed.Substring(0, idx).Trim().ToLowerInvariant();
                        var val = trimmed.Substring(idx + 1).Trim();
                        switch (key)
                        {
                            case "Value":
                                current.Value = new List<string>(val.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries));
                                break;
                            case "Language":
                                current.Language = val;
                                break;
                            case "Region":
                                current.Region = val;
                                break;
                        }
                    }
                }
            }
            if (current != null)
                result.Add(current);
            return result;
        }

        public static string GetRegion(string romName)
        {
            string currentSystem = SharedData.CurrentSystem.ToLowerInvariant();
            string lowerFileName = romName.ToLowerInvariant();

            if (JapanDefaults.Contains(currentSystem))
            {
                return "jp";
            }

            if (currentSystem == "thomson")
            {
                return "eu";
            }

            // Parse the file name
            var matches = Regex.Matches(romName, @"\((.*?)\)");
            foreach (Match match in matches)
            {
                string content = match.Groups[1].Value;
                var parts = content.Split(',', System.StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    string trimmedPart = part.ToLowerInvariant().Trim();
                    if (LanguageLookup.TryGetValue(trimmedPart, out var langData) && !string.IsNullOrWhiteSpace(langData.Region))
                    {
                        return langData.Region; // Return first valid Region found
                    }
                }
            }

            // Arcade system fallback
            if (ArcadeSystems.Contains(currentSystem))
            {
                return lowerFileName.EndsWith("j.zip") ? "jp" : "us";
            }

            return "us";
        }

        public static string GetLanguage(string fileName)
        {
            string currentSystem = SharedData.CurrentSystem.ToLowerInvariant();
            string lowerFileName = fileName.ToLowerInvariant();

            // Special case: if the file name contains (T) or (T-Eng), or [T-En] or [T-Eng], it's a translation (English)
            if (Regex.IsMatch(fileName, @"(\(T(-Eng)?\)|\[T-?En(g)?\])", RegexOptions.IgnoreCase))
            {
                return "en";
            }

            if (JapanDefaults.Contains(currentSystem))
            {
                return "jp";
            }

            if (currentSystem == "thomson")
            {
                return "fr";
            }

            // Fallback: parse file name
            var matchedLanguages = new HashSet<string>();
            var matches = Regex.Matches(fileName, @"\((.*?)\)");

            foreach (Match match in matches)
            {
                string content = match.Groups[1].Value;
                var parts = content.Split(',', System.StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    string trimmedPart = part.ToLowerInvariant().Trim();
                    if (LanguageLookup.TryGetValue(trimmedPart, out var langData) && !string.IsNullOrWhiteSpace(langData.Language))
                    {
                        matchedLanguages.Add(langData.Language);
                    }
                }
            }

            if (ArcadeSystems.Contains(currentSystem) && matchedLanguages.Count == 0)
            {
                return lowerFileName.EndsWith("j.zip") ? "jp" : "en";
            }

            return matchedLanguages.Count > 0 ? string.Join(",", matchedLanguages) : "en";
        }
    }
}