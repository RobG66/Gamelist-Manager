using System.IO;

namespace GamelistManager.classes
{
    public static class PathConverter
    {
        // Method for converting a single string to a relative path
        public static string ConvertToRelativePath(string fullPath)
        {
            string folderName = Path.GetFileName(Path.GetDirectoryName(fullPath))!;
            string fileName = Path.GetFileName(fullPath);
            return $"./{folderName}/{fileName}";
        }

        // Method for converting a list of strings to relative paths
        public static List<string> ConvertListToRelativePaths(List<string> fullPaths)
        {
            return fullPaths.Select(fullPath => ConvertToRelativePath(fullPath)).ToList();
        }
    }
}