using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Gamelist_Manager.Classes.Helpers
{
    public static class EsDePathResolver
    {
        public record DetectedPaths(string? RomDirectory, string? MediaDirectory);

        public static DetectedPaths ReadPathsFromEsDeSettings(string esDeRoot)
        {
            if (string.IsNullOrWhiteSpace(esDeRoot))
                return new DetectedPaths(null, null);

            string? romDirectory = null;
            string? mediaDirectory = null;

            var settingsPath = Path.Combine(esDeRoot, "settings", "es_settings.xml");
            if (File.Exists(settingsPath))
            {
                foreach (var line in File.ReadLines(settingsPath))
                {
                    if (line.Contains("\"ROMDirectory\""))
                    {
                        var match = Regex.Match(line, @"value=""([^""]+)""");
                        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                        {
                            var expanded = FilePathHelper.ExpandTilde(match.Groups[1].Value);
                            var path = Path.TrimEndingDirectorySeparator(expanded);
                            if (Directory.Exists(path))
                                romDirectory = path;
                        }
                    }
                    else if (line.Contains("\"MediaDirectory\""))
                    {
                        var match = Regex.Match(line, @"value=""([^""]+)""");
                        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                        {
                            var expanded = FilePathHelper.ExpandTilde(match.Groups[1].Value);
                            var path = Path.TrimEndingDirectorySeparator(expanded);
                            if (Directory.Exists(path))
                                mediaDirectory = path;
                        }
                    }

                    if (romDirectory != null && mediaDirectory != null)
                        break;
                }
            }

            // Fall back to conventional locations unconditionally if not resolved from settings
            romDirectory ??= Path.GetFullPath(Path.Combine(esDeRoot, "..", "ROMs"));
            mediaDirectory ??= Path.Combine(esDeRoot, "downloaded_media");

            return new DetectedPaths(romDirectory, mediaDirectory);
        }
    }
}