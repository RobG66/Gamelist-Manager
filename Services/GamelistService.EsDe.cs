using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Gamelist_Manager.Services;

public partial class GamelistService
{
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg"];
    private static readonly string[] VideoExtensions = [".mp4", ".avi", ".mkv"];
    private static readonly string[] ManualExtensions = [".pdf"];

    // Resolves ES-DE media paths from the filesystem and writes them into the row values so
    // all downstream consumers (grid, preview, statistics) can read them without branching.
    internal static void PopulateMediaPaths(IList<GameMetadataRow> games, string mediaDirectory)
    {
        if (string.IsNullOrEmpty(mediaDirectory)) return;

        var mediaDecls = GamelistMetaData.GetAllMediaFolderTypes();

        foreach (var game in games)
        {
            var romPath = game.Path;
            if (string.IsNullOrEmpty(romPath)) continue;

            var romName = FilePathHelper.NormalizeRomName(romPath);
            if (string.IsNullOrEmpty(romName)) continue;

            foreach (var decl in mediaDecls)
            {
                if (!decl.IsEsDeSupported) continue;

                var folder = Path.Combine(mediaDirectory, decl.EsDeFolderName);

                var extensions = decl.DataType switch
                {
                    MetaDataType.Video => VideoExtensions,
                    MetaDataType.Document => ManualExtensions,
                    _ => ImageExtensions
                };

                string? resolved = null;
                foreach (var ext in extensions)
                {
                    var candidate = Path.Combine(folder, romName + ext);
                    if (File.Exists(candidate))
                    {
                        resolved = candidate;
                        break;
                    }
                }

                game.SetValue(decl.Key, resolved ?? string.Empty);
            }
        }
    }

    // Reads the ROMDirectory value from ES-DE's settings file.
    // Falls back to <parent of esDeRoot>/ROMS if the setting is empty or missing.
    // Returns null when no ROMs folder can be determined.
    internal static string? ReadRomDirectoryFromEsDeSettings(string esDeRoot)
    {
        if (string.IsNullOrWhiteSpace(esDeRoot)) return null;

        var settingsPath = Path.Combine(esDeRoot, "settings", "es_settings.xml");
        if (File.Exists(settingsPath))
        {
            try
            {
                var doc = XDocument.Load(settingsPath);
                var romDir = doc.Descendants("string")
                    .FirstOrDefault(e => string.Equals(e.Attribute("name")?.Value, "ROMDirectory", StringComparison.Ordinal))
                    ?.Attribute("value")?.Value;

                if (!string.IsNullOrWhiteSpace(romDir))
                {
                    var trimmed = Path.TrimEndingDirectorySeparator(romDir);
                    if (Directory.Exists(trimmed))
                        return trimmed;
                }
            }
            catch { }
        }

        // Default location is one level up from the ES-DE root, in a folder named ROMS
        var parentDir = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(esDeRoot));
        if (parentDir != null)
        {
            var fallback = Path.Combine(parentDir, "ROMS");
            if (Directory.Exists(fallback))
                return fallback;
        }

        return null;
    }
}
