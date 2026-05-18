using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System.Collections.Generic;
using System.IO;

namespace Gamelist_Manager.Services
{
    public static class MediaPathResolver
    {
        public static IReadOnlyList<AvailableMediaFolder> BuildAvailableMedia(
            string profileType,
            string? mediaBaseFolder,
            Dictionary<string, string> mediaPaths)
        {
            var result = new List<AvailableMediaFolder>();

            if (string.IsNullOrEmpty(mediaBaseFolder))
                return result;

            bool isEsDe = profileType == SettingKeys.ProfileTypeEsDe;

            foreach (var decl in GamelistMetaData.GetAllMediaFolderTypes())
            {
                if (isEsDe && !decl.IsEsDeSupported)
                    continue;

                if (!ResolveBoolSetting($"{decl.Type}_enabled", decl.DefaultEnabled, mediaPaths))
                    continue;

                var folderPath = ResolveFolderPath(isEsDe, mediaBaseFolder, decl, mediaPaths);
                var suffix     = isEsDe ? string.Empty : ResolveSuffix(decl, mediaPaths);
                var sfxEnabled = !isEsDe && ResolveSfxEnabled(decl, mediaPaths);

                result.Add(new AvailableMediaFolder(decl.Type, decl.Name, folderPath, suffix, sfxEnabled));
            }

            return result;
        }

        private static string ResolveFolderPath(bool isEsDe, string mediaBaseFolder, MetaDataDecl decl, Dictionary<string, string> mediaPaths)
        {
            if (isEsDe)
                return Path.Combine(mediaBaseFolder, decl.EsDeFolderName);

            var relativePath  = mediaPaths.TryGetValue(decl.Type, out var path) ? path : decl.DefaultPath;
            // Strip leading "./" or ".\" before combining
            var cleanRelative = relativePath.TrimStart('.').TrimStart('/').TrimStart('\\');
            return Path.Combine(mediaBaseFolder, cleanRelative);
        }

        private static string ResolveSuffix(MetaDataDecl decl, Dictionary<string, string> mediaPaths)
            => mediaPaths.TryGetValue($"{decl.Type}_suffix", out var sfx) ? sfx : decl.DefaultSuffix;

        private static bool ResolveSfxEnabled(MetaDataDecl decl, Dictionary<string, string> mediaPaths)
            => ResolveBoolSetting($"{decl.Type}_sfx_enabled", !string.IsNullOrEmpty(decl.DefaultSuffix), mediaPaths);

        private static bool ResolveBoolSetting(string key, bool fallback, Dictionary<string, string> mediaPaths)
            => mediaPaths.TryGetValue(key, out var raw)
                ? bool.TryParse(raw, out var value) && value
                : fallback;
    }
}
