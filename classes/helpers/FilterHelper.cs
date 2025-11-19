namespace GamelistManager.classes.helpers
{
    public static class FilterHelper
    {
        /// Escapes special characters for use in DataView RowFilter expressions.
        /// Handles single quotes, brackets, wildcards, and ampersands.
        public static string EscapeSpecialCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            input = input.Trim();

            // Escape single quotes
            input = input.Replace("'", "''");

            // Escape brackets
            input = input.Replace("[", "[[]").Replace("]", "[]]");

            // Escape wildcards
            input = input.Replace("%", "[%]").Replace("_", "[_]");

            // Escape ampersand for XML/filter compatibility
            input = input.Replace("&", "[&]");

            return input;
        }
    }
}