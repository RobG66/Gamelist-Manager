using GamelistManager.classes.core;
using System.Data;
using System.Windows;

namespace GamelistManager.classes.services
{
    public static class FilterService
    {
        public static void ApplyFilters(string[] filters)
        {
            string mergedFilter = string.Empty;

            if (SharedData.DataSet.Tables.Count == 0)
            {
                return;
            }

            if (filters == null || filters.Length == 0)
            {
                SharedData.DataSet.Tables[0].DefaultView.RowFilter = null;
                return;
            }

            try
            {
                mergedFilter = string.Join(" AND ", filters.Where(f => !string.IsNullOrEmpty(f)));

                if (!string.IsNullOrEmpty(mergedFilter))
                {
                    SharedData.DataSet.Tables[0].DefaultView.RowFilter = mergedFilter;
                }
                else
                {
                    SharedData.DataSet.Tables[0].DefaultView.RowFilter = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The filter \"{mergedFilter}\" has an error!\n{ex.Message}", "Filter Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        public static string MakeFilter(string filterItem, string filterText, string selectedMode)
        {
            string filterExpression = string.Empty;

            if (filterItem.Contains(' '))
            {
                filterItem = $"[{filterItem}]";
            }

            switch (selectedMode)
            {
                case "Is":
                    if (!string.IsNullOrEmpty(filterText))
                    {
                        filterText = EscapeSpecialCharacters(filterText);
                        filterExpression = $"{filterItem} = '{filterText}'";
                    }
                    break;

                case "Is Like":
                    if (!string.IsNullOrEmpty(filterText))
                    {
                        string likePattern = filterText.Replace("*", "%");
                        likePattern = EscapeSpecialCharacters(likePattern);
                        likePattern = $"%{likePattern}%";
                        filterExpression = $"{filterItem} LIKE '{likePattern}'";
                    }
                    break;

                case "Is Not Like":
                    if (!string.IsNullOrEmpty(filterText))
                    {
                        string likePattern = filterText.Replace("*", "%");
                        likePattern = EscapeSpecialCharacters(likePattern);
                        likePattern = $"%{likePattern}%";
                        filterExpression = $"{filterItem} NOT LIKE '{likePattern}'";
                    }
                    break;

                case "Starts With":
                    if (!string.IsNullOrEmpty(filterText))
                    {
                        filterText = EscapeSpecialCharacters(filterText);
                        filterText = $"{filterText}%";
                        filterExpression = $"{filterItem} LIKE '{filterText}'";
                    }
                    break;

                case "Ends With":
                    if (!string.IsNullOrEmpty(filterText))
                    {
                        filterText = EscapeSpecialCharacters(filterText);
                        filterText = $"%{filterText}";
                        filterExpression = $"{filterItem} LIKE '{filterText}'";
                    }
                    break;

                case "Is Empty":
                    filterExpression = $"{filterItem} IS NULL OR {filterItem} = ''";
                    break;

                case "Is Not Empty":
                    filterExpression = $"{filterItem} IS NOT NULL AND {filterItem} <> ''";
                    break;
            }

            return filterExpression;
        }

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