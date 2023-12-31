using System.Drawing;

namespace GamelistManager
{
    internal class ImageProcessor
    {
        public static bool IsSingleColorImage(string imagePath)
        {
            using (Bitmap bitmap = new Bitmap(imagePath))
            {
                // Get the color of the first pixel
                Color firstPixelColor = bitmap.GetPixel(0, 0);

                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        Color pixelColor = bitmap.GetPixel(x, y);

                        // Check if the current pixel color is different from the first pixel color
                        if (pixelColor != firstPixelColor)
                        {
                            return false; // Image contains multiple colors
                        }
                    }
                }
            }

            return true; // Image is a single color image
        }
    }
}