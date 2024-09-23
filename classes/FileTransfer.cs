using GamelistManager.classes.GamelistManager;
using System.IO;
using System.Net.Http;

namespace GamelistManager.classes
{
    internal static class FileTransfer
    {
        public static async Task<bool> DownloadFile(bool verify, bool overWriteData, string fileToDownload, string url)
        {
            var client = HttpClientSingleton.Instance;

            string parentFolder = Path.GetDirectoryName(fileToDownload)!;

            try
            {
                // Ensure the parent directory exists
                if (!Directory.Exists(parentFolder))
                {
                    Directory.CreateDirectory(parentFolder);
                }

                // Handle file overwriting logic
                if (File.Exists(fileToDownload))
                {
                    if (overWriteData)
                    {
                        File.Delete(fileToDownload);
                    }
                    else
                    {
                        return false; // Do not overwrite and the file already exists
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
                        using (FileStream fileStream = new FileStream(fileToDownload, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            // Copy the content stream to the file stream
                            await contentStream.CopyToAsync(fileStream);
                        }
                    }
                }

                string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".tif", ".ico", ".webp" };
                if (verify && imageExtensions.Contains(Path.GetExtension(fileToDownload).ToLowerInvariant()))
                {
                    string verifyResult = ImageVerify.CheckImage(fileToDownload);

                    if (verifyResult != "ok")
                    {
                        File.Delete(fileToDownload); // Delete invalid image file
                        return false; // Treat as a failed download
                    }
                }

                return true; // Return true if the download was successful
            }
            catch
            {
                // Handle any exceptions that occur during the download
                if (File.Exists(fileToDownload))
                {
                    File.Delete(fileToDownload); // Clean up the file if an error occurred
                }

                return false; // Return false if an error occurred
            }
        }
    }
}
