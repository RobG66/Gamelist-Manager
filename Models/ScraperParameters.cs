using Gamelist_Manager.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gamelist_Manager.Models
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
        public Dictionary<string, (string Suffix, bool SfxEnabled)>? MediaSuffixes { get; set; }

        // Pre-built cache of filenames per media type, populated once before the scraping loop
        // to avoid one File.Exists network round-trip per media item per game on remote paths.
        public Dictionary<string, HashSet<string>>? ExistingMediaFiles { get; set; }


        // Cached once before the scraping loop; rebuilt lazily if ElementsToScrape changes.
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


        public static ScraperParameters Create(SharedDataService sharedData, string scraperName, string currentSystem, List<string> elementsToScrape)
        {
            var media = sharedData.MediaSettings;

            var scraperConfig = ScraperConfigService.Instance;

            string? primaryRegion = scraperConfig.GetScraperPrimaryRegionCode(scraperName);
            var regions = scraperConfig.GetScraperFallbackRegionCodes(scraperName).ToList();
            if (!string.IsNullOrEmpty(primaryRegion))
            {
                regions.Remove(primaryRegion);
                regions.Insert(0, primaryRegion);
            }

            return new ScraperParameters
            {
                MediaPaths = media.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Path,
                    StringComparer.OrdinalIgnoreCase),
                MediaSuffixes = media.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (kvp.Value.Suffix, kvp.Value.SfxEnabled),
                    StringComparer.OrdinalIgnoreCase),
                ParentFolderPath = sharedData.GamelistDirectory,
                VerifyImageDownloads = sharedData.VerifyImageDownloads,
                ElementsToScrape = elementsToScrape,
                SystemID = scraperConfig.GetScraperSystemId(scraperName, currentSystem),
                SSLanguage = scraperConfig.GetScraperLanguageCode(scraperName),
                SSRegions = regions,
                ImageSource = ResolveSource(scraperConfig, scraperName, nameof(ImageSource)),
                MarqueeSource = ResolveSource(scraperConfig, scraperName, nameof(MarqueeSource)),
                ThumbnailSource = ResolveSource(scraperConfig, scraperName, nameof(ThumbnailSource)),
                CartridgeSource = ResolveSource(scraperConfig, scraperName, nameof(CartridgeSource)),
                VideoSource = ResolveSource(scraperConfig, scraperName, nameof(VideoSource)),
                BoxArtSource = ResolveSource(scraperConfig, scraperName, nameof(BoxArtSource)),
                MixSource = ResolveSource(scraperConfig, scraperName, nameof(MixSource)),
                WheelSource = ResolveSource(scraperConfig, scraperName, nameof(WheelSource)),
                CacheFolder = Path.Combine(AppContext.BaseDirectory, "cache", scraperName, currentSystem),
                ScrapeNamesLanguageFirst = scraperConfig.GetScraperBoolSetting(scraperName, "NamesLanguageFirst"),
                ScrapeMediaRegionFirst = scraperConfig.GetScraperBoolSetting(scraperName, "MediaRegionFirst"),
            };
        }

        private static string ResolveSource(ScraperConfigService scraperConfig, string scraperName, string sectionName)
        {
            string savedDisplayName = scraperConfig.GetScraperSourceSetting(scraperName, sectionName);
            if (!string.IsNullOrEmpty(savedDisplayName))
            {
                var sources = scraperConfig.GetScraperSources(scraperName, sectionName);
                if (sources.TryGetValue(savedDisplayName, out var apiValue) && !string.IsNullOrEmpty(apiValue))
                    return apiValue;
            }
            return scraperConfig.GetScraperDefaultSource(scraperName, sectionName);
        }

        // Collections are deep-copied so each scraper thread gets its own independent state.
        // MetaLookup is not cloned — the clone rebuilds it lazily when needed.
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
                MediaSuffixes = this.MediaSuffixes != null ? new Dictionary<string, (string, bool)>(this.MediaSuffixes) : null,

                ImageSource = this.ImageSource,
                ThumbnailSource = this.ThumbnailSource,
                BoxArtSource = this.BoxArtSource,
                MixSource = this.MixSource,
                MarqueeSource = this.MarqueeSource,
                WheelSource = this.WheelSource,
                VideoSource = this.VideoSource,
                CartridgeSource = this.CartridgeSource,
                OverwriteName = this.OverwriteName,
                ScrapeEnglishGenreOnly = this.ScrapeEnglishGenreOnly,
                ScrapeAnyMedia = this.ScrapeAnyMedia,
                ScrapeNamesLanguageFirst = this.ScrapeNamesLanguageFirst,
                ScrapeMediaRegionFirst = this.ScrapeMediaRegionFirst,
                OverwriteMedia = this.OverwriteMedia,
                OverwriteMetadata = this.OverwriteMetadata,
                VerifyImageDownloads = this.VerifyImageDownloads,
                UserAccessToken = this.UserAccessToken,
                ScraperPlatform = this.ScraperPlatform,
                MameArcadeName = this.MameArcadeName,
                ScrapeByGameID = this.ScrapeByGameID,
                CacheFolder = this.CacheFolder,
                ScrapeByCache = this.ScrapeByCache,
                SkipNonCached = this.SkipNonCached,
                ExistingMediaFiles = this.ExistingMediaFiles
            };
        }
    }
}
