using Avalonia.Threading;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
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

        string system = _sharedData?.CurrentSystem ?? string.Empty;
        if (string.IsNullOrEmpty(system))
        {
            CacheCountText = "0 items cached";
            IsClearCacheEnabled = false;
            return;
        }

        string cacheFolder = Path.Combine(AppContext.BaseDirectory, "cache", _currentScraper, system);
        if (!Directory.Exists(cacheFolder) || Directory.GetFiles(cacheFolder).Length == 0)
        {
            CacheCountText = "Cache is empty";
            IsClearCacheEnabled = false;
        }
        else
        {
            int count = Directory.GetFiles(cacheFolder).Length;
            CacheCountText = $"{count} items cached";
            IsClearCacheEnabled = true;
        }
    }

    private void OnSharedDataPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SharedDataService.CurrentSystem):
                RefreshCacheCount();
                break;
            case nameof(SharedDataService.IsScraping):
                if (IsScraping != _sharedData.IsScraping)
                    IsScraping = _sharedData.IsScraping;
                break;
            case nameof(SharedDataService.IsBusy):
                OnPropertyChanged(nameof(IsBusy));
                break;
        }
    }
}