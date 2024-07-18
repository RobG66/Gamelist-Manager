using System.Collections.Generic;

namespace GamelistManager
{
    internal static class MediaPathHelper
    {
        public static Dictionary<string, string> GetMediaPaths()
        {
            // Get saved or default media paths
            string regValue = RegistryManager.ReadRegistryValue(null, "MediaPaths");

            if (string.IsNullOrEmpty(regValue))
            {
                regValue = "image=./images," +
                        "marquee=./images," +
                        "thumbnail=./images," +
                        "fanart=./images," +
                        "titleshot=./images," +
                        "manual=./manuals," +
                        "map=./images," +
                        "bezel=./images," +
                        "boxback=./images," +
                        "video=./videos," +
                        "cartridge=./images";
            }

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            string[] pairs = regValue.Split(',');

            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    string key = keyValue[0].Trim();
                    string value = keyValue[1].Trim();
                    dictionary[key] = value;
                }
            }
            return dictionary;
        }
    }
}
