using System.Collections.Generic;
using System.Text;

namespace Gamelist_Manager.Classes.Helpers
{
    public static class EncodingHelper
    {
        // Reverse mapping for the 26 defined characters in CP1252's 0x80–0x9F range.
        // The five undefined CP1252 positions (0x81, 0x8D, 0x8F, 0x90, 0x9D) are not
        // listed here because .NET decodes them as their identical U+00XX code points,
        // which are handled by the direct byte-from-code-point path for U+0080–U+00FF.
        private static readonly Dictionary<char, byte> _cp1252Extended = new()
        {
            ['\u20AC'] = 0x80, // €
            ['\u201A'] = 0x82, // ‚
            ['\u0192'] = 0x83, // ƒ
            ['\u201E'] = 0x84, // „
            ['\u2026'] = 0x85, // …
            ['\u2020'] = 0x86, // †
            ['\u2021'] = 0x87, // ‡
            ['\u02C6'] = 0x88, // ˆ
            ['\u2030'] = 0x89, // ‰
            ['\u0160'] = 0x8A, // Š
            ['\u2039'] = 0x8B, // ‹
            ['\u0152'] = 0x8C, // Œ
            ['\u017D'] = 0x8E, // Ž
            ['\u2018'] = 0x91, // '
            ['\u2019'] = 0x92, // '
            ['\u201C'] = 0x93, // "
            ['\u201D'] = 0x94, // "
            ['\u2022'] = 0x95, // •
            ['\u2013'] = 0x96, // –
            ['\u2014'] = 0x97, // —
            ['\u02DC'] = 0x98, // ˜
            ['\u2122'] = 0x99, // ™
            ['\u0161'] = 0x9A, // š
            ['\u203A'] = 0x9B, // ›
            ['\u0153'] = 0x9C, // œ
            ['\u017E'] = 0x9E, // ž
            ['\u0178'] = 0x9F, // Ÿ
        };

        // Repairs mojibake produced when UTF-8 bytes were incorrectly decoded as
        // Windows-1252 before being stored (e.g. in ScreenScraper's database).
        // Recovers the original bytes by reversing the CP1252 mapping, then
        // re-decodes as UTF-8. The repair is only accepted if no U+FFFD replacement
        // characters are produced — their presence means the bytes were not valid
        // UTF-8 and the original text was legitimate (e.g. accented Western text).
        public static string FixMojibake(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var bytes = new List<byte>(text.Length);
            bool hasNonAscii = false;

            foreach (char c in text)
            {
                if (c < '\u0080')
                {
                    bytes.Add((byte)c);
                }
                else if (c <= '\u00FF')
                {
                    // U+0080–U+00FF maps directly to its byte value. This covers the
                    // Latin-1 range and the five undefined CP1252 positions, which .NET
                    // decodes as their identical code points (U+0081, U+008D, etc.).
                    bytes.Add((byte)c);
                    hasNonAscii = true;
                }
                else if (_cp1252Extended.TryGetValue(c, out byte b))
                {
                    // CP1252-specific character above U+00FF — recover its original byte
                    bytes.Add(b);
                    hasNonAscii = true;
                }
                else
                {
                    // Character has no CP1252 byte representation — text is not mojibake
                    return text;
                }
            }

            if (!hasNonAscii)
                return text;

            try
            {
                string repaired = Encoding.UTF8.GetString(bytes.ToArray());
                if (!repaired.Contains('\uFFFD'))
                    return repaired;
            }
            catch { }

            return text;
        }
    }
}
