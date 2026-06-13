using Gamelist_Manager.Models;
using System.Collections.Generic;
using System.IO;

namespace Gamelist_Manager.Services
{
    public static class MediaPathResolver
    {
        public static IReadOnlyList<AvailableMediaFolder> BuildAvailableMedia(
            string profileType,
            string? currentSystem,
            string? mediaBaseFolder,
            Dictionary<string, string> mediaPaths)
        {
            var result = new List<AvailableMediaFolder>();

            if (string.IsNullOrEmpty(mediaBaseFolder))
                return result;

            var profile = SettingKeys.GetProfileTypeOption(profileType);
            var overrides = !string.IsNullOrEmpty(currentSystem)
                ? SettingsService.Instance.GetSection(SettingKeys.MediaPathOverridesSection)
                : null;

            foreach (var decl in MetadataService.GetAllMediaFolderTypes())
            {
                if (!profile.IncludesMediaFolder(decl))
                    continue;

                var mediaEnabled = ResolveMediaEnabled(decl, currentSystem, mediaPaths, overrides);
                var folderPath = ResolveFolderPath(profile, mediaBaseFolder, decl, mediaPaths);
                var suffix = profile.MediaFilenamesUseSuffixes ? ResolveSuffix(decl, mediaPaths) : string.Empty;
                var isSuffixEnabled = profile.MediaFilenamesUseSuffixes && ResolveIsSuffixEnabled(decl, mediaPaths);

                result.Add(new AvailableMediaFolder(decl.Type, decl.Name, folderPath, suffix, mediaEnabled, isSuffixEnabled));
            }

            return result;
        }

        private static bool ResolveMediaEnabled(
            MetaDataDecl decl,
            string? currentSystem,
            Dictionary<string, string> mediaPaths,
            Dictionary<string, string>? overrides)
        {
            if (overrides != null && !string.IsNullOrEmpty(currentSystem))
            {
                var overrideKey = $"{currentSystem}_{decl.Type}_enabled";
                if (overrides.TryGetValue(overrideKey, out var raw) && bool.TryParse(raw, out var overrideValue))
                    return overrideValue;
            }

            return ResolveBoolSetting($"{decl.Type}_enabled", decl.DefaultEnabled, mediaPaths);
        }

        private static string ResolveFolderPath(
            ProfileTypeOption profile,
            string mediaBaseFolder,
            MetaDataDecl decl,
            Dictionary<string, string> mediaPaths)
        {
            if (!profile.GamelistHasMediaPaths)
                return Path.Combine(mediaBaseFolder, decl.EsDeFolderName);

            var relativePath = mediaPaths.TryGetValue(decl.Type, out var path) ? path : decl.DefaultPath;
            // Strip leading "./" or ".\" before combining
            var cleanRelative = relativePath.TrimStart('.')
                                            .TrimStart(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return Path.Combine(mediaBaseFolder, cleanRelative);
        }

        private static string ResolveSuffix(MetaDataDecl decl, Dictionary<string, string> mediaPaths)
            => mediaPaths.TryGetValue($"{decl.Type}_suffix", out var sfx) ? sfx : decl.DefaultSuffix;

        private static bool ResolveIsSuffixEnabled(MetaDataDecl decl, Dictionary<string, string> mediaPaths)
            => ResolveBoolSetting($"{decl.Type}_suffix_enabled", !string.IsNullOrEmpty(decl.DefaultSuffix), mediaPaths);

        private static bool ResolveBoolSetting(string key, bool fallback, Dictionary<string, string> mediaPaths)
            => mediaPaths.TryGetValue(key, out var raw)
                ? bool.TryParse(raw, out var value) && value
                : fallback;
    }
}
