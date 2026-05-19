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

        public async Task GetEmuMoviesMediaListsAsync(
            string systemId,
            ScraperParameters baseParameters,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await CreateMediaCache().PopulateMediaListsAsync(systemId, baseParameters, msg => Log(msg), cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                Log(ex.Message);
            }
        }

        #endregion

        #region Private Methods

        private void BuildExistingMediaCache(ScraperParameters parameters, CancellationToken cancellationToken)
        {
            if (parameters.MediaPaths == null)
                return;

            try
            {
                var folderCache = new Dictionary<string, HashSet<string>>(FilePathHelper.PathComparer);
                foreach (var (_, folder) in parameters.MediaPaths)
                {
                    if (string.IsNullOrEmpty(folder)) continue;
                    if (folderCache.ContainsKey(folder)) continue;

                    var fileSet = new HashSet<string>(FilePathHelper.PathComparer);
                    if (Directory.Exists(folder))
                        foreach (var file in Directory.EnumerateFiles(folder))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            fileSet.Add(Path.GetFileName(file));
                        }

                    folderCache[folder] = fileSet;
                }

                var mediaCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var (mediaType, folder) in parameters.MediaPaths)
                {
                    if (string.IsNullOrEmpty(folder)) continue;
                    if (folderCache.TryGetValue(folder, out var fileSet))
                        mediaCache[mediaType] = fileSet;
                }

                parameters.ExistingMediaFiles = mediaCache;
            }
            catch (OperationCanceledException)
            {
                parameters.ExistingMediaFiles = null;
                throw;
            }
        }

        #endregion
    }
}