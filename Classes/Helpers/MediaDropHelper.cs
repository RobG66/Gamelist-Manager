using Gamelist_Manager.Classes.IO;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gamelist_Manager.Classes.Helpers
{
    internal static class MediaDropHelper
    {
        internal static async Task<string?> DownloadImageFromUrlAsync(string url)
        {
            try
            {
                var factory = Startup.Services.GetRequiredService<System.Net.Http.IHttpClientFactory>();
                using var httpClient = factory.CreateClient("MediaDropClient");

                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var extension = "png";
                if (response.Content.Headers.ContentType?.MediaType is { } mediaType)
                {
                    extension = mediaType.ToLower() switch
                    {
                        "image/jpeg" => "jpg",
                        "image/png" => "png",
                        "image/gif" => "gif",
                        "image/bmp" => "bmp",
                        "image/webp" => "webp",
                        _ => Path.GetExtension(url).TrimStart('.').ToLower()
                    };
                }
                else
                {
                    extension = Path.GetExtension(url).TrimStart('.').ToLower();
                }

                if (string.IsNullOrEmpty(extension))
                    extension = "png";

                var tempPath = Path.Combine(Path.GetTempPath(), $"dropped_image_{Guid.NewGuid()}.{extension}");
                await using var fileStream = File.Create(tempPath);
                await response.Content.CopyToAsync(fileStream);
                return tempPath;
            }
            catch { }
            return null;
        }

        internal static string? ExtractImageUrlFromHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return null;
            var match = Regex.Match(
                html, @"<img[^>]+src=[""']([^""']+)[""']",
                RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
