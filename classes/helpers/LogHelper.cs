using GamelistManager.classes.core;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace GamelistManager.classes.helpers
{
    // Log entry
    public class LogEntry
    {
        public string? Message { get; set; }
        public Brush? Color { get; set; }
    }

    // Sink interface
    public interface ILogSink
    {
        void Emit(LogEntry entry);
    }

    public class UiLogSink : ILogSink, IDisposable
    {
        private readonly ObservableCollection<LogEntry> _uiList;
        private readonly object _uiLock;

        private readonly ConcurrentQueue<LogEntry> _buffer = new();
        private readonly CancellationTokenSource _cts = new();

        private readonly int _uiWindowSize = 300;
        private readonly TimeSpan _uiUpdateRate = TimeSpan.FromMilliseconds(50);

        private readonly ListBox? _listBox;

        public UiLogSink(ObservableCollection<LogEntry> uiList, object uiLock, ListBox? listBox)
        {
            _uiList = uiList;
            _uiLock = uiLock;
            _listBox = listBox;

            Task.Run(FlushLoopAsync);
        }

        public void Emit(LogEntry entry)
        {
            _buffer.Enqueue(entry);
        }

        private async Task FlushLoopAsync()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(_uiUpdateRate, _cts.Token);

                    if (_buffer.IsEmpty)
                        continue;

                    // Drain buffer fast
                    List<LogEntry> batch = new();
                    while (_buffer.TryDequeue(out var e))
                        batch.Add(e);

                    if (batch.Count == 0)
                        continue;

                    // UI update is THROTTLED and LIGHTWEIGHT
                    _listBox?.Dispatcher.InvokeAsync(() =>
                    {
                        lock (_uiLock)
                        {
                            foreach (var msg in batch)
                                _uiList.Add(msg);

                            // UI window limit: remove oldest
                            while (_uiList.Count > _uiWindowSize)
                                _uiList.RemoveAt(0);
                        }

                        // Only scroll if list has items
                        if (_uiList.Count > 0)
                            _listBox?.ScrollIntoView(_uiList[^1]);

                    }, DispatcherPriority.Background);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on disposal
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }


    // File Sink
    public class FileLogSink : ILogSink
    {
        private readonly ConcurrentBag<string> _buffer = new();
        private string? _filePath;
        private volatile bool _isActive;

        public void StartLog(string folder, string systemName)
        {
            Directory.CreateDirectory(folder);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _filePath = Path.Combine(folder, $"{systemName}_scrape_log_{timestamp}.txt");

            _buffer.Clear();
            _buffer.Add("=".PadRight(80, '='));
            _buffer.Add($"Scrape Log - {DateTime.Now:F}");
            _buffer.Add("=".PadRight(80, '='));
            _buffer.Add("");

            _isActive = true;
        }

        public void Emit(LogEntry entry)
        {
            if (_isActive)
                _buffer.Add(entry.Message ?? string.Empty);
        }

        public async Task FlushAsync()
        {
            var path = _filePath;
            if (!_isActive || string.IsNullOrEmpty(path))
                return;

            _buffer.Add("");
            _buffer.Add("=".PadRight(80, '='));
            _buffer.Add($"Log ended - {DateTime.Now:F}");
            _buffer.Add("=".PadRight(80, '='));

            var lines = _buffer.ToArray();
            await File.WriteAllLinesAsync(path, lines);

            _buffer.Clear();
            _filePath = null;
            _isActive = false;
        }
    }


    public sealed class LogHelper : IDisposable
    {
        private static readonly Lazy<LogHelper> _instance = new(() => new LogHelper());
        public static LogHelper Instance => _instance.Value;

        private readonly ObservableCollection<LogEntry> _logMessages = new();
        private readonly object _collectionLock = new();
        private ListBox? _logListBox;

        private readonly List<ILogSink> _sinks = new();
        private readonly FileLogSink _fileSink = new();
        private bool _disposed;

        private LogHelper()
        {
            BindingOperations.EnableCollectionSynchronization(_logMessages, _collectionLock);

            // Add default file sink
            AddSink(_fileSink);
        }

        // Initialize UI
        public void Initialize(ListBox listBox)
        {
            _logListBox = listBox ?? throw new ArgumentNullException(nameof(listBox));
            _logListBox.ItemsSource = _logMessages;

            // Add updated UI sink
            AddSink(new UiLogSink(_logMessages, _collectionLock, _logListBox));
        }

        // Add custom sink
        public void AddSink(ILogSink sink)
        {
            if (!_sinks.Contains(sink))
                _sinks.Add(sink);
        }

        // Log a message to all sinks (now synchronous)
        public void Log(string message, Brush? color = null)
        {
            var entry = new LogEntry
            {
                Message = $"{DateTime.Now:G}: {message}",
                Color = color ?? Brushes.Black
            };

            foreach (var sink in _sinks)
                sink.Emit(entry);
        }

        // Start file logging
        public void StartFileLog(string folder)
        {
            _fileSink.StartLog(folder, SharedData.CurrentSystem);
        }

        // Flush file log
        public async Task FlushFileLogAsync()
        {
            await _fileSink.FlushAsync();
        }

        // Clear UI log
        public void ClearLog()
        {
            if (_logListBox == null)
                return;

            if (_logListBox.Dispatcher.CheckAccess())
                _logMessages.Clear();
            else
                _logListBox.Dispatcher.Invoke(() => _logMessages.Clear());
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // Dispose all disposable sinks
            foreach (var sink in _sinks.OfType<IDisposable>())
            {
                sink.Dispose();
            }

            _sinks.Clear();
        }
    }
}