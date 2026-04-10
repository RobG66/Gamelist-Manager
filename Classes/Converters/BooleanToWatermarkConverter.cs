using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Gamelist_Manager.Classes.Converters;

// Converts a boolean to a watermark text.
// Returns "no description available" when true (game selected but no description), empty string when false (no game selected).
public class BooleanToWatermarkConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // If HasGameSelected is true, return watermark text (game is selected, show watermark if description is empty)
            // If HasGameSelected is false, return empty string (no game selected, don't show watermark)
            return boolValue ? "no description available" : string.Empty;
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}



