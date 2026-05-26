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
        public static List<string> FilterElementsToScrape(GameMetadataRow row, ScraperParameters parameters)
        {
            var itemsToScrape = new List<string>();

            foreach (var item in parameters.ElementsToScrape!)
            {
                if (!Enum.TryParse<MetaDataKeys>(item, true, out var key))
                {
                    continue;
                }

                string? value = row.GetValue(key)?.ToString();

                // No existing value — always scrape
                if (string.IsNullOrEmpty(value))
                {
                    itemsToScrape.Add(item);
                    continue;
                }

                if (!parameters.MetaLookup!.TryGetValue(item, out var meta))
                    continue;

                var (type, _) = meta;

                // Media with an existing gamelist path — skip if the file is on disk and not overwriting
                if (type is "Image" or "Document" or "Video" &&
                    parameters.MediaPaths != null &&
                    parameters.MediaPaths.TryGetValue(item, out string? folder) &&
                    !string.IsNullOrEmpty(folder))
                {
                    if (parameters.OverwriteMedia)
                    {
                        itemsToScrape.Add(item);
                    }
                    else
                    {
                        string fileName = Path.GetFileName(value);
                        bool fileExists;

                        if (parameters.ExistingMediaFiles != null &&
                            parameters.ExistingMediaFiles.TryGetValue(item, out var fileSet))
                        {
                            fileExists = fileSet.Contains(fileName);
                        }
                        else
                        {
                            fileExists = File.Exists(Path.Combine(folder, fileName));
                        }

                        if (!fileExists)
                            itemsToScrape.Add(item);
                    }

                    continue;
                }

                // Name — only scrape if overwrite name is enabled
                if (item == "name")
                {
                    if (parameters.OverwriteName)
                        itemsToScrape.Add(item);
                    continue;
                }

                // Remaining string metadata — only scrape if overwrite metadata is enabled
                if (type == "String" && parameters.OverwriteMetadata)
                    itemsToScrape.Add(item);
            }

            return itemsToScrape;
        }
    }
}