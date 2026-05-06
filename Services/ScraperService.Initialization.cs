using Gamelist_Manager.Classes.Api;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.IO;
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
            ScraperProperties scraperProperties,
            string currentSystem,
            CancellationToken cancellationToken = default)
        {
            string scraperName = scraperProperties.ScraperName;

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

                scraperProperties.MaxConcurrency = maxThreads;

                var creds = CredentialHelper.GetCredentials(ScraperRegistry.ScreenScraper.Name);
                baseParameters.UserID = creds.UserName;
                baseParameters.UserPassword = creds.Password;

                LogScreenScraperConfiguration(scraperName);
            }
            else if (scraperName == ScraperRegistry.EmuMovies.Name)
            {
                var (success, accessToken) = await AuthenticateEmuMoviesAsync();
                if (!success) return false;

                scraperProperties.MaxConcurrency = ScraperRegistry.EmuMovies.DefaultThreads;
                baseParameters.UserAccessToken = accessToken;

                await GetEmuMoviesMediaListsAsync(baseParameters.SystemID ?? string.Empty, scraperProperties, cancellationToken);
            }

            return true;
        }

        #endregion

        #region Private Methods

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
            var language = scraperConfig.GetScraperSourceSetting(scraperName, "Language");
            var primaryRegion = scraperConfig.GetScraperSourceSetting(scraperName, "PrimaryRegion");
            var fallbackJson = scraperConfig.GetScraperSourceSetting(scraperName, "RegionFallback");

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
