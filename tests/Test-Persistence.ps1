param(
    [string] $BuildDirectory = (Join-Path $PSScriptRoot '../bin/Release/net10.0')
)

# Test the built application in a new isolated host on every run. The host guards
# all settings paths before writes; no installed profiles, gamelists or backups
# are read or modified. Native UI/media startup is not required.
$ErrorActionPreference = 'Stop'
$buildPath = (Resolve-Path -LiteralPath $BuildDirectory).Path
if (-not (Test-Path -LiteralPath (Join-Path $buildPath 'Gamelist_Manager.dll'))) {
    throw 'Build Gamelist_Manager.csproj before running these regression tests.'
}
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$testRoot = Join-Path $tempRoot ('GamelistPersistenceTests-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($testRoot) | Out-Null
try {
    $references = foreach ($assembly in Get-ChildItem -LiteralPath $buildPath -Filter '*.dll') {
        try { [Reflection.AssemblyName]::GetAssemblyName($assembly.FullName) | Out-Null }
        catch [System.BadImageFormatException] { continue }
        '<Reference Include="' + [Security.SecurityElement]::Escape($assembly.BaseName) + '"><HintPath>' + [Security.SecurityElement]::Escape($assembly.FullName) + '</HintPath></Reference>'
    }
    $project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>' + ($references -join [Environment]::NewLine) + '</ItemGroup></Project>'
    Set-Content -LiteralPath (Join-Path $testRoot 'RegressionTests.csproj') -Value $project
    Set-Content -LiteralPath (Join-Path $testRoot 'NuGet.Config') -Value '<configuration><packageSources><clear /></packageSources></configuration>'
    @'
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Platform.Storage;
using DynamicData;
using System.Xml.Linq;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using Gamelist_Manager.ViewModels;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Messages;
using CommunityToolkit.Mvvm.Messaging;
class Program
{
    static int pass, fail;
    static SettingsState S => SettingsState.Instance;
    static SessionState Session => SessionState.Instance;
    static string FixtureProfile = "";
    static void Check(bool ok, string why) { if (!ok) throw new Exception(why); }
    static void Under(string path, string root) { Check(Path.GetFullPath(path).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "Path escapes isolated host: " + path); }
    static void Guard()
    {
        Under(ProfileService.Instance.ActiveProfilePath, AppContext.BaseDirectory);
        var settingsPath = (string)typeof(SettingsService).GetField("_settingsFilePath", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(SettingsService.Instance)!;
        Under(settingsPath, AppContext.BaseDirectory); Check(settingsPath == FixtureProfile, "Unexpected settings destination");
    }
    static void Test(string name, Action action) { try { Guard(); action(); pass++; Console.WriteLine("PASS " + name); } catch (Exception ex) { fail++; Console.WriteLine("FAIL " + name + ": " + ex.Message); } }
    static void Put(string section, params (string, string)[] values) { Guard(); var all = IniFileService.ReadIniFile(FixtureProfile); if (!all.ContainsKey(section)) all[section] = new(); foreach (var (k, v) in values) all[section][k] = v; IniFileService.WriteIniFile(FixtureProfile, all); typeof(SettingsService).GetMethod("InvalidateCache", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(SettingsService.Instance, null); S.Reload(); }
    static void Media(ScraperViewModel vm, bool enabled) { typeof(SessionState).GetProperty("AvailableMedia")!.SetValue(Session, new List<AvailableMediaFolder> { new("image", "Image", AppContext.BaseDirectory, "", enabled, false) }); typeof(ScraperViewModel).GetMethod("OnSessionStatePropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, new object[] { Session, new PropertyChangedEventArgs("AvailableMedia") }); }
    // Exercise the real provider and deployed INI mappings without live API calls.
    sealed class ScreenScraperHandler : System.Net.Http.HttpMessageHandler
    {
        public int Requests;
        public Uri? LastUri;
        public string Body = "<Data><ssuser><requeststoday>7</requeststoday><maxrequestsperday>100</maxrequestsperday></ssuser><jeu id=\"123\"><noms><nom region=\"eu\">Zoop (Europe)</nom></noms><medias/></jeu></Data>";
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Requests++; LastUri = request.RequestUri;
            return Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new System.Net.Http.StringContent(Body) });
        }
    }
    static void ScreenScraperTests()
    {
        var apiType = typeof(ScraperParameters).Assembly.GetType("Gamelist_Manager.Classes.Api.API_ScreenScraper", true)!;
        var method = apiType.GetMethod("ScrapeScreenScraperAsync")!;
        using var handler = new ScreenScraperHandler();
        using var client = new System.Net.Http.HttpClient(handler);
        var api = Activator.CreateInstance(apiType, client)!;
        (bool Success, ScrapedGameData Data, List<string> Messages, int Progress, int Max) Scrape(ScraperParameters p) =>
            ((Task<(bool, ScrapedGameData, List<string>, int, int)>)method.Invoke(api, new object[] { p, CancellationToken.None })!).GetAwaiter().GetResult();
        ScraperParameters Parameters(string system) => new()
        {
            SystemID = ScraperConfigService.Instance.GetScraperSystemId("ScreenScraper", system),
            RomFileName = "Zoop (Europe).zip", RomName = "Zoop (Europe)",
            ElementsToScrape = new() { "name" }, SSRegions = new() { "us", "eu" },
            CacheFolder = Path.Combine(AppContext.BaseDirectory, "scraper-cache-fixture", system),
            UserID = "fixture-user", UserPassword = "fixture-password"
        };
        foreach (var system in new[] { "genesis", "megadrive" })
        {
            Test(system + " sends the shared system ID and saves usable XML to its own cache", () =>
            {
                var p = Parameters(system);
                Check(p.SystemID == "1", "missing or incorrect system mapping");
                int before = handler.Requests;
                var result = Scrape(p);
                Check(result.Success && result.Data.Data["name"] == "Zoop (Europe)", "game metadata not scraped");
                Check(result.Progress == 7 && result.Max == 100, "scrape limits lost");
                Check(handler.Requests == before + 1 && handler.LastUri!.Query.Contains("systemeid=1&"), "incorrect provider request");
                var cache = Path.Combine(p.CacheFolder!, "Zoop (Europe).xml");
                Check(File.Exists(cache), "successful scrape was not cached");
                var saved = XDocument.Load(cache);
                Check(saved.Root!.Element("jeu")!.Attribute("id")!.Value == "123", "wrong cached game");
                p.ScrapeByCache = true; p.SystemID = null;
                result = Scrape(p);
                Check(result.Success && handler.Requests == before + 1, "cache read requires system mapping or uses network");
            });
        }
        Test("unmapped systems fail clearly without sending requests or writing cache", () =>
        {
            var p = Parameters("unmapped-system"); int before = handler.Requests;
            foreach (var id in new string?[] { null, "", " " })
            {
                p.SystemID = id; var result = Scrape(p);
                Check(!result.Success && result.Messages.Any(m => m.Contains("No ScreenScraper system ID")), "missing-ID error not reported");
            }
            Check(handler.Requests == before && !Directory.Exists(p.CacheFolder), "invalid request sent or cached");
            p.ScrapeByCache = true; p.SkipNonCached = true;
            Check(Scrape(p).Messages.Any(m => m.Contains("skip non-cached enabled")) && handler.Requests == before, "skip non-cached behavior changed");
        });
        Test("malformed ScreenScraper responses are still excluded from cache", () =>
        {
            var p = Parameters("malformed-response"); p.SystemID = "1"; handler.Body = "<Data/>\nUnexpected provider output";
            var result = Scrape(p);
            Check(!result.Success && result.Messages.Any(m => m.Contains("Failed to parse XML")), "bad response accepted");
            Check(!Directory.Exists(p.CacheFolder), "invalid XML was cached");
        });
    }
    static void SaveTests()
    {
        using var cache = new SourceCache<GameMetadataRow, string>(g => g.Path);
        cache.AddOrUpdate(new GameMetadataRow { Path = "./saved.zip", Name = "Saved" });
        var dialogs = new FakeDialogs();
        // Isolate the real save/navigation methods from native media and window startup.
        var vm = (MainWindowViewModel)RuntimeHelpers.GetUninitializedObject(typeof(MainWindowViewModel));
        foreach (var (name, value) in new (string, object)[] { ("_sessionState", Session), ("_settingsState", S), ("_sourceCache", cache), ("_windowOwnerProvider", new FakeOwner()), ("_dialogService", dialogs) })
            typeof(MainWindowViewModel).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(vm, value);
        bool CanNavigate() => ((Task<bool>)typeof(MainWindowViewModel).GetMethod("CheckUnsavedChangesAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, null)!).GetAwaiter().GetResult();
        S.EnableSaveReminder = true;
        Test("save without destination reports failure", () => { Session.Clear(); Check(!vm.TrySaveGamelistAsync().GetAwaiter().GetResult(), "missing destination reported success"); });
        var blocked = Path.Combine(AppContext.BaseDirectory, "directory-destination"); Directory.CreateDirectory(blocked);
        Test("failed save blocks navigation and retains edits", () => { Session.SetGamelist(blocked, "test", new()); Session.IsDataChanged = true; dialogs.Result = ThreeButtonResult.Button3; Check(!CanNavigate(), "navigation allowed after failed save"); Check(Session.IsDataChanged && !Session.IsBusy && dialogs.Errors == 1, "failure state changed"); });
        Test("cancel blocks navigation without saving", () => { dialogs.Result = ThreeButtonResult.Button1; Check(!CanNavigate() && dialogs.Errors == 1 && Session.IsDataChanged, "cancel saved or allowed navigation"); });
        Test("explicit discard allows navigation", () => { dialogs.Result = ThreeButtonResult.Button2; Check(CanNavigate() && dialogs.Errors == 1, "discard changed"); });
        Test("save exception blocks navigation and releases busy state", () =>
        {
            var field = typeof(MainWindowViewModel).GetField("_sourceCache", BindingFlags.Instance | BindingFlags.NonPublic)!;
            field.SetValue(vm, null); try { dialogs.Result = ThreeButtonResult.Button3; Check(!CanNavigate() && Session.IsDataChanged && !Session.IsBusy && dialogs.Errors == 2, "exception allowed navigation or left wrong state"); } finally { field.SetValue(vm, cache); }
        });
        Test("successful save permits navigation and persists edits", () => { Session.XmlFilename = Path.Combine(AppContext.BaseDirectory, "success.xml"); dialogs.Result = ThreeButtonResult.Button3; Check(CanNavigate() && !Session.IsDataChanged && !Session.IsBusy && dialogs.Errors == 2, "successful save left wrong state"); Check(XDocument.Load(Session.XmlFilename).Root!.Element("game")!.Element("name")!.Value == "Saved", "edits not saved"); });
    }
    sealed class FakeOwner : IWindowOwnerProvider { public object? GetMainWindowOwner() => this; public IStorageProvider? GetStorageProvider() => null; }
    sealed class FakeDialogs : IDialogService
    {
        public ThreeButtonResult Result = ThreeButtonResult.Button3; public int Errors;
        public string? LastError; public TaskCompletionSource<bool>? Confirmation;
        public Task<ThreeButtonResult> ShowAsync(ThreeButtonDialogConfig config, object? owner = null) => Task.FromResult(Result);
        public Task ShowErrorAsync(string title, string message, string? detail = null, object? owner = null) { Errors++; LastError = detail; return Task.CompletedTask; }
        public Task ShowInfoAsync(string title, string message, string? detail = null, object? owner = null) => Task.CompletedTask;
        public Task ShowWarningAsync(string title, string message, string? detail = null, object? owner = null) => Task.CompletedTask;
        public Task<bool> ShowConfirmAsync(string title, string message, string confirmText = "Yes", string cancelText = "Cancel", DialogIconTheme icon = DialogIconTheme.Question, string? detail = null, object? owner = null) => Confirmation?.Task ?? Task.FromResult(false);
    }

    static void ActivateFixture(string name)
    {
        var path = ProfileService.Instance.GetProfilePath(name);
        Under(path, AppContext.BaseDirectory);
        ProfileService.Instance.SetActiveProfile(name);
        SettingsService.Instance.SwitchProfile(path);
        FixtureProfile = path;
        Guard(); S.Reload();
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference DisposedSettings()
    {
        var vm = new SettingsViewModel(new FakeDialogs());
        vm.Dispose(); vm.Dispose();
        return new WeakReference(vm);
    }
    static void SettingsTests()
    {
        var profiles = ProfileService.Instance;
        var settings = SettingsService.Instance;
        var original = profiles.ActiveProfile;
        Session.Clear();
        Test("recent-file clear survives reload", () =>
        {
            profiles.SaveRecentFiles(new() { "first.xml", "second.xml" });
            profiles.SaveRecentFiles(new());
            settings.SwitchProfile(FixtureProfile);
            Check(settings.GetRecentFiles().Count == 0, "cleared history returned");
        });
        Test("shorter recent-file list removes old tail and preserves settings", () =>
        {
            Put(SettingKeys.AppearanceSection, (SettingKeys.Theme.Key, "Dark"));
            profiles.SaveRecentFiles(new() { "first.xml", "second.xml", "third.xml" });
            profiles.SaveRecentFiles(new() { "replacement.xml" });
            settings.SwitchProfile(FixtureProfile);
            Check(settings.GetRecentFiles().SequenceEqual(new[] { "replacement.xml" }), "old trailing entries survived");
            Check(settings.GetValue(SettingKeys.Theme) == "Dark", "unrelated settings changed");
        });
        Test("batch maximum persists in its declared section", () =>
        {
            using var vm = new SettingsViewModel(new FakeDialogs());
            vm.MaxBatch = 123; vm.SaveSettings();
            settings.SwitchProfile(FixtureProfile); S.Reload();
            using var reopened = new SettingsViewModel(new FakeDialogs());
            Check(settings.GetInt(SettingKeys.BatchProcessingMaximum) == 123 && reopened.MaxBatch == 123, "batch size did not persist");
        });
        Test("ScreenScraper profile switch reloads every provider option", () =>
        {
            Put(SettingKeys.ScraperOptionsSection,
                (SettingKeys.ScreenScraperLanguage.Key, "French (fr)"),
                (SettingKeys.ScreenScraperPrimaryRegion.Key, "Europe (eu)"),
                (SettingKeys.ScreenScraperGenreEnglish.Key, "True"),
                (SettingKeys.ScreenScraperAnyMedia.Key, "False"),
                (SettingKeys.ScreenScraperNamesLanguageFirst.Key, "True"),
                (SettingKeys.ScreenScraperMediaRegionFirst.Key, "True"),
                (SettingKeys.ScreenScraperRegionFallback.Key, "[\"Japan (jp)\"]"));
            using var vm = new SettingsViewModel(new FakeDialogs()); vm.SelectedSetupScraperIndex = 2;
            Check(vm.SelectedScraperLanguage == "French (fr)", "profile A fixture not loaded");
            const string other = "Secondary profile";
            Under(profiles.GetProfilePath(other), AppContext.BaseDirectory);
            Check(profiles.CreateTypedProfile(other, SettingKeys.ProfileTypeEs) == other, "profile B creation failed");
            try
            {
                ActivateFixture(other); vm.LoadSettings();
                Check(!vm.SettingsChanged, "loading a profile dirtied its settings");
                Check(vm.SelectedScraperLanguage == SettingKeys.ScreenScraperLanguage.Default &&
                      vm.SelectedScraperPrimaryRegion == SettingKeys.ScreenScraperPrimaryRegion.Default &&
                      !vm.ScraperGenreAlwaysEnglish && vm.ScraperScrapeAnyMedia &&
                      !vm.ScraperNamesLanguageFirst && !vm.ScraperMediaRegionFirst &&
                      vm.ScraperFallbackRegions.Count > 1, "profile A options leaked into B");
                vm.SaveSettings();
                Check(settings.GetValue(SettingKeys.ScreenScraperLanguage) == "English (en)", "saving B copied A language");
            }
            finally { ActivateFixture(original); }
            Check(settings.GetValue(SettingKeys.ScreenScraperLanguage) == "French (fr)", "profile A changed");
        });
        Test("credential switch saves the captured provider and edits", () =>
        {
            Check(CredentialHelper.SaveCredentials("EmuMovies", "emu-original", "dummy-one"), "cannot create dummy credentials");
            Check(CredentialHelper.SaveCredentials("ScreenScraper", "screen-original", "dummy-two"), "cannot create dummy credentials");
            var dialogs = new FakeDialogs { Confirmation = new() };
            using var vm = new SettingsViewModel(dialogs); vm.SelectedSetupScraperIndex = 1;
            vm.ScraperUsername = "emu-edited"; vm.ScraperPassword = "dummy-edited";
            vm.SelectedSetupScraperIndex = 2;
            Check(vm.ScraperUsername == "screen-original", "new provider not loaded");
            dialogs.Confirmation.SetResult(true);
            Check(CredentialHelper.GetCredentials("EmuMovies") == ("emu-edited", "dummy-edited"), "previous edits were lost");
            Check(CredentialHelper.GetCredentials("ScreenScraper") == ("screen-original", "dummy-two"), "other provider was overwritten");
            Check(vm.ScraperUsername == "screen-original", "prompt completion replaced current fields");
        });
        Test("discarding credential edits preserves both saved accounts", () =>
        {
            var dialogs = new FakeDialogs { Confirmation = new() };
            using var vm = new SettingsViewModel(dialogs); vm.SelectedSetupScraperIndex = 1;
            vm.ScraperUsername = "discard-me"; vm.SelectedSetupScraperIndex = 2;
            dialogs.Confirmation.SetResult(false);
            Check(CredentialHelper.GetCredentials("EmuMovies").UserName == "emu-edited" &&
                  CredentialHelper.GetCredentials("ScreenScraper").UserName == "screen-original", "discard saved edits");
        });
        Test("profile names cannot escape through create, copy, rename or template", () =>
        {
            var sentinel = Path.Combine(AppContext.BaseDirectory, "ini", "escaped.ini");
            File.WriteAllText(sentinel, "must survive");
            foreach (var name in new[] { "../escaped", "..\\escaped", ".", "..", "bad\nname", "bad\0name", Path.Combine(AppContext.BaseDirectory, "absolute") })
            {
                Check(!profiles.CreateProfile(name, true), "copy accepted invalid name");
                Check(profiles.CreateTypedProfile(name, SettingKeys.ProfileTypeEs) == null, "typed create accepted invalid name");
                Check(!profiles.CreateProfileFromTemplate(name, new(), overwrite: true), "template accepted invalid name");
                Check(!profiles.RenameProfile(original, name), "rename accepted invalid name");
                Check(!profiles.DeleteProfile(name), "delete accepted invalid name");
                try { profiles.GetProfilePath(name); throw new Exception("path resolution accepted invalid name"); }
                catch (ArgumentException) { }
            }
            Check(File.ReadAllText(sentinel) == "must survive" && profiles.ActiveProfile == original && File.Exists(FixtureProfile), "invalid input changed files or active profile");
            Check(profiles.CreateTypedProfile("Living Room é", SettingKeys.ProfileTypeEs) == "Living Room é", "normal Unicode name rejected");
        });
        Test("new-profile controls reject invalid names", () =>
        {
            using var vm = new SettingsViewModel(new FakeDialogs()); vm.NewProfileName = "../escape";
            Check(!vm.CreateNewProfileCommand.CanExecute(null) && !vm.CopyProfileCommand.CanExecute(null) && vm.HasProfileNameError, "UI permits invalid profile name");
        });
        Test("reopened scraper reads the last saved provider", () =>
        {
            Put(SettingKeys.ScraperOptionsSection, (SettingKeys.SelectedScraper.Key, "ArcadeDB"));
            using (var scraper = new ScraperViewModel()) { scraper.CurrentScraper = "ScreenScraper"; }
            Check(S.SelectedScraper == "ArcadeDB", "fixture did not exercise stale state");
            using var reopened = new ScraperViewModel();
            Check(reopened.CurrentScraper == "ScreenScraper", "reopened from stale singleton state");
        });
        Test("reset synchronizes disk, memory, controls and recent files", () =>
        {
            Put(SettingKeys.AppearanceSection, (SettingKeys.Theme.Key, "Dark"));
            Session.RecentFiles.Add(new RecentFileItem("old.xml")); profiles.SaveRecentFiles(new() { "old.xml" });
            using var vm = new SettingsViewModel(new FakeDialogs());
            vm.ResetAllSettings();
            Check(settings.GetValue(SettingKeys.Theme) == "Light" && S.Theme == "Light" && vm.SelectedThemeIndex == 0, "reset left stale appearance");
            Check(Session.RecentFiles.Count == 0 && settings.GetRecentFiles().Count == 0 && !vm.SettingsChanged, "reset left history or dirty state");
            vm.SaveSettings(); Check(settings.GetValue(SettingKeys.Theme) == "Light", "save resurrected old settings");
        });
        Test("reset discards open scraper drafts before disposal", () =>
        {
            using var scraper = new ScraperViewModel(); scraper.MetaName = true;
            var receiver = new object(); bool notified = false;
            WeakReferenceMessenger.Default.Register<SettingsAppliedMessage>(receiver, (_, message) =>
            { if (message.ResetScraperSettings) { notified = true; scraper.ResetSettings(); } });
            try
            {
                using var vm = new SettingsViewModel(new FakeDialogs()); vm.ResetAllSettings();
                Check(notified && !scraper.MetaName && scraper.CurrentScraper == "ArcadeDB", "reset kept stale panel draft");
                scraper.Dispose();
                Check(!settings.GetBool(SettingKeys.ScrapersSection, "ArcadeDB_MetaName"), "disposing panel undid reset");
            }
            finally { WeakReferenceMessenger.Default.UnregisterAll(receiver); }
        });
        Test("disposed settings stop receiving singleton notifications", () =>
        {
            using var vm = new SettingsViewModel(new FakeDialogs()); int calls = 0;
            vm.PropertyChanged += (_, _) => calls++;
            vm.Dispose(); vm.Dispose();
            Session.CurrentSystem = "disposed-test";
            Check(calls == 0, "disposed settings still receive singleton events"); Session.Clear();
        });
        Test("disposed settings can be garbage collected", () =>
        {
            var weak = DisposedSettings(); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            Check(!weak.IsAlive, "singleton retains disposed view model");
        });
    }
    static void LoadFailureTests()
    {
        var malformed = Path.Combine(AppContext.BaseDirectory, "malformed.xml");
        File.WriteAllText(malformed, "<gameList>\n<game>");
        Test("malformed XML reports parser details", () =>
        {
            try { GamelistService.LoadGamelist(malformed); throw new Exception("malformed XML accepted"); }
            catch (System.Xml.XmlException ex) { Check(ex.LineNumber > 0, "missing error location"); }
        });
        Test("wrong-root XML is rejected but valid empty gamelists load", () =>
        {
            var file = Path.Combine(AppContext.BaseDirectory, "root.xml"); File.WriteAllText(file, "<notAGamelist/>");
            try { GamelistService.LoadGamelist(file); throw new Exception("wrong root accepted"); }
            catch (InvalidDataException) { }
            File.WriteAllText(file, "<gameList/>"); Check(GamelistService.LoadGamelist(file).Games.Count == 0, "valid empty gamelist rejected");
        });
        Test("failed load preserves the current session, edits and status", () =>
        {
            var previous = Path.Combine(AppContext.BaseDirectory, "previous.xml");
            var games = new System.Collections.ObjectModel.ObservableCollection<GameMetadataRow> { new() { Path = "./keep.zip", Name = "Keep" } };
            Session.SetGamelist(previous, "prior", games); Session.IsDataChanged = true; S.RootRomFolder = "";
            using var cache = new SourceCache<GameMetadataRow, string>(g => g.Path); cache.AddOrUpdate(games);
            var dialogs = new FakeDialogs(); var vm = (MainWindowViewModel)RuntimeHelpers.GetUninitializedObject(typeof(MainWindowViewModel));
            foreach (var (name, value) in new (string, object)[] { ("_sessionState", Session), ("_settingsState", S), ("_sourceCache", cache), ("_windowOwnerProvider", new FakeOwner()), ("_dialogService", dialogs) })
                typeof(MainWindowViewModel).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(vm, value);
            vm.FileStatusText = "previous status"; vm.LastModifiedText = "previous time";
            var loaded = ((Task<bool>)typeof(MainWindowViewModel).GetMethod("LoadGamelistFromFileAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, new object[] { malformed })!).GetAwaiter().GetResult();
            Check(!loaded && dialogs.Errors == 1 && !string.IsNullOrEmpty(dialogs.LastError), "load failure not reported");
            Check(Session.XmlFilename == previous && ReferenceEquals(Session.GamelistData, games) && Session.IsDataChanged && !Session.IsBusy && cache.Count == 1, "failed load changed session");
            Check(vm.FileStatusText == "previous status" && vm.LastModifiedText == "previous time", "failed load changed current status");
            Check(File.ReadAllText(malformed) == "<gameList>\n<game>", "malformed source changed");
        });
        Session.Clear(); S.Reload();
    }

    static int Main(string[] args)
    {
        // Fail before accessing ANY application singleton unless this process is hosted beneath the dedicated review directory.
        Under(AppContext.BaseDirectory, args[0]);
        FixtureProfile = ProfileService.Instance.ActiveProfilePath;
        Guard();
        Console.WriteLine("ISOLATION: executable=" + AppContext.BaseDirectory + " profile=" + FixtureProfile);
        // ProfileService and SettingsService constructors were audited: all their default paths are under AppContext.BaseDirectory.
        // Profile creation, activation, reset and invalid-input probes below operate only inside this checked temporary host; native application startup is not used.
        Put(SettingKeys.ProfileSection, (SettingKeys.ProfileType.Key, SettingKeys.ProfileTypeEs));
        Put(SettingKeys.ScraperOptionsSection, ("SelectedScraper", "ScreenScraper"), ("ScraperConfigSave", "1"));
        Put(SettingKeys.ScrapersSection, ("ScreenScraper_alpha_MetaName", "True"), ("ScreenScraper_alpha_MediaImage", "True"), ("ScreenScraper_alpha_ImageSource", "Title"), ("ScreenScraper_beta_MetaName", "False"));
        Session.SetGamelist(Path.Combine(AppContext.BaseDirectory, "fixture.xml"), "alpha", new());
        using (var vm = new ScraperViewModel())
        {
            Test("unrelated settings save", () => { vm.MetaName = false; Put(SettingKeys.ScraperOptionsSection, ("ShowLogTimestamp", "True")); Session.RefreshAvailableMedia(); vm.ApplySettingsChanges(); Check(!vm.MetaName && vm.ShowLogTimestamp, "draft changed"); });
            Test("unavailable preferences excluded from request", () => { Media(vm, true); vm.MediaImage = true; Media(vm, false); var elements = (List<string>)typeof(ScraperViewModel).GetMethod("BuildElementsToScrape", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, null)!; Check(vm.MediaImage && !vm.MediaImageEnabled && !elements.Contains("image"), "preference or filtering failed"); Media(vm, true); Check(vm.MediaImageEnabled && vm.MediaImage && vm.ImageSources[vm.SelectedImageSource] == "Title", "media/source lost"); });
            Test("system A B A", () => { vm.MetaName = true; Session.CurrentSystem = "beta"; Check(!vm.MetaName, "beta not loaded"); Session.CurrentSystem = "alpha"; Check(vm.MetaName, "alpha not restored"); });
            Test("global/system save mode transition", () => { Put(SettingKeys.ScraperOptionsSection, ("ScraperConfigSave", "0")); vm.ApplySettingsChanges(); Check(!vm.MetaName, "global default not loaded"); Put(SettingKeys.ScraperOptionsSection, ("ScraperConfigSave", "1")); vm.ApplySettingsChanges(); Check(vm.MetaName, "system config lost"); });
            Test("provider A B A", () => { vm.CurrentScraper = "ArcadeDB"; vm.MetaName = false; vm.CurrentScraper = "ScreenScraper"; Check(vm.MetaName, "provider config lost"); });
            Test("unload does not overwrite global config", () => { Session.Clear(); vm.Dispose(); Check(SettingsService.Instance.GetValue(SettingKeys.ScrapersSection, "ScreenScraper_MetaName", "") == "False", "global config overwritten"); });
        }
        Test("new system uses its own cache default", () => { Session.CurrentSystem = "alpha"; using var vm = new ScraperViewModel(); vm.ScrapeFromCache = false; Session.CurrentSystem = "unsaved"; Check(vm.ScrapeFromCache, "new unsaved system inherits previous system cache=False instead of default=True"); Session.CurrentSystem = "alpha"; Check(!vm.ScrapeFromCache, "explicitly saved cache=False was lost"); });
        Test("full save preserves xml:space", () =>
        {
            var file = Path.Combine(AppContext.BaseDirectory, "space-preserve.xml"); File.WriteAllText(file, "<gameList><game xml:space=\"preserve\"> <path>./a.zip</path> <future/> </game></gameList>"); var (g, _) = GamelistService.LoadGamelist(file); var before = XDocument.Load(file, LoadOptions.PreserveWhitespace).Root!.Element("game")!.Nodes().OfType<XText>().Select(t => t.Value).ToArray(); Check(GamelistService.SaveGamelist(file, g, true), "save failed"); var after = XDocument.Load(file, LoadOptions.PreserveWhitespace).Root!.Element("game")!.Nodes().OfType<XText>().Select(t => t.Value).ToArray(); Check(before.SequenceEqual(after), "save succeeds while changing protected whitespace");
        });
        Test("full save preserves whitespace CDATA", () =>
        {
            var file = Path.Combine(AppContext.BaseDirectory, "cdata-preserve.xml"); File.WriteAllText(file, "<gameList><game><path>./a.zip</path><![CDATA[   ]]><future/></game></gameList>"); var (g, _) = GamelistService.LoadGamelist(file); Check(GamelistService.SaveGamelist(file, g, true), "save failed"); Check(XDocument.Load(file, LoadOptions.PreserveWhitespace).DescendantNodes().OfType<XCData>().Any(), "save succeeds while deleting whitespace CDATA");
        });
        Test("inherited xml:space preservation", () =>
        {
            var file = Path.Combine(AppContext.BaseDirectory, "inherited-space.xml"); File.WriteAllText(file, "<gameList xml:space=\"preserve\"> <game> <path>./a.zip</path>\n <future/> </game> </gameList>"); var before = XDocument.Load(file, LoadOptions.PreserveWhitespace); var (g, _) = GamelistService.LoadGamelist(file); Check(GamelistService.SaveGamelist(file, g, true), "save failed"); Check(XNode.DeepEquals(before.Root, XDocument.Load(file, LoadOptions.PreserveWhitespace).Root), "inherited preservation lost");
        });
        Test("xml:space default overrides inherited preserve", () =>
        {
            var file = Path.Combine(AppContext.BaseDirectory, "default-space.xml"); File.WriteAllText(file, "<gameList xml:space=\"preserve\"> <game xml:space=\"default\"> <path>./a.zip</path> </game> </gameList>"); var (g, _) = GamelistService.LoadGamelist(file); Check(GamelistService.SaveGamelist(file, g, true), "save failed"); var root = XDocument.Load(file, LoadOptions.PreserveWhitespace).Root!; Check(root.Nodes().OfType<XText>().All(t => t.Value == " "), "root preservation changed"); Check(root.Element("game")!.Nodes().OfType<XText>().Any(t => t.Value.Contains('\n')), "explicit default did not permit formatting");
        });
        Test("CDATA directly inside gameList survives", () =>
        {
            var file = Path.Combine(AppContext.BaseDirectory, "root-cdata.xml"); File.WriteAllText(file, "<gameList><![CDATA[ \t ]]><game><path>./a.zip</path></game></gameList>"); var (g, _) = GamelistService.LoadGamelist(file); Check(GamelistService.SaveGamelist(file, g, true), "save failed"); Check(XDocument.Load(file, LoadOptions.PreserveWhitespace).Root!.Nodes().OfType<XCData>().Single().Value == " \t ", "root CDATA changed");
        });
        Test("EmuMovies cache restriction does not leak to a new provider", () =>
        {
            Session.CurrentSystem = "new-provider-system"; using var vm = new ScraperViewModel(); vm.CurrentScraper = "EmuMovies"; Check(!vm.ScrapeFromCache && !vm.ScrapeFromCacheEnabled, "EmuMovies behavior changed"); vm.CurrentScraper = "ArcadeDB"; Check(vm.ScrapeFromCache && vm.ScrapeFromCacheEnabled, "forced cache preference leaked");
        });
        Session.Clear(); SaveTests(); SettingsTests(); LoadFailureTests(); ScreenScraperTests();
        Console.WriteLine($"{pass} passed; {fail} failed"); return fail == 0 ? 0 : 1;
    }
}
'@ | Set-Content -LiteralPath (Join-Path $testRoot 'Program.cs')
    dotnet restore (Join-Path $testRoot 'RegressionTests.csproj') --configfile (Join-Path $testRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw 'Could not prepare the isolated test host.' }
    $testIni = Join-Path $testRoot 'bin/Release/net10.0/ini'
    [IO.Directory]::CreateDirectory($testIni) | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $buildPath 'ini') -Filter '*_options.ini' | Copy-Item -Destination $testIni
    dotnet run --project (Join-Path $testRoot 'RegressionTests.csproj') --configuration Release --no-restore -- $testRoot
    if ($LASTEXITCODE -ne 0) { throw 'Persistence regression tests failed.' }
}
finally {
    $cleanupPath = [IO.Path]::GetFullPath($testRoot)
    if ([IO.Path]::GetDirectoryName($cleanupPath) -eq [IO.Path]::TrimEndingDirectorySeparator($tempRoot) -and
        [IO.Path]::GetFileName($cleanupPath) -match '^GamelistPersistenceTests-[0-9a-f]{32}$') {
        Remove-Item -LiteralPath $cleanupPath -Recurse -Force
    }
}