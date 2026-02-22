using GamelistManager.classes.gamelist;

namespace GamelistManager.classes.api
{
    public class ScraperParameters
    {
        public string? RomFileName { get; set; }
        public string? RomName { get; set; }
        public string? GameID { get; set; }
        public string? SystemID { get; set; }
        public string? UserID { get; set; }
        public string? UserPassword { get; set; }
        public string? ParentFolderPath { get; set; }
        public string? SSLanguage { get; set; }
        public List<string>? SSRegions { get; set; }
        public string? ImageSource { get; set; }
        public string? ThumbnailSource { get; set; }
        public string? BoxArtSource { get; set; }
        public string? MarqueeSource { get; set; }
        public string? WheelSource { get; set; }
        public string? VideoSource { get; set; }
        public string? CartridgeSource { get; set; }
        public bool OverwriteName { get; set; }
        public bool ScrapeEnglishGenreOnly { get; set; }
        public bool ScrapeAnyMedia { get; set; }
        public bool OverwriteMedia { get; set; }
        public bool OverwriteMetadata { get; set; }
        public bool VerifyImageDownloads { get; set; }
        public string? UserAccessToken { get; set; }
        public string? ScraperPlatform { get; set; }
        public string? MameArcadeName { get; set; }
        public bool ScrapeByGameID { get; set; }
        public string? CacheFolder { get; set; }
        public bool ScrapeByCache { get; set; }
        public bool SkipNonCached { get; set; }
        public List<string>? ElementsToScrape
        {
            get => _elementsToScrape;
            set
            {
                _elementsToScrape = value;
                _metaLookup = null; // reset cache when new list assigned
            }
        }
        private List<string>? _elementsToScrape;

        public Dictionary<string, string>? MediaPaths { get; set; }


        //  Cached Metadata Lookup (Computed Once Per Scrape)
        //  item → (MetaDataType, ColumnName)
        private Dictionary<string, (string Type, string Column)>? _metaLookup;

        public Dictionary<string, (string Type, string Column)> MetaLookup
        {
            get
            {
                if (_metaLookup == null)
                {
                    if (ElementsToScrape == null)
                        throw new InvalidOperationException("ElementsToScrape must be set before accessing MetaLookup.");

                    _metaLookup = ElementsToScrape.ToDictionary(
                        item => item,
                        item => (
                            GamelistMetaData.GetMetadataDataTypeByType(item),
                            GamelistMetaData.GetMetadataNameByType(item)
                        )
                    );
                }

                return _metaLookup;
            }
        }


        /// Creates a deep clone of this ScraperParameters instance.
        /// All collection properties are copied to new instances to ensure thread safety.
        /// MetaLookup is *not* cloned — the clone will rebuild it lazily when needed.
        public ScraperParameters Clone()
        {
            return new ScraperParameters
            {
                RomFileName = this.RomFileName,
                RomName = this.RomName,
                GameID = this.GameID,
                SystemID = this.SystemID,
                UserID = this.UserID,
                UserPassword = this.UserPassword,
                ParentFolderPath = this.ParentFolderPath,
                SSLanguage = this.SSLanguage,

                SSRegions = this.SSRegions != null ? new List<string>(this.SSRegions) : null,
                ElementsToScrape = this.ElementsToScrape != null ? new List<string>(this.ElementsToScrape) : null,
                MediaPaths = this.MediaPaths != null ? new Dictionary<string, string>(this.MediaPaths) : null,

                ImageSource = this.ImageSource,
                ThumbnailSource = this.ThumbnailSource,
                BoxArtSource = this.BoxArtSource,
                MarqueeSource = this.MarqueeSource,
                WheelSource = this.WheelSource,
                VideoSource = this.VideoSource,
                CartridgeSource = this.CartridgeSource,
                OverwriteName = this.OverwriteName,
                ScrapeEnglishGenreOnly = this.ScrapeEnglishGenreOnly,
                ScrapeAnyMedia = this.ScrapeAnyMedia,
                OverwriteMedia = this.OverwriteMedia,
                OverwriteMetadata = this.OverwriteMetadata,
                VerifyImageDownloads = this.VerifyImageDownloads,
                UserAccessToken = this.UserAccessToken,
                ScraperPlatform = this.ScraperPlatform,
                MameArcadeName = this.MameArcadeName,
                ScrapeByGameID = this.ScrapeByGameID,
                CacheFolder = this.CacheFolder,
                ScrapeByCache = this.ScrapeByCache,
                SkipNonCached = this.SkipNonCached
            };
        }
    }
}
