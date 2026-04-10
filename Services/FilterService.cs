using Gamelist_Manager.Models;
using System;

namespace Gamelist_Manager.Services
{
    public static class FilterService
    {
        public static Func<GameMetadataRow, bool> MakeFilter(
            string filterItem,
            string filterText,
            string selectedMode)
        {
            if (string.IsNullOrWhiteSpace(filterText) && selectedMode != "Is Empty" && selectedMode != "Is Not Empty")
            {
                return _ => true; // No filter
            }

            return selectedMode switch
            {
                "Is" => game => GetProperty(game, filterItem)?.Equals(filterText, StringComparison.OrdinalIgnoreCase) == true,

                "Is Like" => game =>
                {
                    var value = GetProperty(game, filterItem);
                    return value != null && value.Contains(filterText, StringComparison.OrdinalIgnoreCase);
                }
                ,

                "Is Not Like" => game =>
                {
                    var value = GetProperty(game, filterItem);
                    return value == null || !value.Contains(filterText, StringComparison.OrdinalIgnoreCase);
                }
                ,

                "Starts With" => game =>
                {
                    var value = GetProperty(game, filterItem);
                    return value != null && value.StartsWith(filterText, StringComparison.OrdinalIgnoreCase);
                }
                ,

                "Ends With" => game =>
                {
                    var value = GetProperty(game, filterItem);
                    return value != null && value.EndsWith(filterText, StringComparison.OrdinalIgnoreCase);
                }
                ,

                "Is Empty" => game => string.IsNullOrWhiteSpace(GetProperty(game, filterItem)),

                "Is Not Empty" => game => !string.IsNullOrWhiteSpace(GetProperty(game, filterItem)),

                _ => _ => true
            };
        }

        private static string? GetProperty(GameMetadataRow game, string propertyName)
        {
            var type = GamelistMetaData.GetMetadataTypeByName(propertyName);
            if (!string.IsNullOrEmpty(type))
                return game[type]?.ToString();
            return null;
        }
    }
}
