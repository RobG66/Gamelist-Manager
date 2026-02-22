namespace GamelistManager.classes.api
{
    public class ScrapedGameData
    {
        // Data dictionary
        public Dictionary<string, string> Data { get; set; } = new();

        // Media results
        public List<MediaResult> Media { get; set; } = new();

        // Tracking
        public bool WasSuccessful { get; set; }
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