using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gamelist_Manager.Services
{
    public static class MetadataService
    {
        #region Private Caches

        private static readonly Dictionary<MetaDataKeys, MetaDataDecl> metaDataDictionary;
        private static readonly Dictionary<string, MetaDataDecl> typeToDecl;
        private static readonly Dictionary<string, MetaDataDecl> nameToDecl;

        #endregion

        #region Static Constructor

        static MetadataService()
        {
            var gameDecls = MetaDataDecl.AllDeclarations;

            metaDataDictionary = gameDecls.ToDictionary(d => d.Key, d => d);
            typeToDecl = gameDecls.ToDictionary(d => d.Type, d => d, StringComparer.OrdinalIgnoreCase);
            nameToDecl = gameDecls.ToDictionary(d => d.Name, d => d, StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region Private Helpers

        private static bool IsSupportedByCurrentProfile(MetaDataDecl decl) =>
            SessionState.Instance.ProfileType == SettingKeys.ProfileTypeEsDe
                ? decl.EsDeSupported
                : decl.EsSupported;

        private static bool IsToggleable(MetaDataDecl decl) =>
            decl.Viewable && !decl.AlwaysVisible && decl.Key != MetaDataKeys.desc && !decl.IsMedia;

        #endregion

        #region Public Accessors

        public static IReadOnlyDictionary<MetaDataKeys, MetaDataDecl> GetMetaDataDictionary() => metaDataDictionary;

        // Unfiltered — returns all regardless of profile, used for load/reset operations
        public static IReadOnlyList<MetaDataDecl> GetAllBoolMetadata() =>
            MetaDataDecl.AllDeclarations.Where(d => d.DataType == MetaDataType.Bool).ToList();

        public static IReadOnlyList<MetaDataDecl> GetAllViewableFields() =>
            MetaDataDecl.AllDeclarations.Where(d => d.Viewable).ToList();

        public static IReadOnlyList<MetaDataDecl> GetAllMediaFolderTypes() =>
            MetaDataDecl.AllDeclarations.Where(d => d.IsMedia).ToList();

        public static IReadOnlyList<MetaDataDecl> GetMediaMetadata() =>
            MetaDataDecl.AllDeclarations
                .Where(d => d.DataType == MetaDataType.Image || d.DataType == MetaDataType.Document || d.DataType == MetaDataType.Video)
                .OrderBy(d => d.DataType == MetaDataType.Video ? 1 : 0)
                .ThenBy(d => d.Key)
                .ToList();

        // Profile-filtered
        public static IReadOnlyList<MetaDataDecl> GetBoolMetadata() =>
            MetaDataDecl.AllDeclarations.Where(d => d.DataType == MetaDataType.Bool).Where(IsSupportedByCurrentProfile).ToList();

        public static IReadOnlyList<MetaDataDecl> GetMediaDeclarations() =>
            MetaDataDecl.AllDeclarations.Where(d => d.IsMedia).Where(IsSupportedByCurrentProfile).ToList();

        public static IReadOnlyList<MetaDataDecl> GetColumnDeclarations() =>
            MetaDataDecl.AllDeclarations.Where(d => d.Viewable).Where(IsSupportedByCurrentProfile).ToList();

        public static IReadOnlyList<MetaDataDecl> GetToggleableColumns() =>
            MetaDataDecl.AllDeclarations.Where(IsToggleable).Where(IsSupportedByCurrentProfile).ToList();

        public static IReadOnlyList<MetaDataDecl> GetXmlPersistedFields() =>
            MetaDataDecl.AllDeclarations.Where(d => d.Viewable).Where(IsSupportedByCurrentProfile).ToList();

        public static IReadOnlyList<string> GetScraperElements(string scraperName) =>
            ScraperConfigService.Instance.GetScraperElements(scraperName);

        public static string? GetPropertyName(MetaDataKeys key) =>
            metaDataDictionary.TryGetValue(key, out var decl) ? decl.PropertyName : null;

        public static string GetMetadataNameByType(string type) =>
            typeToDecl.TryGetValue(type, out var decl) ? decl.Name : string.Empty;

        public static string GetMetadataDataTypeByType(string type) =>
            typeToDecl.TryGetValue(type, out var decl) ? decl.DataType.ToString() : string.Empty;

        public static string GetMetadataTypeByName(string name) =>
            nameToDecl.TryGetValue(name, out var decl) ? decl.Type : string.Empty;

        public static MetaDataDecl? GetDeclByType(string type) =>
            typeToDecl.TryGetValue(type, out var decl) ? decl : null;

        public static MetaDataDecl? GetDeclByName(string name) =>
            nameToDecl.TryGetValue(name, out var decl) ? decl : null;

        #endregion
    }
}