using System;
using System.Collections.Generic;
using System.Linq;

namespace Gamelist_Manager.Models
{
    // ReSharper disable InconsistentNaming
    // Lowercase enum names match the actual gamelist XML format
#pragma warning disable IDE1006 // Naming Styles
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
        mix,
        family,
        arcadesystemname,
        genreIds,
        kidgame,
        crc32,
        md5,
        cheevosHash,
        cheevosId,
        scraperId
    }
#pragma warning restore IDE1006 // Naming Styles
    // ReSharper restore InconsistentNaming

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
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public MetaDataType DataType { get; set; }
        public List<string> Scrapers { get; set; } = [];
        public bool Viewable { get; set; } = true;
        public bool AlwaysVisible { get; set; } = false;
        public bool DefaultVisible { get; set; } = false;
        public bool Editable { get; set; } = false;

        public bool IsMedia => DataType is not (MetaDataType.String or MetaDataType.Bool);

        // Media settings defaults (set during static initialization)
        public string DefaultPath { get; init; } = string.Empty;
        public bool DefaultEnabled { get; init; } = true;
        public string DefaultSuffix { get; init; } = string.Empty;

        // Runtime media settings (populated by LoadMediaSettings from INI)
        public string MediaFolderPath { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public string Suffix { get; set; } = string.Empty;
        public bool SfxEnabled { get; set; } = true;
    }

    public static class GamelistMetaData
    {
        private static readonly Dictionary<MetaDataKeys, MetaDataDecl> metaDataDictionary;
        private static readonly Dictionary<MetaDataKeys, object> metadataValues;
        private static readonly List<MetaDataDecl> mediaMetadataCache;
        private static readonly List<MetaDataDecl> columnDeclarationsCache;
        private static readonly List<MetaDataDecl> toggleableColumnCache;
        private static readonly List<MetaDataDecl> allMediaFolderCache; // All media folder types in declaration order (Image, Document, Video, Music)


        static GamelistMetaData()
        {
            var gameDecls = new List<MetaDataDecl>
            {
                new() { Key = MetaDataKeys.hidden, Type = "hidden", Name = "Hidden", PropertyName = nameof(GameMetadataRow.Hidden), DataType = MetaDataType.Bool, Viewable = true, AlwaysVisible = true, Editable = false },
                new() { Key = MetaDataKeys.favorite, Type = "favorite", Name = "Favorite", PropertyName = nameof(GameMetadataRow.Favorite), DataType = MetaDataType.Bool, Viewable = true, Editable = false },
                new() { Key = MetaDataKeys.path, Type = "path", Name = "Rom Path", PropertyName = nameof(GameMetadataRow.Path), DataType = MetaDataType.String, Scrapers = ["ArcadeDB", "ScreenScraper", "EmuMovies"], Viewable = true, AlwaysVisible = true, Editable = false },
                new() { Key = MetaDataKeys.id, Type = "id", Name = "Game Id", PropertyName = nameof(GameMetadataRow.Id), DataType = MetaDataType.String, Scrapers = ["ScreenScraper"], Viewable = true, Editable = true },
                new() { Key = MetaDataKeys.name, Type = "name", Name = "Name", PropertyName = nameof(GameMetadataRow.Name), DataType = MetaDataType.String, Scrapers = ["ArcadeDB", "ScreenScraper"], Viewable = true, AlwaysVisible = true, Editable = true },
                new() { Key = MetaDataKeys.genre, Type = "genre", Name = "Genre", PropertyName = nameof(GameMetadataRow.Genre), DataType = MetaDataType.String, Scrapers = ["ArcadeDB", "ScreenScraper"], Viewable = true, DefaultVisible = true, Editable = true },
                new() { Key = MetaDataKeys.releasedate, Type = "releasedate", Name = "Release Date", PropertyName = nameof(GameMetadataRow.Releasedate), DataType = MetaDataType.String, Scrapers = ["ArcadeDB", "ScreenScraper"], Viewable = true, Editable = true },
                new() { Key = MetaDataKeys.players, Type = "players", Name = "Players", PropertyName = nameof(GameMetadataRow.Players), DataType = MetaDataType.String, Scrapers = ["ArcadeDB", "ScreenScraper"], Viewable = true, Editable = true },
                new() { Key = MetaDataKeys.rating, Type = "rating", Name = "Rating", PropertyName = nameof(GameMetadataRow.Rating), DataType = MetaDataType.String, Scrapers = ["ArcadeDB", "ScreenScraper"], Viewable = true, Editable = true },
                new() { Key = MetaDataKeys.lang, Type = "lang", Name = "Language", PropertyName = nameof(GameMetadataRow.Lang), DataType = MetaDataType.String, Scrapers = ["ScreenScraper", "ArcadeDB"], Viewable = true, Editable = true },
                new() { Key = MetaDataKeys.region, Type = "region", Name = "Region", PropertyName = nameof(GameMetadataRow.Region), DataType = MetaDataType.String, Scrapers = ["ScreenScraper", "ArcadeDB"], Viewable = true, Editable = true },
                new() { Key = MetaDataKeys.publisher, Type = "publisher", Name = "Publisher", PropertyName = nameof(GameMetadataRow.Publisher), DataType = MetaDataType.String, Scrapers = ["ArcadeDB", "ScreenScraper"], Viewable = true, DefaultVisible = true, Editable = true },
                new() { Key = MetaDataKeys.developer, Type = "developer", Name = "Developer", PropertyName = nameof(GameMetadataRow.Developer), DataType = MetaDataType.String, Scrapers = ["ScreenScraper"], Viewable = true, Editable = true },
                new() { Key = MetaDataKeys.playcount, Type = "playcount", Name = "Play Count", PropertyName = nameof(GameMetadataRow.Playcount), DataType = MetaDataType.String, Viewable = true, Editable = false },
                new() { Key = MetaDataKeys.gametime, Type = "gametime", Name = "Game Time", PropertyName = nameof(GameMetadataRow.Gametime), DataType = MetaDataType.String, Viewable = true, Editable = false },
                new() { Key = MetaDataKeys.lastplayed, Type = "lastplayed", Name = "Last Played", PropertyName = nameof(GameMetadataRow.Lastplayed), DataType = MetaDataType.String, Viewable = true, Editable = false },
                new() { Key = MetaDataKeys.desc, Type = "desc", Name = "Description", PropertyName = nameof(GameMetadataRow.Desc), DataType = MetaDataType.String, Scrapers = ["ArcadeDB", "ScreenScraper"], Viewable = true, Editable = false },
                new() { Key = MetaDataKeys.image, Type = "image", Name = "Image", PropertyName = nameof(GameMetadataRow.Image), DataType = MetaDataType.Image, Scrapers = ["ArcadeDB", "ScreenScraper", "EmuMovies"], Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "image" },
                new() { Key = MetaDataKeys.marquee, Type = "marquee", Name = "Marquee", PropertyName = nameof(GameMetadataRow.Marquee), DataType = MetaDataType.Image, Scrapers = ["ArcadeDB", "ScreenScraper", "EmuMovies"], Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "marquee" },
                new() { Key = MetaDataKeys.thumbnail, Type = "thumbnail", Name = "Thumbnail", PropertyName = nameof(GameMetadataRow.Thumbnail), DataType = MetaDataType.Image, Scrapers = ["ArcadeDB", "ScreenScraper", "EmuMovies"], Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "thumb" },
                new() { Key = MetaDataKeys.boxback, Type = "boxback", Name = "Box Back", PropertyName = nameof(GameMetadataRow.Boxback), DataType = MetaDataType.Image, Scrapers = ["ScreenScraper", "EmuMovies"], Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "boxback" },
                new() { Key = MetaDataKeys.wheel, Type = "wheel", Name = "Wheel", PropertyName = nameof(GameMetadataRow.Wheel), DataType = MetaDataType.Image, Scrapers = ["ArcadeDB", "ScreenScraper", "EmuMovies"], Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = false, DefaultSuffix = "wheel" },
                new() { Key = MetaDataKeys.boxart, Type = "boxart", Name = "Box Art", PropertyName = nameof(GameMetadataRow.Boxart), DataType = MetaDataType.Image, Scrapers = ["ScreenScraper", "EmuMovies"], Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "boxart" },
                new() { Key = MetaDataKeys.fanart, Type = "fanart", Name = "Fan Art", PropertyName = nameof(GameMetadataRow.Fanart), DataType = MetaDataType.Image, Scrapers = ["ScreenScraper", "EmuMovies"], Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "fanart" },
                new() { Key = MetaDataKeys.map, Type = "map", Name = "Map", PropertyName = nameof(GameMetadataRow.Map), DataType = MetaDataType.Image, Scrapers = ["ScreenScraper", "EmuMovies"], Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = false, DefaultSuffix = "map" },
                new() { Key = MetaDataKeys.bezel, Type = "bezel", Name = "Bezel", PropertyName = nameof(GameMetadataRow.Bezel), DataType = MetaDataType.Image, Scrapers = ["ScreenScraper", "EmuMovies", "ArcadeDB"], Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "bezel" },
                new() { Key = MetaDataKeys.cartridge, Type = "cartridge", Name = "Cartridge", PropertyName = nameof(GameMetadataRow.Cartridge), DataType = MetaDataType.Image, Scrapers = ["ScreenScraper", "EmuMovies"], Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "cartridge" },
                new() { Key = MetaDataKeys.titleshot, Type = "titleshot", Name = "Titleshot", PropertyName = nameof(GameMetadataRow.Titleshot), DataType = MetaDataType.Image, Scrapers = ["ArcadeDB", "ScreenScraper", "EmuMovies"], Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "titleshot" },
                new() { Key = MetaDataKeys.video, Type = "video", Name = "Video", PropertyName = nameof(GameMetadataRow.Video), DataType = MetaDataType.Video, Scrapers = ["ArcadeDB", "ScreenScraper", "EmuMovies"], Viewable = true, Editable = false, DefaultPath = "./videos", DefaultEnabled = true, DefaultSuffix = "video" },
                new() { Key = MetaDataKeys.music, Type = "music", Name = "Music", PropertyName = nameof(GameMetadataRow.Music), DataType = MetaDataType.Music, Scrapers = ["EmuMovies"], Viewable = true, Editable = false, DefaultPath = "./music", DefaultEnabled = true, DefaultSuffix = "" },
                new() { Key = MetaDataKeys.manual, Type = "manual", Name = "Manual", PropertyName = nameof(GameMetadataRow.Manual), DataType = MetaDataType.Document, Scrapers = ["ScreenScraper", "EmuMovies", "ArcadeDB"], Viewable = true, Editable = false, DefaultPath = "./manuals", DefaultEnabled = true, DefaultSuffix = "manual" },
                new() { Key = MetaDataKeys.magazine, Type = "magazine", Name = "Magazine", PropertyName = nameof(GameMetadataRow.Magazine), DataType = MetaDataType.Image, Scrapers = ["ScreenScraper", "EmuMovies"], Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = false, DefaultSuffix = "magazine" },
                new() { Key = MetaDataKeys.mix, Type = "mix", Name = "Mix", PropertyName = nameof(GameMetadataRow.Mix), DataType = MetaDataType.Image, Scrapers = ["ScreenScraper", "EmuMovies"], Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "mix" },
                new() { Key = MetaDataKeys.family, Type = "family", Name = "Family", PropertyName = nameof(GameMetadataRow.Family), DataType = MetaDataType.String, Scrapers = ["ScreenScraper"], Viewable = true, Editable = true },
                new() { Key = MetaDataKeys.arcadesystemname, Type = "arcadesystemname", Name = "Arcade System Name", PropertyName = nameof(GameMetadataRow.Arcadesystemname), DataType = MetaDataType.String, Scrapers = ["ScreenScraper"], Viewable = true, Editable = true },
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

            // Initialize runtime media settings from defaults
            foreach (var decl in metaDataDictionary.Values)
            {
                if (decl.DataType is MetaDataType.String or MetaDataType.Bool)
                    continue;
                decl.MediaFolderPath = decl.DefaultPath;
                decl.Enabled = decl.DefaultEnabled;
                decl.Suffix = decl.DefaultSuffix;
            }

            allMediaFolderCache = gameDecls
                .Where(d => d.DataType != MetaDataType.String && d.DataType != MetaDataType.Bool)
                .ToList();

            // Initialize media metadata cache (Image, Document, Video - excluding Music)
            mediaMetadataCache = metaDataDictionary.Values
                .Where(decl => decl.DataType == MetaDataType.Image ||
                               decl.DataType == MetaDataType.Document ||
                               decl.DataType == MetaDataType.Video)
                .OrderBy(decl => decl.DataType == MetaDataType.Video ? 1 : 0)
                .ThenBy(decl => decl.Key)
                .ToList();

            // All viewable columns in declaration order (used by DataGrid builder)
            columnDeclarationsCache = gameDecls
                .Where(d => d.Viewable)
                .ToList();

            // Columns that appear in the toggle menu (non-AlwaysVisible, non-desc, non-media)
            toggleableColumnCache = gameDecls
                .Where(d => d.Viewable && !d.AlwaysVisible
                            && d.Key != MetaDataKeys.desc
                            && !d.IsMedia)
                .ToList();
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

        public static IReadOnlyList<MetaDataDecl> GetColumnDeclarations() => columnDeclarationsCache;

        public static IReadOnlyList<MetaDataDecl> GetToggleableColumns() => toggleableColumnCache;

        public static List<MetaDataDecl> GetMediaMetadata()
        {
            return mediaMetadataCache;
        }

        public static IReadOnlyList<MetaDataDecl> GetAllMediaFolderTypes() => allMediaFolderCache;


        // Conversion methods

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
