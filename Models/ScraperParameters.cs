using System.Collections.Generic;

namespace Gamelist_Manager.Models
{
    public class ScraperParameters
    {
        public ScraperParameters ShallowClone() => (ScraperParameters)MemberwiseClone();

        public string ScraperName { get; set; } = "";
        public int LogVerbosity { get; set; }
        public int MaxConcurrency { get; set; } = 1;
        public bool BatchProcessing { get; set; }
        public Dictionary<string, List<string>> EmuMoviesMediaLists { get; set; } = new();

        public string? RomFilePath { get; set; }
        public string? RomFileName { get; set; }
        public string? RomName { get; set; }
        public string? GameID { get; set; }
        public string? SystemID { get; set; }
        public string? UserID { get; set; }
        public string? UserPassword { get; set; }
        public string? SSLanguage { get; set; }
        public List<string>? SSRegions { get; set; }
        public string? ImageSource { get; set; }
        public string? ThumbnailSource { get; set; }
        public string? BoxArtSource { get; set; }
        public string? MixSource { get; set; }
        public string? MarqueeSource { get; set; }
        public string? WheelSource { get; set; }
        public string? VideoSource { get; set; }
        public string? CartridgeSource { get; set; }
        public bool OverwriteName { get; set; }
        public bool ScrapeEnglishGenreOnly { get; set; }
        public bool ScrapeAnyMedia { get; set; }
        public bool ScrapeNamesLanguageFirst { get; set; }
        public bool ScrapeMediaRegionFirst { get; set; }
        public bool OverwriteMedia { get; set; }
        public bool OverwriteMetadata { get; set; }
        public bool VerifyImageDownloads { get; set; }
        public string? UserAccessToken { get; set; }
        public string? MameArcadeName { get; set; }
        public bool ScrapeByGameID { get; set; }
        public string? CacheFolder { get; set; }
        public bool ScrapeByCache { get; set; }
        public bool SkipNonCached { get; set; }
        public bool RemoveZzzNotGamePrefix { get; set; }

        public List<string>? ElementsToScrape { get; set; }
        public Dictionary<string, string>? MediaPaths { get; set; }
        public Dictionary<string, (string Suffix, bool SfxEnabled)>? MediaSuffixes { get; set; }
        public Dictionary<string, HashSet<string>>? ExistingMediaFiles { get; set; }
        public Dictionary<string, (string Type, string Column)>? MetaLookup { get; set; }
    }
}