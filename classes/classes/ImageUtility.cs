using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace GamelistManager.classes
{
    internal static class ImageUtility
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
                    Color firstPixelColor = bitmap.GetPixel(0, 0);

                    // Skip every 3rd pixel and alternate lines
                    int skipInterval = 3;

                    for (int y = 0; y < bitmap.Height; y += 2) // Skip every other line
                    {
                        for (int x = 0; x < bitmap.Width; x += skipInterval)
                        {
                            Color pixelColor = bitmap.GetPixel(x, y);

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
    }
}
