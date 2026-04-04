using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Services;
using System;
using System.Globalization;
using System.IO;

namespace Gamelist_Manager.Classes.Converters;

public class PathToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            string fullPath = path;
            if (!Path.IsPathRooted(path))
            {
                var gamelistDirectory = SharedDataService.Instance.GamelistDirectory;
                if (!string.IsNullOrEmpty(gamelistDirectory))
                    fullPath = FilePathHelper.GamelistPathToFullPath(path, gamelistDirectory);
            }

            if (!File.Exists(fullPath))
                return null;

            return ImageHelper.LoadImageWithoutLock(fullPath);
        }
        catch { }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}