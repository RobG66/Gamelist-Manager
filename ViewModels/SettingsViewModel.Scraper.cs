using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;

namespace Gamelist_Manager.ViewModels;

public partial class SettingsViewModel
{
    #region Fields

    private static readonly string DefaultFallbackJson =
        "[\"USA (us)\",\"Europe (eu)\",\"United Kingdom (uk)\",\"World (wor)\",\"Japan (jp)\",\"ScreenScraper (ss)\"]";

    private string SetupScraperName => SelectedSetupScraperIndex switch
    {
        0 => ScraperRegistry.ArcadeDB.Name,
        1 => ScraperRegistry.EmuMovies.Name,
        2 => ScraperRegistry.ScreenScraper.Name,
        _ => ScraperRegistry.ArcadeDB.Name
    };

    #endregion

    #region Observable Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSetupScreenScraper))]
    [NotifyPropertyChangedFor(nameof(IsSetupRequiresCredentials))]
    [NotifyPropertyChangedFor(nameof(IsSetupArcadeDB))]
    private int _selectedSetupScraperIndex;

    [ObservableProperty] private string _scraperUsername = string.Empty;
    [ObservableProperty] private string _scraperPassword = string.Empty;
    [ObservableProperty] private string? _selectedScraperLanguage;
    [ObservableProperty] private string? _selectedScraperPrimaryRegion;
    [ObservableProperty] private bool _scraperGenreAlwaysEnglish;
    [ObservableProperty] private bool _scraperScrapeAnyMedia;
    [ObservableProperty] private bool _scraperNamesLanguageFirst;
    [ObservableProperty] private bool _scraperMediaRegionFirst;
    [ObservableProperty] private string? _selectedScraperAvailableRegion;
    [ObservableProperty] private string? _selectedScraperFallbackRegion;

    #endregion

    #region Public Properties

    public ObservableCollection<string> ScraperLanguages { get; } = new();
    public ObservableCollection<string> ScraperPrimaryRegions { get; } = new();
    public ObservableCollection<string> ScraperAvailableRegions { get; } = new();
    public ObservableCollection<string> ScraperFallbackRegions { get; } = new();

    public bool IsSetupScreenScraper => SelectedSetupScraperIndex == 2;
    public bool IsSetupRequiresCredentials => SelectedSetupScraperIndex > 0;
    public bool IsSetupArcadeDB => SelectedSetupScraperIndex == 0;

    #endregion

    #region Property Change Callbacks

    partial void OnSelectedSetupScraperIndexChanged(int value)
    {
        if (!_isLoading)
            LoadScraperCredentials();
    }

    partial void OnSelectedScraperAvailableRegionChanged(string? value) =>
        AddScraperRegionCommand.NotifyCanExecuteChanged();

    partial void OnSelectedScraperFallbackRegionChanged(string? value)
    {
        RemoveScraperRegionCommand.NotifyCanExecuteChanged();
        MoveScraperRegionUpCommand.NotifyCanExecuteChanged();
        MoveScraperRegionDownCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Load / Save

    private void LoadScraperCredentials()
    {
        var scraperName = SetupScraperName;
        var (userName, password) = CredentialHelper.GetCredentials(scraperName);
        ScraperUsername = userName ?? string.Empty;
        ScraperPassword = password ?? string.Empty;

        if (scraperName == ScraperRegistry.ScreenScraper.Name)
            LoadRegionsAndLanguages();
    }

    private void LoadRegionsAndLanguages()
    {
        ScraperLanguages.Clear();
        ScraperPrimaryRegions.Clear();

        foreach (var region in ScraperConfigService.Instance.GetScraperRegions(ScraperRegistry.ScreenScraper.Name))
            ScraperPrimaryRegions.Add(region);

        foreach (var lang in ScraperConfigService.Instance.GetScraperLanguages(ScraperRegistry.ScreenScraper.Name))
            ScraperLanguages.Add(lang);

        var settings = SettingsService.Instance;
        var savedLanguage = settings.GetValue(SettingKeys.ScraperSection, "ScreenScraper_Language", string.Empty);
        SelectedScraperLanguage = ScraperLanguages.FirstOrDefault(l => l == savedLanguage) ?? ScraperLanguages.FirstOrDefault();

        var savedRegion = settings.GetValue(SettingKeys.ScraperSection, "ScreenScraper_PrimaryRegion", string.Empty);
        SelectedScraperPrimaryRegion = ScraperPrimaryRegions.FirstOrDefault(r => r == savedRegion) ?? ScraperPrimaryRegions.FirstOrDefault();

        ScraperGenreAlwaysEnglish = settings.GetBool(SettingKeys.ScraperSection, "ScreenScraper_GenreEnglish", false);
        ScraperScrapeAnyMedia = settings.GetBool(SettingKeys.ScraperSection, "ScreenScraper_AnyMedia", false);
        ScraperNamesLanguageFirst = settings.GetBool(SettingKeys.ScraperSection, "ScreenScraper_NamesLanguageFirst", false);
        ScraperMediaRegionFirst = settings.GetBool(SettingKeys.ScraperSection, "ScreenScraper_MediaRegionFirst", false);

        LoadScraperFallbackRegions();
    }

    private void LoadScraperFallbackRegions()
    {
        ScraperAvailableRegions.Clear();
        ScraperFallbackRegions.Clear();

        foreach (var region in ScraperPrimaryRegions)
            ScraperAvailableRegions.Add(region);

        var json = SettingsService.Instance.GetValue(SettingKeys.ScraperSection, "ScreenScraper_RegionFallback", DefaultFallbackJson);
        List<string> fallback;
        try { fallback = JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { fallback = JsonSerializer.Deserialize<List<string>>(DefaultFallbackJson) ?? []; }

        foreach (var region in fallback)
        {
            if (ScraperAvailableRegions.Remove(region))
                ScraperFallbackRegions.Add(region);
        }
    }

    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanAddScraperRegion))]
    private void AddScraperRegion()
    {
        if (SelectedScraperAvailableRegion == null) return;
        var item = SelectedScraperAvailableRegion;
        ScraperAvailableRegions.Remove(item);
        ScraperFallbackRegions.Add(item);
        SelectedScraperAvailableRegion = null;
        SelectedScraperFallbackRegion = item;
    }

    private bool CanAddScraperRegion() => SelectedScraperAvailableRegion != null;

    [RelayCommand(CanExecute = nameof(CanRemoveScraperRegion))]
    private void RemoveScraperRegion()
    {
        if (SelectedScraperFallbackRegion == null) return;
        var item = SelectedScraperFallbackRegion;
        ScraperFallbackRegions.Remove(item);
        ScraperAvailableRegions.Add(item);
        SelectedScraperFallbackRegion = null;
        SelectedScraperAvailableRegion = item;
    }

    private bool CanRemoveScraperRegion() => SelectedScraperFallbackRegion != null;

    [RelayCommand(CanExecute = nameof(CanMoveScraperRegionUp))]
    private void MoveScraperRegionUp()
    {
        if (SelectedScraperFallbackRegion == null) return;
        var i = ScraperFallbackRegions.IndexOf(SelectedScraperFallbackRegion);
        if (i <= 0) return;
        var item = SelectedScraperFallbackRegion;
        SelectedScraperFallbackRegion = null;
        ScraperFallbackRegions.Move(i, i - 1);
        SelectedScraperFallbackRegion = item;
        MoveScraperRegionUpCommand.NotifyCanExecuteChanged();
        MoveScraperRegionDownCommand.NotifyCanExecuteChanged();
    }

    private bool CanMoveScraperRegionUp() =>
        SelectedScraperFallbackRegion != null &&
        ScraperFallbackRegions.IndexOf(SelectedScraperFallbackRegion) > 0;

    [RelayCommand(CanExecute = nameof(CanMoveScraperRegionDown))]
    private void MoveScraperRegionDown()
    {
        if (SelectedScraperFallbackRegion == null) return;
        var i = ScraperFallbackRegions.IndexOf(SelectedScraperFallbackRegion);
        if (i < 0 || i >= ScraperFallbackRegions.Count - 1) return;
        var item = SelectedScraperFallbackRegion;
        SelectedScraperFallbackRegion = null;
        ScraperFallbackRegions.Move(i, i + 1);
        SelectedScraperFallbackRegion = item;
        MoveScraperRegionUpCommand.NotifyCanExecuteChanged();
        MoveScraperRegionDownCommand.NotifyCanExecuteChanged();
    }

    private bool CanMoveScraperRegionDown() =>
        SelectedScraperFallbackRegion != null &&
        ScraperFallbackRegions.IndexOf(SelectedScraperFallbackRegion) < ScraperFallbackRegions.Count - 1;

    [RelayCommand]
    private void ResetScraperFallback()
    {
        SettingsService.Instance.SetValue("Scraper", "ScreenScraper_RegionFallback", DefaultFallbackJson);
        LoadScraperFallbackRegions();
    }

    #endregion
}
