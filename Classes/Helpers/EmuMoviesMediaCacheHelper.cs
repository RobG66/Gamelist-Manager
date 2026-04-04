using Gamelist_Manager.Classes.Api;
using Gamelist_Manager.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.Classes.Helpers
{
    internal class EmuMoviesMediaCacheHelper
    {
        private static List<string> _mediaTypes = new();
        private static readonly ConcurrentDictionary<(string SystemId, string MediaType), List<string>> _mediaListCache = new();
        private static readonly SemaphoreSlim _fetchLock = new(1, 1);

        public async Task PopulateMediaListsAsync(API_EmuMovies api, string systemId, ScraperProperties scraperProperties, Action<string>? log = null, CancellationToken cancellationToken = default)
        {
            bool hasListsForSystem = _mediaTypes.Count > 0 &&
                _mediaTypes.TrueForAll(mt => _mediaListCache.ContainsKey((systemId, mt)));

            if (hasListsForSystem)
            {
                foreach (string mediaType in _mediaTypes)
                {
                    if (_mediaListCache.TryGetValue((systemId, mediaType), out var cachedList))
                        scraperProperties.EmuMoviesMediaLists[mediaType] = cachedList;
                }
                return;
            }

            // If WaitAsync is cancelled before acquiring, the semaphore is not held and
            // the finally block is not reached — so Release() is only called when acquired.
            await _fetchLock.WaitAsync(cancellationToken);
            bool downloading = false;
            try
            {
                // Re-check inside lock in case another thread populated while we waited
                bool stillNeeded = _mediaTypes.Count == 0 ||
                    !_mediaTypes.TrueForAll(mt => _mediaListCache.ContainsKey((systemId, mt)));

                if (!stillNeeded)
                    return;

                log?.Invoke("Downloading EmuMovies media lists...");
                downloading = true;

                var (success, mediaTypes, error) = await api.GetMediaTypesAsync(systemId, cancellationToken);
                if (!success)
                    throw new InvalidOperationException(error);

                _mediaTypes = mediaTypes;

                foreach (string mediaType in _mediaTypes)
                {
                    var key = (systemId, mediaType);
                    if (_mediaListCache.ContainsKey(key))
                    {
                        scraperProperties.EmuMoviesMediaLists[mediaType] = _mediaListCache[key];
                        continue;
                    }

                    var (listSuccess, mediaList, _) = await api.GetMediaListAsync(systemId, mediaType, cancellationToken);
                    if (!listSuccess)
                        mediaList = new List<string>();

                    _mediaListCache[key] = mediaList;
                    scraperProperties.EmuMoviesMediaLists[mediaType] = mediaList;
                }
            }
            catch (OperationCanceledException) when (downloading)
            {
                // Discard any partially-built static state so the next attempt starts clean.
                Clear();
                throw;
            }
            finally
            {
                _fetchLock.Release();
            }
        }

        public void Clear()
        {
            _mediaListCache.Clear();
            _mediaTypes = new();
        }
    }
}
