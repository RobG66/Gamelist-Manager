public static class DictionaryHelper
{
    public static void EnsureNestedDictionaryExists(
        Dictionary<string, Dictionary<string, Dictionary<string, string>>> mainDictionary,
        string scraper,
        string system,
        Dictionary<string, string> defaultValue)
    {
        if (!mainDictionary.ContainsKey(scraper))
        {
            mainDictionary[scraper] = new Dictionary<string, Dictionary<string, string>>();
        }

        var nestedDictionary = mainDictionary[scraper];
        nestedDictionary[system] = defaultValue;

    }
}
