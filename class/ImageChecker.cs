using System.Drawing;
using System.IO;
using System.Threading.Tasks;

// Check if an image is single color or corrupt

namespace GamelistManager
{
    internal class ImageChecker
    {
        public static Task<string> CheckImage(string imagePath)
        {
            // First check if file exists
            bool fileExists = File.Exists(imagePath);

            if (!fileExists)
            {
                return Task.FromResult("missing");
            }

            try
            {
                using (Bitmap bitmap = new Bitmap(imagePath))
                {
                    Color firstPixelColor = bitmap.GetPixel(0, 0);
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        for (int y = 0; y < bitmap.Height; y++)
                        {
                            Color pixelColor = bitmap.GetPixel(x, y);

                            // Check if the current pixel color is different from the first pixel color
                            if (pixelColor != firstPixelColor)
                            {
                                return Task.FromResult("ok"); // Image contains multiple colors
                            }
                        }
                    }
                    return Task.FromResult("singlecolor");
                }
            }
            catch
            {
                return Task.FromResult("corrupt");
            }
        }
    }
}