using GamelistManager.classes.core;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
                        ArcadeSystems.Add(parts[1].Trim().ToLowerInvariant());
                }
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

            // Load language data
            languages = new List<LanguageData>();
            string languageDataIniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ini", "language_data.ini");
            if (File.Exists(languageDataIniPath))
            {
                languages.AddRange(ParseLangDataIni(languageDataIniPath));
            }

            // Build a lookup dictionary: token (lowercase) => LanguageData
            LanguageLookup = languages
                .SelectMany(ld => ld.Value.Select(v => (v: v.ToLowerInvariant(), ld)))
                .OrderByDescending(x => x.v.Length) // longest tokens first
                .ToDictionary(x => x.v, x => x.ld, StringComparer.InvariantCultureIgnoreCase);
        }

        public class LanguageData
        {
            public List<string> Value { get; set; } = new();
            public string Language { get; set; } = string.Empty;
            public string Region { get; set; } = string.Empty;
        }

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
                    if (current != null) result.Add(current);
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
                            case "value":
                                current.Value = val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                                break;
                            case "lang":
                                current.Language = val;
                                break;
                            case "region":
                                current.Region = val;
                                break;
                        }
                    }
                }
            }
            if (current != null) result.Add(current);
            return result;
        }

        public static string GetRegion(string romName)
        {
            string currentSystem = SharedData.CurrentSystem.ToLowerInvariant();
            string lowerFileName = romName.ToLowerInvariant();

            // Japan default systems
            if (JapanDefaults.Contains(currentSystem))
                return "jp";

            // Special case for Thomson
            if (currentSystem == "thomson")
                return "eu";

            // Parse tokens in parentheses
            var matches = Regex.Matches(romName, @"\((.*?)\)");
            foreach (Match match in matches)
            {
                string content = match.Groups[1].Value;

                var tokens = content
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.ToLowerInvariant())
                    .OrderByDescending(t => t.Length) // longest tokens first
                    .ToList();

                foreach (var token in tokens)
                {
                    if (LanguageLookup.TryGetValue(token, out var data) && !string.IsNullOrWhiteSpace(data.Region))
                    {
                        return data.Region;
                    }
                }
            }

            // Arcade system fallback
            if (ArcadeSystems.Contains(currentSystem))
                return lowerFileName.EndsWith("j.zip") ? "jp" : "us";

            // Default fallback
            return "us";
        }

        public static string GetLanguage(string fileName)
        {
            string currentSystem = SharedData.CurrentSystem.ToLowerInvariant();

            // Special translation handling
            if (Regex.IsMatch(fileName, @"(\(T(-Eng)?\)|\[T-?En(g)?\])", RegexOptions.IgnoreCase))
                return "en";

            if (JapanDefaults.Contains(currentSystem))
                return "jp";

            if (currentSystem == "thomson")
                return "fr";

            var matchedLanguages = new HashSet<string>();
            var matches = Regex.Matches(fileName, @"\((.*?)\)");

            foreach (Match match in matches)
            {
                string content = match.Groups[1].Value;
                var tokens = content.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                    .Select(t => t.ToLowerInvariant())
                                    .OrderByDescending(t => t.Length);

                foreach (var token in tokens)
                {
                    if (LanguageLookup.TryGetValue(token, out var data) && !string.IsNullOrWhiteSpace(data.Language))
                    {
                        matchedLanguages.Add(data.Language);
                    }
                }
            }

            // Arcade fallback
            if (ArcadeSystems.Contains(currentSystem) && matchedLanguages.Count == 0)
                return fileName.ToLowerInvariant().EndsWith("j.zip") ? "jp" : "en";

            return matchedLanguages.Count > 0 ? string.Join(",", matchedLanguages) : "en";
        }
    }
}
