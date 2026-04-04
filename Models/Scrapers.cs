using System;
using System.Collections.Generic;
using System.Linq;

namespace Gamelist_Manager.Models
{
    public class ScraperConfig
    {
        public string Name { get; init; } = string.Empty;
        public bool RequiresAuthentication { get; init; }
        public bool SupportsBatchProcessing { get; init; }
        public bool ArcadeOnly { get; init; }
        public int DefaultThreads { get; init; } = 1;
    }

    public static class ScraperRegistry
    {
        public static readonly ScraperConfig ArcadeDB = new()
        {
            Name = "ArcadeDB",
            RequiresAuthentication = false,
            SupportsBatchProcessing = true,
            ArcadeOnly = true,
            DefaultThreads = 1
        };

        public static readonly ScraperConfig EmuMovies = new()
        {
            Name = "EmuMovies",
            RequiresAuthentication = true,
            SupportsBatchProcessing = false,
            ArcadeOnly = false,
            DefaultThreads = 2
        };

        public static readonly ScraperConfig ScreenScraper = new()
        {
            Name = "ScreenScraper",
            RequiresAuthentication = true,
            SupportsBatchProcessing = false,
            ArcadeOnly = false,
            DefaultThreads = 1
        };

        public static IReadOnlyList<ScraperConfig> All => [ArcadeDB, EmuMovies, ScreenScraper];

        public static ScraperConfig? Find(string name) =>
            All.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}