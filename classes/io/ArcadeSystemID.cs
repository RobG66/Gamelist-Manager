using System.IO;
using System.Windows;

namespace GamelistManager.classes.io
{
    public static class ArcadeSystemID
    {
        private static readonly string DefaultIniPath = "ini\\arcadesystems.ini";
        public static IReadOnlyDictionary<ushort, string> ArcadeSystems { get; private set; } = new Dictionary<ushort, string>();

        // Static constructor ensures automatic loading when the class is first accessed
        static ArcadeSystemID()
        {
            LoadArcadeSystems(DefaultIniPath);
        }

        public static void LoadArcadeSystems(string filePath)
        {
            var systems = new Dictionary<ushort, string>();

            if (File.Exists(filePath))
            {
                foreach (var line in File.ReadAllLines(filePath))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                        continue; // Skip empty lines and section headers

                    var parts = line.Split('=');
                    if (parts.Length == 2 && ushort.TryParse(parts[0].Trim(), out ushort id))
                    {
                        systems[id] = parts[1].Trim(); // Store only the short name
                    }
                }
            }
            else
            {
                MessageBox.Show("ini/arcadesystems.ini file is missing!", "File Missing", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ArcadeSystems = systems;
        }

        public static string GetArcadeSystemNameByID(ushort id) =>
            ArcadeSystems.TryGetValue(id, out var shortName) ? shortName : string.Empty;

        public static bool HasArcadeSystemName(string name) =>
            !string.IsNullOrWhiteSpace(name) &&
            ArcadeSystems.Values.Contains(name, StringComparer.OrdinalIgnoreCase);

    }
}
