using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace Gamelist_Manager.Classes.Converters
{
    public class BooleanToGridLengthConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Determine visibility - handle both booleans and objects
            bool isVisible;
            
            if (value is bool boolValue)
            {
                isVisible = boolValue;
            }
            else
            {
                // Treat non-null objects as true, null as false
                isVisible = value != null;
            }
            
            if (!isVisible)
            {
                return new GridLength(0);
            }

            // Parse the parameter to determine what default width to use
            if (parameter is string paramStr)
            {
                if (paramStr == "Auto")
                {
                    return GridLength.Auto;
                }
                else if (paramStr.EndsWith("*"))
                {
                    // Star sizing (e.g., "2*")
                    var starValue = paramStr.TrimEnd('*');
                    if (string.IsNullOrEmpty(starValue))
                    {
                        return new GridLength(1, GridUnitType.Star);
                    }
                    else if (double.TryParse(starValue, out double stars))
                    {
                        return new GridLength(stars, GridUnitType.Star);
                    }
                }
                else if (double.TryParse(paramStr, out double pixels))
                {
                    // Pixel sizing
                    return new GridLength(pixels);
                }
            }

            // Default to Auto if no valid parameter
            return GridLength.Auto;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}



