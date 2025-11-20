using System.IO;

namespace GamelistManager.classes.helpers
{
    public static class PathHelper
    {
        /// Examples:
        /// fullPath:   "C:/folder1/file.zip"
        /// parentPath: "C:/folder1"
        /// result:     "./file.zip"
        ///
        /// fullPath:   "C:/1/2/3/4.txt"
        /// parentPath: "C:/1/2"
        /// result:     "./3/4.txt"
        public static string ConvertPathToGamelistRomPath(string fullPath, string parentPath)
        {
            string relative = Path.GetRelativePath(parentPath, fullPath).Replace("\\", "/");
            return $"./{relative}";
        }

        /// Examples:
        /// fullPath:   "C:/folder1/folder2/file.zip"
        /// parentPath: "C:/folder1"
        /// result:     "folder2/file.zip"
        ///
        /// fullPath:   "C:/1/2/3/4.txt"
        /// parentPath: "C:/1/2"
        /// result:     "3/4.txt"
        public static string ConvertPathToRelativePath(string fullPath, string parentPath)
        {
            return Path.GetRelativePath(parentPath, fullPath).Replace("\\", "/");
        }

        /// Examples:
        /// parentPath: "C:/1/2"
        /// relPath:    "./3/4.txt"  → "C:\\1\\2\\3\\4.txt"
        /// relPath:    "3/4.txt"    → "C:\\1\\2\\3\\4.txt"
        public static string ConvertGamelistPathToFullPath(string relativePath, string parentPath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return string.Empty;

            // Normalize slashes first
            relativePath = relativePath.Replace('/', '\\');

            // Remove ANY leading dots or slashes
            relativePath = relativePath.TrimStart('.', '\\');

            // Combine and normalize path
            string combined = Path.Combine(parentPath, relativePath);
            return Path.GetFullPath(combined);
        }



    }
}
