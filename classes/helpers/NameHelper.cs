using System.IO;

namespace GamelistManager.classes.helpers
{
    public static class NameHelper
    {
        // Returns the normalized ROM name from a given file path.
        // For example, "C:\Games\SuperMarioWorld.zip" becomes "SuperMarioWorld".
        // Path and file extension are removed.
        public static string NormalizeRomName(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }
            path = path.Trim(); // remove leading/trailing whitespace
            int lastSlash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
            string fileName = lastSlash >= 0 ? path[(lastSlash + 1)..] : path;
            return Path.GetFileNameWithoutExtension(fileName).Trim();
        }

    }
}