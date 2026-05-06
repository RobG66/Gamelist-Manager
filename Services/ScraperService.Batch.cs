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

        public async Task<int> GetItemsInBatchModeAsync(
            ScraperParameters parameters,
            int batchMaximum,
            IReadOnlyList<GameMetadataRow> rows,
            string cacheFolder,
            CancellationToken cancellationToken)
        {
            try
            {
                Log("Starting batch API fetch...");

                var cacheFilesSet = BuildCacheFileSet(cacheFolder, cancellationToken);

                var itemsToFetch = new List<string>();
                foreach (var row in rows)
                {
                    string romPath = row.GetValue(MetaDataKeys.path)?.ToString() ?? string.Empty;
                    if (string.IsNullOrEmpty(romPath)) continue;
                    string name = Path.GetFileNameWithoutExtension(romPath);
                    if (!cacheFilesSet.Contains(name + ".json"))
                        itemsToFetch.Add(name);
                }

                int alreadyCached = rows.Count - itemsToFetch.Count;
                if (alreadyCached > 0) Log($"{alreadyCached} items already in cache");

                if (itemsToFetch.Count == 0)
                {
                    Log("All games already cached!");
                    return 0;
                }

                Log($"Fetching {itemsToFetch.Count} games from API in batches...");

                int totalBatches = (int)Math.Ceiling((double)itemsToFetch.Count / batchMaximum);
                int currentBatch = 0;
                int totalFetched = 0;

                for (int i = 0; i < itemsToFetch.Count; i += batchMaximum)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentBatch++;
                    var batch = itemsToFetch.GetRange(i, Math.Min(batchMaximum, itemsToFetch.Count - i));

                    try
                    {
                        var batchResults = await CreateArcadeDb().ScrapeArcadeDBBatchAsync(batch, parameters, cancellationToken);
                        int found = batchResults.Count;
                        int notFound = batch.Count - found;
                        totalFetched += found;

                        Log(notFound > 0
                            ? $"Batch {currentBatch}/{totalBatches}: Fetched {found}, not found {notFound}"
                            : $"Batch {currentBatch}/{totalBatches}: Fetched {found}");
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        Log($"Batch {currentBatch}/{totalBatches} error: {ex.Message}");
                    }
                }

                Log($"Batch fetch complete: {totalFetched} games downloaded to cache");
                return totalFetched;
            }
            catch (OperationCanceledException)
            {
                Log("Batch processing cancelled");
                throw;
            }
        }

        #endregion

        #region Private Methods

        private static HashSet<string> BuildCacheFileSet(string cacheFolder, CancellationToken cancellationToken)
        {
            var set = new HashSet<string>(FilePathHelper.PathComparer);
            if (!string.IsNullOrEmpty(cacheFolder) && Directory.Exists(cacheFolder))
                foreach (var file in Directory.EnumerateFiles(cacheFolder, "*.json"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    set.Add(Path.GetFileName(file));
                }
            return set;
        }

        #endregion
    }
}
