using System;
using System.Globalization;
using System.Linq;

namespace Gamelist_Manager.Classes.Helpers
{
    // Converts between various date formats and the ISO 8601 format used in gamelists
    internal static class Iso8601Helper
    {
        public static string ConvertToIso8601(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
                return string.Empty;

            string[] formats = { "yyyy", "yyyy-MM-dd", "yyyy/MM/dd" };

            // Try each format using LINQ
            var result = formats
                .Select(format =>
                {
                    DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date);
                    return date;
                })
                .Where(date => date != default)
                .Select(date => date.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture))
                .FirstOrDefault();

            if (result != null)
                return result;

            // Try general parsing as fallback
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDate))
                return parsedDate.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture);


            return string.Empty;
        }

        public static string ConvertFromIso8601(string isoDateString, string outputFormat = "yyyy-MM-dd")
        {
            if (string.IsNullOrEmpty(isoDateString))
                return string.Empty;

            try
            {
                var date = DateTime.ParseExact(isoDateString, "yyyyMMddTHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                return date.ToString(outputFormat, CultureInfo.InvariantCulture);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
