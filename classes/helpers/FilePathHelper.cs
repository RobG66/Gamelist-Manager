using System.IO;

namespace GamelistManager.classes.helpers
{
    public static class FilePathHelper
    {
        /// Converts an absolute file path to a gamelist-relative path (prefixed with "./").
        /// Examples:
        /// fullPath:   "C:/folder1/file.zip"
        /// parentPath: "C:/folder1"
        /// result:     "./file.zip"
        public static string ConvertPathToGamelistRomPath(string fullPath, string parentPath)
        {
            string relative = Path.GetRelativePath(parentPath, fullPath).Replace("\\", "/");
            return $"./{relative}";
        }

        /// Converts an absolute path to a relative path from parentPath, without "./" prefix.
        /// Examples:
        /// fullPath:   "C:/folder1/folder2/file.zip"
        /// parentPath: "C:/folder1"
        /// result:     "folder2/file.zip"
        public static string ConvertPathToRelativePath(string fullPath, string parentPath)
        {
            return Path.GetRelativePath(parentPath, fullPath).Replace("\\", "/");
        }

        /// Converts a gamelist-relative path or relative path back to a full absolute path.
        /// Examples:
        /// parentPath: "C:/1/2"
        /// relPath:    "./3/4.txt"  → "C:\1\2\3\4.txt"
        /// relPath:    "3/4.txt"    → "C:\1\2\3\4.txt"
        public static string ConvertGamelistPathToFullPath(string relativePath, string parentPath)
        {         
            relativePath = relativePath.Replace('/', '\\'); // normalize slashes
            relativePath = relativePath.TrimStart('.', '\\'); // remove leading dots/slashes
            return Path.GetFullPath(Path.Combine(parentPath, relativePath));
        }

        /// Returns the normalized ROM name from a given file path.
        /// Example: "C:\Games\SuperMarioWorld.zip" → "SuperMarioWorld"
        public static string NormalizeRomName(string path)
        {
            // Trim whitespace and any trailing slashes
            path = path.Trim().TrimEnd('/', '\\');

            // Find the last slash (forward or back)
            int lastSlash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));

            // Extract the file name part
            string fileName = lastSlash >= 0 ? path[(lastSlash + 1)..] : path;

            // Remove extension and trim again
            return Path.GetFileNameWithoutExtension(fileName).Trim();
        }

    }
}
