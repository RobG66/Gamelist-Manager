using System;
using System.Collections.Generic;
using System.Linq;

[AttributeUsage(AttributeTargets.Property)]
public class MediaTypeAttribute : Attribute
{
}
namespace GamelistManager
{
    public class ScraperData
    {
        // These names match the batocera names 
        public string name { get; set; }
        public string desc { get; set; }
        public string genre { get; set; }
        public string players { get; set; }
        public string rating { get; set; }
        public string region { get; set; }
        public string lang { get; set; }

        [MediaType]
        public string image { get; set; }

        [MediaType]
        public string marquee { get; set; }

        [MediaType]
        public string thumbnail { get; set; }

        [MediaType]
        public string video { get; set; }

        [MediaType]
        public string titleshot { get; set; }

        public string releasedate { get; set; }
        public string map { get; set; }

        [MediaType]
        public string manual { get; set; }

        public string publisher { get; set; }
        public string developer { get; set; }
        public string bezel { get; set; }
        public string genreid { get; set; }
        public string arcadesystemname { get; set; }

        [MediaType]
        public string boxback { get; set; }

        [MediaType]
        public string fanart { get; set; }

        public string id { get; set; }

        [MediaType]
        public string cartridge { get; set; }
    }

    public static class ScraperMediaTypes
    {
        public static IEnumerable<string> GetMediaTypesOnly()
        {
            var scraperDataType = typeof(ScraperData);
            var scraperMediaTypes = scraperDataType.GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(MediaTypeAttribute)))
                .Select(prop => prop.Name);

            return scraperMediaTypes;
        }
    }
}

