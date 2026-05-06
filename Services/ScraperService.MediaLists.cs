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

        public async Task GetEmuMoviesMediaListsAsync(string systemId, ScraperProperties scraperProperties, CancellationToken cancellationToken = default)
        {
            try
            {
                await CreateMediaCache().PopulateMediaListsAsync(systemId, scraperProperties, msg => Log(msg), cancellationToken);
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
            if (parameters.MediaPaths == null || string.IsNullOrEmpty(parameters.ParentFolderPath))
                return;

            try
            {
                // Build a per-folder file set, deduplicating folders that appear multiple times.
                var folderCache = new Dictionary<string, HashSet<string>>(FilePathHelper.PathComparer);
                foreach (var (_, folder) in parameters.MediaPaths)
                {
                    if (string.IsNullOrEmpty(folder)) continue;
                    string absolute = FilePathHelper.GamelistPathToFullPath(folder, parameters.ParentFolderPath);
                    if (folderCache.ContainsKey(absolute)) continue;

                    var fileSet = new HashSet<string>(FilePathHelper.PathComparer);
                    if (Directory.Exists(absolute))
                        foreach (var file in Directory.EnumerateFiles(absolute))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            fileSet.Add(Path.GetFileName(file));
                        }

                    folderCache[absolute] = fileSet;
                }

                // Map each media type to its folder's file set.
                var mediaCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var (mediaType, folder) in parameters.MediaPaths)
                {
                    if (string.IsNullOrEmpty(folder)) continue;
                    string absolute = FilePathHelper.GamelistPathToFullPath(folder, parameters.ParentFolderPath);
                    if (folderCache.TryGetValue(absolute, out var fileSet))
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
