using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GamelistManager.classes
{
    public static class TextSearchHelper
    {
        // Cache multiple normalized name dictionaries identified by hash keys
        private static readonly Dictionary<int, Dictionary<string, string>> CachedNormalizedLists = new();

        // Normalize a string by removing file extensions, parentheses, diacritics, punctuation, etc.
        private static string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Normalize to a consistent Unicode form (NFC is commonly used)
            text = text.Normalize(NormalizationForm.FormC);

            // Remove text after the first underscore, including the underscore
            int underscoreIndex = text.IndexOf('_');
            if (underscoreIndex >= 0)
            {
                text = text.Substring(0, underscoreIndex);
            }

            // Remove text in brackets (including the brackets)
            string pattern = @"\([^)]*\)|\[.*?\]";
            text = Regex.Replace(text, pattern, string.Empty);

            // Remove diacritics (accents)
            text = RemoveDiacritics(text);

            // Remove file extension using Path.GetFileNameWithoutExtension
            text = Path.GetFileNameWithoutExtension(text);

            // Remove punctuation, symbols, and non-word characters
            text = Regex.Replace(text, @"[\W_]+", " ");

            // Convert possible Roman numerals to numbers (up to 7)
            text = ConvertRomanNumerals(text);

            // Define stop words to exclude, only for longer strings
            if (text.Length > 7)
            {
                var stopWords = new HashSet<string> { "the ", " the ", " the", " a ", " an ", " and ", " in ", " of ", " on ", " at ", " for ", " by ", " to ", " is ", " it " };

                // Remove stop words while preserving other short words
                text = string.Join(" ", text
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(word => !stopWords.Contains(word.ToLower())));
            }

            text = text.Replace(" ", string.Empty);

            // Trim whitespace and convert to lowercase
            return text.Trim().ToLower();
        }

        // Helper method to remove diacritics (accents) from characters
        private static string RemoveDiacritics(string text)
        {
            string normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        // Helper method to convert Roman numerals to numbers
        private static string ConvertRomanNumerals(string text)
        {
            var romanToNumber = new Dictionary<string, string>
            {
                { "VII", "7" },
                { "VI", "6" },
                { "V", "5" },
                { "IV", "4" },
                { "III", "3" },
                { "II", "2" },
                { "I", "1" }
            };
            foreach (var roman in romanToNumber)
            {
                text = Regex.Replace(text, $@"\b{roman.Key}\b", roman.Value, RegexOptions.IgnoreCase);
            }

            return text;
        }

        // Precompute and cache normalized names for a specific list
        private static int CacheNormalizedNames(List<string> names)
        {
            // Compute a hash of the input list to uniquely identify it
            int namesHash = names.Aggregate(17, (current, name) => current * 31 + name.GetHashCode());

            // If the cache already exists for the given hash, return the hash
            if (CachedNormalizedLists.ContainsKey(namesHash))
                return namesHash;

            // Compute the normalized names dictionary
            var normalizedNames = names
                .GroupBy(name => NormalizeText(name)) // Group by normalized names
                .Select(group => new { Normalized = group.Key, Original = group.First() }) // Select the first occurrence of each group
                .ToDictionary(x => x.Normalized, x => x.Original); // Create dictionary with normalized name as the key

            // Store the normalized names dictionary in the cache
            CachedNormalizedLists[namesHash] = normalizedNames;

            return namesHash; // Return the hash as the cache key
        }

        // Find the closest match to the searchName in a specific cached list
        public static string FindTextMatch(string searchName, List<string> names)
        {
            // Cache the normalized names for the given list and get its hash
            int cacheKey = CacheNormalizedNames(names);

            // Normalize the search name
            string normalizedSearchName = NormalizeText(searchName);

            // Attempt to find the match using TryGetValue
            if (CachedNormalizedLists[cacheKey].TryGetValue(normalizedSearchName, out var originalName))
            {
                return originalName;
            }

            return string.Empty;
        }

        // Clear all caches
        public static void ClearCache()
        {
            CachedNormalizedLists.Clear();
        }
    }
}
