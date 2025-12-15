using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GamelistManager.classes.helpers
{
    internal static class ImageHelper
    {
        private const int MIN_IMAGE_WIDTH = 8;
        private const int MIN_IMAGE_HEIGHT = 8;
        private const int SAMPLE_INTERVAL = 3;
        private const int COLOR_TOLERANCE = 5; // Allow slight variations due to compression

        public static void ConvertToPng(string inputFilePath, string outputFilePath)
        {
            using (Image image = Image.FromFile(inputFilePath))
            {
                image.Save(outputFilePath, ImageFormat.Png);
            }
        }

        public static string CheckImage(string imagePath)
        {
            // First check if file exists
            if (!File.Exists(imagePath))
            {
                return "Missing";
            }

            Bitmap? bitmap = null;
            BitmapData? bitmapData = null;

            try
            {
                bitmap = new Bitmap(imagePath);

                // Check minimum dimensions
                if (bitmap.Width < MIN_IMAGE_WIDTH || bitmap.Height < MIN_IMAGE_HEIGHT)
                {
                    return "Too Small";
                }

                // Lock the bitmap for fast pixel access
                Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                // Get the address of the first line
                IntPtr ptr = bitmapData.Scan0;

                // Calculate the number of bytes
                int bytes = Math.Abs(bitmapData.Stride) * bitmap.Height;
                byte[] rgbValues = new byte[bytes];

                // Copy the RGB values into the array
                Marshal.Copy(ptr, rgbValues, 0, bytes);

                // Get first pixel color (BGRA format)
                byte firstB = rgbValues[0];
                byte firstG = rgbValues[1];
                byte firstR = rgbValues[2];
                byte firstA = rgbValues[3];

                int stride = bitmapData.Stride;
                int bytesPerPixel = 4; // 32bpp ARGB

                // Sample pixels across the image
                for (int y = 0; y < bitmap.Height; y += 2) // Skip every other row
                {
                    for (int x = 0; x < bitmap.Width; x += SAMPLE_INTERVAL)
                    {
                        int position = (y * stride) + (x * bytesPerPixel);

                        // Bounds check
                        if (position + 3 >= rgbValues.Length)
                            continue;

                        byte b = rgbValues[position];
                        byte g = rgbValues[position + 1];
                        byte r = rgbValues[position + 2];
                        byte a = rgbValues[position + 3];

                        // Check if color differs beyond tolerance
                        if (Math.Abs(r - firstR) > COLOR_TOLERANCE ||
                            Math.Abs(g - firstG) > COLOR_TOLERANCE ||
                            Math.Abs(b - firstB) > COLOR_TOLERANCE ||
                            Math.Abs(a - firstA) > COLOR_TOLERANCE)
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
                // Ensure proper cleanup
                if (bitmapData != null && bitmap != null)
                {
                    try
                    {
                        bitmap.UnlockBits(bitmapData);
                    }
                    catch
                    {
                        // Ignore unlock errors
                    }
                }
                bitmap?.Dispose();
            }
        }

        public static ImageSource? LoadImageWithoutLock(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;

            try
            {
                byte[] imageData = File.ReadAllBytes(filePath);

                using (var ms = new MemoryStream(imageData))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze(); // Makes it cross-thread accessible and read-only
                    return image;
                }
            }
            catch (OutOfMemoryException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}