using System.IO;
using System.Net;
using System.Net.Http;

namespace GamelistManager.classes
{
    internal class FileTransfer
    {
        private readonly HttpClient _httpClientService;

        public FileTransfer(HttpClient httpClientService)
        {
            _httpClientService = httpClientService;
        }

        public async Task<bool> DownloadFile(bool verify, string fileDownloadPath, string url,string bearerToken)
        {

            string fileExtension = Path.GetExtension(fileDownloadPath).ToLowerInvariant();
            string parentFolder = Path.GetDirectoryName(fileDownloadPath)!;

            try
            {
                // Ensure the parent directory exists
                if (!Directory.Exists(parentFolder))
                {
                    Directory.CreateDirectory(parentFolder);
                }

                // If we are this far, always overwrite
                if (File.Exists(fileDownloadPath))
                {
                    File.Delete(fileDownloadPath);
                }

                string fileName = Path.GetFileName(fileDownloadPath);
                           
                await Logger.Instance.LogAsync($"Downloading file: {fileName}", System.Windows.Media.Brushes.Blue);
                                
                // Download the file using HttpClient
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    // Check if the bearer token is not empty or null before adding it to the request headers
                    if (!string.IsNullOrEmpty(bearerToken))
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
                    }
                         
                    using (HttpResponseMessage response = await _httpClientService.SendAsync(request))
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
                    string fileName = Path.GetFileName(fileDownloadPath);
                    await Logger.Instance.LogAsync($"Discarding bad image '{fileName}'", System.Windows.Media.Brushes.Red);
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
