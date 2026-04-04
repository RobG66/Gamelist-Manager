using Avalonia.Media.Imaging;

namespace Gamelist_Manager.Models;

public class SystemItem
{
    public string Name { get; init; } = string.Empty;
    public string GamelistPath { get; init; } = string.Empty;
    public Bitmap? Logo { get; init; }
}
