using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gamelist_Manager.Classes.Helpers
{
    internal static class MediaExtensions
    {
        private sealed record Format(string Extension, bool EsDeDiscoverySupported = true, bool PreviewSupported = true);

        // Declaration order preserves the existing discovery preference order.
        // Preview and download verification support additional image formats.
        private static readonly Format[] ImageFormats =
        [
            new(".png"), new(".jpg"), new(".jpeg"), new(".webp"),
            new(".bmp", EsDeDiscoverySupported: false),
            new(".gif", EsDeDiscoverySupported: false),
            new(".tiff", EsDeDiscoverySupported: false),
            new(".tif", EsDeDiscoverySupported: false),
            new(".ico", EsDeDiscoverySupported: false, PreviewSupported: false)
        ];

        private static readonly Format[] VideoFormats =
        [
            new(".mp4"), new(".avi"), new(".mkv"), new(".webm"), new(".m4v"),
            new(".mov", EsDeDiscoverySupported: false),
            new(".wmv", EsDeDiscoverySupported: false),
            new(".flv", EsDeDiscoverySupported: false)
        ];

        public static IReadOnlyList<string> EsDeImageExtensions { get; } =
            Array.AsReadOnly(ImageFormats.Where(f => f.EsDeDiscoverySupported).Select(f => f.Extension).ToArray());

        public static IReadOnlyList<string> EsDeVideoExtensions { get; } =
            Array.AsReadOnly(VideoFormats.Where(f => f.EsDeDiscoverySupported).Select(f => f.Extension).ToArray());

        public static IReadOnlyList<string> ManualExtensions { get; } = Array.AsReadOnly(new[] { ".pdf" });
        public static IReadOnlyList<string> MusicExtensions { get; } = Array.AsReadOnly(new[] { ".mp3", ".ogg", ".wav", ".flac" });

        private static readonly HashSet<string> PreviewImages = new(
            ImageFormats.Where(f => f.PreviewSupported).Select(f => f.Extension), StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> PreviewVideos = new(
            VideoFormats.Where(f => f.PreviewSupported).Select(f => f.Extension), StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> DownloadImages = new(
            ImageFormats.Select(f => f.Extension), StringComparer.OrdinalIgnoreCase);

        public static bool IsPreviewImageFile(string filePath) => PreviewImages.Contains(Path.GetExtension(filePath));
        public static bool IsPreviewVideoFile(string filePath) => PreviewVideos.Contains(Path.GetExtension(filePath));
        public static bool IsImageExtension(string extension) => DownloadImages.Contains(extension);
        public static bool IsPdfFile(string filePath) => ManualExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase);
    }
}
