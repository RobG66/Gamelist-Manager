using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Gamelist_Manager.Classes.Converters;

// Converts a boolean to a PlaceholderText text.
// Returns "no description available" when true (game selected but no description), empty string when false (no game selected).
public class BooleanToPlaceholderTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // If HasGameSelected is true, return PlaceholderText text (game is selected, show PlaceholderText if description is empty)
            // If HasGameSelected is false, return empty string (no game selected, don't show PlaceholderText)
            return boolValue ? "no description available" : string.Empty;
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}



