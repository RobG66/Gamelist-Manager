using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;
using System;
using System.IO;

namespace Gamelist_Manager.Classes.Helpers;

public static class ImageHelper
{
    #region Constants

    private const int MinImageWidth = 8;
    private const int MinImageHeight = 8;
    private const int ColumnSampleInterval = 3;
    private const int RowSampleInterval = 2;
    private const int ColorTolerance = 20;

    #endregion

    #region Public API

    public static void ConvertToPng(string inputFilePath, string outputFilePath)
    {
        using var input = SKBitmap.Decode(inputFilePath);
        if (input == null)
            throw new InvalidOperationException("Failed to decode image");

        using var image = SKImage.FromBitmap(input);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputFilePath);
        data.SaveTo(stream);
    }

    public static string CheckImage(string imagePath)
    {
        if (!File.Exists(imagePath))
            return "Missing";

        SKBitmap? bitmap = null;
        try
        {
            bitmap = SKBitmap.Decode(imagePath);
            if (bitmap == null) return "Invalid Format";
            if (bitmap.Width < MinImageWidth || bitmap.Height < MinImageHeight) return "Too Small";

            var firstPixel = bitmap.GetPixel(0, 0);
            for (var y = 0; y < bitmap.Height; y += RowSampleInterval)
                for (var x = 0; x < bitmap.Width; x += ColumnSampleInterval)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (Math.Abs(pixel.Red - firstPixel.Red) > ColorTolerance ||
                        Math.Abs(pixel.Green - firstPixel.Green) > ColorTolerance ||
                        Math.Abs(pixel.Blue - firstPixel.Blue) > ColorTolerance ||
                        Math.Abs(pixel.Alpha - firstPixel.Alpha) > ColorTolerance)
                        return "OK";
                }
            return "Single Color";
        }
        catch (OutOfMemoryException) { return "Too Large"; }
        catch (ArgumentException) { return "Invalid Format"; }
        catch { return "Corrupt"; }
        finally { bitmap?.Dispose(); }
    }

    public static Bitmap? LoadImageWithoutLock(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return null;
        try
        {
            var imageData = File.ReadAllBytes(filePath);
            using var ms = new MemoryStream(imageData);
            return new Bitmap(ms);
        }
        catch { return null; }
    }

    public static Bitmap ToAvaloniaBitmap(SKBitmap skBitmap)
    {
        var src = skBitmap.GetPixels();
        if (src == IntPtr.Zero)
            throw new InvalidOperationException("SKBitmap has no pixel data");

        var wb = new WriteableBitmap(
            new PixelSize(skBitmap.Width, skBitmap.Height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);

        using var fb = wb.Lock();
        int rowBytes = skBitmap.Width * 4;
        bool needsSwizzle = skBitmap.ColorType != SKColorType.Bgra8888;

        if (!needsSwizzle && rowBytes == fb.RowBytes)
        {
            unsafe
            {
                Buffer.MemoryCopy(
                    (void*)src, (void*)fb.Address,
                    (long)fb.RowBytes * skBitmap.Height,
                    (long)skBitmap.RowBytes * skBitmap.Height);
            }
        }
        else
        {
            unsafe
            {
                byte* s = (byte*)src;
                byte* d = (byte*)fb.Address;
                int w = skBitmap.Width, h = skBitmap.Height;
                for (int y = 0; y < h; y++)
                {
                    uint* srcRow = (uint*)(s + y * skBitmap.RowBytes);
                    uint* dstRow = (uint*)(d + y * fb.RowBytes);
                    for (int x = 0; x < w; x++)
                    {
                        uint px = srcRow[x];
                        dstRow[x] = (px & 0xFF00FF00u)
                                  | ((px & 0x000000FFu) << 16)
                                  | ((px & 0x00FF0000u) >> 16);
                    }
                }
            }
        }
        return wb;
    }

    public static void SaveBitmapAsPng(SKBitmap bitmap, string path)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var temp = path + ".tmp";
        using (var fs = File.OpenWrite(temp))
            data.SaveTo(fs);
        File.Move(temp, path, overwrite: true);
    }

    public static SKBitmap? AutoCrop(SKBitmap source, int paddingPercent = 0,
        bool cropVertical = true, bool cropHorizontal = true)
    {
        int width = source.Width, height = source.Height;
        int top = 0, bottom = height - 1, left = 0, right = width - 1;
        bool found = false;

        unsafe
        {
            byte* p = (byte*)source.GetPixels();
            int rb = source.RowBytes;

            for (int y = 0; y < height && !found; y++)
            {
                byte* row = p + y * rb;
                for (int x = 0; x < width && !found; x++)
                    if (row[x * 4 + 3] > 0) { top = y; found = true; }
            }

            if (!found) return null;
            found = false;

            for (int y = height - 1; y >= top && !found; y--)
            {
                byte* row = p + y * rb;
                for (int x = 0; x < width && !found; x++)
                    if (row[x * 4 + 3] > 0) { bottom = y; found = true; }
            }

            found = false;
            for (int x = 0; x < width && !found; x++)
                for (int y = top; y <= bottom && !found; y++)
                    if ((p + y * rb)[x * 4 + 3] > 0) { left = x; found = true; }

            found = false;
            for (int x = width - 1; x >= left && !found; x--)
                for (int y = top; y <= bottom && !found; y++)
                    if ((p + y * rb)[x * 4 + 3] > 0) { right = x; found = true; }
        }

        if (paddingPercent > 0)
        {
            int contentW = right - left + 1;
            int contentH = bottom - top + 1;
            int padX = contentW * paddingPercent / 100;
            int padY = contentH * paddingPercent / 100;

            if (cropVertical) { top = Math.Max(0, top - padY); bottom = Math.Min(height - 1, bottom + padY); }
            if (cropHorizontal) { left = Math.Max(0, left - padX); right = Math.Min(width - 1, right + padX); }
        }

        if (!cropVertical) { top = 0; bottom = height - 1; }
        if (!cropHorizontal) { left = 0; right = width - 1; }

        if (top == 0 && left == 0 && bottom == height - 1 && right == width - 1)
        {
            var copy = new SKBitmap(new SKImageInfo(width, height, source.ColorType, source.AlphaType));
            using var copyCanvas = new SKCanvas(copy);
            copyCanvas.DrawBitmap(source, 0, 0);
            return copy;
        }

        int cropWidth = right - left + 1;
        int cropHeight = bottom - top + 1;

        var info = new SKImageInfo(cropWidth, cropHeight, source.ColorType, source.AlphaType);
        var result = new SKBitmap(info);
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(source,
            new SKRect(left, top, right + 1, bottom + 1),
            new SKRect(0, 0, cropWidth, cropHeight));
        return result;
    }

    #endregion
}
