using System.Globalization;

namespace GamelistManager
{
    /// <summary>
    /// Provides methods to convert date strings to ISO 8601 format.
    /// </summary>
    internal static class ISO8601Converter
    {
        /// <summary>
        /// Converts a date string to ISO 8601 format (yyyyMMddTHHmmss).
        /// </summary>
        /// <param name="dateString">The date string to convert.</param>
        /// <returns>The date in ISO 8601 format, or null if conversion fails.</returns>
        public static string ConvertToISO8601(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
            {
                return null;
            }

            try
            {
                DateTime date;

                // Try parsing with "yyyy"
                if (DateTime.TryParseExact(dateString, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture);
                }

                // Try parsing with "yyyy-MM-dd"
                if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture);
                }

                // Try general parsing
                if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    return date.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
