using System.Collections.Generic;

namespace Gamelist_Manager.Models
{
    public class ScrapedGameData
    {
        public Dictionary<string, string> Data { get; set; } = new();
        public List<MediaResult> Media { get; set; } = new();
        public List<string> Messages { get; set; } = new();

        public class MediaResult
        {
            public string Url { get; set; } = string.Empty;
            public string FileExtension { get; set; } = string.Empty;
            public string Region { get; set; } = string.Empty;
            public string MediaType { get; set; } = string.Empty;
        }
    }
}
