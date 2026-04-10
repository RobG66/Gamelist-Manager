using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Gamelist_Manager.Classes.Converters
{
    // Converts a boolean to ScrollBarVisibility
    // True (Size To Fit ON) = Disabled (no horizontal scrollbar)
    // False (Size To Fit OFF) = Auto (show horizontal scrollbar when needed)
    public class BooleanToScrollBarVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool sizeToFit)
            {
                // When SizeToFit is true, disable horizontal scrollbar
                // When SizeToFit is false, enable auto horizontal scrollbar
                return sizeToFit ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
            }

            return ScrollBarVisibility.Auto;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

