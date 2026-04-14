namespace Gamelist_Manager.Models;

// Runtime media configuration for a single media type (e.g. "image", "video").
// Built by SharedDataService from user settings; the corresponding MetaDataDecl
// holds the immutable schema defaults these values override.
public class MediaTypeSettings
{
    public required string Type { get; init; }
    public bool Enabled { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;
    public bool SfxEnabled { get; set; }
}
