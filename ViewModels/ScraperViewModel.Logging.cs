using Avalonia.Threading;
using Gamelist_Manager.Models;
using System;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class ScraperViewModel
{
    private Channel<string>? _logChannel;
    private Task? _logWriterTask;

    private void StartLogFileSession(string scraper, string system)
    {
        if (!_sharedData.LogToDisk) return;

        string logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string logFile = Path.Combine(logDir, $"scraper_{scraper}_{system}_{timestamp}.log");

        _logChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true
        });

        var reader = _logChannel.Reader;
        _logWriterTask = Task.Run(async () =>
        {
            await using var writer = new StreamWriter(logFile, append: false, System.Text.Encoding.UTF8);
            writer.AutoFlush = false;

            await foreach (var line in reader.ReadAllAsync())
            {
                await writer.WriteLineAsync(line);
            }

            await writer.FlushAsync();
        });
    }

    private async Task StopLogFileSession()
    {
        if (_logChannel == null) return;
        _logChannel.Writer.TryComplete();
        if (_logWriterTask != null)
            await _logWriterTask;
        _logChannel = null;
        _logWriterTask = null;
    }

    public void Log(string message, LogLevel level = LogLevel.Default, string? prefix = null, LogLevel prefixLevel = LogLevel.Default)
    {
        // Write to file channel first — non-blocking, safe from any thread
        if (_logChannel != null)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string prefixStr = prefix != null ? $"{prefix} " : string.Empty;
            _logChannel.Writer.TryWrite($"[{timestamp}] {prefixStr}{message}");
        }

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
}
