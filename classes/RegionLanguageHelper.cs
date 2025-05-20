using System.Text.RegularExpressions;

namespace GamelistManager.classes
{

    public class RegionLanguageHelper
    {
        public HashSet<string> Languages { get; set; } = new();

        private static readonly string[] JapanDefaults =
        {
        "pc88", "pc98", "pcenginecd", "pcfx", "satellaview",
        "sg1000", "sufami", "wswan", "wswanc", "x68000"
    };

        private static readonly string[] ArcadeSystems =
        {
        "mame", "naomi", "naomi2", "atomiswave","chihiro","daphne","namco2x6",
        "neogeo", "model2", "model3", "hikaru", "fbneo","cave","cave3rd","neogeo64","teknoparrot"
    };

        public class LangData
        {
            public List<string> value { get; set; } = new();
            public string lang { get; set; } = string.Empty;
            public string region { get; set; } = string.Empty;
        }

        public class RegionLanguageInfo
        {
            public string Region { get; set; } = string.Empty;
            public string Languages { get; set; } = string.Empty;
        }

        private static readonly List<LangData> langDatas = new()
    {
        new LangData { value = new() { "usa", "us", "u" }, lang = "en", region = "us" },
        new LangData { value = new() { "europe", "eu", "e", "ue", "euro" }, lang = "", region = "eu" },
        new LangData { value = new() { "w", "wor", "world" }, lang = "en", region = "wr" },
        new LangData { value = new() { "uk", "gb" }, lang = "en", region = "eu" },
        new LangData { value = new() { "es", "spain", "s" }, lang = "es", region = "eu" },
        new LangData { value = new() { "fr", "france", "fre", "french", "f" }, lang = "fr", region = "eu" },
        new LangData { value = new() { "de", "germany", "d" }, lang = "de", region = "eu" },
        new LangData { value = new() { "it", "italy", "i" }, lang = "it", region = "eu" },
        new LangData { value = new() { "nl", "netherlands" }, lang = "nl", region = "eu" },
        new LangData { value = new() { "gr", "greece" }, lang = "gr", region = "eu" },
        new LangData { value = new() { "no" }, lang = "no", region = "eu" },
        new LangData { value = new() { "sw", "sweden", "se" }, lang = "sw", region = "eu" },
        new LangData { value = new() { "pt", "portugal" }, lang = "pt", region = "eu" },
        new LangData { value = new() { "pl", "poland" }, lang = "pl", region = "eu" },
        new LangData { value = new() { "en" }, lang = "en", region = "" },
        new LangData { value = new() { "jp", "japan", "ja", "j" }, lang = "jp", region = "jp" },
        new LangData { value = new() { "br", "brazil" }, lang = "br", region = "br" },
        new LangData { value = new() { "ru", "r" }, lang = "ru", region = "ru" },
        new LangData { value = new() { "kr", "korea", "k" }, lang = "kr", region = "kr" },
        new LangData { value = new() { "cn", "china", "hong", "kong", "ch", "hk", "as", "tw" }, lang = "cn", region = "cn" },
        new LangData { value = new() { "canada", "ca", "c", "fc" }, lang = "fr", region = "wr" },
        new LangData { value = new() { "in", "ìndia" }, lang = "in", region = "in" }
    };

        // Optional: for quicker lookups
        private static readonly Dictionary<string, LangData> LangLookup = langDatas
            .SelectMany(ld => ld.value.Select(v => (v, ld)))
            .ToDictionary(x => x.v, x => x.ld);

        public static string GetRegion(string fileName)
        {
            bool returnFirstOnly = true; // Should there only be 1???

            string currentSystem = SharedData.CurrentSystem.ToLowerInvariant();
            string lowerFileName = fileName.ToLowerInvariant();

            var matchedRegions = new HashSet<string>();

            if (JapanDefaults.Contains(currentSystem))
            {
                matchedRegions.Add("jp");
            }
            else if (currentSystem == "thomson")
            {
                matchedRegions.Add("eu");
            }
            else
            {
                // Fallback: parse file name
                var matches = Regex.Matches(fileName, @"\((.*?)\)");
                foreach (Match match in matches)
                {
                    string content = match.Groups[1].Value;
                    var parts = content.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var part in parts)
                    {
                        string trimmedPart = part.ToLowerInvariant().Trim();
                        if (LangLookup.TryGetValue(trimmedPart, out var langData) && !string.IsNullOrWhiteSpace(langData.region))
                        {
                            matchedRegions.Add(langData.region);
                            if (returnFirstOnly)
                            {
                                return langData.region;
                            }
                        }
                    }
                }
            }

            // Personally I find the matching to be very kludgy, but what can you do?
            if (ArcadeSystems.Contains(currentSystem) && matchedRegions.Count == 0)
            {
                matchedRegions.Add(lowerFileName.EndsWith("j.zip") ? "jp" : "us");
            }

            if (matchedRegions.Count > 0)
            {
                return returnFirstOnly ? matchedRegions.First() : string.Join(",", matchedRegions);
            }

            return "us";
        }


        public static string GetLanguage(string fileName)
        {
            string currentSystem = SharedData.CurrentSystem.ToLowerInvariant();
            string lowerFileName = fileName.ToLowerInvariant();

            // Special case: if the file name contains (T), it's a translation (English)
            if (Regex.IsMatch(fileName, @"\(T(-Eng)?", RegexOptions.IgnoreCase))
            {
                return "en";
            }

            if (JapanDefaults.Contains(currentSystem))
            {
                return "jp";
            }

            if (currentSystem == "thomson")
            {
                return "fr";
            }

            // Fallback: parse file name
            var matchedLanguages = new HashSet<string>();
            var matches = Regex.Matches(fileName, @"\((.*?)\)");

            foreach (Match match in matches)
            {
                string content = match.Groups[1].Value;
                var parts = content.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    string trimmedPart = part.ToLowerInvariant().Trim();
                    if (LangLookup.TryGetValue(trimmedPart, out var langData) && !string.IsNullOrWhiteSpace(langData.lang))
                    {
                        matchedLanguages.Add(langData.lang);
                    }
                }
            }

            if (ArcadeSystems.Contains(currentSystem) && matchedLanguages.Count == 0)
            {
                return lowerFileName.EndsWith("j.zip") ? "jp" : "en";
            }

            return matchedLanguages.Count > 0 ? string.Join(",", matchedLanguages) : "en";
        }



        public static RegionLanguageInfo ExtractRegionAndLanguages(string fileName)
        {
            return new RegionLanguageInfo
            {
                Region = GetRegion(fileName),
                Languages = GetLanguage(fileName)
            };
        }
    }
}