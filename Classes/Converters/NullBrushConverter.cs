using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Gamelist_Manager.Classes.Converters
{
    /// <summary>
    /// Returns AvaloniaProperty.UnsetValue for null brushes so the property
    /// falls back to the inherited theme foreground instead of rendering invisible.
    /// </summary>
    public class NullBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value ?? AvaloniaProperty.UnsetValue;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
