using System;
using System.IO;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace Gamelist_Manager.Classes.Helpers
{
    public static class ImageHelper
    {
        private const int MinImageWidth = 8;
        private const int MinImageHeight = 8;
        private const int SampleInterval = 3;
        private const int ColorTolerance = 5; // Allow slight variations due to compression

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
            // First check if file exists
            if (!File.Exists(imagePath))
            {
                return "Missing";
            }

            SKBitmap? bitmap = null;

            try
            {
                bitmap = SKBitmap.Decode(imagePath);
                
                if (bitmap == null)
                    return "Invalid Format";
                
                // Check minimum dimensions
                if (bitmap.Width < MinImageWidth || bitmap.Height < MinImageHeight)
                    return "Too Small";
                

                // Get first pixel color
                var firstPixel = bitmap.GetPixel(0, 0);

                // Sample pixels across the image
                for (var y = 0; y < bitmap.Height; y += 2) // Skip every other row
                {
                    for (var x = 0; x < bitmap.Width; x += SampleInterval)
                    {
                        var pixel = bitmap.GetPixel(x, y);

                        // Check if color differs beyond tolerance
                        if (Math.Abs(pixel.Red - firstPixel.Red) > ColorTolerance ||
                            Math.Abs(pixel.Green - firstPixel.Green) > ColorTolerance ||
                            Math.Abs(pixel.Blue - firstPixel.Blue) > ColorTolerance ||
                            Math.Abs(pixel.Alpha - firstPixel.Alpha) > ColorTolerance)
                        {
                            return "OK"; // Image contains multiple colors
                        }
                    }
                }

                return "Single Color";
            }
            catch (OutOfMemoryException)
            {
                return "Too Large";
            }
            catch (ArgumentException)
            {
                return "Invalid Format";
            }
            catch (Exception)
            {
                return "Corrupt";
            }
            finally
            {
                bitmap?.Dispose();
            }
        }

        public static Bitmap? LoadImageWithoutLock(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;

            try
            {
                // Load image data into memory to avoid file locking
                var imageData = File.ReadAllBytes(filePath);
                
                using var ms = new MemoryStream(imageData);
                return new Bitmap(ms);
            }
            catch
            {
                return null;
            }
        }

        public static bool IsImageFile(string filePath)
        {
            var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
            return fileExtension is ".jpg" or ".jpeg" or ".png" or ".bmp" or
                   ".gif" or ".tiff" or ".tif" or ".webp";
        }
    }
}
