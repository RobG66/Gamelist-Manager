using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.Classes.Helpers
{
    public static class TextSearchHelper
    {
        private const int MaxCacheEntries = 50;

        private static readonly Dictionary<List<string>, Dictionary<string, string>> CachedNormalizedLists
            = new Dictionary<List<string>, Dictionary<string, string>>(ReferenceEqualityComparer.Instance);

        private static readonly Dictionary<List<string>, Dictionary<string, string>> CachedExactLists
            = new Dictionary<List<string>, Dictionary<string, string>>(ReferenceEqualityComparer.Instance);

        private static readonly LinkedList<List<string>> CacheUsageOrder = new LinkedList<List<string>>();
        private static readonly Lock CacheLock = new Lock();

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

            int underscoreIndex = text.IndexOf('_');
            if (underscoreIndex > 0)
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
            var normalizedString = text.Normalize(NormalizationForm.FormD);

            if (normalizedString.Length <= 256)
            {
                Span<char> buffer = stackalloc char[normalizedString.Length];
                var index = 0;

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

        private static void EnsureCached(List<string> names)
        {
            lock (CacheLock)
            {
                if (CachedNormalizedLists.ContainsKey(names))
                {
                    CacheUsageOrder.Remove(names);
                    CacheUsageOrder.AddLast(names);
                    return;
                }

                if (CachedNormalizedLists.Count >= MaxCacheEntries)
                {
                    var oldest = CacheUsageOrder.First!.Value;
                    CacheUsageOrder.RemoveFirst();
                    CachedNormalizedLists.Remove(oldest);
                    CachedExactLists.Remove(oldest);
                }

                // Exact match dict: filename-without-extension (case-insensitive) → original
                var exactNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var m in names)
                {
                    var key = Path.GetFileNameWithoutExtension(m);
                    exactNames.TryAdd(key, m);
                }

                // Normalized fuzzy dict: NormalizeText(filename) → original
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
                        .Select(g => new { g.Key, Value = g.First() })
                        .ToDictionary(x => x.Key, x => x.Value);
                }

                CachedExactLists[names] = exactNames;
                CachedNormalizedLists[names] = normalizedNames;
                CacheUsageOrder.AddLast(names);
            }
        }

        public static string? FindExactMatch(string searchName, List<string> names)
        {
            EnsureCached(names);

            lock (CacheLock)
            {
                return CachedExactLists.TryGetValue(names, out var dict) && dict.TryGetValue(searchName, out var original)
                    ? original
                    : null;
            }
        }

        public static string? FindTextMatch(string searchName, List<string> names)
        {
            EnsureCached(names);
            string normalized = NormalizeText(searchName);

            lock (CacheLock)
            {
                return CachedNormalizedLists.TryGetValue(names, out var dict) && dict.TryGetValue(normalized, out var original)
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
            lock (CacheLock)
            {
                CachedNormalizedLists.Clear();
                CachedExactLists.Clear();
                CacheUsageOrder.Clear();
            }
        }

        public static void ClearCache(List<string> names)
        {
            lock (CacheLock)
            {
                CachedNormalizedLists.Remove(names);
                CachedExactLists.Remove(names);
                CacheUsageOrder.Remove(names);
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
                    var romPath = romTuple.RomPath;
                    var romName = romTuple.Name;
                    var normalizedRomName = FilePathHelper.NormalizeRomName(romPath);

                    string? matchedFile;

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
