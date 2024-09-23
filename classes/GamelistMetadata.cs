namespace GamelistManager.classes
{
    public enum MetaDataKeys
    {
        hidden,
        favorite,
        path,
        id,
        name,
        genre,
        releasedate,
        players,
        rating,
        lang,
        region,
        publisher,
        developer,
        playcount,
        gametime,
        lastplayed,
        desc,
        image,
        marquee,
        thumbnail,
        boxback,
        fanart,
        map,
        bezel,
        cartridge,
        titleshot,
        video,
        music,
        manual,
        magazine,
        mix,
        family,
        genreIds,
        arcadesystemname,
        kidgame,
        crc32,
        md5,
        cheevosHash,
        cheevosId,
        scraperId
    }

    public enum MetaDataType
    {
        String,
        Bool,
        Image,
        Document,
        Video,
        Music
    }

    public class MetaDataDecl
    {
        public MetaDataKeys Key { get; set; }
        public string Name { get; set; } = string.Empty; // Default to empty string
        public string Type { get; set; } = string.Empty; // Default to empty string
        public MetaDataType DataType { get; set; }
        public List<string> Scrapers { get; set; } = new List<string>(); // Property to store scraper names
        public bool Viewable { get; set; } = true; // Property to determine if metadata is viewable
        public bool AlwaysVisible { get; set; } = false; // Property to determine if metadata is always visible
        public bool editible { get; set; } = false; // Property to determine if metadata is editable
    }

    public class MetaDataList
    {
        private Dictionary<MetaDataKeys, MetaDataDecl> metaDataDictionary;
        private Dictionary<MetaDataKeys, object> metadataValues; // Store metadata values here

        public MetaDataList()
        {
            var gameDecls = new List<MetaDataDecl>
            {
                new MetaDataDecl { Key = MetaDataKeys.hidden, Type = "hidden", Name = "Hidden", DataType = MetaDataType.Bool, Viewable = true, AlwaysVisible = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.favorite, Type = "favorite", Name = "Favorite", DataType = MetaDataType.Bool, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.path, Type = "path", Name = "Rom Path", DataType = MetaDataType.String, Scrapers = new List<string>{ "ArcadeDB", "ScreenScraper", "EmuMovies" }, Viewable = true, AlwaysVisible = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.id, Type = "id", Name = "Game Id", DataType = MetaDataType.String, Scrapers = new List<string>{ "ScreenScraper" }, Viewable = true, editible = true },
                new MetaDataDecl { Key = MetaDataKeys.name, Type = "name", Name = "Name", DataType = MetaDataType.String, Scrapers = new List<string>{ "ArcadeDB", "ScreenScraper" }, Viewable = true, AlwaysVisible = true, editible = true },
                new MetaDataDecl { Key = MetaDataKeys.genre, Type = "genre", Name = "Genre", DataType = MetaDataType.String, Scrapers = new List<string>{ "ArcadeDB", "ScreenScraper" }, Viewable = true, editible = true },
                new MetaDataDecl { Key = MetaDataKeys.releasedate, Type = "releasedate", Name = "Release Date", DataType = MetaDataType.String, Scrapers = new List<string>{ "ArcadeDB", "ScreenScraper" }, Viewable = true, editible = true },
                new MetaDataDecl { Key = MetaDataKeys.players, Type = "players", Name = "Players", DataType = MetaDataType.String, Scrapers = new List<string>{ "ArcadeDB", "ScreenScraper" }, Viewable = true, editible = true },
                new MetaDataDecl { Key = MetaDataKeys.rating, Type = "rating", Name = "Rating", DataType = MetaDataType.String, Scrapers = new List<string>{ "ArcadeDB", "ScreenScraper" }, Viewable = true, editible = true },
                new MetaDataDecl { Key = MetaDataKeys.lang, Type = "lang", Name = "Language", DataType = MetaDataType.String, Scrapers = new List<string>{ "ScreenScraper" }, Viewable = true, editible = true },
                new MetaDataDecl { Key = MetaDataKeys.region, Type = "region", Name = "Region", DataType = MetaDataType.String, Scrapers = new List<string>{ "ScreenScraper" }, Viewable = true, editible = true },
                new MetaDataDecl { Key = MetaDataKeys.publisher, Type = "publisher", Name = "Publisher", DataType = MetaDataType.String, Scrapers = new List<string>{ "ArcadeDB", "ScreenScraper" }, Viewable = true, editible = true },
                new MetaDataDecl { Key = MetaDataKeys.developer, Type = "developer", Name = "Developer", DataType = MetaDataType.String, Scrapers = new List<string>{ "ScreenScraper" }, Viewable = true, editible = true },
                new MetaDataDecl { Key = MetaDataKeys.playcount, Type = "playcount", Name = "Play Count", DataType = MetaDataType.String, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.gametime, Type = "gametime", Name = "Game Time", DataType = MetaDataType.String, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.lastplayed, Type = "lastplayed", Name = "Last Played", DataType = MetaDataType.String, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.desc, Type = "desc", Name = "Description", DataType = MetaDataType.String, Scrapers = new List<string>{ "ArcadeDB", "ScreenScraper" }, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.image, Type = "image", Name = "Image", DataType = MetaDataType.Image, Scrapers = new List<string>{ "ArcadeDB", "ScreenScraper", "EmuMovies" }, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.marquee, Type = "marquee", Name = "Logo", DataType = MetaDataType.Image, Scrapers = new List<string>{ "ArcadeDB", "ScreenScraper", "EmuMovies" }, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.thumbnail, Type = "thumbnail", Name = "Box", DataType = MetaDataType.Image, Scrapers = new List<string>{ "ArcadeDB", "ScreenScraper", "EmuMovies" }, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.boxback, Type = "boxback", Name = "Box Back", DataType = MetaDataType.Image, Scrapers = new List<string>{ "ScreenScraper", "EmuMovies" }, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.fanart, Type = "fanart", Name = "Fan Art", DataType = MetaDataType.Image, Scrapers = new List<string>{ "ScreenScraper", "EmuMovies" }, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.map, Type = "map", Name = "Map", DataType = MetaDataType.Image, Scrapers = new List<string>{ "ScreenScraper", "EmuMovies" }, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.bezel, Type = "bezel", Name = "Bezel (16:9)", DataType = MetaDataType.Image, Scrapers = new List<string>{ "ScreenScraper", "EmuMovies", "ArcadeDB" }, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.cartridge, Type = "cartridge", Name = "Cartridge", DataType = MetaDataType.Image, Scrapers = new List<string>{ "EmuMovies" }, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.titleshot, Type = "titleshot", Name = "Title Shot", DataType = MetaDataType.Image, Scrapers = new List<string>{ "ArcadeDB", "ScreenScraper", "EmuMovies" }, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.video, Type = "video", Name = "Video", DataType = MetaDataType.Video, Scrapers = new List<string>{ "ArcadeDB", "ScreenScraper", "EmuMovies" }, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.music, Type = "music", Name = "Music", DataType = MetaDataType.Music, Scrapers = new List<string>{ "EmuMovies" }, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.manual, Type = "manual", Name = "Manual", DataType = MetaDataType.Document, Scrapers = new List<string>{ "ScreenScraper", "EmuMovies", "ArcadeDB" }, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.magazine, Type = "magazine", Name = "Magazine", DataType = MetaDataType.Image, Scrapers = new List<string>{ "ScreenScraper", "EmuMovies" }, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.mix, Type = "mix", Name = "Mix", DataType = MetaDataType.Image, Scrapers = new List<string>{ "ScreenScraper", "EmuMovies" }, Viewable = true, editible = false },
                new MetaDataDecl { Key = MetaDataKeys.family, Type = "family", Name = "Family", DataType = MetaDataType.String, Scrapers = new List<string>{ "ScreenScraper" }, Viewable = true, editible = false },
                //new MetaDataDecl { Key = MetaDataKeys.genreIds, Type = "genreIds", Name = "Genre Ids", DataType = MetaDataType.String, Scrapers = new List<string>{ "ScreenScraper" }, Viewable = true, editible = true },
                new MetaDataDecl { Key = MetaDataKeys.arcadesystemname, Type = "arcadesystemname", Name = "Arcade System Name", DataType = MetaDataType.String, Scrapers = new List<string>{ "ArcadeDB", "ScreenScraper" }, Viewable = true, editible = true },
                //new MetaDataDecl { Key = MetaDataKeys.kidgame, Type = "kidgame", Name = "Kid Game", DataType = MetaDataType.Bool, Viewable = false, editible = false },
                //new MetaDataDecl { Key = MetaDataKeys.crc32, Type = "crc32", Name = "Crc32", DataType = MetaDataType.String, Viewable = false, editible = false },
                //new MetaDataDecl { Key = MetaDataKeys.md5, Type = "md5", Name = "Md5", DataType = MetaDataType.String, Viewable = false, editible = false },
                //new MetaDataDecl { Key = MetaDataKeys.cheevosHash, Type = "cheevosHash", Name = "Cheevos Hash", DataType = MetaDataType.String, Viewable = false, editible = false },
                //new MetaDataDecl { Key = MetaDataKeys.cheevosId, Type = "cheevosId", Name = "Cheevos Id", DataType = MetaDataType.String, Viewable = false, editible = false },
                //new MetaDataDecl { Key = MetaDataKeys.scraperId, Type = "scraperId", Name = "Scraper Id", DataType = MetaDataType.String, Viewable = false, editible = false },
            };

            // Initialize the dictionaries
            metaDataDictionary = gameDecls.ToDictionary(
                decl => decl.Key,
                decl => decl
            );

            metadataValues = new Dictionary<MetaDataKeys, object>();
        }

        public List<string> GetScraperElements(string scraperName)
        {
            return metaDataDictionary.Values
                .Where(decl => decl.Scrapers.Contains(scraperName))
                .Select(decl => decl.Type)
                .ToList();
        }

        public Dictionary<MetaDataKeys, MetaDataDecl> GetMetaDataDictionary()
        {
            return metaDataDictionary;
        }

        public object? GetMetadataValue(string keyName)
        {
            // Try to parse the string keyName to MetaDataKeys enum
            if (Enum.TryParse<MetaDataKeys>(keyName, true, out MetaDataKeys key))
            {
                // If the key is successfully parsed, try to get the value from metadataValues
                if (metadataValues.TryGetValue(key, out var value))
                {
                    return value;
                }
            }
            // Return null if the key is not found or cannot be parsed
            return null;
        }


        public void SetMetadataValue(MetaDataKeys key, object value)
        {
            if (metaDataDictionary.ContainsKey(key) && value != null)
            {
                var metadataDecl = metaDataDictionary[key];

                if (IsValueTypeCompatible(metadataDecl.DataType, value))
                {
                    metadataValues[key] = value;
                }
                else
                {
                    throw new ArgumentException("Value type is not compatible with the metadata type.");
                }
            }

        }

        private bool IsValueTypeCompatible(MetaDataType dataType, object value)
        {
            switch (dataType)
            {
                case MetaDataType.Music:
                    return value is string;
                case MetaDataType.String:
                    return value is string;
                case MetaDataType.Bool:
                    return value is bool;
                case MetaDataType.Image:
                    return value is string;
                case MetaDataType.Document:
                    return value is string;
                case MetaDataType.Video:
                    return value is string;
                default:
                    return false;
            }
        }
    }
}
