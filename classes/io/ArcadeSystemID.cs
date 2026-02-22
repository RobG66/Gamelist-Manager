using System.Collections.Immutable;
using System.IO;
using System.Diagnostics;

namespace GamelistManager.classes.io
{
    public static class ArcadeSystemID
    {
        private static readonly string DefaultIniPath = "ini\\arcadesystems.ini";
        private static readonly object _lock = new object();
        private static ImmutableDictionary<ushort, string> _arcadeSystems = ImmutableDictionary<ushort, string>.Empty;
        private static HashSet<string>? _systemNamesCache;

        public static IReadOnlyDictionary<ushort, string> ArcadeSystems => _arcadeSystems;
        public static bool IsInitialized => _arcadeSystems.Count > 0;

        static ArcadeSystemID()
        {
            try
            {
                LoadArcadeSystems(DefaultIniPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load arcade systems: {ex.Message}");
            }
        }

        public static void LoadArcadeSystems(string filePath)
        {
            var systems = new Dictionary<ushort, string>();

            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.WriteLine($"Arcade systems file not found: {filePath}");
                    return;
                }

                int lineNumber = 0;
                foreach (var line in File.ReadAllLines(filePath))
                {
                    lineNumber++;

                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("[") || line.StartsWith(";"))
                        continue;

                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length != 2)
                    {
                        Debug.WriteLine($"Skipping invalid line {lineNumber}: {line}");
                        continue;
                    }

                    if (!ushort.TryParse(parts[0].Trim(), out ushort id))
                    {
                        Debug.WriteLine($"Invalid ID on line {lineNumber}: {parts[0]}");
                        continue;
                    }

                    string systemName = parts[1].Trim();

                    if (string.IsNullOrEmpty(systemName))
                    {
                        Debug.WriteLine($"Empty system name on line {lineNumber}");
                        continue;
                    }

                    // Warn about duplicates but keep the last one
                    if (systems.ContainsKey(id))
                    {
                        Debug.WriteLine($"Warning: Duplicate ID {id} on line {lineNumber}. " +
                                      $"Replacing '{systems[id]}' with '{systemName}'");
                    }

                    systems[id] = systemName;
                }

                Debug.WriteLine($"Loaded {systems.Count} arcade systems from {filePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading arcade systems from {filePath}: {ex.Message}");
            }

            lock (_lock)
            {
                _arcadeSystems = systems.ToImmutableDictionary();
                _systemNamesCache = null; // Invalidate cache
            }
        }

        /// Gets the arcade system name for a given API-returned ID.
        /// Used when scraping: ArcadeDB returns a system ID, we lookup the name.
        public static string GetArcadeSystemNameByID(ushort id) =>
            _arcadeSystems.TryGetValue(id, out var shortName) ? shortName : string.Empty;

        /// Checks if a system name is an arcade system.
        /// Used for validation: prevents non-arcade systems from using ArcadeDB scraper.
        public static bool HasArcadeSystemName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Build cache on first use (thread-safe double-check locking)
            if (_systemNamesCache == null)
            {
                lock (_lock)
                {
                    if (_systemNamesCache == null)
                    {
                        _systemNamesCache = new HashSet<string>(
                            _arcadeSystems.Values,
                            StringComparer.OrdinalIgnoreCase);
                    }
                }
            }

            return _systemNamesCache.Contains(name);
        }
    }
}