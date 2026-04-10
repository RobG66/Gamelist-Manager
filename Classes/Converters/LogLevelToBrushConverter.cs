using Avalonia;
using Avalonia.Data.Converters;
using Gamelist_Manager.Models;
using System;
using System.Globalization;

namespace Gamelist_Manager.Classes.Converters;

public class LogLevelToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not LogLevel level || level == LogLevel.Default)
            return AvaloniaProperty.UnsetValue;

        string resourceKey = level switch
        {
            LogLevel.Info => "SubtleTextBrush",
            LogLevel.Status => "InfoTextBrush",
            LogLevel.Success => "SuccessTextBrush",
            LogLevel.Warning => "WarningTextBrush",
            LogLevel.Error => "ErrorTextBrush",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(resourceKey))
            return AvaloniaProperty.UnsetValue;

        var app = Application.Current;
        if (app != null && app.Resources.TryGetResource(resourceKey, app.ActualThemeVariant, out var brush))
            return brush;

        return AvaloniaProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
