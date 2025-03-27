using System.Globalization;

namespace GamelistManager
{
    /// <summary>
    /// Provides methods to convert date strings to and from ISO 8601 format.
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
                return string.Empty;
            }

            DateTime date;
            string[] formats = { "yyyy", "yyyy-MM-dd", "yyyy/MM/dd" }; // Additional formats as needed

            foreach (string format in formats)
            {
                if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date))
                {
                    return date.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture);
                }
            }

            // Try general parsing as fallback
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date))
            {
                return date.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture);
            }

            return string.Empty;
        }

        /// <summary>
        /// Converts an ISO 8601 date string (yyyyMMddTHHmmss) to a specified date format.
        /// </summary>
        /// <param name="isoDateString">The ISO 8601 date string to convert.</param>
        /// <param name="outputFormat">The desired output format (e.g., "yyyy-MM-dd", "MM/dd/yyyy").</param>
        /// <returns>The date in the specified format, or null if conversion fails.</returns>
        public static string ConvertFromISO8601(string isoDateString, string outputFormat = "yyyy-MM-dd")
        {
            if (string.IsNullOrEmpty(isoDateString))
            {
                return string.Empty;
            }

            try
            {
                DateTime date = DateTime.ParseExact(isoDateString, "yyyyMMddTHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                return date.ToString(outputFormat, CultureInfo.InvariantCulture);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
