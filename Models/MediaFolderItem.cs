using CommunityToolkit.Mvvm.ComponentModel;

namespace Gamelist_Manager.Models
{
    public partial class MediaFolderItem : ObservableObject
    {
        public string Key { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string DefaultPath { get; init; } = string.Empty;
        public string DefaultSuffix { get; init; } = string.Empty;
        public bool DefaultEnabled { get; init; } = true;
        public bool DefaultSfxEnabled => !string.IsNullOrEmpty(DefaultSuffix);

        [ObservableProperty] private bool _enabled;
        [ObservableProperty] private string _path = string.Empty;
        [ObservableProperty] private string _suffix = string.Empty;
        [ObservableProperty] private bool _sfxEnabled;

        public void ResetToDefaults()
        {
            Path = DefaultPath;
            Suffix = DefaultSuffix;
            SfxEnabled = DefaultSfxEnabled;
        }
    }
}