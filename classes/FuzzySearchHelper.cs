﻿using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GamelistManager
{
    public class FuzzySearchHelper
    {
        // Calculate the Levenshtein distance between two strings (case insensitive)
        private int LevenshteinDistance(string a, string b)
        {
            a = a.ToLower();
            b = b.ToLower();

            int[,] costs = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++)
                costs[i, 0] = i;
            for (int j = 0; j <= b.Length; j++)
                costs[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                    costs[i, j] = Math.Min(Math.Min(costs[i - 1, j] + 1, costs[i, j - 1] + 1), costs[i - 1, j - 1] + cost);
                }
            }

            return costs[a.Length, b.Length];
        }

        // Normalize a string by removing file extensions, parentheses and their contents, diacritics, punctuation, etc.
        private string NormalizeText(string text)
        {
            // Remove file extension
            text = Path.GetFileNameWithoutExtension(text);

            // Remove text within parentheses and the parentheses themselves
            text = Regex.Replace(text, @"\([^()]*\)", "");

            // Other normalizations (add more as needed)
            text = RemoveDiacritics(text); // Remove diacritics (accents)
            text = Regex.Replace(text, @"[\W_]+", ""); // Remove non-word characters and underscores

            return text.ToLower(); // Convert to lowercase
        }

        // Helper method to remove diacritics (accents) from characters
        private string RemoveDiacritics(string text)
        {
            // Replace accented characters with their regular counterparts
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

        // Determine cutoff value based on the length of the searchName
        private int GetCutoffValue(string searchName)
        {
            int length = searchName.Length;
            if (length <= 5)
                return 0;
            else if (length <= 8)
                return 2;
            else
                return 3; // Adjust this value as needed for longer strings
        }

        // Find the closest match to the searchName within the provided names list, using a dynamic cutoff
        // and ensuring the first `startCharCount` characters match
        public string FuzzySearch(string searchName, List<string> names, int startCharCount = 1)
        {
            string bestMatch = null;
            int lowestDistance = int.MaxValue;

            searchName = NormalizeText(searchName); // Normalize searchName
            int cutoff = GetCutoffValue(searchName); // Get dynamic cutoff based on searchName length
            int matchLength = Math.Min(startCharCount, searchName.Length); // Ensure startCharCount is within bounds

            // Use Parallel.ForEach for parallel processing
            Parallel.ForEach(names, name =>
            {
                string normalizedName = NormalizeText(name); // Normalize name

                // Check if the first `startCharCount` characters match
                bool startsMatch = searchName.Length >= matchLength &&
                                    normalizedName.Length >= matchLength &&
                                    searchName.Substring(0, matchLength) == normalizedName.Substring(0, matchLength);

                if (startsMatch)
                {
                    int distance = LevenshteinDistance(searchName, normalizedName);
                    lock (this) // Ensure thread-safe access to shared variables
                    {
                        if (distance < lowestDistance && distance <= cutoff)
                        {
                            lowestDistance = distance;
                            bestMatch = name;
                        }
                    }
                }
            });

            return bestMatch;
        }
    }
}
