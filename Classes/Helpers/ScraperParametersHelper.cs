using Gamelist_Manager.Models;
using System.Collections.Generic;
using System.Linq;

namespace Gamelist_Manager.Classes.Helpers
{
    public static class ScraperParametersHelper
    {
        public static ScraperParameters Clone(this ScraperParameters source)
        {
            var clone = source.ShallowClone();

            clone.SSRegions = source.SSRegions != null ? new List<string>(source.SSRegions) : null;
            clone.ElementsToScrape = source.ElementsToScrape != null ? new List<string>(source.ElementsToScrape) : null;
            clone.MediaPaths = source.MediaPaths != null ? new Dictionary<string, string>(source.MediaPaths) : null;
            clone.MediaSuffixes = source.MediaSuffixes != null ? new Dictionary<string, (string, bool)>(source.MediaSuffixes) : null;
            clone.ExistingMediaFiles = source.ExistingMediaFiles?.ToDictionary(
                kvp => kvp.Key,
                kvp => new HashSet<string>(kvp.Value, kvp.Value.Comparer));
            clone.EmuMoviesMediaLists = source.EmuMoviesMediaLists.ToDictionary(
                kvp => kvp.Key,
                kvp => new List<string>(kvp.Value));

            return clone;
        }

        public static void BuildMetaLookup(this ScraperParameters parameters)
        {
            if (parameters.ElementsToScrape == null)
                return;

            parameters.MetaLookup = parameters.ElementsToScrape.ToDictionary(
                item => item,
                item => (
                    GamelistMetaData.GetMetadataDataTypeByType(item),
                    GamelistMetaData.GetMetadataNameByType(item)
                )
            );
        }
    }
}