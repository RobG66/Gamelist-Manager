using System;
using System.IO;

namespace Gamelist_Manager.Models
{
    public class RecentFileItem
    {
        public string FilePath { get; }
        public string FileName { get; }
        public string DirectoryPath { get; }
        public bool FileExists { get; }
        public DateTime? LastModified { get; }

        public RecentFileItem(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            DirectoryPath = Path.GetDirectoryName(filePath) ?? string.Empty;

            // Check if file exists and get last modified time
            if (File.Exists(filePath))
            {
                FileExists = true;
                try
                {
                    LastModified = File.GetLastWriteTime(filePath);
                }
                catch
                {
                    LastModified = null;
                }
            }
            else
            {
                FileExists = false;
                LastModified = null;
            }
        }

        public string ToolTip
        {
            get
            {
                if (!FileExists)
                    return "[File not found]";

                if (LastModified.HasValue)
                    return $"Last Modified: {LastModified.Value:yyyy-MM-dd HH:mm:ss}";

                return string.Empty;
            }
        }

        public string DisplayPath => FilePath;
    }
}
