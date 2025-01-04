using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace GamelistManager.classes
{
    public sealed class Logger
    {
        // Lazy Singleton instance for thread-safe initialization
        private static readonly Lazy<Logger> _instance = new Lazy<Logger>(() => new Logger());
        private ObservableCollection<LogEntry> _logMessages;
        private ListBox _logListBox;

        // Private constructor to prevent external instantiation
        private Logger()
        {
            _logMessages = new ObservableCollection<LogEntry>();
        }

        // Public Singleton instance
        public static Logger Instance => _instance.Value;

        // Initialize the logger with a ListBox
        public void Initialize(ListBox logListBox)
        {
            if (logListBox == null)
                throw new ArgumentNullException(nameof(logListBox), "ListBox cannot be null.");

            _logListBox = logListBox;
            _logListBox.ItemsSource = _logMessages;
        }

        // Log a message with optional color asynchronously
        public async Task LogAsync(string message, Brush color = null)
        {
            //EnsureInitialized();

            var logEntry = new LogEntry
            {
                Message = $"{DateTime.Now:G}: {message}",
                Color = color ?? Brushes.Black
            };

            // Use Task.Run to offload the work to another thread
            await Task.Run(() =>
            {
                if (_logListBox.Dispatcher.CheckAccess())
                {
                    AppendMessage(logEntry);
                }
                else
                {
                    _logListBox.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        AppendMessage(logEntry);
                    }));
                }
            });
        }

        // Clear all logs asynchronously
        public async Task ClearLogAsync()
        {
            EnsureInitialized();

            // Use Task.Run to offload the work to another thread
            await Task.Run(() =>
            {
                if (_logListBox.Dispatcher.CheckAccess())
                {
                    _logMessages.Clear();
                }
                else
                {
                    _logListBox.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _logMessages.Clear();
                    }));
                }
            });
        }

        // Private method to append a message to the log
        private void AppendMessage(LogEntry logEntry)
        {
            _logMessages.Add(logEntry);

            // Scroll to the latest entry
            _logListBox.ScrollIntoView(_logMessages[^1]);

            // Limit the log size to prevent performance degradation
            const int MaxLogEntries = 500;
            if (_logMessages.Count > MaxLogEntries)
            {
                _logMessages.RemoveAt(0);
            }
        }

        // Ensure the logger is initialized
        private void EnsureInitialized()
        {
            if (_logListBox == null)
            {
                throw new InvalidOperationException("Logger must be initialized with a ListBox before use.");
            }
        }
    }

    // LogEntry class to support messages with color
    public class LogEntry
    {
        public string Message { get; set; }
        public Brush Color { get; set; }
    }
}
