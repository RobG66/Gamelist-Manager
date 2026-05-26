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
    public static class EmuMoviesTextSearchHelper
    {
        private const int MaxCacheEntries = 50;

        private static readonly Dictionary<List<string>, Dictionary<string, string>> CachedNormalizedLists
            = new Dictionary<List<string>, Dictionary<string, string>>(ReferenceEqualityComparer.Instance);

        private static readonly Dictionary<List<string>, Dictionary<string, string>> CachedExactLists
            = new Dictionary<List<string>, Dictionary<string, string>>(ReferenceEqualityComparer.Instance);

        private static readonly LinkedList<List<string>> CacheUsageOrder = new LinkedList<List<string>>();
        private static readonly Lock CacheLock = new Lock();

        // Cache for normalized search terms — pays NormalizeText cost once per unique input
        private static readonly ConcurrentDictionary<string, string> NormalizedSearchCache
            = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> StopWordsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "the", "a", "an", "and", "in", "of", "on", "at", "for", "by", "to", "is", "it" };

        private static readonly Regex BracketRegex = new Regex(@"\([^)]*\)|\[.*?\]", RegexOptions.Compiled);
        private static readonly Regex NonWordRegex = new Regex(@"[\W_]+", RegexOptions.Compiled);

        private static readonly (Regex Pattern, string Replacement)[] RomanNumeralRules =
        {
            (new Regex(@"\bXIV\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "14"),
            (new Regex(@"\bXIII\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "13"),
            (new Regex(@"\bXII\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "12"),
            (new Regex(@"\bXI\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "11"),
            (new Regex(@"\bX\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "10"),
            (new Regex(@"\bIX\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "9"),
            (new Regex(@"\bVIII\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "8"),
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
                if (Path.HasExtension(text) || text.AsSpan().IndexOfAnyInRange('0', '9') >= 0)
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

        /// <summary>
        /// Returns a cached normalized form of the input — use this instead of calling
        /// NormalizeText directly when the same search term may appear multiple times.
        /// </summary>
        public static string GetNormalizedCached(string text)
            => NormalizedSearchCache.GetOrAdd(text, static t => NormalizeText(t));

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

        public static void EnsureCached(List<string> names)
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

                var exactNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var m in names)
                {
                    var key = Path.GetFileNameWithoutExtension(m);
                    exactNames.TryAdd(key, m);
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

        /// <summary>
        /// Standard overload — normalizes searchName internally (cached).
        /// </summary>
        public static string? FindTextMatch(string searchName, List<string> names)
        {
            EnsureCached(names);
            string normalized = GetNormalizedCached(searchName);

            lock (CacheLock)
            {
                return CachedNormalizedLists.TryGetValue(names, out var dict) && dict.TryGetValue(normalized, out var original)
                    ? original
                    : null;
            }
        }

        /// <summary>
        /// Fast overload — accepts an already-normalized search term, skipping NormalizeText entirely.
        /// Use when the caller pre-normalizes search terms (e.g. batch scraping).
        /// </summary>
        public static string? FindTextMatchNormalized(string normalizedSearchName, List<string> names)
        {
            EnsureCached(names);

            lock (CacheLock)
            {
                return CachedNormalizedLists.TryGetValue(names, out var dict) && dict.TryGetValue(normalizedSearchName, out var original)
                    ? original
                    : null;
            }
        }

        public static string? FindTextMatchSingle(string searchName, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            string normalizedSearch = GetNormalizedCached(searchName);
            string normalizedFile = GetNormalizedCached(fileName);

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
            NormalizedSearchCache.Clear();
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

            var mediaFiles = Directory.GetFiles(folderToScan, "*.*", SearchOption.TopDirectoryOnly)
                .Select(f => new { FileName = Path.GetFileName(f), FullPath = f })
                .ToList();

            // Build normalized media dict — collision-safe
            var normalizedMediaDict = mediaFiles
                .AsParallel()
                .WithCancellation(cancellationToken)
                .GroupBy(m => EmuMoviesTextSearchHelper.NormalizeText(m.FileName), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().FullPath, StringComparer.OrdinalIgnoreCase);

            // Pre-normalize all ROM names once before the parallel loop
            var normalizedRomList = romList
                .AsParallel()
                .WithCancellation(cancellationToken)
                .Select(r => new
                {
                    r.RomPath,
                    NormalizedPath = FilePathHelper.NormalizeRomName(r.RomPath),
                    NormalizedName = EmuMoviesTextSearchHelper.GetNormalizedCached(r.Name)
                })
                .ToList();

            var foundMedia = new ConcurrentBag<MediaSearchItem>();

            await Task.Run(() =>
            {
                Parallel.ForEach(normalizedRomList, new ParallelOptions { CancellationToken = cancellationToken }, rom =>
                {
                    string? matchedFile;

                    if (normalizedMediaDict.TryGetValue(rom.NormalizedPath, out var match))
                        matchedFile = match;
                    else
                        normalizedMediaDict.TryGetValue(rom.NormalizedName, out matchedFile);

                    if (!string.IsNullOrEmpty(matchedFile))
                    {
                        foundMedia.Add(new MediaSearchItem
                        {
                            RomPath = rom.RomPath,
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
