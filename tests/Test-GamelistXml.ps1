param(
    [string] $BuildDirectory = (Join-Path $PSScriptRoot '../bin/Debug/net10.0')
)

# Run against the actual built application in an isolated temporary host. No
# production profiles, gamelists, media or backups are read or written.
$ErrorActionPreference = 'Stop'
$buildPath = (Resolve-Path -LiteralPath $BuildDirectory).Path
if (-not (Test-Path -LiteralPath (Join-Path $buildPath 'Gamelist_Manager.dll'))) {
    throw 'Build Gamelist_Manager.csproj before running these regression tests.'
}

$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$testRoot = Join-Path $tempRoot ('GamelistXmlTests-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($testRoot) | Out-Null
try {
    $references = foreach ($assembly in Get-ChildItem -LiteralPath $buildPath -Filter '*.dll') {
        try { [Reflection.AssemblyName]::GetAssemblyName($assembly.FullName) | Out-Null }
        catch [System.BadImageFormatException] { continue }
        $name = [Security.SecurityElement]::Escape($assembly.BaseName)
        $path = [Security.SecurityElement]::Escape($assembly.FullName)
        "<Reference Include=`"$name`"><HintPath>$path</HintPath></Reference>"
    }
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup>
    $($references -join [Environment]::NewLine)
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $testRoot 'RegressionTests.csproj')

    @'
using System.Collections.ObjectModel;
using System.Xml.Linq;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;

internal static class Program
{
    private static readonly string TestFiles = Path.Combine(AppContext.BaseDirectory, "fixtures");
    private static int passed;
    private static int failed;

    private const string Fixture = """
        <?xml version="1.0" encoding="utf-8" standalone="yes"?>
        <!--document comment-->
        <?before keep?>
        <gameList version="future" xmlns:ext="urn:future" ext:flag="preserve">
          <alternativeEmulator>example</alternativeEmulator>
          <folder custom="yes"><path>./Folder</path><name>Folder name</name><future><![CDATA[folder <data>]]></future><!--folder comment--></folder>
          <game id="123" custom="keep"><path>./Folder/First.zip</path><name>First</name><favorite>true</favorite><rating>0.5</rating><releasedate>19950101T000000</releasedate><playtime>120</playtime><lastplayed>20261001T000000</lastplayed><future mode="dual">other screen</future><ext:data><![CDATA[game <data>]]></ext:data><!--game comment--></game>
          <!--between games-->
          <futureRoot enabled="true"><game><path>./Nested.zip</path><name>Nested metadata</name></game><text>  significant  spaces  </text></futureRoot>
          <?inside keep?>
          <game><path>./Second.zip</path><name>Second</name></game>
        </gameList>
        <?after keep?>
        """;

    public static int Main()
    {
        Directory.CreateDirectory(TestFiles);
        foreach (var profile in new[] { SettingKeys.ProfileTypeEsDe, SettingKeys.ProfileTypeEs })
        {
            SettingsState.Instance.ProfileType = profile;
            Run(profile, "load only direct game entries", () => WithFile(Fixture, file =>
            {
                var (games, _) = GamelistService.LoadGamelist(file);
                Assert(games.Count == 2, "Unknown nested game was exposed as an editable game.");
            }));
            Run(profile, "preserve root metadata and attributes", () => RoundTrip(Fixture, (before, after) =>
            {
                Equal(before.Root!.Element("alternativeEmulator"), after.Root!.Element("alternativeEmulator"));
                Equal(before.Root.Element("futureRoot"), after.Root.Element("futureRoot"));
                Assert(before.Root.Attributes().Select(a => a.ToString()).SequenceEqual(after.Root.Attributes().Select(a => a.ToString())), "Root attributes changed.");
            }));
            Run(profile, "preserve complete folder entries", () => RoundTrip(Fixture, (before, after) =>
                Equal(before.Root!.Element("folder"), after.Root!.Element("folder"))));
            Run(profile, "preserve unknown per-game metadata", () => RoundTrip(Fixture, (before, after) =>
            {
                var oldGame = before.Root!.Elements("game").First();
                var newGame = after.Root!.Elements("game").First();
                Equal(oldGame.Element("future"), newGame.Element("future"));
                Equal(oldGame.Element(XName.Get("data", "urn:future")), newGame.Element(XName.Get("data", "urn:future")));
                Assert(newGame.Attribute("custom")?.Value == "keep", "Unknown game attribute lost.");
                Assert(newGame.Nodes().OfType<XComment>().Single().Value == "game comment", "Game comment lost.");
            }));
            Run(profile, "preserve document trivia and declaration", () => RoundTrip(Fixture, (before, after) =>
            {
                Assert(after.Declaration?.Version == before.Declaration?.Version && after.Declaration?.Standalone == "yes", "Declaration lost.");
                Assert(after.Nodes().OfType<XComment>().Single().Value == "document comment", "Document comment lost.");
                Assert(after.Nodes().OfType<XProcessingInstruction>().Select(p => p.ToString()).SequenceEqual(before.Nodes().OfType<XProcessingInstruction>().Select(p => p.ToString())), "Document processing instructions lost.");
                Assert(after.Root!.Nodes().OfType<XProcessingInstruction>().Single().ToString() == "<?inside keep?>", "Root processing instruction lost.");
                Assert(after.Root.Nodes().OfType<XComment>().Single().Value == "between games", "Root comment lost.");
            }));
            Run(profile, "preserve ordering while editing", () => WithFile(Fixture, file =>
            {
                var before = XDocument.Load(file);
                var (games, _) = GamelistService.LoadGamelist(file);
                games[0].Name = "Edited & renamed";
                Save(file, games);
                var after = XDocument.Load(file);
                Assert(after.Root!.Elements("game").First().Element("name")?.Value == "Edited & renamed", "Edit lost.");
                Assert(before.Root!.Nodes().Select(NodeKey).SequenceEqual(after.Root.Nodes().Select(NodeKey)), "Unrelated node order changed.");
            }));
            Run(profile, "delete one game and append a new game", () => WithFile(Fixture, file =>
            {
                var before = XDocument.Load(file);
                var (games, _) = GamelistService.LoadGamelist(file);
                games.RemoveAt(0);
                games.Add(new GameMetadataRow { Path = "./New.zip", Name = "New game" });
                Save(file, games);
                var after = XDocument.Load(file);
                Assert(after.Root!.Elements("game").Select(g => g.Element("path")!.Value).SequenceEqual(new[] { "./Second.zip", "./New.zip" }), "Game add/delete behavior changed.");
                Equal(before.Root!.Element("folder"), after.Root.Element("folder"));
                Equal(before.Root.Element("futureRoot"), after.Root.Element("futureRoot"));
            }));
            Run(profile, "delete all games while preserving other XML", () => WithFile(Fixture, file =>
            {
                Save(file, []);
                var after = XDocument.Load(file);
                Assert(!after.Root!.Elements("game").Any(), "Deleted games remain.");
                Assert(after.Root.Element("folder") != null && after.Root.Element("futureRoot") != null && after.Root.Element("alternativeEmulator") != null, "Unrelated XML deleted.");
            }));
            Run(profile, "round-trip a folder-only gamelist", () => RoundTrip("<gameList custom='keep'><folder><path>./Folder</path><name>Folder</name></folder></gameList>", Equal));
            Run(profile, "preserve pathless entries without duplicating", () => RoundTrip("<gameList><game custom='keep'><name>No path</name><future>keep</future></game><game><path>./Valid.zip</path><name>Valid</name></game></gameList>", Equal));
            Run(profile, "keep existing duplicate-path cleanup", () => WithFile("<gameList><folder><path>./Folder</path></folder><game><path>./A.zip</path><name>First</name><future>keep</future></game><game><path>./A.zip</path><name>Duplicate</name></game></gameList>", file =>
            {
                var (games, duplicates) = GamelistService.LoadGamelist(file, ignoreDuplicates: true);
                Assert(games.Count == 1 && duplicates.Count == 1, "Duplicate detection changed.");
                Save(file, games);
                var after = XDocument.Load(file);
                Assert(after.Root!.Elements("game").Count() == 1 && after.Root.Element("game")!.Element("future")?.Value == "keep", "Duplicate cleanup changed or first entry lost.");
                Assert(after.Root.Element("folder") != null, "Folder lost during duplicate cleanup.");
            }));
            Run(profile, "reject duplicate editor rows without overwriting", () => WithFile("<gameList><game><path>./A.zip</path><name>A</name></game></gameList>", file =>
            {
                var before = File.ReadAllBytes(file);
                var (games, _) = GamelistService.LoadGamelist(file);
                games.Add(new GameMetadataRow { Path = "./A.zip", Name = "Duplicate" });
                Assert(!GamelistService.SaveGamelist(file, games), "Ambiguous editor rows were saved.");
                Assert(before.SequenceEqual(File.ReadAllBytes(file)), "Failed save overwrote original.");
            }));
            Run(profile, "preserve path spelling and case", () => WithFile("<gameList><game><path>./A.zip</path><name>A</name></game><game><path>./a.zip</path><name>a</name></game><game><path>./Folder\\B.zip</path><name>B</name></game><game><path>/roms/C.zip</path><name>C</name></game><game><path>Q:/roms/D.zip</path><name>D</name></game><game><path>\\\\server\\roms\\E.zip</path><name>E</name></game></gameList>", file =>
            {
                var expected = XDocument.Load(file).Root!.Elements("game").Select(g => g.Element("path")!.Value).ToArray();
                var (games, _) = GamelistService.LoadGamelist(file);
                Save(file, games);
                Assert(expected.SequenceEqual(XDocument.Load(file).Root!.Elements("game").Select(g => g.Element("path")!.Value)), "ROM paths normalized or case variants lost.");
            }));
            Run(profile, "keep existing field serialization", () => WithFile(Fixture, file =>
            {
                var (games, _) = GamelistService.LoadGamelist(file);
                games[0].Id = "456";
                games[0].Name = "";
                games[0].Favorite = false;
                games[0].Hidden = true;
                games[0].Rating = "0,75";
                games[0].Releasedate = "2000-02-03";
                games[0].Lastplayed = "2026-10-09";
                games[0].Playtime = "300";
                games[0].Image = "./images/first.png";
                Save(file, games);
                var game = XDocument.Load(file).Root!.Element("game")!;
                Assert(game.Attribute("id")?.Value == "456", "ID serialization changed.");
                Assert(game.Element("name") == null && game.Element("favorite") == null && game.Element("hidden")?.Value == "true", "Empty or boolean serialization changed.");
                Assert(game.Element("rating")?.Value == "0.75" && game.Element("releasedate")?.Value == "20000203T000000" && game.Element("lastplayed")?.Value == "20261009T000000", "Rating or date serialization changed.");
                Assert(game.Element("playtime")?.Value == (profile == SettingKeys.ProfileTypeEsDe ? "300" : "120"), "Profile field filtering changed.");
                Assert(game.Element("image")?.Value == "./images/first.png", "Current media serialization changed outside this fix's scope.");
            }));
            Run(profile, "create and reload a new gamelist", () =>
            {
                var file = Path.Combine(TestFiles, Guid.NewGuid() + ".xml");
                Save(file, [new GameMetadataRow { Path = "./New.zip", Name = "New & 日本語", Favorite = true }]);
                var (games, _) = GamelistService.LoadGamelist(file);
                Assert(games.Count == 1 && games[0].Name == "New & 日本語" && games[0].Favorite, "New gamelist or Unicode round trip failed.");
                Save(file, games);
                Assert(XDocument.Load(file).Root!.Elements("game").Count() == 1, "Repeated save duplicates game.");
            });
            Run(profile, "reject malformed XML without overwriting", () => WithFile("<gameList><game>", file =>
            {
                var before = File.ReadAllBytes(file);
                Assert(!GamelistService.SaveGamelist(file, []), "Invalid XML was overwritten.");
                Assert(before.SequenceEqual(File.ReadAllBytes(file)) && !File.Exists(file + ".tmp"), "Failed save changed file or left temp output.");
            }));
            Run(profile, "reject unexpected root without overwriting", () => WithFile("<other><game><path>./A.zip</path></game></other>", file =>
            {
                var before = File.ReadAllBytes(file);
                Assert(!GamelistService.SaveGamelist(file, []), "Unexpected XML root was overwritten.");
                Assert(before.SequenceEqual(File.ReadAllBytes(file)), "Unexpected root save changed original.");
            }));
            Run(profile, "preserve whitespace and document type", () => WithFile("<!DOCTYPE gameList [<!ELEMENT gameList ANY>]>\n<gameList>\n  <futureRoot>  text  <child />  trailing  </futureRoot>\n  <game><path>./A.zip</path><name>A</name></game>\n</gameList>", file =>
            {
                var before = XDocument.Load(file, LoadOptions.PreserveWhitespace);
                var (games, _) = GamelistService.LoadGamelist(file);
                Save(file, games);
                var after = XDocument.Load(file, LoadOptions.PreserveWhitespace);
                Equal(before.DocumentType, after.DocumentType);
                Equal(before.Root!.Element("futureRoot"), after.Root!.Element("futureRoot"));
                // XML writers may use platform line endings; indentation and mixed
                // content must still survive without being reformatted.
                Assert(File.ReadAllText(file).Replace("\r\n", "\n").Contains("\n  <futureRoot>  text  <child />  trailing  </futureRoot>\n"), "Unrelated whitespace reformatted.");
            }));
            Run(profile, "repeat saves without losing or duplicating metadata", () => WithFile(Fixture, file =>
            {
                var (games, _) = GamelistService.LoadGamelist(file);
                Save(file, games);
                var first = XDocument.Load(file, LoadOptions.PreserveWhitespace);
                var (reloaded, _) = GamelistService.LoadGamelist(file);
                Save(file, reloaded);
                Equal(first, XDocument.Load(file, LoadOptions.PreserveWhitespace));
            }));
        }
        Console.WriteLine($"\n{passed} passed; {failed} failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void Run(string profile, string name, Action test)
    {
        try { test(); passed++; Console.WriteLine($"PASS [{profile}] {name}"); }
        catch (Exception ex) { failed++; Console.WriteLine($"FAIL [{profile}] {name}: {ex.Message}"); }
    }
    private static void WithFile(string xml, Action<string> test)
    {
        var file = Path.Combine(TestFiles, Guid.NewGuid() + ".xml");
        File.WriteAllText(file, xml);
        test(file);
    }
    private static void RoundTrip(string xml, Action<XDocument, XDocument> verify) => WithFile(xml, file =>
    {
        var before = XDocument.Load(file);
        var (games, _) = GamelistService.LoadGamelist(file);
        if (games.Count > 0 && games[0].Path.Length > 0) games[0].Name = "First";
        Save(file, games);
        verify(before, XDocument.Load(file));
    });
    private static string NodeKey(XNode node) => node is XElement element ? "element:" + element.Name : node.ToString();
    private static void Save(string file, ObservableCollection<GameMetadataRow> games) => Assert(GamelistService.SaveGamelist(file, games), "Save failed.");
    private static void Equal(XNode? expected, XNode? actual) => Assert(XNode.DeepEquals(expected, actual), "XML content changed or was lost.");
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
}
'@ | Set-Content -LiteralPath (Join-Path $testRoot 'Program.cs')

    # The host uses existing application assemblies and needs no NuGet packages.
    '<configuration><packageSources><clear /></packageSources></configuration>' |
        Set-Content -LiteralPath (Join-Path $testRoot 'NuGet.Config')
    dotnet restore (Join-Path $testRoot 'RegressionTests.csproj') --configfile (Join-Path $testRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw 'Could not prepare the isolated test host.' }
    dotnet run --project (Join-Path $testRoot 'RegressionTests.csproj') --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Gamelist XML regression tests failed.' }
}
finally {
    # Resolve and constrain the generated cleanup target to this run's temp folder.
    $cleanupPath = [IO.Path]::GetFullPath($testRoot)
    $expectedParent = [IO.Path]::TrimEndingDirectorySeparator($tempRoot)
    if ([IO.Path]::GetDirectoryName($cleanupPath) -eq $expectedParent -and
        [IO.Path]::GetFileName($cleanupPath) -match '^GamelistXmlTests-[0-9a-f]{32}$') {
        Remove-Item -LiteralPath $cleanupPath -Recurse -Force
    }
}
