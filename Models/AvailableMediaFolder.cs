namespace Gamelist_Manager.Models;

// Resolved media folder info for a single media type, as calculated by SettingsService.
// Callers receive a flat list of these — no knowledge of profile type, enabled state, or path resolution required.
public record AvailableMediaFolder(
    string Type,
    string Name,
    string FolderPath,
    string Suffix,
    bool SfxEnabled
);
