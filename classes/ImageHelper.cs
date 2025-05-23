using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace GamelistManager.classes
{
    internal static class ImageHelper
    {
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

            try
            {
                using (Bitmap bitmap = new Bitmap(imagePath))
                {
                    System.Drawing.Color firstPixelColor = bitmap.GetPixel(0, 0);

                    // Skip every 3rd pixel and alternate lines
                    int skipInterval = 3;

                    for (int y = 0; y < bitmap.Height; y += 2) // Skip every other line
                    {
                        for (int x = 0; x < bitmap.Width; x += skipInterval)
                        {
                            System.Drawing.Color pixelColor = bitmap.GetPixel(x, y);

                            // Check if the current pixel color is different from the first pixel color
                            if (pixelColor != firstPixelColor)
                            {
                                return "OK"; // Image contains multiple colors
                            }
                        }
                    }

                    return "Single Color";
                }
            }
            catch
            {
                return "Corrupt";
            }
        }

        public static ImageSource? LoadImageWithoutLock(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;

            try
            {
                byte[] imageData = File.ReadAllBytes(filePath); // Read the image data into memory

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
            catch
            {
                return null;
            }
        }
    }
}
