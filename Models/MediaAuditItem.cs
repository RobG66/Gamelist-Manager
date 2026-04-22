using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace Gamelist_Manager.Models;

public class MediaAuditItem
{
    private static readonly Lazy<Bitmap?> s_imageIcon = new(() => LoadAsset("image.png"));
    private static readonly Lazy<Bitmap?> s_videoIcon = new(() => LoadAsset("video.png"));
    private static readonly Lazy<Bitmap?> s_documentIcon = new(() => LoadAsset("manual.png"));

    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }
    public int Total { get; init; }
    public string Summary => $"{Name}: {Count} / {Total}";
    public double Percentage => Total > 0 ? (double)Count / Total * 100 : 0;
    public IBrush BarBrush { get; init; } = Brushes.DodgerBlue;
    public MetaDataType DataType { get; init; } = MetaDataType.String;

    public Bitmap? Icon => DataType switch
    {
        MetaDataType.Image => s_imageIcon.Value,
        MetaDataType.Video => s_videoIcon.Value,
        MetaDataType.Document => s_documentIcon.Value,
        _ => null
    };

    private static Bitmap? LoadAsset(string fileName)
    {
        try
        {
            var uri = new Uri($"avares://Gamelist_Manager/Assets/Icons/{fileName}");
            return new Bitmap(AssetLoader.Open(uri));
        }
        catch { return null; }
    }
}
