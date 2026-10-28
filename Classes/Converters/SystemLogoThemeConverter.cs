using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Gamelist_Manager.Classes.Converters;

public class SystemLogoThemeConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 4 && ThemeVariant.Light.Equals(values[3]) && values[1] is Bitmap lightLogo)
            return lightLogo;

        if (values.Count == 4 && ThemeVariant.Dark.Equals(values[3]) && values[2] is Bitmap darkLogo)
            return darkLogo;

        return values.Count > 0 ? values[0] as Bitmap : null;
    }
}
