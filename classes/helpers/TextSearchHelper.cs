using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GamelistManager.classes.helpers
{
    public static class TextSearchHelper
    {
        private const int MaxCacheEntries = 50;

        private static readonly Dictionary<int, Dictionary<string, string>> CachedNormalizedLists
            = new Dictionary<int, Dictionary<string, string>>();

        private static readonly LinkedList<int> CacheUsageOrder = new LinkedList<int>();
        private static readonly object cacheLock = new object();

        private static readonly HashSet<string> StopWordsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "the", "a", "an", "and", "in", "of", "on", "at", "for", "by", "to", "is", "it" };

        private static readonly Regex BracketRegex = new Regex(@"\([^)]*\)|\[.*?\]", RegexOptions.Compiled);
        private static readonly Regex NonWordRegex = new Regex(@"[\W_]+", RegexOptions.Compiled);

        private static readonly (Regex Pattern, string Replacement)[] RomanNumeralRules =
        {
            (new Regex(@"\bVII\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "7"),
            (new Regex(@"\bVI\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "6"),
            (new Regex(@"\bV\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "5"),
            (new Regex(@"\bIV\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "4"),
            (new Regex(@"\bIII\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "3"),
            (new Regex(@"\bII\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "2"),
            (new Regex(@"\bI\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "1")
        };

        public static string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text = text.Normalize(NormalizationForm.FormC);

            if (text.Contains('_') && text.IndexOf('_') is int underscoreIndex && underscoreIndex > 0)
            {
                if (Path.HasExtension(text) || text.Any(char.IsDigit))
                    text = text[..underscoreIndex];
            }

            text = BracketRegex.Replace(text, string.Empty);
            text = RemoveDiacritics(text);

            if (text.Contains('.') && Path.GetExtension(text).Length > 1)
                text = Path.GetFileNameWithoutExtension(text);

            text = NonWordRegex.Replace(text, " ");
            text = ConvertRomanNumerals(text);

            if (text.Length > 7)
            {
                text = string.Join(" ",
                    text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Where(w => !StopWordsSet.Contains(w)));
            }

            return text.Replace(" ", string.Empty).Trim().ToLowerInvariant();
        }

        private static string RemoveDiacritics(string text)
        {
            string normalizedString = text.Normalize(NormalizationForm.FormD);

            if (normalizedString.Length <= 256)
            {
                Span<char> buffer = stackalloc char[normalizedString.Length];
                int index = 0;

                foreach (char c in normalizedString)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                        buffer[index++] = c;
                }

                return new string(buffer[..index]).Normalize(NormalizationForm.FormC);
            }
            else
            {
                var sb = new StringBuilder(normalizedString.Length);
                foreach (char c in normalizedString)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                        sb.Append(c);
                }
                return sb.ToString().Normalize(NormalizationForm.FormC);
            }
        }

        private static string ConvertRomanNumerals(string text)
        {
            foreach (var (pattern, replacement) in RomanNumeralRules)
                text = pattern.Replace(text, replacement);

            return text;
        }

        private static int ComputeListHash(List<string> names)
        {
            using SHA1 sha1 = SHA1.Create();
            byte[] raw = sha1.ComputeHash(Encoding.UTF8.GetBytes(string.Join("|", names)));
            return BitConverter.ToInt32(raw, 0);
        }

        private static int CacheNormalizedNames(List<string> names)
        {
            int key = ComputeListHash(names);

            lock (cacheLock)
            {
                if (CachedNormalizedLists.ContainsKey(key))
                {
                    CacheUsageOrder.Remove(key);
                    CacheUsageOrder.AddLast(key);
                    return key;
                }

                if (CachedNormalizedLists.Count >= MaxCacheEntries)
                {
                    int oldestKey = CacheUsageOrder.First.Value;
                    CacheUsageOrder.RemoveFirst();
                    CachedNormalizedLists.Remove(oldestKey);
                }

                Dictionary<string, string> normalizedNames;
                if (names.Count > 2000)
                {
                    normalizedNames = names
                        .AsParallel()
                        .Select(n => new { Original = n, Normalized = NormalizeText(n) })
                        .GroupBy(x => x.Normalized)
                        .Select(g => new { g.Key, Value = g.First().Original })
                        .ToDictionary(x => x.Key, x => x.Value);
                }
                else
                {
                    normalizedNames = names
                        .GroupBy(n => NormalizeText(n))
                        .Select(g => new { Key = g.Key, Value = g.First() })
                        .ToDictionary(x => x.Key, x => x.Value);
                }

                CachedNormalizedLists[key] = normalizedNames;
                CacheUsageOrder.AddLast(key);
            }

            return key;
        }

        public static string? FindTextMatch(string searchName, List<string> names)
        {
            int key = CacheNormalizedNames(names);
            string normalized = NormalizeText(searchName);

            lock (cacheLock)
            {
                return CachedNormalizedLists.TryGetValue(key, out var dict) && dict.TryGetValue(normalized, out var original)
                    ? original
                    : null;
            }
        }

        public static string? FindTextMatchSingle(string searchName, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            string normalizedSearch = NormalizeText(searchName);
            string normalizedFile = NormalizeText(fileName);

            return string.Equals(normalizedSearch, normalizedFile, StringComparison.OrdinalIgnoreCase)
                ? fileName
                : null;
        }

        public static void ClearCache()
        {
            lock (cacheLock)
            {
                CachedNormalizedLists.Clear();
                CacheUsageOrder.Clear();
            }
        }

        public static void ClearCache(List<string> names)
        {
            int key = ComputeListHash(names);
            lock (cacheLock)
            {
                CachedNormalizedLists.Remove(key);
                CacheUsageOrder.Remove(key);
            }
        }
    }

    public static class MediaScannerHelper
    {
        public static async Task<List<MediaSearchItem>> ScanMediaAsync(
            string folderToScan,
            List<(string RomPath, string Name)> romList,
            string mediaType,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(folderToScan) || !Directory.Exists(folderToScan))
                throw new DirectoryNotFoundException($"Folder not found: {folderToScan}");

            // Precompute normalized media dictionary in parallel
            var mediaFiles = Directory.GetFiles(folderToScan, "*.*", SearchOption.TopDirectoryOnly)
                .Select(f => new { FileName = Path.GetFileName(f), FullPath = f })
                .ToList();

            var normalizedMediaDict = mediaFiles
                .AsParallel()
                .WithCancellation(cancellationToken)
                .ToDictionary(
                    m => TextSearchHelper.NormalizeText(m.FileName),
                    m => m.FullPath,
                    StringComparer.OrdinalIgnoreCase
                );

            var foundMedia = new ConcurrentBag<MediaSearchItem>();

            // Process ROMs in parallel
            await Task.Run(() =>
            {
                Parallel.ForEach(romList, new ParallelOptions { CancellationToken = cancellationToken }, romTuple =>
                {
                    string romPath = romTuple.RomPath!;
                    string romName = romTuple.Name!;
                    string normalizedRomName = FilePathHelper.NormalizeRomName(romPath);

                    string? matchedFile = null;

                    // Try normalized ROM path first
                    if (normalizedMediaDict.TryGetValue(normalizedRomName, out var match))
                        matchedFile = match;
                    else
                    {
                        // Try ROM display name
                        string normalizedDisplayName = TextSearchHelper.NormalizeText(romName);
                        normalizedMediaDict.TryGetValue(normalizedDisplayName, out matchedFile);
                    }

                    if (!string.IsNullOrEmpty(matchedFile))
                    {
                        foundMedia.Add(new MediaSearchItem
                        {
                            RomPath = romPath,
                            MediaType = mediaType,
                            MatchedFile = matchedFile
                        });
                    }
                });
            }, cancellationToken);

            return foundMedia.ToList();
        }
    }

    public class MediaSearchItem
    {
        public string RomPath { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public string MatchedFile { get; set; } = string.Empty;
    }
}
