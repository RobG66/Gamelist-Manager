using Gamelist_Manager.Classes.Api;
using Gamelist_Manager.Classes.IO;
using Gamelist_Manager.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.Classes.Helpers
{
    internal static class MediaDownloadHelper
    {
        public static async Task DownloadMediaFilesAsync(
            ScrapedGameData scrapedData,
            ScraperParameters parameters,
            FileTransfer fileTransfer,
            Action<string> recordDownload,
            Action<string, LogLevel, string?, LogLevel>? log,
            CancellationToken cancellationToken = default)
        {
            if (scrapedData.Media == null || scrapedData.Media.Count == 0)
                return;

            foreach (var mediaResult in scrapedData.Media)
            {
                string mediaType = mediaResult.MediaType;
                string url = mediaResult.Url;
                string extension = mediaResult.FileExtension;

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (parameters.MediaPaths == null || !parameters.MediaPaths.TryGetValue(mediaType, out string? mediaFolder))
                        continue;

                    if (!string.IsNullOrEmpty(extension) && !extension.StartsWith("."))
                        extension = "." + extension;

                    string fileNamePrefix = Path.GetFileNameWithoutExtension(parameters.RomFileName) ?? string.Empty;
                    string fileName;
                    if (parameters.MediaSuffixes != null &&
                        parameters.MediaSuffixes.TryGetValue(mediaType, out var sfxInfo) &&
                        sfxInfo.SfxEnabled &&
                        !string.IsNullOrEmpty(sfxInfo.Suffix))
                        fileName = $"{fileNamePrefix}-{sfxInfo.Suffix}{extension}";
                    else
                        fileName = $"{fileNamePrefix}{extension}";

                    // Resolve the gamelist-relative mediaFolder (e.g. "./titleshots") to an absolute
                    // directory before combining — Path.Combine does not strip the leading "./" and
                    // would produce a malformed path 
                    string absoluteFolder = FilePathHelper.GamelistPathToFullPath(mediaFolder, parameters.ParentFolderPath!);
                    string fullPath = Path.Combine(absoluteFolder, fileName);

                    string regionDisplay = !string.IsNullOrEmpty(mediaResult.Region) ? $" ({mediaResult.Region})" : string.Empty;

                    bool downloadSuccess = await fileTransfer.DownloadFile(
                        verify: parameters.VerifyImageDownloads,
                        fileDownloadPath: fullPath,
                        url: url,
                        bearerToken: parameters.UserAccessToken ?? string.Empty);

                    if (downloadSuccess)
                    {
                        // Derive the gamelist entry from the actual saved path — avoids any double-prefix
                        // issues with mediaFolder values that already carry a "./" prefix.
                        scrapedData.Data[mediaType] = FilePathHelper.PathToRelativePathWithDotSlashPrefix(fullPath, parameters.ParentFolderPath!);
                        recordDownload(mediaType);
                        log?.Invoke($"{mediaType}{regionDisplay}: {Path.GetFileName(fullPath)}", LogLevel.Default, LogPrefix.Download, LogLevel.Success);
                    }
                    else
                    {
                        log?.Invoke($"{mediaType}{regionDisplay}: {Path.GetFileName(fullPath)}", LogLevel.Default, LogPrefix.Download, LogLevel.Error);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    log?.Invoke($"Error downloading {mediaType}: {ex.Message}", LogLevel.Default, null, LogLevel.Default);
                }
            }
        }
    }
}
