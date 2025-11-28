using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace GamelistManager.classes.helpers
{
    public sealed class LogHelper
    {
        // Lazy Singleton instance for thread-safe initialization
        private static readonly Lazy<LogHelper> _instance = new(() => new LogHelper());
        private ObservableCollection<LogEntry> _logMessages;
        private ListBox? _logListBox;

        // Private constructor to prevent external instantiation
        private LogHelper()
        {
            _logMessages = [];
        }

        // Public Singleton instance
        public static LogHelper Instance => _instance.Value;

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

            // Small delay so UI doesn’t get overwhelmed
            await Task.Delay(1);

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

        // Clear log
        public void ClearLog()
        {
            EnsureInitialized();

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
        }

        // Private method to append a message to the log
        private void AppendMessage(LogEntry logEntry)
        {
            _logMessages.Add(logEntry);

            // Scroll to the latest entry
            _logListBox.ScrollIntoView(_logMessages[^1]);

            // Limit the log size to prevent performance degradation
            const int MaxLogEntries = 200;
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
                throw new InvalidOperationException("LogHelper must be initialized with a ListBox before use.");
            }
        }
    }

    // LogEntry class to support messages with color
    public class LogEntry
    {
        public string? Message { get; set; }
        public Brush? Color { get; set; }
    }
}
