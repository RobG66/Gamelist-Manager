using System.Collections.Generic;

namespace Gamelist_Manager.Models
{
    public class CustomColumnDecl
    {
        public required string Type { get; init; }
        public required string Name { get; init; }
        public required string PropertyName { get; init; }
        public string SortPropertyName { get; init; } = string.Empty;
        public bool DefaultVisible { get; init; } = true;

        public static readonly IReadOnlyList<CustomColumnDecl> AllDeclarations = new List<CustomColumnDecl>
        {
            new() { Type = "RomSize", Name = "Rom Size", PropertyName = nameof(GameMetadataRow.RomFileSize), SortPropertyName = nameof(GameMetadataRow.RomFileSizeBytes), DefaultVisible = false },
            new() { Type = "RomExtension", Name = "Extension", PropertyName = nameof(GameMetadataRow.RomExtension), DefaultVisible = false },
      new() { Type = "MissingMedia", Name = "Missing Media", PropertyName = nameof(GameMetadataRow.MissingMedia), DefaultVisible = false }  };

    }
}