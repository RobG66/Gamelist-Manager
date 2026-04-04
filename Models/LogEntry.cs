namespace Gamelist_Manager.Models;

public class LogEntry
{
    public string Timestamp { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public LogLevel PrefixLevel { get; set; }
    public string Message { get; set; } = string.Empty;
    public LogLevel Level { get; set; }
}
