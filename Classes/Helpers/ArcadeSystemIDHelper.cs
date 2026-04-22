using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Gamelist_Manager.Classes.Helpers
{
    public static class ArcadeSystemIDHelper
    {
        private static readonly string DefaultIniPath = Path.Combine(AppContext.BaseDirectory, "ini", "arcadesystems.ini");
        private static readonly object _lock = new object();
        private static ImmutableDictionary<ushort, string> _arcadeSystems = ImmutableDictionary<ushort, string>.Empty;
        private static HashSet<string>? _systemNamesCache;

        public static IReadOnlyDictionary<ushort, string> ArcadeSystems => _arcadeSystems;
        public static bool IsInitialized => _arcadeSystems.Count > 0;

        static ArcadeSystemIDHelper()
        {
            try
            {
                LoadArcadeSystems(DefaultIniPath);
            }
            catch
            {
                // Silent fail
            }
        }

        public static void LoadArcadeSystems(string filePath)
        {
            var systems = new Dictionary<ushort, string>();

            try
            {
                if (!File.Exists(filePath))
                    return;

                int lineNumber = 0;
                foreach (var line in File.ReadAllLines(filePath))
                {
                    lineNumber++;

                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("[") || line.StartsWith(";"))
                        continue;

                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length != 2)
                        continue;

                    if (!ushort.TryParse(parts[0].Trim(), out ushort id))
                        continue;

                    string systemName = parts[1].Trim();

                    if (string.IsNullOrEmpty(systemName))
                        continue;

                    systems[id] = systemName;
                }
            }
            catch
            {
                // Silent fail for now
            }

            lock (_lock)
            {
                _arcadeSystems = systems.ToImmutableDictionary();
                _systemNamesCache = null;
            }
        }

        public static string GetArcadeSystemNameByID(ushort id) =>
            _arcadeSystems.TryGetValue(id, out var shortName) ? shortName : string.Empty;

        public static bool HasArcadeSystemName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

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