using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.Services
{
    internal partial class ScraperService
    {
        #region Public Methods

        public async Task<bool> InitializeScraperAsync(
            ScraperParameters baseParameters,
            string currentSystem,
            CancellationToken cancellationToken = default)
        {
            string scraperName = baseParameters.ScraperName;

            if (scraperName == ScraperRegistry.ArcadeDB.Name)
            {
                if (!ArcadeSystemIDHelper.IsInitialized)
                {
                    Log("Arcade systems configuration is missing.", LogLevel.Error);
                    return false;
                }
                if (!ArcadeSystemIDHelper.HasArcadeSystemName(currentSystem))
                {
                    Log($"'{currentSystem}' is not an arcade system.", LogLevel.Error);
                    return false;
                }
            }

            Directory.CreateDirectory(baseParameters.CacheFolder!);

            if (scraperName == ScraperRegistry.ScreenScraper.Name)
            {
                var (success, maxThreads) = await AuthenticateScreenScraperAsync();
                if (!success) return false;

                baseParameters.MaxConcurrency = maxThreads;

                var creds = CredentialHelper.GetCredentials(ScraperRegistry.ScreenScraper.Name);
                baseParameters.UserID = creds.UserName;
                baseParameters.UserPassword = creds.Password;

                LogScreenScraperConfiguration(scraperName);
            }
            else if (scraperName == ScraperRegistry.EmuMovies.Name)
            {
                var (success, accessToken) = await AuthenticateEmuMoviesAsync();
                if (!success) return false;

                baseParameters.MaxConcurrency = ScraperRegistry.EmuMovies.DefaultThreads;
                baseParameters.UserAccessToken = accessToken;

                await GetEmuMoviesMediaListsAsync(baseParameters.SystemID ?? string.Empty, baseParameters, cancellationToken);
            }

            return true;
        }

        public static ScraperParameters CreateScraperParameters(
            bool verifyImageDownloads,
            string profileType,
            IReadOnlyList<AvailableMediaFolder> availableMedia,
            string scraperName,
            string currentSystem,
            List<string> elementsToScrape)
        {
            var scraperConfig = ScraperConfigService.Instance;

            string? primaryRegion = scraperConfig.GetScraperPrimaryRegionCode(scraperName);
            var regions = scraperConfig.GetScraperFallbackRegionCodes(scraperName).ToList();
            if (!string.IsNullOrEmpty(primaryRegion))
            {
                regions.Remove(primaryRegion);
                regions.Insert(0, primaryRegion);
            }

            var profile = SettingKeys.GetProfileTypeOption(profileType);

            var parameters = new ScraperParameters
            {
                SkipMD5 = ArcadeSystemIDHelper.HasArcadeSystemName(currentSystem),
                ScraperName = scraperName,
                VerifyImageDownloads = verifyImageDownloads,
                ElementsToScrape = elementsToScrape,
                SystemID = scraperConfig.GetScraperSystemId(scraperName, currentSystem),
                SSLanguage = scraperConfig.GetScraperLanguageCode(scraperName),
                SSRegions = regions,
                ImageSource = ResolveSource(scraperConfig, scraperName, nameof(ScraperParameters.ImageSource), currentSystem),
                MarqueeSource = ResolveSource(scraperConfig, scraperName, nameof(ScraperParameters.MarqueeSource), currentSystem),
                ThumbnailSource = ResolveSource(scraperConfig, scraperName, nameof(ScraperParameters.ThumbnailSource), currentSystem),
                CartridgeSource = ResolveSource(scraperConfig, scraperName, nameof(ScraperParameters.CartridgeSource), currentSystem),
                VideoSource = ResolveSource(scraperConfig, scraperName, nameof(ScraperParameters.VideoSource), currentSystem),
                BoxArtSource = ResolveSource(scraperConfig, scraperName, nameof(ScraperParameters.BoxArtSource), currentSystem),
                MixSource = ResolveSource(scraperConfig, scraperName, nameof(ScraperParameters.MixSource), currentSystem),
                WheelSource = ResolveSource(scraperConfig, scraperName, nameof(ScraperParameters.WheelSource), currentSystem),
                CacheFolder = Path.Combine(AppContext.BaseDirectory, "cache", scraperName, currentSystem),
                ScrapeNamesLanguageFirst = scraperConfig.GetScraperBoolSetting(scraperName, "NamesLanguageFirst"),
                ScrapeMediaRegionFirst = scraperConfig.GetScraperBoolSetting(scraperName, "MediaRegionFirst"),
                ScrapeAnyMedia = scraperConfig.GetScraperBoolSetting(scraperName, "AnyMedia"),
                ScrapeEnglishGenreOnly = scraperConfig.GetScraperBoolSetting(scraperName, "GenreEnglish"),
                RemoveZzzNotGamePrefix = scraperConfig.GetScraperBoolSetting(scraperName, "RemoveZzzNotGamePrefix"),



                MediaPaths = availableMedia
                    .Where(m => m.MediaEnabled)
                    .ToDictionary(
                        m => m.Type,
                        m => m.FolderPath,
                        StringComparer.OrdinalIgnoreCase),

                MediaSuffixes = profile.MediaFilenamesUseSuffixes
                    ? availableMedia
                        .Where(m => m.MediaEnabled)
                        .ToDictionary(
                            m => m.Type,
                            m => (m.Suffix, m.IsSuffixEnabled),
                            StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, (string Suffix, bool IsSuffixEnabled)>(StringComparer.OrdinalIgnoreCase)
            };

            return parameters;
        }

        #endregion

        #region Private Methods

        private static string ResolveSource(ScraperConfigService scraperConfig, string scraperName, string sectionName, string currentSystem)
        {
            string savedDisplayName = scraperConfig.GetScraperSourceSetting(scraperName, sectionName, currentSystem);
            if (!string.IsNullOrEmpty(savedDisplayName))
            {
                var sources = scraperConfig.GetScraperSources(scraperName, sectionName);
                if (sources.TryGetValue(savedDisplayName, out var apiValue) && !string.IsNullOrEmpty(apiValue))
                    return apiValue;
            }
            return scraperConfig.GetScraperDefaultSource(scraperName, sectionName);
        }

        private async Task<(bool Success, string? AccessToken)> AuthenticateEmuMoviesAsync()
        {
            var creds = CredentialHelper.GetCredentials(ScraperRegistry.EmuMovies.Name);
            if (string.IsNullOrEmpty(creds.UserName))
            {
                Log($"{ScraperRegistry.EmuMovies.Name} credentials have not been configured yet.");
                return (false, null);
            }

            Log("Verifying EmuMovies credentials...");
            var (success, accessToken, error) = await CreateEmuMovies().AuthenticateAsync(creds.UserName, creds.Password);
            if (!success) Log(error);
            return (success, success ? accessToken : null);
        }

        private async Task<(bool Success, int MaxThreads)> AuthenticateScreenScraperAsync()
        {
            var creds = CredentialHelper.GetCredentials(ScraperRegistry.ScreenScraper.Name);
            if (string.IsNullOrEmpty(creds.UserName))
            {
                Log($"{ScraperRegistry.ScreenScraper.Name} credentials have not been configured yet.");
                return (false, 0);
            }

            Log("Verifying ScreenScraper credentials...");
            var (success, maxThreads, error) = await CreateScreenScraper().AuthenticateAsync(creds.UserName, creds.Password);
            if (!success) Log(error);
            return (success, success ? maxThreads : 0);
        }

        private void LogScreenScraperConfiguration(string scraperName)
        {
            var scraperConfig = ScraperConfigService.Instance;
            var language = scraperConfig.GetScraperOptionSetting(scraperName, "Language");
            var primaryRegion = scraperConfig.GetScraperOptionSetting(scraperName, "PrimaryRegion");
            var fallbackJson = scraperConfig.GetScraperOptionSetting(scraperName, "RegionFallback");

            if (!string.IsNullOrEmpty(language))
                Log($"Language: {language}");
            if (!string.IsNullOrEmpty(primaryRegion))
                Log($"Primary region: {primaryRegion}");

            if (!string.IsNullOrEmpty(fallbackJson))
            {
                try
                {
                    var fallback = JsonSerializer.Deserialize<List<string>>(fallbackJson);
                    if (fallback?.Count > 0)
                        Log($"Region fallback: {string.Join(", ", fallback)}");
                }
                catch { /* Malformed fallback JSON — skip */ }
            }
        }

        #endregion
    }
}