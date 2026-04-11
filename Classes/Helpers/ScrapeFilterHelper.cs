using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace Gamelist_Manager.Classes.Helpers
{
    // Determines which elements actually need scraping for a given row, so API calls and
    // file writes are skipped for data the user already has or has chosen not to overwrite.
    public static class ScrapeFilterHelper
    {
        public static List<string> FilterElementsToScrape(GameMetadataRow row, ScraperParameters baseParameters)
        {
            var itemsToScrape = new List<string>();

            foreach (var item in baseParameters.ElementsToScrape!)
            {
                // Non-metadata items (region, lang, etc.) — always include
                if (!Enum.TryParse<MetaDataKeys>(item, true, out var key))
                {
                    itemsToScrape.Add(item);
                    continue;
                }

                var (type, _) = baseParameters.MetaLookup.TryGetValue(item, out var meta) ? meta : ("String", string.Empty);
                string? value = row.GetValue(key)?.ToString();

                // No existing value — always scrape (covers Music, which is never stored in the gamelist)
                if (string.IsNullOrEmpty(value))
                {
                    itemsToScrape.Add(item);
                    continue;
                }

                // Media with an existing gamelist path — skip if the file is on disk and not overwriting
                if (type is "Image" or "Document" or "Video" &&
                    baseParameters.MediaPaths != null &&
                    baseParameters.MediaPaths.TryGetValue(item, out string? folder) &&
                    !string.IsNullOrEmpty(folder))
                {
                    if (baseParameters.OverwriteMedia)
                    {
                        itemsToScrape.Add(item);
                    }
                    else
                    {
                        string fileName = Path.GetFileName(value);
                        bool fileExists;

                        if (baseParameters.ExistingMediaFiles != null &&
                            baseParameters.ExistingMediaFiles.TryGetValue(item, out var fileSet))
                        {
                            fileExists = fileSet.Contains(fileName);
                        }
                        else
                        {
                            string absoluteFolder = FilePathHelper.GamelistPathToFullPath(folder, baseParameters.ParentFolderPath!);
                            fileExists = File.Exists(Path.Combine(absoluteFolder, fileName));
                        }

                        if (!fileExists)
                            itemsToScrape.Add(item);
                    }

                    continue;
                }

                // Name — only scrape if overwrite name is enabled
                if (item == "name")
                {
                    if (baseParameters.OverwriteName)
                        itemsToScrape.Add(item);
                    continue;
                }

                // Remaining string metadata — only scrape if overwrite metadata is enabled
                if (type == "String" && baseParameters.OverwriteMetadata)
                    itemsToScrape.Add(item);
            }

            return itemsToScrape;
        }
    }
}
