using System;
using System.Drawing;
using System.IO;

namespace GamelistManager
{
    public class ImageConverter
    {
        // Convert an image to a base64 string
        public static string ImageToBase64(Image image)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Save image to memory stream
                image.Save(memoryStream, image.RawFormat);

                // Convert byte array to base64 string
                byte[] imageBytes = memoryStream.ToArray();
                string base64String = Convert.ToBase64String(imageBytes);

                return base64String;
            }
        }

        // Convert a base64 string to an image
        public static Image Base64ToImage(string base64String)
        {
            // Convert base64 string to byte array
            byte[] imageBytes = Convert.FromBase64String(base64String);

            using (MemoryStream memoryStream = new MemoryStream(imageBytes))
            {
                // Create Image from memory stream
                Image image = Image.FromStream(memoryStream);
                return image;
            }
        }
    }
}