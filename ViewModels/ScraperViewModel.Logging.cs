using Avalonia.Threading;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.IO;

namespace Gamelist_Manager.ViewModels;

public partial class ScraperViewModel
{
    public void Log(string message, LogLevel level = LogLevel.Default, string? prefix = null, LogLevel prefixLevel = LogLevel.Default)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var entry = new LogEntry
            {
                Timestamp = $"[{DateTime.Now:HH:mm:ss}]",
                Prefix = prefix ?? string.Empty,
                PrefixLevel = prefixLevel,
                Message = message,
                Level = level
            };

            if (prefix == LogPrefix.Download)
            {
                DownloadLogEntries.Add(entry);
                while (DownloadLogEntries.Count > 100)
                    DownloadLogEntries.RemoveAt(0);
            }
            else
            {
                LogEntries.Add(entry);
                while (LogEntries.Count > 100)
                    LogEntries.RemoveAt(0);
            }

            if (prefix == LogPrefix.Scrape)
            {
                if (prefixLevel == LogLevel.Success) _scrapeSuccessCount++;
                else if (prefixLevel == LogLevel.Error) _scrapeFailedCount++;
                ScrapeSuccessText = _scrapeSuccessCount.ToString();
                ScrapeFailedText = _scrapeFailedCount.ToString();
            }
            else if (prefix == LogPrefix.Download)
            {
                if (prefixLevel == LogLevel.Success) _dlSuccessCount++;
                else if (prefixLevel == LogLevel.Error) _dlFailedCount++;
                DownloadSuccessText = _dlSuccessCount.ToString();
                DownloadFailedText = _dlFailedCount.ToString();
            }
        });
    }

    private void RefreshCacheCount()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(RefreshCacheCount);
            return;
        }

        if (CurrentScraper == "EmuMovies")
        {
            CacheCountText = "";
            IsClearCacheEnabled = false;
            return;
        }

        string system = _sharedData?.CurrentSystem ?? string.Empty;
        if (string.IsNullOrEmpty(system))
        {
            CacheCountText = "0 items cached";
            IsClearCacheEnabled = false;
            return;
        }

        string cacheFolder = Path.Combine(AppContext.BaseDirectory, "cache", CurrentScraper, system);
        var files = Directory.Exists(cacheFolder) ? Directory.GetFiles(cacheFolder) : [];

        if (files.Length == 0)
        {
            IsClearCacheEnabled = false;
            CacheCountText = "Cache is empty";
        }
        else
        {
            CacheCountText = $"{files.Length} items cached";
            IsClearCacheEnabled = true;
        }
    }
}