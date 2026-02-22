using GamelistManager.classes.helpers;
using System.IO;
using System.Net.Http;

namespace GamelistManager.classes.io
{
    internal class FileTransfer
    {
        private readonly HttpClient _httpClientService;

        public FileTransfer(HttpClient httpClientService)
        {
            _httpClientService = httpClientService;
        }

        public async Task<bool> DownloadFile(
            bool verify,
            string fileDownloadPath,
            string url,
            string bearerToken)
        {
            string fileExtension = Path.GetExtension(fileDownloadPath).ToLowerInvariant();
            string? parentFolder = Path.GetDirectoryName(fileDownloadPath);

            if (string.IsNullOrEmpty(parentFolder))
                return false;

            string tempFilePath = fileDownloadPath + ".tmp";

            try
            {
                // Ensure the parent directory exists
                if (!Directory.Exists(parentFolder))
                {
                    Directory.CreateDirectory(parentFolder);
                }

                // Remove stale temp file if it exists
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch
                    {
                        // If we can't delete it, try with a unique name
                        tempFilePath = $"{fileDownloadPath}.{Guid.NewGuid():N}.tmp";
                    }
                }

                using HttpRequestMessage request = new(HttpMethod.Get, url);

                if (!string.IsNullOrEmpty(bearerToken))
                {
                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
                }

                using HttpResponseMessage response =
                    await _httpClientService.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead);

                response.EnsureSuccessStatusCode();

                // Download to TEMP file with optimized buffer
                await using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                await using (FileStream fileStream = new(
                    tempFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920, // 80KB buffer for better performance
                    useAsync: true))
                {
                    await contentStream.CopyToAsync(fileStream);
                    await fileStream.FlushAsync(); // Ensure data is written
                }
            }
            catch
            {
                // Clean up temp file on download failure
                CleanupTempFile(tempFilePath);
                return false;
            }

            // Verify image if requested (only for image files)
            if (verify && IsImageExtension(fileExtension))
            {
                // Check if file exists and has content before verifying
                if (!File.Exists(tempFilePath))
                {
                    return false;
                }

                FileInfo fileInfo = new(tempFilePath);
                if (fileInfo.Length == 0)
                {
                    CleanupTempFile(tempFilePath);
                    return false;
                }

                // Verify the image
                string verifyResult = ImageHelper.CheckImage(tempFilePath);
                if (verifyResult != "OK")
                {
                    CleanupTempFile(tempFilePath);
                    return false;
                }
            }

            // Replace existing file atomically
            try
            {
                // Delete target file if it exists (faster than File.Move overwrite on some systems)
                if (File.Exists(fileDownloadPath))
                {
                    File.Delete(fileDownloadPath);
                }

                // Move temp file to final location
                File.Move(tempFilePath, fileDownloadPath);
            }
            catch
            {
                CleanupTempFile(tempFilePath);
                return false;
            }

            return true;
        }

        private static void CleanupTempFile(string tempFilePath)
        {
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
            catch
            {
                // Ignore cleanup failures
            }
        }

        private static bool IsImageExtension(string extension)
        {
            return extension is
                ".jpg" or ".jpeg" or ".png" or ".bmp" or
                ".gif" or ".tiff" or ".tif" or ".ico" or ".webp";
        }
    }
}