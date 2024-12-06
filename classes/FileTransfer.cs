using GamelistManager.classes.GamelistManager;
using System.IO;
using System.Net.Http;

namespace GamelistManager.classes
{
     internal static class FileTransfer
    {
        public static async Task<bool> DownloadFile(bool verify, bool overWriteExistingFile, string fileDownloadPath, string url)
        {
            var client = HttpClientSingleton.Instance;

            string fileExtension = Path.GetExtension(fileDownloadPath).ToLowerInvariant();
            string parentFolder = Path.GetDirectoryName(fileDownloadPath)!;

            try
            {
                // Ensure the parent directory exists
                if (!Directory.Exists(parentFolder))
                {
                    Directory.CreateDirectory(parentFolder);
                }

                // Handle file overwriting logic
                if (File.Exists(fileDownloadPath))
                {
                    if (overWriteExistingFile)
                    {
                        File.Delete(fileDownloadPath);
                    }
                    else
                    {
                        return true;
                    }
                }

                // Download the file using HttpClient
                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode(); // Throw if the status code is not success

                    // Read the response content as a stream
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        // Open the file stream for writing
                        using (FileStream fileStream = new FileStream(fileDownloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            // Copy the content stream to the file stream
                            await contentStream.CopyToAsync(fileStream);
                        }
                    }
                }
            }
            catch
            {
            // Handle any exceptions that occur during the download
            if (File.Exists(fileDownloadPath))
            {
                File.Delete(fileDownloadPath); // Clean up the file if an error occurred
            }

                return false;
            }
            
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".tif", ".ico", ".webp" };

            if (verify && imageExtensions.Contains(fileExtension))
            {
                string verifyResult = ImageUtility.CheckImage(fileDownloadPath);
                if (verifyResult != "OK")
                {
                    File.Delete(fileDownloadPath); // Delete invalid image file
                    return false; // Treat as a failed download
                }
            }

            /*
            if (convertToPNG)
            {
                if (fileExtension != ".png" && imageExtensions.Contains(fileExtension))
                {
                    string pngFilePath = Path.ChangeExtension(fileDownloadPath, ".png");
                    ImageUtility.ConvertToPng(fileDownloadPath, pngFilePath);
                    File.Delete(fileDownloadPath); // Remove the original file
                    return pngFilePath;
                }
            }
            */

            return true;
        }
    }
}
