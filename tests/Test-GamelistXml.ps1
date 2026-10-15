param(
    [string] $BuildDirectory = (Join-Path $PSScriptRoot '../bin/Debug/net10.0')
)

# Check XML saving, media discovery and ROM discovery against the actual built application.
# Run in an isolated temporary host. No
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
using System.Globalization;
using System.Xml.Linq;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using Gamelist_Manager.ViewModels;

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
                Assert(!GamelistService.SaveGamelist(file, games, SessionState.Instance.GamelistHasMediaPaths), "Ambiguous editor rows were saved.");
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
                Assert(game.Element("image")?.Value == (profile == SettingKeys.ProfileTypeEsDe ? null : "./images/first.png"), "Profile media persistence policy failed.");
            }));
            foreach (var field in new[] { "lastplayed", "releasedate" })
            foreach (var cultureName in new[] { "en-US", "it-IT", "fr-FR" })
            {
                Run(profile, $"preserve {field} timestamps across repeated saves ({cultureName})", () =>
                {
                    var originalCulture = CultureInfo.CurrentCulture;
                    var originalUiCulture = CultureInfo.CurrentUICulture;
                    try
                    {
                        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                        CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
                        string?[] timestamps =
                        [
                            "20261001T224230", "20260817T232330", "20261001T081503",
                            "20261001T000000", "20240229T235959", "20260101T000001", null, ""
                        ];
                        string GetTimestamp(GameMetadataRow game) => field == "lastplayed" ? game.Lastplayed : game.Releasedate;
                        var fixture = new XDocument(new XElement("gameList", timestamps.Select((timestamp, index) =>
                            new XElement("game", new XElement("path", $"./Game{index}.zip"),
                                new XElement("name", $"Game{index}"),
                                timestamp == null ? null : new XElement(field, timestamp)))));
                        WithFile(fixture.ToString(), file =>
                        {
                            var (games, _) = GamelistService.LoadGamelist(file);
                            // Edit a game with no timestamp; every other game must keep its time.
                            games[6].Name = "Edited";
                            for (var save = 0; save < 3; save++)
                            {
                                Save(file, games);
                                var savedGames = XDocument.Load(file).Root!.Elements("game").ToArray();
                                Assert(savedGames.Length == timestamps.Length && savedGames[6].Element("name")?.Value == "Edited", "Unrelated edit or game count changed.");
                                for (var index = 0; index < timestamps.Length; index++)
                                {
                                    var expected = string.IsNullOrEmpty(timestamps[index]) ? null : timestamps[index];
                                    Assert(savedGames[index].Element(field)?.Value == expected, $"Game {index} lost its {field} timestamp on save {save + 1}.");
                                }
                                // Cover saving the same model twice and then saving after a reload.
                                if (save == 1) (games, _) = GamelistService.LoadGamelist(file);
                            }
                            Assert(GetTimestamp(games[0]) == "2026-10-01 22:42:30" &&
                                GetTimestamp(games[1]) == "2026-08-17 23:23:30", "Loaded model changed the expected timestamp text.");
                            Assert(GetTimestamp(games[3]) == "2026-10-01 00:00:00", "Midnight timestamp changed.");
                            Assert(GetTimestamp(games[6]) == "" && GetTimestamp(games[7]) == "", "Missing or empty timestamps acquired a value.");
                            var sameDayOrder = new[] { games[0], games[2], games[3] }.OrderBy(GetTimestamp).Select(game => game.Path);
                            Assert(sameDayOrder.SequenceEqual(new[] { "./Game3.zip", "./Game2.zip", "./Game0.zip" }), "Same-day chronological sort order was lost.");
                            var newFile = Path.Combine(TestFiles, Guid.NewGuid() + ".xml");
                            Save(newFile, games);
                            var createdGames = XDocument.Load(newFile).Root!.Elements("game").ToArray();
                            for (var index = 0; index < timestamps.Length; index++)
                            {
                                var expected = string.IsNullOrEmpty(timestamps[index]) ? null : timestamps[index];
                                Assert(createdGames[index].Element(field)?.Value == expected, $"New file lost the {field} timestamp for game {index}.");
                            }
                        });
                        if (field == "releasedate")
                        {
                            // ArcadeDB supplies years; ScreenScraper supplies years or ISO dates.
                            string[] releaseText = ["2000", "2009", "2004", "2008-12-17", "2008-03-18", "1997-05-01", "2004-02-29"];
                            string[] expectedXml = ["20000101T000000", "20090101T000000", "20040101T000000", "20081217T000000", "20080318T000000", "19970501T000000", "20040229T000000"];
                            var fixtureWithScrapedDates = new XDocument(new XElement("gameList", releaseText.Select((value, index) =>
                                new XElement("game", new XElement("path", $"./Release{index}.zip"), new XElement("releasedate", value)))));
                            // Also repair release dates written in plain text by 9.7.7.
                            WithFile(fixtureWithScrapedDates.ToString(), file =>
                            {
                                var (games, _) = GamelistService.LoadGamelist(file);
                                for (var round = 0; round < 2; round++)
                                {
                                    Save(file, games);
                                    Assert(XDocument.Load(file).Root!.Elements("game").Select(game => game.Element("releasedate")!.Value).SequenceEqual(expectedXml), "Scraped release dates were not saved in EmulationStation format.");
                                    (games, _) = GamelistService.LoadGamelist(file);
                                }
                                // Cover freshly scraped values and manual edits on new rows.
                                var newFile = Path.Combine(TestFiles, Guid.NewGuid() + ".xml");
                                var newGames = new ObservableCollection<GameMetadataRow>(releaseText.Select((value, index) =>
                                    new GameMetadataRow { Path = $"./NewRelease{index}.zip", Releasedate = value }));
                                Save(newFile, newGames);
                                Assert(XDocument.Load(newFile).Root!.Elements("game").Select(game => game.Element("releasedate")!.Value).SequenceEqual(expectedXml), "New release dates were not saved in EmulationStation format.");
                                (newGames, _) = GamelistService.LoadGamelist(newFile);
                                Save(newFile, newGames);
                                Assert(XDocument.Load(newFile).Root!.Elements("game").Select(game => game.Element("releasedate")!.Value).SequenceEqual(expectedXml), "Release dates changed after reload and save.");
                            });
                        }
                    }
                    finally
                    {
                        CultureInfo.CurrentCulture = originalCulture;
                        CultureInfo.CurrentUICulture = originalUiCulture;
                    }
                });
            }
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
                Assert(!GamelistService.SaveGamelist(file, [], SessionState.Instance.GamelistHasMediaPaths), "Invalid XML was overwritten.");
                Assert(before.SequenceEqual(File.ReadAllBytes(file)) && !File.Exists(file + ".tmp"), "Failed save changed file or left temp output.");
            }));
            Run(profile, "reject unexpected root without overwriting", () => WithFile("<other><game><path>./A.zip</path></game></other>", file =>
            {
                var before = File.ReadAllBytes(file);
                Assert(!GamelistService.SaveGamelist(file, [], SessionState.Instance.GamelistHasMediaPaths), "Unexpected XML root was overwritten.");
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
            Run(profile, "keep media available to the UI independently of XML persistence", () =>
            {
                foreach (var key in new[] { MetaDataKeys.image, MetaDataKeys.video, MetaDataKeys.manual, MetaDataKeys.mix })
                {
                    Assert(MetadataService.GetColumnDeclarations().Any(d => d.Key == key), $"Media column disappeared: {key}.");
                    Assert(MetadataService.GetMediaDeclarations().Any(d => d.Key == key), $"Media-page field disappeared: {key}.");
                }
            });
            Run(profile, "save discovered media according to profile", () => WithMedia((roms, media) =>
            {
                WithFile("<gameList><alternativeEmulator>keep</alternativeEmulator><game><path>./Game.zip</path><name>Game</name><future>keep</future></game></gameList>", file =>
                {
                    var image = WriteMedia(media, "screenshots", "Game.webp");
                    var video = WriteMedia(media, "videos", "Game.m4v");
                    var manual = WriteMedia(media, "manuals", "Game.pdf");
                    var mix = WriteMedia(media, "miximages", "Game.webp");
                    var (games, _) = GamelistService.LoadGamelist(file);
                    GamelistService.PopulateMediaPaths(games, media, roms);
                    games[0].Name = "Edited";
                    Save(file, games);
                    var saved = XDocument.Load(file).Root!.Element("game")!;
                    Assert(saved.Element("name")?.Value == "Edited" && saved.Element("future")?.Value == "keep", "Non-media data changed.");
                    Assert(games[0].Image == image && games[0].Video == video && games[0].Manual == manual && games[0].Mix == mix, "Saving cleared in-memory media paths.");
                    if (profile == SettingKeys.ProfileTypeEsDe)
                        Assert(!saved.Elements().Any(e => MetadataService.GetDeclByType(e.Name.LocalName)?.IsMedia == true), "Discovered media leaked into ES-DE XML.");
                    else
                        Assert(saved.Element("image")?.Value == image && saved.Element("video")?.Value == video && saved.Element("manual")?.Value == manual && saved.Element("mix")?.Value == mix, "Standard ES media paths were not saved.");
                });
            }));
            Run(profile, "preserve existing media XML for ES-DE", () => WithFile("<gameList><game><path>./Game.zip</path><name>Game</name><image custom='keep'><![CDATA[./original/image.png]]><!--keep comment--></image><video>./original/video.mp4</video><manual>./original/manual.pdf</manual><futureMedia>keep</futureMedia></game></gameList>", file =>
            {
                var before = XDocument.Load(file).Root!.Element("game")!;
                var (games, _) = GamelistService.LoadGamelist(file);
                games[0].Image = "./changed/image.webp";
                games[0].Video = "";
                games[0].Manual = "./changed/manual.pdf";
                games[0].Name = "Edited";
                Save(file, games);
                var after = XDocument.Load(file).Root!.Element("game")!;
                Assert(after.Element("name")?.Value == "Edited", "Metadata edit lost.");
                Equal(before.Element("futureMedia"), after.Element("futureMedia"));
                if (profile == SettingKeys.ProfileTypeEsDe)
                {
                    Equal(before.Element("image"), after.Element("image"));
                    Equal(before.Element("video"), after.Element("video"));
                    Equal(before.Element("manual"), after.Element("manual"));
                }
                else
                    Assert(after.Element("image")?.Value == "./changed/image.webp" && after.Element("video") == null && after.Element("manual")?.Value == "./changed/manual.pdf", "Standard ES media update/clear behavior changed.");
            }));
            Run(profile, "apply media policy to new files and newly added games", () =>
            {
                var file = Path.Combine(TestFiles, Guid.NewGuid() + ".xml");
                var game = new GameMetadataRow { Path = "./First.zip", Name = "First" };
                foreach (var media in MetadataService.GetAllMediaFolderTypes()) game.SetValue(media.Key, "./media/fixture");
                Save(file, [game]);
                var newGame = new GameMetadataRow { Path = "./Added.zip", Name = "Added" };
                foreach (var media in MetadataService.GetAllMediaFolderTypes()) newGame.SetValue(media.Key, "./media/added");
                Save(file, [game, newGame]);
                var savedGames = XDocument.Load(file).Root!.Elements("game").ToArray();
                Assert(savedGames.Length == 2 && savedGames[1].Element("name")?.Value == "Added", "New-game save failed.");
                foreach (var saved in savedGames)
                {
                    var mediaTags = saved.Elements().Where(e => MetadataService.GetDeclByType(e.Name.LocalName)?.IsMedia == true).ToArray();
                    Assert(profile == SettingKeys.ProfileTypeEsDe ? mediaTags.Length == 0 : mediaTags.Length == MetadataService.GetAllMediaFolderTypes().Count, "New file or new game violated media persistence policy.");
                }
            });
            Run(profile, "honor the explicit media flag independently of profile", () => WithFile("<gameList><game><path>./Game.zip</path><name>Game</name></game></gameList>", file =>
            {
                var (games, _) = GamelistService.LoadGamelist(file);
                games[0].Image = "./scraped/new.png";
                Assert(GamelistService.SaveGamelist(file, games, saveMediaPaths: false), "Save with media disabled failed.");
                Assert(XDocument.Load(file).Root!.Element("game")!.Element("image") == null && games[0].Image == "./scraped/new.png", "Media flag was ignored or model was cleared.");
                Assert(GamelistService.SaveGamelist(file, games, saveMediaPaths: true), "Save with media enabled failed.");
                Assert(XDocument.Load(file).Root!.Element("game")!.Element("image")?.Value == "./scraped/new.png", "Explicit media flag did not enable serialization.");
                games[0].Image = "";
                Assert(GamelistService.SaveGamelist(file, games, saveMediaPaths: false), "Save preserving media failed.");
                Assert(XDocument.Load(file).Root!.Element("game")!.Element("image")?.Value == "./scraped/new.png", "Disabled media flag removed existing XML.");
                Assert(GamelistService.SaveGamelist(file, games, saveMediaPaths: true), "Save clearing media failed.");
                Assert(XDocument.Load(file).Root!.Element("game")!.Element("image") == null, "Enabled media flag failed to clear media.");
            }));
            Run(profile, "apply newly scraped and replacement image paths before saving", () => WithMedia((roms, media) =>
            {
                var systemFolder = Path.Combine(roms, "test");
                Directory.CreateDirectory(systemFolder);
                var file = Path.Combine(systemFolder, "gamelist.xml");
                File.WriteAllText(file, "<gameList><game><path>./Game.zip</path><name>Game</name></game></gameList>");
                var (games, _) = GamelistService.LoadGamelist(file);
                SettingsState.Instance.RootRomFolder = roms;
                SessionState.Instance.SetGamelist(file, "test", games);

                // Exercise the actual scraper result-application method without HTTP
                // requests. The HTTP factory is unused by this method.
                var scraperType = typeof(GamelistService).Assembly.GetType("Gamelist_Manager.Services.ScraperService")!;
                var scraper = Activator.CreateInstance(scraperType, new object?[] { null })!;
                foreach (var filename in new[] { "Game.png", "Game.webp" })
                {
                    var downloaded = profile == SettingKeys.ProfileTypeEs
                        ? WriteMedia(systemFolder, "images", filename)
                        : WriteMedia(media, "screenshots", filename);
                    var data = new ScrapedGameData { Data = new() { ["image"] = downloaded } };
                    var parameters = new ScraperParameters { ElementsToScrape = ["image"] };
                    var task = (System.Threading.Tasks.Task)scraperType.GetMethod("SaveScrapedDataAsync")!.Invoke(scraper, new object[] { games[0], data, parameters })!;
                    task.GetAwaiter().GetResult();
                    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                    var expected = profile == SettingKeys.ProfileTypeEs ? "./images/" + filename : downloaded;
                    Assert(games[0].Image == expected, "New scraper result failed to update the row or changed path semantics.");
                    Save(file, games);
                    Assert(games[0].Image == expected, "Saving changed the scraped path in memory.");
                    Assert(XDocument.Load(file).Root!.Element("game")!.Element("image")?.Value == (profile == SettingKeys.ProfileTypeEs ? expected : null), "Newly scraped or replacement image was saved with the wrong policy.");
                }
                SessionState.Instance.Clear();
            }));
        }
        Run("profiles", "refresh media persistence policy when switching profiles", () => WithFile("<gameList><game><path>./Game.zip</path><name>Game</name></game></gameList>", file =>
        {
            var (games, _) = GamelistService.LoadGamelist(file);
            SettingsState.Instance.ProfileType = SettingKeys.ProfileTypeEs;
            games[0].Image = "./first.png";
            Save(file, games);
            Assert(XDocument.Load(file).Root!.Element("game")!.Element("image")?.Value == "./first.png", "ES profile did not save media.");
            SettingsState.Instance.ProfileType = SettingKeys.ProfileTypeEsDe;
            games[0].Image = "./second.webp";
            games[0].Name = "ES-DE edit";
            Save(file, games);
            var saved = XDocument.Load(file).Root!.Element("game")!;
            Assert(saved.Element("image")?.Value == "./first.png" && saved.Element("name")?.Value == "ES-DE edit", "ES-DE inherited the ES media policy.");
            SettingsState.Instance.ProfileType = SettingKeys.ProfileTypeEs;
            Save(file, games);
            Assert(XDocument.Load(file).Root!.Element("game")!.Element("image")?.Value == "./second.webp", "ES inherited the ES-DE media policy.");
        }));
        RunMediaTests();
        RunRomDiscoveryTests();
        RunMediaFolderGuardTests();
        Console.WriteLine($"\n{passed} passed; {failed} failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void RunMediaTests()
    {
        SettingsState.Instance.ProfileType = SettingKeys.ProfileTypeEsDe;
        foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".webp", ".mp4", ".avi", ".mkv", ".webm", ".m4v" })
        {
            Run("media", $"discover {extension}", () => WithMedia((roms, media) =>
            {
                var video = new[] { ".mp4", ".avi", ".mkv", ".webm", ".m4v" }.Contains(extension);
                var expected = WriteMedia(media, video ? "videos" : "screenshots", "Game" + extension);
                var game = new GameMetadataRow { Path = "./Game.zip", Image = "stale", Video = "stale" };
                GamelistService.PopulateMediaPaths([game], media, roms);
                Assert((video ? game.Video : game.Image) == expected, $"{extension} media was not discovered.");
                Assert(video ? MediaItemViewModel.IsVideoFile(expected) : MediaItemViewModel.IsImageFile(expected), "Discovered format not accepted by preview.");
            }));
        }
        Run("media", "preserve existing format preference order", () => WithMedia((roms, media) =>
        {
            foreach (var formats in new[] { new[] { ".png", ".jpg", ".jpeg", ".webp" }, new[] { ".mp4", ".avi", ".mkv", ".webm", ".m4v" } })
            {
                var video = formats[0] == ".mp4";
                var files = formats.Select(ext => WriteMedia(media, video ? "videos" : "screenshots", "Game" + ext)).ToArray();
                var game = new GameMetadataRow { Path = "./Game.zip" };
                foreach (var expected in files)
                {
                    GamelistService.PopulateMediaPaths([game], media, roms);
                    Assert((video ? game.Video : game.Image) == expected, "Existing media preference order changed.");
                    File.Delete(expected);
                }
            }
        }));
        Run("media", "discover WebP in mapped image categories", () => WithMedia((roms, media) =>
        {
            var expected = new Dictionary<MetaDataKeys, string>
            {
                [MetaDataKeys.thumbnail] = WriteMedia(media, "covers", "Game.webp"),
                [MetaDataKeys.marquee] = WriteMedia(media, "marquees", "Game.webp"),
                [MetaDataKeys.boxback] = WriteMedia(media, "backcovers", "Game.webp"),
                [MetaDataKeys.boxart] = WriteMedia(media, "3dboxes", "Game.webp"),
                [MetaDataKeys.fanart] = WriteMedia(media, "fanart", "Game.webp"),
                [MetaDataKeys.cartridge] = WriteMedia(media, "physicalmedia", "Game.webp"),
                [MetaDataKeys.titleshot] = WriteMedia(media, "titlescreens", "Game.webp"),
                [MetaDataKeys.mix] = WriteMedia(media, "miximages", "Game.webp")
            };
            var game = new GameMetadataRow { Path = "./Game.zip" };
            GamelistService.PopulateMediaPaths([game], media, roms);
            foreach (var item in expected) Assert(game.GetValue(item.Key)?.ToString() == item.Value, $"WebP discovery failed for {item.Key}.");
        }));
        Run("media", "mirror nested ROM folders for new formats", () => WithMedia((roms, media) =>
        {
            var expectedVideo = WriteMedia(media, Path.Combine("videos", "Collection"), "日本語 Game.m4v");
            var expectedImage = WriteMedia(media, Path.Combine("screenshots", "Collection"), "日本語 Game.webp");
            WriteMedia(media, "videos", "日本語 Game.mp4");
            var game = new GameMetadataRow { Path = "./Collection/日本語 Game.zip" };
            GamelistService.PopulateMediaPaths([game], media, roms);
            Assert(game.Video == expectedVideo && game.Image == expectedImage, "Nested media was flattened or filename changed.");
        }));
        Run("media", "preserve directory ROM extensions when matching", () => WithMedia((roms, media) =>
        {
            Directory.CreateDirectory(Path.Combine(roms, "Collection", "Game.ps3"));
            var expected = WriteMedia(media, Path.Combine("videos", "Collection"), "Game.ps3.webm");
            WriteMedia(media, Path.Combine("videos", "Collection"), "Game.mp4");
            var game = new GameMetadataRow { Path = "./Collection/Game.ps3" };
            GamelistService.PopulateMediaPaths([game], media, roms);
            Assert(game.Video == expected, "Directory ROM extension was stripped.");
        }));
        Run("media", "clear missing media and retain unmapped categories", () => WithMedia((roms, media) =>
        {
            WriteMedia(media, "screenshots", "Game.bmp");
            WriteMedia(media, "videos", "Game.mov");
            var game = new GameMetadataRow { Path = "./Game.zip", Image = "stale", Video = "stale", Manual = "stale", Bezel = "keep", Music = "keep" };
            GamelistService.PopulateMediaPaths([game], media, roms);
            Assert(game.Image == "" && game.Video == "" && game.Manual == "", "Missing media stayed linked or discovery expanded beyond ES-DE formats.");
            Assert(game.Bezel == "keep" && game.Music == "keep", "Unmapped category changed.");
        }));
        Run("media", "preserve manual discovery and empty-root behavior", () => WithMedia((roms, media) =>
        {
            var expected = WriteMedia(media, "manuals", "Game.pdf");
            var game = new GameMetadataRow { Path = "./Game.zip", Manual = "stale" };
            GamelistService.PopulateMediaPaths([game], "", roms);
            Assert(game.Manual == "stale", "Empty-root behavior changed.");
            GamelistService.PopulateMediaPaths([game], media, roms);
            Assert(game.Manual == expected, "PDF discovery changed.");
        }));
        Run("media", "preserve preview image formats and case handling", () =>
        {
            foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".gif", ".tiff", ".tif" })
            {
                Assert(MediaItemViewModel.IsImageFile("Game" + extension), $"Preview rejected {extension}.");
                Assert(MediaItemViewModel.IsImageFile("Game" + extension.ToUpperInvariant()), $"Preview rejected uppercase {extension}.");
            }
            foreach (var filename in new[] { "Game.ico", "Game.svg", "Game.avif", "Game.mp4", "Game.png.tmp", "Game" })
                Assert(!MediaItemViewModel.IsImageFile(filename), $"Preview accepted unsupported image {filename}.");
        });
        Run("media", "preserve preview video formats and case handling", () =>
        {
            foreach (var extension in new[] { ".mp4", ".avi", ".mkv", ".webm", ".m4v", ".mov", ".wmv", ".flv" })
            {
                Assert(MediaItemViewModel.IsVideoFile("Game" + extension), $"Preview rejected {extension}.");
                Assert(MediaItemViewModel.IsVideoFile("Game" + extension.ToUpperInvariant()), $"Preview rejected uppercase {extension}.");
            }
            foreach (var filename in new[] { "Game.mpeg", "Game.ogv", "Game.png", "Game.mp4.tmp", "Game" })
                Assert(!MediaItemViewModel.IsVideoFile(filename), $"Preview accepted unsupported video {filename}.");
        });
        Run("media", "preserve PDF preview classification", () =>
        {
            Assert(MediaItemViewModel.IsPdfFile("Game.pdf") && MediaItemViewModel.IsPdfFile("Game.PDF"), "PDF preview classification changed.");
            Assert(!MediaItemViewModel.IsPdfFile("Game.pdf.tmp") && !MediaItemViewModel.IsPdfFile("Game.png") && !MediaItemViewModel.IsPdfFile("Game"), "Unsupported manual was accepted.");
        });
        Run("media", "preserve scraper image-verification formats including ICO", () =>
        {
            var transferType = typeof(GamelistService).Assembly.GetType("Gamelist_Manager.Classes.Helpers.FileTransferHelper")!;
            var classify = transferType.GetMethod("IsImageExtension", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
            foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".gif", ".tiff", ".tif", ".ico" })
                Assert((bool)classify.Invoke(null, new object[] { extension })!, $"Scraper stopped verifying {extension} images.");
            foreach (var extension in new[] { ".svg", ".avif", ".mp4", ".pdf", "" })
                Assert(!(bool)classify.Invoke(null, new object[] { extension })!, $"Scraper would verify unsupported {extension} as an image.");
        });
    }

    private static void RunRomDiscoveryTests()
    {
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
        var scan = typeof(MainWindowViewModel).GetMethod("ScanForRoms", flags)!;
        var findMissing = typeof(MainWindowViewModel).GetMethod("FindMissingPaths", flags)!;
        var buildRow = typeof(MainWindowViewModel).GetMethod("BuildNewItemRow", flags)!;
        var csv = SettingsService.Instance.GetFileTypes()["xbox360"];
        var extensions = csv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var withoutExtensionless = extensions.Where(e => e != ".").ToHashSet(StringComparer.OrdinalIgnoreCase);
        Run("ROMs", "bundle Xbox 360 extensionless support without broadening other systems", () =>
        {
            Assert(extensions.Contains("."), "Xbox 360 filetypes does not enable extensionless ROMs.");
            Assert(SettingsService.Instance.GetFileTypes().Where(p => p.Value.Split(',').Contains(".")).Select(p => p.Key).SequenceEqual(new[] { "xbox360" }), "Extensionless matching was enabled for unrelated systems.");
        });
        WithMedia((roms, _) =>
        {
            var xbla = WriteMedia(roms, "", "Castle Crashers (XBLA)");
            var hashName = WriteMedia(roms, "", "584108B7A0123456789ABCDEF");
            var nested = WriteMedia(roms, "Collection.v1", "日本語 Game");
            var iso = WriteMedia(roms, "", "Disc.ISO");
            var unsupported = WriteMedia(roms, "", "notes.txt");
            var excluded = WriteMedia(roms, "images", "MediaWithoutExtension");
            var empty = Path.Combine(roms, "Empty");
            var package = Path.Combine(roms, "Game.ps3");
            Directory.CreateDirectory(empty);
            var internalIso = WriteMedia(package, "", "Internal.iso");
            var emptyPackage = Path.Combine(roms, "Empty.ps3");
            Directory.CreateDirectory(emptyPackage);
            var withPackages = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase) { ".ps3" };
            var media = new HashSet<string>(FilePathHelper.PathComparer) { "images" };
            List<string> Scan(HashSet<string> allowed, int depth) => (List<string>)scan.Invoke(null, new object[] { roms, allowed, media, depth })!;
            var found = Scan(withPackages, 3);
            Run("ROMs", "discover extensionless XBLA and hash filenames", () =>
                Assert(found.Contains(xbla) && found.Contains(hashName), "Extensionless Xbox 360 ROMs were missed."));
            Run("ROMs", "require explicit extensionless opt-in", () =>
            {
                var ordinary = Scan(withoutExtensionless, 3);
                Assert(!ordinary.Contains(xbla) && !ordinary.Contains(hashName) && !ordinary.Contains(nested) && ordinary.Contains(iso), "Extensionless ROMs were accepted without the dot token or normal extension matching changed.");
            });
            Run("ROMs", "preserve named-extension and media exclusion behavior", () =>
                Assert(found.Contains(iso) && !found.Contains(unsupported) && !found.Contains(excluded), "Extensionless support matched unrelated file types or changed media exclusion."));
            Run("ROMs", "ignore plain empty and collection directories", () =>
                Assert(!found.Contains(empty) && !found.Contains(Path.GetDirectoryName(nested)!) && !found.Contains(Path.Combine(roms, "images")), "Dot token turned an extensionless directory into a game."));
            Run("ROMs", "preserve directory-as-game behavior without scanning package contents", () =>
                Assert(found.Contains(package) && found.Contains(emptyPackage) && !found.Contains(internalIso), "Directory ROM recognition or package traversal changed."));
            Run("ROMs", "respect search depth for nested extensionless files", () =>
                Assert(found.Contains(nested) && !Scan(withPackages, 0).Contains(nested) && Scan(withPackages, 0).Contains(xbla), "Nested extensionless discovery ignored the depth limit or a dot in a parent folder."));
            Run("ROMs", "preserve extensionless names and relative gamelist paths", () =>
            {
                var row = (GameMetadataRow)buildRow.Invoke(null, new object[] { nested, roms })!;
                Assert(row.Path == "./Collection.v1/日本語 Game" && row.Name == "日本語 Game", "Extensionless name or relative path was changed.");
            });
            var rows = new List<GameMetadataRow>
            {
                new() { Path = "./Castle Crashers (XBLA)" }, new() { Path = "./Collection.v1/日本語 Game" },
                new() { Path = "./MissingExtensionless" }, new() { Path = "./Collection.v1/MissingExtensionless" },
                new() { Path = "./Missing.ISO" }, new() { Path = "./Missing.txt" },
                new() { Path = "./Empty" }, new() { Path = "./Empty.ps3" }, new() { Path = "" }
            };
            HashSet<string> Missing(HashSet<string> allowed) => (HashSet<string>)findMissing.Invoke(null, new object[] { rows, roms, allowed })!;
            Run("ROMs", "find missing extensionless files while retaining existing files and directories", () =>
                Assert(Missing(withPackages).SetEquals(new[] { "./MissingExtensionless", "./Collection.v1/MissingExtensionless", "./Missing.ISO" }), "Missing-file check misclassified extensionless paths, existing folders, or unsupported files."));
            Run("ROMs", "require dot token for missing extensionless checks", () =>
                Assert(Missing(withoutExtensionless).SetEquals(new[] { "./Missing.ISO" }), "Missing-file filter checked extensionless paths without opt-in."));
            Run("ROMs", "preserve unfiltered missing checks when no filetypes are defined", () =>
                Assert(Missing([]).SetEquals(new[] { "./MissingExtensionless", "./Collection.v1/MissingExtensionless", "./Missing.ISO", "./Missing.txt" }), "Empty extension configuration changed existing missing-file behavior."));
        });
    }

    private static void RunMediaFolderGuardTests()
    {
        var esDe = SettingKeys.GetProfileTypeOption(SettingKeys.ProfileTypeEsDe);
        var es = SettingKeys.GetProfileTypeOption(SettingKeys.ProfileTypeEs);
        MetaDataDecl Declaration() => new()
        {
            Key = MetaDataKeys.wheel, Type = "wheel", Name = "Wheel",
            PropertyName = nameof(GameMetadataRow.Wheel), DataType = MetaDataType.Image
        };
        Run("folders", "guard an omitted folder assignment", () =>
        {
            var decl = Declaration();
            Assert(!decl.IsEsDeSupported && !esDe.IncludesMediaFolder(decl), "Omitted folder assignment was accepted.");
        });
        foreach (var folder in new string?[] { null, "", " \t\r\n" })
        {
            Run("folders", $"guard {(folder == null ? "null" : folder.Length == 0 ? "empty" : "whitespace")} folder names", () =>
            {
                var decl = new MetaDataDecl
                {
                    Key = MetaDataKeys.wheel, Type = "wheel", Name = "Wheel",
                    PropertyName = nameof(GameMetadataRow.Wheel), DataType = MetaDataType.Image,
                    EsDeFolderName = folder!
                };
                var item = new MediaFolderItem { Path = "./custom-images" };
                item.RefreshDisplayState(esDe, decl, TestFiles, "test");
                Assert(!decl.IsEsDeSupported && !item.CanMediaOverrideCheckboxBeEnabled && item.DisplayPath == "", "Invalid folder generated a usable ES-DE path or checkbox.");
                item.RefreshDisplayState(esDe, decl, "", null);
                Assert(item.DisplayPath == "", "Invalid fallback folder was displayed.");
                item.RefreshDisplayState(es, decl, TestFiles, "test");
                Assert(item.CanMediaOverrideCheckboxBeEnabled && item.DisplayPath == "./custom-images", "Missing ES-DE mapping disabled ES media or changed its configured path.");
            });
        }
        Run("folders", "preserve mapped folder display and fallback", () =>
        {
            var marquee = MetaDataDecl.AllDeclarations.Single(d => d.Key == MetaDataKeys.marquee);
            var item = new MediaFolderItem();
            item.RefreshDisplayState(esDe, marquee, TestFiles, "test");
            Assert(item.CanMediaOverrideCheckboxBeEnabled && item.DisplayPath == Path.Combine(TestFiles, "test", "marquees"), "Valid mapping changed.");
            item.RefreshDisplayState(esDe, marquee, "", null);
            Assert(item.DisplayPath == "marquees", "Valid mapping fallback changed.");
        });
        Run("media", "ignore unsupported wheel folders while discovering marquee", () => WithMedia((roms, media) =>
        {
            WriteMedia(media, "wheel", "Game.png");
            WriteMedia(media, "", "Game.png");
            var expectedMarquee = WriteMedia(media, "marquees", "Game.png");
            var game = new GameMetadataRow { Path = "./Game.zip", Wheel = "keep-existing" };
            GamelistService.PopulateMediaPaths([game], media, roms);
            Assert(game.Wheel == "keep-existing" && game.Marquee == expectedMarquee, "Unmapped wheel was discovered, existing metadata cleared, or marquee discovery broke.");
        }));

        // Provider capabilities and saved settings are fixtures in the isolated host.
        // Both media types are deliberately enabled to catch accidental fallback disabling.
        var ini = Path.Combine(AppContext.BaseDirectory, "ini");
        Directory.CreateDirectory(ini);
        File.WriteAllText(Path.Combine(ini, "screenscraper_options.ini"), """
            [SupportedFields]
            wheel
            marquee
            [WheelSource]
            Clear Logo=wheel-hd
            [MarqueeSource]
            Clear Logo=wheel-hd
            """);
        var profileFile = Path.Combine(TestFiles, "wheel-profile.ini");
        File.WriteAllText(profileFile, """
            [MediaPathOverrides]
            test_wheel_enabled=true
            [Scrapers]
            ScreenScraper_MediaWheel=true
            ScreenScraper_MediaMarquee=true
            """);
        SettingsService.Instance.SwitchProfile(profileFile);
        SettingsState.Instance.SelectedScraper = "ScreenScraper";
        SettingsState.Instance.ScraperConfigSave = 0;
        var settings = new Dictionary<string, string>
        {
            ["wheel_enabled"] = "true", ["wheel"] = "./custom-images",
            ["wheel_suffix"] = "custom-wheel", ["wheel_suffix_enabled"] = "true",
            ["marquee_enabled"] = "true"
        };
        foreach (var profile in new[] { SettingKeys.ProfileTypeEsDe, SettingKeys.ProfileTypeEs })
        {
            SettingsState.Instance.ProfileType = profile;
            var available = MediaPathResolver.BuildAvailableMedia(profile, "test", TestFiles, settings);
            var isEs = profile == SettingKeys.ProfileTypeEs;
            Run(profile, "respect wheel capability despite saved enabled settings", () =>
            {
                Assert(SettingsService.Instance.GetSection(SettingKeys.MediaPathOverridesSection)!.ContainsKey("test_wheel_enabled"), "Override fixture was not loaded.");
                var wheel = available.SingleOrDefault(m => m.Type == "wheel");
                Assert(isEs ? wheel != null && wheel.MediaEnabled && wheel.FolderPath == Path.Combine(TestFiles, "custom-images") && wheel.Suffix == "custom-wheel" && wheel.IsSuffixEnabled : wheel == null, "Wheel availability or ES path/suffix behavior changed.");
                Assert(available.Any(m => m.Type == "marquee" && m.MediaEnabled), "Marquee became unavailable.");
            });
            Run(profile, "enforce wheel capability in scraper controls and destinations", () =>
            {
                // Supply the actual resolver output to the session without requiring a UI profile wizard.
                typeof(SessionState).GetProperty(nameof(SessionState.AvailableMedia))!.SetValue(SessionState.Instance, available);
                using var vm = new ScraperViewModel();
                Assert(vm.CurrentScraper == "ScreenScraper" && MetadataService.GetScraperElements(vm.CurrentScraper).Contains("wheel"), "Provider fixture was not loaded.");
                Assert(vm.MediaWheelEnabled == isEs && vm.MediaWheel == isEs, "Wheel scraper control did not respect folder capability or saved selection.");
                Assert(vm.MediaMarqueeEnabled && vm.MediaMarquee, "Marquee scraper control changed.");
                var scraperType = typeof(GamelistService).Assembly.GetType("Gamelist_Manager.Services.ScraperService")!;
                var parameters = (ScraperParameters)scraperType.GetMethod("CreateScraperParameters")!.Invoke(null,
                    new object[] { true, profile, available, "ScreenScraper", "test", new List<string> { "wheel", "marquee" } })!;
                Assert(parameters.MediaPaths!.ContainsKey("wheel") == isEs, "Unsupported wheel gained a download destination or ES wheel lost its destination.");
                Assert(parameters.MediaPaths.ContainsKey("marquee") && parameters.MarqueeSource == "wheel-hd", "Marquee destination or selectable logo source changed.");
                Assert(isEs ? parameters.MediaSuffixes!["wheel"] == ("custom-wheel", true) : parameters.MediaSuffixes!.Count == 0, "Scraper suffix policy changed.");
            });
        }
        SessionState.Instance.Clear();
    }

    private static void WithMedia(Action<string, string> test)
    {
        var root = Path.Combine(TestFiles, Guid.NewGuid().ToString());
        var roms = Path.Combine(root, "roms");
        var media = Path.Combine(root, "media");
        Directory.CreateDirectory(roms);
        Directory.CreateDirectory(media);
        test(roms, media);
    }
    private static string WriteMedia(string media, string category, string filename)
    {
        var folder = Path.Combine(media, category);
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, filename);
        // Discovery checks filenames, not codecs; no native media player is started.
        File.WriteAllText(file, "discovery fixture");
        return file;
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
    private static void Save(string file, ObservableCollection<GameMetadataRow> games) => Assert(GamelistService.SaveGamelist(file, games, SessionState.Instance.GamelistHasMediaPaths), "Save failed.");
    private static void Equal(XNode? expected, XNode? actual) => Assert(XNode.DeepEquals(expected, actual), "XML content changed or was lost.");
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
}
'@ | Set-Content -LiteralPath (Join-Path $testRoot 'Program.cs')

    # The host uses existing application assemblies and needs no NuGet packages.
    '<configuration><packageSources><clear /></packageSources></configuration>' |
        Set-Content -LiteralPath (Join-Path $testRoot 'NuGet.Config')
    dotnet restore (Join-Path $testRoot 'RegressionTests.csproj') --configfile (Join-Path $testRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw 'Could not prepare the isolated test host.' }
    $testIniPath = Join-Path $testRoot 'bin/Release/net10.0/ini'
    [IO.Directory]::CreateDirectory($testIniPath) | Out-Null
    Copy-Item -LiteralPath (Join-Path $buildPath 'ini/filetypes.ini') -Destination (Join-Path $testIniPath 'filetypes.ini')
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
