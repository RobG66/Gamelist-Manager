using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Services;
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
        kidgame,
        path,
        id,
        name,
        sortname,
        genre,
        releasedate,
        players,
        rating,
        lang,
        region,
        publisher,
        developer,
        completed,
        broken,
        playcount,
        gametime,
        playtime,
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
        collectionsortname,
        nogamecount,
        nomultiscrape,
        hidemetadata,
        controller,
        altemulator,
        folderlink
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
        public required MetaDataKeys Key { get; init; }
        public required string Name { get; init; }
        public required string Type { get; init; }
        public required string PropertyName { get; init; }
        public required MetaDataType DataType { get; init; }
        public bool Viewable { get; init; } = true;
        public bool AlwaysVisible { get; init; }
        public bool DefaultVisible { get; init; }
        public bool Editable { get; init; }
        public bool EsDeOnly { get; init; }
        public bool EsOnly { get; init; }

        public bool IsMedia => DataType is not (MetaDataType.String or MetaDataType.Bool);

        public string DefaultPath { get; init; } = string.Empty;
        public bool DefaultEnabled { get; init; } = true;
        public string DefaultSuffix { get; init; } = string.Empty;

        // ES-DE subfolder name under <EsDeRoot>/downloaded_media/<system>/ for this type.
        // Empty string means ES-DE does not support this media type.
        public string EsDeFolderName { get; init; } = string.Empty;

        public bool IsEsDeSupported => !string.IsNullOrEmpty(EsDeFolderName);
    }

    public static class GamelistMetaData
    {
        private static readonly Dictionary<MetaDataKeys, MetaDataDecl> metaDataDictionary;
        private static readonly List<MetaDataDecl> mediaMetadataCache;
        private static readonly List<MetaDataDecl> columnDeclarationsCache;
        private static readonly List<MetaDataDecl> toggleableColumnCache;
        private static readonly List<MetaDataDecl> allMediaFolderCache;
        private static readonly Dictionary<string, MetaDataDecl> typeToDecl;
        private static readonly Dictionary<string, MetaDataDecl> nameToDecl;


        static GamelistMetaData()
        {
            var gameDecls = new List<MetaDataDecl>
            {
                new() { Key = MetaDataKeys.hidden, Type = "hidden", Name = "Hidden", PropertyName = nameof(GameMetadataRow.Hidden), DataType = MetaDataType.Bool, Viewable = true, AlwaysVisible = true, Editable = false },
                new() { Key = MetaDataKeys.favorite, Type = "favorite", Name = "Favorite", PropertyName = nameof(GameMetadataRow.Favorite), DataType = MetaDataType.Bool, Viewable = true, Editable = false },
                new() { Key = MetaDataKeys.path, Type = "path", Name = "Rom Path", PropertyName = nameof(GameMetadataRow.Path), DataType = MetaDataType.String, Viewable = true, AlwaysVisible = true, Editable = false },
                new() { Key = MetaDataKeys.id, Type = "id", Name = "Game Id", PropertyName = nameof(GameMetadataRow.Id), DataType = MetaDataType.String, Viewable = true, Editable = true, EsOnly = true },
                new() { Key = MetaDataKeys.name, Type = "name", Name = "Name", PropertyName = nameof(GameMetadataRow.Name), DataType = MetaDataType.String, Viewable = true, AlwaysVisible = true, Editable = true },
                new() { Key = MetaDataKeys.sortname, Type = "sortname", Name = "Sort Name", PropertyName = nameof(GameMetadataRow.Sortname), DataType = MetaDataType.String, Viewable = true, Editable = true, EsDeOnly = true },
                new() { Key = MetaDataKeys.collectionsortname, Type = "collectionsortname", Name = "Collection Sort Name", PropertyName = nameof(GameMetadataRow.Collectionsortname), DataType = MetaDataType.String, Viewable = true, Editable = true, EsDeOnly = true },
                new() { Key = MetaDataKeys.genre, Type = "genre", Name = "Genre", PropertyName = nameof(GameMetadataRow.Genre), DataType = MetaDataType.String, Viewable = true, DefaultVisible = true, Editable = true },
                new() { Key = MetaDataKeys.releasedate, Type = "releasedate", Name = "Release Date", PropertyName = nameof(GameMetadataRow.Releasedate), DataType = MetaDataType.String, Viewable = true, Editable = true },
                new() { Key = MetaDataKeys.players, Type = "players", Name = "Players", PropertyName = nameof(GameMetadataRow.Players), DataType = MetaDataType.String, Viewable = true, Editable = true },
                new() { Key = MetaDataKeys.rating, Type = "rating", Name = "Rating", PropertyName = nameof(GameMetadataRow.Rating), DataType = MetaDataType.String, Viewable = true, Editable = true },
                new() { Key = MetaDataKeys.lang, Type = "lang", Name = "Language", PropertyName = nameof(GameMetadataRow.Lang), DataType = MetaDataType.String, Viewable = true, Editable = true, EsOnly = true },
                new() { Key = MetaDataKeys.region, Type = "region", Name = "Region", PropertyName = nameof(GameMetadataRow.Region), DataType = MetaDataType.String, Viewable = true, Editable = true, EsOnly = true },
                new() { Key = MetaDataKeys.publisher, Type = "publisher", Name = "Publisher", PropertyName = nameof(GameMetadataRow.Publisher), DataType = MetaDataType.String, Viewable = true, DefaultVisible = true, Editable = true },
                new() { Key = MetaDataKeys.developer, Type = "developer", Name = "Developer", PropertyName = nameof(GameMetadataRow.Developer), DataType = MetaDataType.String, Viewable = true, Editable = true },
                new() { Key = MetaDataKeys.completed, Type = "completed", Name = "Completed", PropertyName = nameof(GameMetadataRow.Completed), DataType = MetaDataType.Bool, Viewable = true, Editable = true, EsDeOnly = true },
                new() { Key = MetaDataKeys.broken, Type = "broken", Name = "Broken", PropertyName = nameof(GameMetadataRow.Broken), DataType = MetaDataType.Bool, Viewable = true, Editable = true, EsDeOnly = true },
                new() { Key = MetaDataKeys.nogamecount, Type = "nogamecount", Name = "No Game Count", PropertyName = nameof(GameMetadataRow.Nogamecount), DataType = MetaDataType.Bool, Viewable = true, Editable = true, EsDeOnly = true },
                new() { Key = MetaDataKeys.nomultiscrape, Type = "nomultiscrape", Name = "No Multi-Scrape", PropertyName = nameof(GameMetadataRow.Nomultiscrape), DataType = MetaDataType.Bool, Viewable = true, Editable = true, EsDeOnly = true },
                new() { Key = MetaDataKeys.hidemetadata, Type = "hidemetadata", Name = "Hide Metadata", PropertyName = nameof(GameMetadataRow.Hidemetadata), DataType = MetaDataType.Bool, Viewable = true, Editable = true, EsDeOnly = true },
                new() { Key = MetaDataKeys.kidgame, Type = "kidgame", Name = "Kid Game", PropertyName = nameof(GameMetadataRow.Kidgame), DataType = MetaDataType.Bool, Viewable = true, Editable = true },
                new() { Key = MetaDataKeys.playcount, Type = "playcount", Name = "Play Count", PropertyName = nameof(GameMetadataRow.Playcount), DataType = MetaDataType.String, Viewable = true, Editable = false },
                new() { Key = MetaDataKeys.gametime, Type = "gametime", Name = "Game Time", PropertyName = nameof(GameMetadataRow.Gametime), DataType = MetaDataType.String, Viewable = true, Editable = false, EsOnly = true },
                new() { Key = MetaDataKeys.playtime, Type = "playtime", Name = "Play Time", PropertyName = nameof(GameMetadataRow.Playtime), DataType = MetaDataType.String, Viewable = true, Editable = false, EsDeOnly = true },
                new() { Key = MetaDataKeys.lastplayed, Type = "lastplayed", Name = "Last Played", PropertyName = nameof(GameMetadataRow.Lastplayed), DataType = MetaDataType.String, Viewable = true, Editable = false },
                new() { Key = MetaDataKeys.desc, Type = "desc", Name = "Description", PropertyName = nameof(GameMetadataRow.Desc), DataType = MetaDataType.String, Viewable = true, Editable = false },
                new() { Key = MetaDataKeys.image, Type = "image", Name = "Image", PropertyName = nameof(GameMetadataRow.Image), DataType = MetaDataType.Image, Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "image", EsDeFolderName = "screenshots" },
                new() { Key = MetaDataKeys.marquee, Type = "marquee", Name = "Marquee", PropertyName = nameof(GameMetadataRow.Marquee), DataType = MetaDataType.Image, Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "marquee", EsDeFolderName = "marquees" },
                new() { Key = MetaDataKeys.thumbnail, Type = "thumbnail", Name = "Thumbnail", PropertyName = nameof(GameMetadataRow.Thumbnail), DataType = MetaDataType.Image, Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "thumb", EsDeFolderName = "covers" },
                new() { Key = MetaDataKeys.boxback, Type = "boxback", Name = "Box Back", PropertyName = nameof(GameMetadataRow.Boxback), DataType = MetaDataType.Image, Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "boxback", EsDeFolderName = "backcovers" },
                new() { Key = MetaDataKeys.wheel, Type = "wheel", Name = "Wheel", PropertyName = nameof(GameMetadataRow.Wheel), DataType = MetaDataType.Image, Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = false, DefaultSuffix = "wheel", EsDeFolderName = "wheel" },
                new() { Key = MetaDataKeys.boxart, Type = "boxart", Name = "Box Art", PropertyName = nameof(GameMetadataRow.Boxart), DataType = MetaDataType.Image, Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "boxart", EsDeFolderName = "3dboxes" },
                new() { Key = MetaDataKeys.fanart, Type = "fanart", Name = "Fan Art", PropertyName = nameof(GameMetadataRow.Fanart), DataType = MetaDataType.Image, Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "fanart", EsDeFolderName = "fanart" },
                new() { Key = MetaDataKeys.map, Type = "map", Name = "Map", PropertyName = nameof(GameMetadataRow.Map), DataType = MetaDataType.Image, Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = false, DefaultSuffix = "map" },
                new() { Key = MetaDataKeys.bezel, Type = "bezel", Name = "Bezel", PropertyName = nameof(GameMetadataRow.Bezel), DataType = MetaDataType.Image, Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "bezel" },
                new() { Key = MetaDataKeys.cartridge, Type = "cartridge", Name = "Cartridge", PropertyName = nameof(GameMetadataRow.Cartridge), DataType = MetaDataType.Image, Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "cartridge", EsDeFolderName = "physicalmedia" },
                new() { Key = MetaDataKeys.titleshot, Type = "titleshot", Name = "Titleshot", PropertyName = nameof(GameMetadataRow.Titleshot), DataType = MetaDataType.Image, Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "titleshot", EsDeFolderName = "titlescreens" },
                new() { Key = MetaDataKeys.video, Type = "video", Name = "Video", PropertyName = nameof(GameMetadataRow.Video), DataType = MetaDataType.Video, Viewable = true, Editable = false, DefaultPath = "./videos", DefaultEnabled = true, DefaultSuffix = "video", EsDeFolderName = "videos" },
                new() { Key = MetaDataKeys.music, Type = "music", Name = "Music", PropertyName = nameof(GameMetadataRow.Music), DataType = MetaDataType.Music, Viewable = true, Editable = false, DefaultPath = "./music", DefaultEnabled = true, DefaultSuffix = "" },
                new() { Key = MetaDataKeys.manual, Type = "manual", Name = "Manual", PropertyName = nameof(GameMetadataRow.Manual), DataType = MetaDataType.Document, Viewable = true, Editable = false, DefaultPath = "./manuals", DefaultEnabled = true, DefaultSuffix = "manual", EsDeFolderName = "manuals" },
                new() { Key = MetaDataKeys.magazine, Type = "magazine", Name = "Magazine", PropertyName = nameof(GameMetadataRow.Magazine), DataType = MetaDataType.Image, Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = false, DefaultSuffix = "magazine" },
                new() { Key = MetaDataKeys.mix, Type = "mix", Name = "Mix", PropertyName = nameof(GameMetadataRow.Mix), DataType = MetaDataType.Image, Viewable = true, Editable = false, DefaultPath = "./images", DefaultEnabled = true, DefaultSuffix = "mix", EsDeFolderName = "miximages" },
                new() { Key = MetaDataKeys.controller, Type = "controller", Name = "Controller", PropertyName = nameof(GameMetadataRow.Controller), DataType = MetaDataType.String, Viewable = true, Editable = true, EsDeOnly = true },
                new() { Key = MetaDataKeys.altemulator, Type = "altemulator", Name = "Alt Emulator", PropertyName = nameof(GameMetadataRow.Altemulator), DataType = MetaDataType.String, Viewable = true, Editable = true, EsDeOnly = true },
                new() { Key = MetaDataKeys.folderlink, Type = "folderlink", Name = "Folder Link", PropertyName = nameof(GameMetadataRow.Folderlink), DataType = MetaDataType.String, Viewable = true, Editable = false, EsDeOnly = true },
                new() { Key = MetaDataKeys.family, Type = "family", Name = "Family", PropertyName = nameof(GameMetadataRow.Family), DataType = MetaDataType.String, Viewable = true, Editable = true, EsOnly = true },
                new() { Key = MetaDataKeys.arcadesystemname, Type = "arcadesystemname", Name = "Arcade System Name", PropertyName = nameof(GameMetadataRow.Arcadesystemname), DataType = MetaDataType.String, Viewable = true, Editable = true, EsOnly = true },
            };

            metaDataDictionary = gameDecls.ToDictionary(
                decl => decl.Key,
                decl => decl
            );

            allMediaFolderCache = gameDecls
                .Where(d => d.IsMedia)
                .ToList();

            // Scrapeable media types — excludes Music which has no visual preview
            mediaMetadataCache = metaDataDictionary.Values
                .Where(decl => decl.DataType == MetaDataType.Image ||
                               decl.DataType == MetaDataType.Document ||
                               decl.DataType == MetaDataType.Video)
                .OrderBy(decl => decl.DataType == MetaDataType.Video ? 1 : 0)
                .ThenBy(decl => decl.Key)
                .ToList();

            // All viewable columns in declaration order, including ES-DE-only entries.
            // GetColumnDeclarations() filters by mode at call time.
            columnDeclarationsCache = gameDecls
                .Where(d => d.Viewable)
                .ToList();

            // All toggleable columns including ES-DE-only entries.
            // GetToggleableColumns() filters by mode at call time.
            toggleableColumnCache = gameDecls
                .Where(d => d.Viewable && !d.AlwaysVisible
                            && d.Key != MetaDataKeys.desc
                            && !d.IsMedia)
                .ToList();

            typeToDecl = gameDecls.ToDictionary(
                d => d.Type,
                d => d,
                StringComparer.OrdinalIgnoreCase);

            nameToDecl = gameDecls.ToDictionary(
                d => d.Name,
                d => d,
                StringComparer.OrdinalIgnoreCase);
        }

        public static IReadOnlyList<string> GetScraperElements(string scraperName)
        {
            return ScraperConfigService.Instance.GetScraperElements(scraperName);
        }

        public static IReadOnlyDictionary<MetaDataKeys, MetaDataDecl> GetMetaDataDictionary()
        {
            return metaDataDictionary;
        }

        public static IReadOnlyList<MetaDataDecl> GetColumnDeclarations()
        {
            var profileType = SharedDataService.Instance.ProfileType;
            return profileType switch
            {
                SettingKeys.ProfileTypeEsDe => columnDeclarationsCache.Where(d => !d.EsOnly).ToList(),
                _ => columnDeclarationsCache.Where(d => !d.EsDeOnly).ToList()
            };
        }

        public static IReadOnlyList<MetaDataDecl> GetToggleableColumns()
        {
            var profileType = SharedDataService.Instance.ProfileType;
            return profileType switch
            {
                SettingKeys.ProfileTypeEsDe => toggleableColumnCache.Where(d => !d.EsOnly).ToList(),
                _ => toggleableColumnCache.Where(d => !d.EsDeOnly).ToList()
            };
        }

        public static IReadOnlyList<MetaDataDecl> GetMediaMetadata()
        {
            return mediaMetadataCache;
        }

        public static IReadOnlyList<MetaDataDecl> GetAllMediaFolderTypes() => allMediaFolderCache;


        public static IReadOnlyList<MetaDataDecl> GetAllToggleableColumns() => toggleableColumnCache;

        public static string? GetPropertyName(MetaDataKeys key)
        {
            return metaDataDictionary.TryGetValue(key, out var decl) ? decl.PropertyName : null;
        }

        public static string GetMetadataNameByType(string type)
        {
            return typeToDecl.TryGetValue(type, out var decl) ? decl.Name : string.Empty;
        }

        public static string GetMetadataDataTypeByType(string type)
        {
            return typeToDecl.TryGetValue(type, out var decl) ? decl.DataType.ToString() : string.Empty;
        }

        public static string GetMetadataTypeByName(string name)
        {
            return nameToDecl.TryGetValue(name, out var decl) ? decl.Type : string.Empty;
        }

    }
}
