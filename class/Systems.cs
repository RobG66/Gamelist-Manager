using System.Collections.Generic;
using System.IO;

namespace GamelistManager
{
    public class SystemIdResolver
    {
        private Dictionary<string, int> systemValuesDictionary;

        public SystemIdResolver(string iniFilePath)
        {
            systemValuesDictionary = LoadFromIni(iniFilePath);
        }

        private Dictionary<string, int> LoadFromIni(string iniFilePath)
        {
            var dictionary = new Dictionary<string, int>();

            if (File.Exists(iniFilePath))
            {
                var lines = File.ReadAllLines(iniFilePath);

                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line) && line.Contains("="))
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim().ToLower();
                            if (int.TryParse(parts[1].Trim(), out int value))
                            {
                                dictionary[key] = value;
                            }
                        }
                    }
                }
            }

            return dictionary;
        }

        public int ResolveSystemId(string systemKey)
        {
            if (systemValuesDictionary.TryGetValue(systemKey.ToLower(), out int systemId))
            {
                return systemId;
            }

            return 0;
        }
    }
}
