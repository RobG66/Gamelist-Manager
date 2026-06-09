using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Gamelist_Manager.Services
{
    public static class IniFileService
    {
        #region Public Methods

        public static Dictionary<string, Dictionary<string, string>> ReadIniFile(string filePath)
        {
            var sections = new Dictionary<string, Dictionary<string, string>>();

            if (!File.Exists(filePath))
            {
                return sections; // Return empty if file is missing
            }

            string[] lines = File.ReadAllLines(filePath);
            string currentSection = string.Empty;

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(';'))
                    continue;

                if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    sections[currentSection] = new Dictionary<string, string>();
                }
                else if (!string.IsNullOrEmpty(currentSection))
                {
                    int indexOfEquals = trimmedLine.IndexOf('=');
                    if (indexOfEquals != -1)
                    {
                        string key = trimmedLine.Substring(0, indexOfEquals).Trim();
                        string value = trimmedLine[(indexOfEquals + 1)..].Trim();
                        sections[currentSection][key] = value;
                    }
                    else
                    {
                        sections[currentSection][trimmedLine] = trimmedLine;
                    }
                }
            }

            return sections;
        }

        public static Dictionary<string, string>? GetSection(string filePath, string sectionName)
        {
            var sections = ReadIniFile(filePath);
            return sections.TryGetValue(sectionName, out var section) ? section : null;
        }

        public static void WriteIniFile(string filePath, Dictionary<string, Dictionary<string, string>> sections)
        {
            var sb = new StringBuilder();

            foreach (var section in sections)
            {
                sb.AppendLine($"[{section.Key}]");

                foreach (var kvp in section.Value)
                {
                    sb.AppendLine($"{kvp.Key}={kvp.Value}");
                }

                sb.AppendLine(); // Empty line between sections
            }

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, sb.ToString());
        }

        public static void DeleteKeysWithPrefix(string filePath, string sectionName, string prefix)
        {
            var sections = ReadIniFile(filePath);
            if (!sections.TryGetValue(sectionName, out var section)) return;

            var keysToRemove = section.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (keysToRemove.Count == 0) return;

            foreach (var key in keysToRemove)
                section.Remove(key);

            WriteIniFile(filePath, sections);
        }

        #endregion
    }
}
