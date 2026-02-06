namespace GamelistManager.classes.gamelist
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
        family,
        genreIds,
        arcadesystemname,
        kidgame,
        crc32,
        md5,
        cheevosHash,
        cheevosId,
        scraperId,
        image,
        marquee,
        thumbnail,
        boxback,
        boxart,
        fanart,
        map,
        bezel,
        cartridge,
        titleshot,
        video,
        music,
        wheel,
        manual,
        magazine,
        mix      
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
        public List<string> Scrapers { get; set; } = []; // Property to store scraper names
        public bool Viewable { get; set; } = true; // Property to determine if metadata is viewable
        public bool AlwaysVisible { get; set; } = false; // Property to determine if metadata is always visible
        public bool editible { get; set; } = false; // Property to determine if metadata is editable
    }

    public static class GamelistMetaData
    {
        private static readonly Dictionary<MetaDataKeys, MetaDataDecl> metaDataDictionary;
        private static readonly Dictionary<MetaDataKeys, object> metadataValues; // Store metadata values here


        static GamelistMetaData()
        {
            var gameDecls = new List<MetaDataDecl>
            {
                new() { Key = MetaDataKeys.hidden, Type = "hidden", Name = "Hidden", DataType = MetaDataType.Bool, Viewable = true, AlwaysVisible = true, editible = false },
                new() { Key = MetaDataKeys.favorite, Type = "favorite", Name = "Favorite", DataType = MetaDataType.Bool, Viewable = true, editible = false },
                new() { Key = MetaDataKeys.path, Type = "path", Name = "Rom Path", DataType = MetaDataType.String, Scrapers = ["ArcadeDB", "ScreenScraper", "EmuMovies"], Viewable = true, AlwaysVisible = true, editible = false },
                new() { Key = MetaDataKeys.id, Type = "id", Name = "Game Id", DataType = MetaDataType.String, Scrapers = ["ScreenScraper"], Viewable = true, editible = true },
                new() { Key = MetaDataKeys.name, Type = "name", Name = "Name", DataType = MetaDataType.String, Scrapers = ["ArcadeDB", "ScreenScraper"], Viewable = true, AlwaysVisible = true, editible = true },
                new() { Key = MetaDataKeys.genre, Type = "genre", Name = "Genre", DataType = MetaDataType.String, Scrapers = ["ArcadeDB", "ScreenScraper"], Viewable = true, editible = true },
                new() { Key = MetaDataKeys.releasedate, Type = "releasedate", Name = "Release Date", DataType = MetaDataType.String, Scrapers = ["ArcadeDB", "ScreenScraper"], Viewable = true, editible = true },
                new() { Key = MetaDataKeys.players, Type = "players", Name = "Players", DataType = MetaDataType.String, Scrapers = ["ArcadeDB", "ScreenScraper"], Viewable = true, editible = true },
                new() { Key = MetaDataKeys.rating, Type = "rating", Name = "Rating", DataType = MetaDataType.String, Scrapers = ["ArcadeDB", "ScreenScraper"], Viewable = true, editible = true },
                new() { Key = MetaDataKeys.lang, Type = "lang", Name = "Language", DataType = MetaDataType.String, Scrapers = ["ScreenScraper", "ArcadeDB"], Viewable = true, editible = true },
                new() { Key = MetaDataKeys.region, Type = "region", Name = "Region", DataType = MetaDataType.String, Scrapers = ["ScreenScraper", "ArcadeDB"], Viewable = true, editible = true },
                new() { Key = MetaDataKeys.publisher, Type = "publisher", Name = "Publisher", DataType = MetaDataType.String, Scrapers = ["ArcadeDB", "ScreenScraper"], Viewable = true, editible = true },
                new() { Key = MetaDataKeys.developer, Type = "developer", Name = "Developer", DataType = MetaDataType.String, Scrapers = ["ScreenScraper"], Viewable = true, editible = true },
                new() { Key = MetaDataKeys.playcount, Type = "playcount", Name = "Play Count", DataType = MetaDataType.String, Viewable = true, editible = false },
                new() { Key = MetaDataKeys.gametime, Type = "gametime", Name = "Game Time", DataType = MetaDataType.String, Viewable = true, editible = false },
                new() { Key = MetaDataKeys.lastplayed, Type = "lastplayed", Name = "Last Played", DataType = MetaDataType.String, Viewable = true, editible = false },
                new() { Key = MetaDataKeys.desc, Type = "desc", Name = "Description", DataType = MetaDataType.String, Scrapers = ["ArcadeDB", "ScreenScraper"], Viewable = true, editible = false },
                new() { Key = MetaDataKeys.family, Type = "family", Name = "Family", DataType = MetaDataType.String, Scrapers = ["ScreenScraper"], Viewable = true, editible = true },
                new() { Key = MetaDataKeys.arcadesystemname, Type = "arcadesystemname", Name = "Arcade System Name", DataType = MetaDataType.String, Scrapers = ["ScreenScraper"], Viewable = true, editible = true },
                new() { Key = MetaDataKeys.image, Type = "image", Name = "Image", DataType = MetaDataType.Image, Scrapers = ["ArcadeDB", "ScreenScraper", "EmuMovies"], Viewable = true, editible = false },
                new() { Key = MetaDataKeys.marquee, Type = "marquee", Name = "Marquee", DataType = MetaDataType.Image, Scrapers = ["ArcadeDB", "ScreenScraper", "EmuMovies"], Viewable = true, editible = false },
                new() { Key = MetaDataKeys.thumbnail, Type = "thumbnail", Name = "Thumbnail", DataType = MetaDataType.Image, Scrapers = ["ArcadeDB", "ScreenScraper", "EmuMovies"], Viewable = true, editible = false },
                new() { Key = MetaDataKeys.boxback, Type = "boxback", Name = "Box Back", DataType = MetaDataType.Image, Scrapers = ["ScreenScraper", "EmuMovies"], Viewable = true, editible = false },
                new() { Key = MetaDataKeys.wheel, Type = "wheel", Name = "Wheel", DataType = MetaDataType.Image, Scrapers = ["ArcadeDB", "ScreenScraper", "EmuMovies"], Viewable = true, editible = false },
                new() { Key = MetaDataKeys.boxart, Type = "boxart", Name = "Box Art", DataType = MetaDataType.Image, Scrapers = ["ScreenScraper", "EmuMovies"], Viewable = true, editible = false },
                new() { Key = MetaDataKeys.fanart, Type = "fanart", Name = "Fan Art", DataType = MetaDataType.Image, Scrapers = ["ScreenScraper", "EmuMovies"], Viewable = true, editible = false },
                new() { Key = MetaDataKeys.map, Type = "map", Name = "Map", DataType = MetaDataType.Image, Scrapers = ["ScreenScraper", "EmuMovies"], Viewable = true, editible = false },
                new() { Key = MetaDataKeys.bezel, Type = "bezel", Name = "Bezel", DataType = MetaDataType.Image, Scrapers = ["ScreenScraper", "EmuMovies", "ArcadeDB"], Viewable = true, editible = false },
                new() { Key = MetaDataKeys.cartridge, Type = "cartridge", Name = "Cartridge", DataType = MetaDataType.Image, Scrapers = ["ScreenScraper", "EmuMovies"], Viewable = true, editible = false },
                new() { Key = MetaDataKeys.titleshot, Type = "titleshot", Name = "Titleshot", DataType = MetaDataType.Image, Scrapers = ["ArcadeDB", "ScreenScraper", "EmuMovies"], Viewable = true, editible = false },
                new() { Key = MetaDataKeys.video, Type = "video", Name = "Video", DataType = MetaDataType.Video, Scrapers = ["ArcadeDB", "ScreenScraper", "EmuMovies"], Viewable = true, editible = false },
                new() { Key = MetaDataKeys.music, Type = "music", Name = "Music", DataType = MetaDataType.Music, Scrapers = ["EmuMovies"], Viewable = true, editible = false },
                new() { Key = MetaDataKeys.manual, Type = "manual", Name = "Manual", DataType = MetaDataType.Document, Scrapers = ["ScreenScraper", "EmuMovies", "ArcadeDB"], Viewable = true, editible = false },
                new() { Key = MetaDataKeys.magazine, Type = "magazine", Name = "Magazine", DataType = MetaDataType.Image, Scrapers = ["ScreenScraper", "EmuMovies"], Viewable = true, editible = false },
                new() { Key = MetaDataKeys.mix, Type = "mix", Name = "Mix", DataType = MetaDataType.Image, Scrapers = ["ScreenScraper", "EmuMovies"], Viewable = true, editible = false },
                //new MetaDataDecl { Key = MetaDataKeys.genreIds, Type = "genreIds", Name = "Genre Ids", DataType = MetaDataType.String, Scrapers = new List<string>{ "ScreenScraper" }, Viewable = true, editible = true },
                //new MetaDataDecl { Key = MetaDataKeys.kidgame, Type = "kidgame", Name = "Kid Game", DataType = MetaDataType.Bool, Viewable = false, editible = false },
                //new MetaDataDecl { Key = MetaDataKeys.crc32, Type = "crc32", Name = "Crc32", DataType = MetaDataType.String, Viewable = false, editible = false },
                //new MetaDataDecl { Key = MetaDataKeys.md5, Type = "md5", Name = "Md5", DataType = MetaDataType.String, Viewable = false, editible = false },
                //new MetaDataDecl { Key = MetaDataKeys.cheevosHash, Type = "cheevosHash", Name = "Cheevos Hash", DataType = MetaDataType.String, Viewable = false, editible = false },
                //new MetaDataDecl { Key = MetaDataKeys.cheevosId, Type = "cheevosId", Name = "Cheevos Id", DataType = MetaDataType.String, Viewable = false, editible = false },
                //new MetaDataDecl { Key = MetaDataKeys.scraperId, Type = "scraperId", Name = "Scraper Id", DataType = MetaDataType.String, Viewable = false, editible = false },
            };

            // Initialize the dictionary
            metaDataDictionary = gameDecls.ToDictionary(
                decl => decl.Key,
                decl => decl
            );

            metadataValues = [];
        }

        public static Dictionary<string, string> NameToTypeMap => metaDataDictionary
           .ToDictionary(
               kvp => kvp.Value.Type,  // Element type
               kvp => kvp.Value.Name   // Dataset column type
           );

        public static List<string> GetScraperElements(string scraperName)
        {
            return metaDataDictionary.Values
                .Where(decl => decl.Scrapers.Contains(scraperName))
                .Select(decl => decl.Type)
                .ToList();
        }

        public static Dictionary<MetaDataKeys, MetaDataDecl> GetMetaDataDictionary()
        {
            return metaDataDictionary;
        }

        // Conversion methods below

   
        public static string GetMetadataNameByType(string type)
        {
            var metaDataDecl = metaDataDictionary
                .Values
                .FirstOrDefault(decl => decl.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

            // Return the DataType (enum) type in lowercase
            return metaDataDecl?.Name ?? string.Empty;
        }

        public static string GetMetadataDataTypeByType(string type)
        {
            var metaDataDecl = metaDataDictionary
                .Values
                .FirstOrDefault(decl => decl.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

            // Return the DataType (enum) type in lowercase
            return metaDataDecl?.DataType.ToString() ?? string.Empty;
        }

        public static string GetMetadataTypeByName(string name)
        {
            var metaDataDecl = metaDataDictionary
                .Values
                .FirstOrDefault(decl => decl.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            return metaDataDecl?.Type ?? string.Empty;
        }
    }
}
