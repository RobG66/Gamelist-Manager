using System.Collections.Generic;

namespace Gamelist_Manager.Classes.Api
{
    public class ScraperProperties
    {
        public string ScraperName { get; set; } = "";
        public int LogVerbosity { get; set; }
        public int MaxConcurrency { get; set; } = 1;
        public bool BatchProcessing { get; set; }
        public Dictionary<string, List<string>> EmuMoviesMediaLists { get; set; } = new();
    }
}