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
        public Task<ThreeButtonResult> ShowAsync(ThreeButtonDialogConfig config, object? owner = null) => Task.FromResult(Result);
        public Task ShowErrorAsync(string title, string message, string? detail = null, object? owner = null) { Errors++; return Task.CompletedTask; }
        public Task ShowInfoAsync(string title, string message, string? detail = null, object? owner = null) => Task.CompletedTask;
        public Task ShowWarningAsync(string title, string message, string? detail = null, object? owner = null) => Task.CompletedTask;
        public Task<bool> ShowConfirmAsync(string title, string message, string confirmText = "Yes", string cancelText = "Cancel", DialogIconTheme icon = DialogIconTheme.Question, string? detail = null, object? owner = null) => Task.FromResult(false);
    }
    static int Main(string[] args)
    {
        // Fail before accessing ANY application singleton unless this process is hosted beneath the dedicated review directory.
        Under(AppContext.BaseDirectory, args[0]);
        FixtureProfile = ProfileService.Instance.ActiveProfilePath;
        Guard();
        Console.WriteLine("ISOLATION: executable=" + AppContext.BaseDirectory + " profile=" + FixtureProfile);
        // ProfileService and SettingsService constructors were audited: all their default paths are under AppContext.BaseDirectory.
        // Only this host's checked fixture INI is written; no create-profile, activate-profile, reset, delete, or real application startup calls occur.
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
        Session.Clear(); SaveTests();
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