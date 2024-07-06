using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GamelistManager
{
    public class NormalizeText
    {
        public string Normalize(string input)
        {

            if (string.IsNullOrEmpty(input)) return string.Empty;
            // Convert to lowercase
            input = input.ToLowerInvariant();

            // Remove text between brackets including the brackets
            input = Regex.Replace(input, @"\s*\(.*?\)\s*", " ");

            // Remove diacritics (accents)
            input = RemoveDiacritics(input);

            // Remove punctuation and special characters
            input = Regex.Replace(input, @"\p{P}|\p{S}", "");

            // Remove file extension
            int index = input.LastIndexOf('.');
            if (index > 0)
            {
                input = input.Substring(0, index);
            }

            // Remove common words
            string[] CommonWords = { "and", "the", "or", "of", "to", "in", "with", "a", "an", "for", "on" };
            var words = input.Split(' ');
            var filteredWords = words.Where(word => !CommonWords.Contains(word)).ToArray();
            input = string.Join(" ", filteredWords);

            // Convert Roman numerals to numbers
            input = ConvertRomanNumerals(input);

            // Trim and reduce multiple spaces to a single space
            input = Regex.Replace(input.Trim(), @"\s+", " ");

            return input;
        }

        private string ConvertRomanNumerals(string text)
        {
            // Define a regex pattern to match valid Roman numerals preceded by a space or at the start of the string
            var romanNumeralPattern = @"(?<!\S)\bM{0,4}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})\b";
            return Regex.Replace(text, romanNumeralPattern, match =>
            {
                // Check if the match is preceded by a space or at the beginning of the string
                if (Regex.IsMatch(match.Value, @"^\bM{0,4}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})\b$"))
                {
                    return RomanToNumber(match.Value).ToString();
                }
                else
                {
                    return match.Value; // Return the original match if not preceded correctly
                }
            });
        }

        private int RomanToNumber(string roman)
        {
            var romanNumerals = new Dictionary<char, int>
            {
                { 'I', 1 },
                { 'V', 5 },
                { 'X', 10 },
                { 'L', 50 },
                { 'C', 100 },
                { 'D', 500 },
                { 'M', 1000 }
            };

            int number = 0;
            for (int i = 0; i < roman.Length; i++)
            {
                if (i + 1 < roman.Length && romanNumerals[roman[i]] < romanNumerals[roman[i + 1]])
                {
                    number -= romanNumerals[roman[i]];
                }
                else
                {
                    number += romanNumerals[roman[i]];
                }
            }

            return number;
        }

        private string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
