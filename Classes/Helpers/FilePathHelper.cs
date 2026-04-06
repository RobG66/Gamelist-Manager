using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Gamelist_Manager.Classes.Helpers
{
    public static class FilePathHelper
    {
        private static readonly Regex ValidSuffixRegex = new(@"^[a-zA-Z0-9]{1,20}$", RegexOptions.Compiled);

        // Use case-insensitive comparison on Windows (FAT/NTFS) and case-sensitive on Linux/macOS.
        public static readonly StringComparison PathComparison =
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        public static readonly StringComparer PathComparer =
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        // Validates a raw media folder path before normalization.
        // Checks for null/empty, illegal characters, colon (absolute path), UNC paths,
        // and directory traversal segments.
        // Returns true if the path is safe to normalize, false if it should be rejected.
        public static bool IsValidMediaFolderPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // Reject colons — indicates an absolute/Windows drive path (e.g. C:/images)
            if (path.Contains(':'))
                return false;

            // Reject UNC-style paths (\\server or //server)
            if (path.StartsWith("\\\\") || path.StartsWith("//"))
                return false;

            // Reject illegal path characters
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                return false;

            // Reject any directory traversal segments (..)
            // Normalize slashes first so we catch both /../ and \..\
            var normalized = path.Replace('\\', '/');
            foreach (var segment in normalized.Split('/'))
            {
                if (segment == "..")
                    return false;
            }

            return true;
        }

        // Validates a media folder suffix.
        // Suffixes must be alphanumeric only (a-z, A-Z, 0-9) and no longer than 20 characters.
        // Returns true if valid, false otherwise.
        public static bool IsValidMediaFolderSuffix(string suffix)
        {
            return !string.IsNullOrWhiteSpace(suffix) && ValidSuffixRegex.IsMatch(suffix);
        }

        // Converts an absolute path to a relative path from parentPath, without "./" prefix.
        // Examples:
        //   fullPath:   "C:/folder1/folder2/file.zip"
        //   parentPath: "C:/folder1"
        //   result:     "folder2/file.zip"
        public static string PathToRelativePath(string fullPath, string parentPath)
        {
            return Path.GetRelativePath(parentPath, fullPath).Replace("\\", "/");
        }

        // Converts an absolute file path to a gamelist-relative path (prefixed with "./").
        // Delegates to NormalizePathWithDotSlashPrefix for consistent normalization.
        // Examples:
        //   fullPath:   "C:/folder1/file.zip"
        //   parentPath: "C:/folder1"
        //   result:     "./file.zip"
        public static string PathToRelativePathWithDotSlashPrefix(string fullPath, string parentPath)
        {
            return NormalizePathWithDotSlashPrefix(PathToRelativePath(fullPath, parentPath)) ?? string.Empty;
        }

        // Normalizes a relative media folder path and ensures it has a "./" prefix.
        // Should only be called after IsValidMediaFolderPath returns true.
        // Handles any combination of slashes, leading dots, and redundant separators.
        //
        // Examples:
        //   "images"        → "./images"
        //   ".\images"      → "./images"
        //   "./images"      → "./images"
        //   "/images"       → "./images"
        //   "media\videos"  → "./media/videos"
        //   "media//videos" → "./media/videos"
        //   "./"            → null  (root-only, nothing after prefix)
        //   ""              → null  (empty)
        //
        // Returns null if nothing valid remains after normalization.
        public static string? NormalizePathWithDotSlashPrefix(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            // Convert all backslashes to forward slashes
            path = path.Replace('\\', '/');

            // Split into segments and discard empty segments, lone dots, and traversal
            var segments = path.Split('/');
            var clean = new List<string>();
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment) || segment == "." || segment == "..")
                    continue;
                clean.Add(segment);
            }

            if (clean.Count == 0)
                return null;

            return "./" + string.Join("/", clean);
        }

        // Resolves a path from a gamelist entry to a full absolute path.
        // Absolute paths are returned as-is after normalisation.
        // Examples:
        //   relativePath: "./images/game.png", parentPath: "C:/roms/snes"  → "C:/roms/snes/images/game.png"
        //   relativePath: "images/game.png",   parentPath: "C:/roms/snes"  → "C:/roms/snes/images/game.png"
        //   relativePath: "/home/rob/media/game.png"                        → "/home/rob/media/game.png"
        public static string GamelistPathToFullPath(string relativePath, string parentPath)
        {
            relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar)
                                       .Replace('\\', Path.DirectorySeparatorChar);

            // Absolute paths are returned as-is
            if (Path.IsPathRooted(relativePath))
                return Path.GetFullPath(relativePath);

            return Path.GetFullPath(Path.Combine(parentPath, relativePath));
        }

        // Returns the normalized ROM name from a given file path.
        // Example: "C:\Games\SuperMarioWorld.zip" → "SuperMarioWorld"
        public static string NormalizeRomName(string path)
        {
            // Trim whitespace and any trailing slashes
            path = path.Trim().TrimEnd('/', '\\');

            // Find the last slash (forward or back)
            var lastSlashPosition = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));

            // Extract the file name part
            var fileName = lastSlashPosition >= 0 ? path[(lastSlashPosition + 1)..] : path;

            // Remove extension and trim again
            return Path.GetFileNameWithoutExtension(fileName).Trim();
        }
    }
}