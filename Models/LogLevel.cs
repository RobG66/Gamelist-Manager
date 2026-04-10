namespace Gamelist_Manager.Models;

// Mapped to theme brushes by LogLevelToBrushConverter.
public enum LogLevel
{
    Default,
    Info,
    Status,
    Success,
    Warning,
    Error
}

// Checked by ScraperViewModel.Logging to route entries to the correct counter.
internal static class LogPrefix
{
    internal const string Scrape = "→ ";
    internal const string Download = "↓ ";
}
