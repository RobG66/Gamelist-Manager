using System.Data;

namespace GamelistManager.classes
{
    public class ScraperParameters
    {
        public string? RomFileNameWithExtension { get; set; }
        public string? RomFileNameWithoutExtension { get; set; }
        public string? Name { get; set; }
        public string? GameID { get; set; }
        public string? SystemID { get; set; }
        public string? UserID { get; set; }
        public string? UserPassword { get; set; }
        public string? ParentFolderPath { get; set; }
        public string? Language { get; set; }
        public string? Region { get; set; }
        public string? ImageSource { get; set; }
        public string? ThumbnailSource { get; set; }
        public string? BoxartSource { get; set; }
        public string? MarqueeSource { get; set; }
        public string? VideoSource { get; set; }
        public string? CartridgeSource { get; set; }
        public bool OverwriteNames { get; set; }
        public bool ScrapeEnglishGenreOnly { get; set; }
        public bool OverwriteMedia { get; set; }
        public bool OverwriteMetadata { get; set; }
        public bool Verify { get; set; }
        public DataRowView CurrentRow { get; set; }
        public string? UserAccessToken { get; set; }
        public string? ScraperPlatform { get; set; }
        public bool ScrapeByGameID { get; set; }
        public string? CacheFolder { get; set; }
        public bool ScrapeByCache { get; set; }
        public bool SkipNonCached { get; set; }
        public List<string>? ElementsToScrape { get; set; }
        public Dictionary<string, string>? MediaPaths { get; set; }

        // Add the Clone method
        public ScraperParameters Clone()
        {
            return (ScraperParameters)this.MemberwiseClone();
        }
    }
}
