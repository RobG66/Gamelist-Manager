using System.Text.RegularExpressions;
using System.IO;
using System.Windows;

namespace GamelistManager.classes
{
    public static class RegionLanguageHelper
    {
        private static readonly HashSet<string> ArcadeSystems;
        private static readonly HashSet<string> JapanDefaults;
        private static readonly List<LangData> langDatas;
        private static readonly Dictionary<string, LangData> LangLookup;

        static RegionLanguageHelper()
        {
            // Load ArcadeSystems
            ArcadeSystems = new HashSet<string>();
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
            JapanDefaults = new HashSet<string>();
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

            // Load LangData from INI file
            langDatas = new List<LangData>();
            string langDataIniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ini", "langdata.ini");
            if (File.Exists(langDataIniPath))
            {
                langDatas.AddRange(ParseLangDataIni(langDataIniPath));
            }
            else
            {
                MessageBox.Show("langdata.ini could not be loaded. Language/region detection will be disabled.", "Configuration Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Build LangLookup as before
            LangLookup = langDatas
                .SelectMany(ld => ld.value.Select(v => (v, ld)))
                .ToDictionary(x => x.v, x => x.ld);
        }

        public class LangData
        {
            public List<string> value { get; set; } = new();
            public string lang { get; set; } = string.Empty;
            public string region { get; set; } = string.Empty;
        }

        public class RegionLanguageInfo
        {
            public string Region { get; set; } = string.Empty;
            public string Languages { get; set; } = string.Empty;
        }

        // INI parser for langdata.ini
        private static IEnumerable<LangData> ParseLangDataIni(string path)
        {
            var result = new List<LangData>();
            LangData current = null;

            foreach (var line in File.ReadLines(path))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    if (current != null)
                        result.Add(current);
                    current = new LangData();
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
                            case "value":
                                current.value = new List<string>(val.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries));
                                break;
                            case "lang":
                                current.lang = val;
                                break;
                            case "region":
                                current.region = val;
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
                    if (LangLookup.TryGetValue(trimmedPart, out var langData) && !string.IsNullOrWhiteSpace(langData.region))
                    {
                        return langData.region; // Return first valid region found
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
                    if (LangLookup.TryGetValue(trimmedPart, out var langData) && !string.IsNullOrWhiteSpace(langData.lang))
                    {
                        matchedLanguages.Add(langData.lang);
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