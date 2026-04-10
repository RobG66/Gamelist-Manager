using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace Gamelist_Manager.Models
{
    public class GameMetadataRow : ObservableObject
    {
        private static readonly Dictionary<MetaDataKeys, string> KeyToPropertyName = new()
        {
            { MetaDataKeys.hidden, nameof(Hidden) },
            { MetaDataKeys.favorite, nameof(Favorite) },
            { MetaDataKeys.path, nameof(Path) },
            { MetaDataKeys.id, nameof(Id) },
            { MetaDataKeys.name, nameof(Name) },
            { MetaDataKeys.genre, nameof(Genre) },
            { MetaDataKeys.releasedate, nameof(Releasedate) },
            { MetaDataKeys.players, nameof(Players) },
            { MetaDataKeys.rating, nameof(Rating) },
            { MetaDataKeys.lang, nameof(Lang) },
            { MetaDataKeys.region, nameof(Region) },
            { MetaDataKeys.publisher, nameof(Publisher) },
            { MetaDataKeys.developer, nameof(Developer) },
            { MetaDataKeys.playcount, nameof(Playcount) },
            { MetaDataKeys.gametime, nameof(Gametime) },
            { MetaDataKeys.lastplayed, nameof(Lastplayed) },
            { MetaDataKeys.desc, nameof(Desc) },
            { MetaDataKeys.image, nameof(Image) },
            { MetaDataKeys.marquee, nameof(Marquee) },
            { MetaDataKeys.thumbnail, nameof(Thumbnail) },
            { MetaDataKeys.boxback, nameof(Boxback) },
            { MetaDataKeys.wheel, nameof(Wheel) },
            { MetaDataKeys.boxart, nameof(Boxart) },
            { MetaDataKeys.fanart, nameof(Fanart) },
            { MetaDataKeys.map, nameof(Map) },
            { MetaDataKeys.bezel, nameof(Bezel) },
            { MetaDataKeys.cartridge, nameof(Cartridge) },
            { MetaDataKeys.titleshot, nameof(Titleshot) },
            { MetaDataKeys.video, nameof(Video) },
            { MetaDataKeys.music, nameof(Music) },
            { MetaDataKeys.manual, nameof(Manual) },
            { MetaDataKeys.magazine, nameof(Magazine) },
            { MetaDataKeys.mix, nameof(Mix) },
            { MetaDataKeys.family, nameof(Family) },
            { MetaDataKeys.arcadesystemname, nameof(Arcadesystemname) },
        };

        private readonly Dictionary<MetaDataKeys, object?> _values = new();

        public object? GetValue(MetaDataKeys key)
            => _values.TryGetValue(key, out var value) ? value : null;

        public void SetValue(MetaDataKeys key, object? value)
        {
            if (_values.TryGetValue(key, out var existing) && Equals(existing, value))
                return;

            _values[key] = value;
            OnPropertyChanged($"Item[{key}]");
            if (KeyToPropertyName.TryGetValue(key, out var propName))
                OnPropertyChanged(propName);
        }

        // Single string indexer kept for backward compatibility
        public object? this[string columnName]
        {
            get
            {
                if (System.Enum.TryParse<MetaDataKeys>(columnName, true, out var key))
                    return GetValue(key);
                return null;
            }
            set
            {
                if (System.Enum.TryParse<MetaDataKeys>(columnName, true, out var key))
                    SetValue(key, value is bool b ? b : value);
            }
        }

        public IEnumerable<object?> GetAllValues() => _values.Values;

        private string GetString(MetaDataKeys key) => GetValue(key)?.ToString() ?? string.Empty;
        private bool GetBool(MetaDataKeys key) => GetValue(key) is true;

        public bool Hidden { get => GetBool(MetaDataKeys.hidden); set => SetValue(MetaDataKeys.hidden, value); }
        public bool Favorite { get => GetBool(MetaDataKeys.favorite); set => SetValue(MetaDataKeys.favorite, value); }
        public string Path { get => GetString(MetaDataKeys.path); set => SetValue(MetaDataKeys.path, value); }
        public string Id { get => GetString(MetaDataKeys.id); set => SetValue(MetaDataKeys.id, value); }
        public string Name { get => GetString(MetaDataKeys.name); set => SetValue(MetaDataKeys.name, value); }
        public string Genre { get => GetString(MetaDataKeys.genre); set => SetValue(MetaDataKeys.genre, value); }
        public string Releasedate { get => GetString(MetaDataKeys.releasedate); set => SetValue(MetaDataKeys.releasedate, value); }
        public string Players { get => GetString(MetaDataKeys.players); set => SetValue(MetaDataKeys.players, value); }
        public string Rating { get => GetString(MetaDataKeys.rating); set => SetValue(MetaDataKeys.rating, value); }
        public string Lang { get => GetString(MetaDataKeys.lang); set => SetValue(MetaDataKeys.lang, value); }
        public string Region { get => GetString(MetaDataKeys.region); set => SetValue(MetaDataKeys.region, value); }
        public string Publisher { get => GetString(MetaDataKeys.publisher); set => SetValue(MetaDataKeys.publisher, value); }
        public string Developer { get => GetString(MetaDataKeys.developer); set => SetValue(MetaDataKeys.developer, value); }
        public string Playcount { get => GetString(MetaDataKeys.playcount); set => SetValue(MetaDataKeys.playcount, value); }
        public string Gametime { get => GetString(MetaDataKeys.gametime); set => SetValue(MetaDataKeys.gametime, value); }
        public string Lastplayed { get => GetString(MetaDataKeys.lastplayed); set => SetValue(MetaDataKeys.lastplayed, value); }
        public string Desc { get => GetString(MetaDataKeys.desc); set => SetValue(MetaDataKeys.desc, value); }
        public string Image { get => GetString(MetaDataKeys.image); set => SetValue(MetaDataKeys.image, value); }
        public string Marquee { get => GetString(MetaDataKeys.marquee); set => SetValue(MetaDataKeys.marquee, value); }
        public string Thumbnail { get => GetString(MetaDataKeys.thumbnail); set => SetValue(MetaDataKeys.thumbnail, value); }
        public string Boxback { get => GetString(MetaDataKeys.boxback); set => SetValue(MetaDataKeys.boxback, value); }
        public string Wheel { get => GetString(MetaDataKeys.wheel); set => SetValue(MetaDataKeys.wheel, value); }
        public string Boxart { get => GetString(MetaDataKeys.boxart); set => SetValue(MetaDataKeys.boxart, value); }
        public string Fanart { get => GetString(MetaDataKeys.fanart); set => SetValue(MetaDataKeys.fanart, value); }
        public string Map { get => GetString(MetaDataKeys.map); set => SetValue(MetaDataKeys.map, value); }
        public string Bezel { get => GetString(MetaDataKeys.bezel); set => SetValue(MetaDataKeys.bezel, value); }
        public string Cartridge { get => GetString(MetaDataKeys.cartridge); set => SetValue(MetaDataKeys.cartridge, value); }
        public string Titleshot { get => GetString(MetaDataKeys.titleshot); set => SetValue(MetaDataKeys.titleshot, value); }
        public string Video { get => GetString(MetaDataKeys.video); set => SetValue(MetaDataKeys.video, value); }
        public string Music { get => GetString(MetaDataKeys.music); set => SetValue(MetaDataKeys.music, value); }
        public string Manual { get => GetString(MetaDataKeys.manual); set => SetValue(MetaDataKeys.manual, value); }
        public string Magazine { get => GetString(MetaDataKeys.magazine); set => SetValue(MetaDataKeys.magazine, value); }
        public string Mix { get => GetString(MetaDataKeys.mix); set => SetValue(MetaDataKeys.mix, value); }
        public string Family { get => GetString(MetaDataKeys.family); set => SetValue(MetaDataKeys.family, value); }
        public string Arcadesystemname { get => GetString(MetaDataKeys.arcadesystemname); set => SetValue(MetaDataKeys.arcadesystemname, value); }

        public void NotifyDataChanged() => OnPropertyChanged(string.Empty);
    }
}
