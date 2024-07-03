using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace GamelistManager
{
    internal static class FileTransfer
    {
        public static async Task<bool> DownloadFile(bool overWriteData, string fileToDownload, string url)
        {
            //Console.WriteLine(fileToDownload);
            //Console.WriteLine(url);
            try
            {
                string parentFolder = Path.GetDirectoryName(fileToDownload);
                if (!Directory.Exists(parentFolder))
                {
                    Directory.CreateDirectory(parentFolder);
                }

                if (File.Exists(fileToDownload))
                {
                    if (overWriteData)
                    {
                        File.Delete(fileToDownload);
                    }
                    else
                    {
                        return false;
                    }
                }

                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync(new Uri(url), fileToDownload);
                    return true; // Return true if the download was successful
                }
            }
            catch
            {
                if (File.Exists(fileToDownload))
                {
                    File.Delete(fileToDownload);
                }
                // Console.WriteLine(url);
                return false; // Return false if an exception occurred during the download
            }
        }
    }
}
