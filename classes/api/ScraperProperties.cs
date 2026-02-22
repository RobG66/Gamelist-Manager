namespace GamelistManager.classes.api
{
    public class ScraperProperties
    {
        public string Name { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string SystemID { get; set; } = "";

        public int LogVerbosity { get; set; } = 0;
        public bool IsAuthenticated { get; set; }
        public int MaxConcurrency { get; set; } = 1;

        // Optional token (EmuMovies)
        public string? AccessToken { get; set; }

        // Cache Folder
        public string? CacheFolder { get; set; }

        // ScreenScraper-specific
        public string? Language { get; set; }
        public List<string> Regions { get; set; } = new();
        
        // ArcadeDB batch processing 
        public bool BatchProcessing { get; set; } = false;
        // EmuMovies-specific
        public Dictionary<string, List<string>> EmuMoviesMediaLists { get; set; } = new();
    }
}