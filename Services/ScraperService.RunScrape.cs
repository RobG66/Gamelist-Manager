using Gamelist_Manager.Classes.Api;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.Services
{
    internal partial class ScraperService
    {
        #region Public Methods

        public async Task<bool> RunScrapeAsync(
            ScraperParameters baseParameters,
            ScraperProperties scraperProperties,
            IReadOnlyList<GameMetadataRow> rows,
            int maxBatch,
            ScrapingCallbacks callbacks,
            CancellationToken cancellationToken)
        {
            if (ScraperRegistry.Find(scraperProperties.ScraperName)?.SupportsBatchProcessing == true
                && scraperProperties.BatchProcessing && rows.Count >= BatchProcessingMinimum)
            {
                baseParameters.ScrapeByCache = true;
                await GetItemsInBatchModeAsync(
                    baseParameters, maxBatch, rows,
                    baseParameters.CacheFolder ?? string.Empty, cancellationToken);
            }

            BuildExistingMediaCache(baseParameters, cancellationToken);

            int maxConcurrency = scraperProperties.MaxConcurrency;
            if (maxConcurrency == 0)
            {
                Log("Max concurrency is 0, aborting.", LogLevel.Error);
                return false;
            }

            using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

            var tasks = new List<Task>();
            int doneCount = 0;
            int totalCount = rows.Count;
            bool completed = false;

            try
            {
                foreach (var row in rows)
                {
                    // Stagger task starts to avoid hitting rate limits immediately and to give UI time to update progress
                    if (doneCount < maxConcurrency)
                        await Task.Delay(100, cancellationToken);

                    await semaphore.WaitAsync(cancellationToken);
                    
                    int current = Interlocked.Increment(ref doneCount);
                    string romPath = row.GetValue(MetaDataKeys.path)?.ToString() ?? string.Empty;
                    callbacks.OnProgress(current, totalCount, Path.GetFileName(romPath));

                  
                    tasks.Add(Task.Run(() => ProcessRowAsync(
                        row, baseParameters, scraperProperties, callbacks, semaphore, cancellationToken), cancellationToken));
                }

                completed = true;
            }
            catch (OperationCanceledException)
            {
                Log("Scraping cancelled.", LogLevel.Warning);
            }
            finally
            {
                try { await Task.WhenAll(tasks); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Log($"Unexpected error during task completion: {ex.Message}", LogLevel.Error); }
            }

            return completed;
        }

        #endregion

        #region Private Methods

        private async Task ProcessRowAsync(
            GameMetadataRow row,
            ScraperParameters baseParameters,
            ScraperProperties scraperProperties,
            ScrapingCallbacks callbacks,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                string romName = Path.GetFileNameWithoutExtension(
                    row.GetValue(MetaDataKeys.path)?.ToString() ?? string.Empty);
                
                for (int attempt = 0; attempt <= TimeoutRetryCount; attempt++)
                {
                    try
                    {
                        var (success, data) = await ScrapeGameAsync(
                            row, baseParameters, scraperProperties, scraperProperties.ScraperName,
                            callbacks.OnLimitUpdate, callbacks.OnQuotaExceeded, cancellationToken);

                        if (success || data.Data.ContainsKey(nameof(MetaDataKeys.region)) || data.Data.ContainsKey(nameof(MetaDataKeys.lang)))
                        {
                            await SaveScrapedDataAsync(row, data, baseParameters);
                            callbacks.OnDataChanged?.Invoke();
                        }

                        break;
                    }
                    catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && IsRetryableNetworkError(ex))
                    {
                        if (attempt < TimeoutRetryCount)
                        {
                            double delaySecs = Math.Min(
                                TimeoutRetryBaseDelaySeconds * Math.Pow(2, attempt),
                                TimeoutRetryMaxDelaySeconds);
                            Log($"Timeout for '{romName}', retry {attempt + 1}/{TimeoutRetryCount} in {delaySecs:0}s...", LogLevel.Warning);
                            await Task.Delay(TimeSpan.FromSeconds(delaySecs), cancellationToken);
                        }
                        else
                        {
                            Log($"Skipping '{romName}' after {TimeoutRetryCount} retries.", LogLevel.Error);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Log($"Error: {ex.Message}", LogLevel.Error); }
            finally { semaphore.Release(); }
        }

        private static bool IsRetryableNetworkError(OperationCanceledException ex) =>
            ex.InnerException is TimeoutException or IOException;

        #endregion
    }
}
