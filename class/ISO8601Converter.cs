using System;
using System.Windows.Forms;

namespace GamelistManager
{
    internal static class ISO8601Converter
    {
        public static string ConvertToISO8601(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
            {
                return null;
            }
            try
            {
                DateTime date;
                if (DateTime.TryParseExact(dateString, "yyyy", null, System.Globalization.DateTimeStyles.None, out date))
                {
                    return date.ToString("yyyyMMddTHHmmss");
                }

                if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out date))
                {
                    return date.ToString("yyyyMMddTHHmmss");
                }

                DateTime.TryParse(dateString, out DateTime dateTime);
                return dateTime.ToString("yyyyMMddTHHmmss");
            }
            catch
            {
                return null;
            }
        }
    }
}
