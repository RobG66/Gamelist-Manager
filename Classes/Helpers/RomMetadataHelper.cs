using Gamelist_Manager.Models;
using System.IO;

namespace Gamelist_Manager.Classes.Helpers
{
    public static class RomMetadataHelper
    {
        public static string GetRomFileName(string romPath)
            => Path.GetFileName(romPath);

        public static string GetRomFileNameNoExtension(string romPath)
            => Path.GetFileNameWithoutExtension(romPath);

        public static string GetRomName(GameMetadataRow row, string romFileNameNoExtension)
            => row.GetValue(MetaDataKeys.name)?.ToString() ?? romFileNameNoExtension;

        public static string GetRegion(string romFileNameNoExtension, string? mameArcadeName)
        {
            string nameValue = !string.IsNullOrEmpty(mameArcadeName) ? mameArcadeName : romFileNameNoExtension;
            string region = RegionLanguageHelper.GetRegion(nameValue);
            return string.IsNullOrEmpty(region) ? "us" : region;
        }

        public static string GetLanguage(string romFileNameNoExtension, string? mameArcadeName)
        {
            string nameValue = !string.IsNullOrEmpty(mameArcadeName) ? mameArcadeName : romFileNameNoExtension;
            string language = RegionLanguageHelper.GetLanguage(nameValue);
            return string.IsNullOrEmpty(language) ? "en" : language;
        }

        public static string? GetMameArcadeName(string romFileNameNoExtension)
            => MameNamesHelper.Names.TryGetValue(romFileNameNoExtension, out string? arcadeName) ? arcadeName : null;
    }
}
