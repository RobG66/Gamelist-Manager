param(
    [string] $BuildDirectory = (Join-Path $PSScriptRoot '../bin/Release/net10.0'),
    [string] $SampleDat
)

# Test the built parser and reporting pipeline without UI startup, profiles,
# external report applications, or reading/writing ROM files.
$ErrorActionPreference = 'Stop'
$buildPath = (Resolve-Path -LiteralPath $BuildDirectory).Path
$fixturePath = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot 'Fixtures/NoIntroParentClone.dat')).Path
if (-not (Test-Path -LiteralPath (Join-Path $buildPath 'Gamelist_Manager.dll'))) {
    throw 'Build Gamelist_Manager.csproj before running these tests.'
}
$samplePath = if ($SampleDat) { (Resolve-Path -LiteralPath $SampleDat).Path } else { '' }
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$testRoot = Join-Path $tempRoot ('GamelistDatTests-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($testRoot) | Out-Null
try {
    $references = foreach ($assembly in Get-ChildItem -LiteralPath $buildPath -Filter '*.dll') {
        try { [Reflection.AssemblyName]::GetAssemblyName($assembly.FullName) | Out-Null }
        catch [System.BadImageFormatException] { continue }
        '<Reference Include="' + [Security.SecurityElement]::Escape($assembly.BaseName) + '"><HintPath>' + [Security.SecurityElement]::Escape($assembly.FullName) + '</HintPath></Reference>'
    }
    $project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>' + ($references -join [Environment]::NewLine) + '</ItemGroup></Project>'
    Set-Content -LiteralPath (Join-Path $testRoot 'DatTests.csproj') -Value $project
    Set-Content -LiteralPath (Join-Path $testRoot 'NuGet.Config') -Value '<configuration><packageSources><clear /></packageSources></configuration>'
    @'
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Gamelist_Manager.Models;
using Gamelist_Manager.ViewModels;

class Program
{
    static int pass, fail;
    static MethodInfo Method(string name) => typeof(DatToolViewModel).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
    static void Check(bool ok, string why) { if (!ok) throw new Exception(why); }
    static async Task Test(string name, Func<Task> action)
    {
        try { await action(); pass++; Console.WriteLine("PASS " + name); }
        catch (Exception ex) { fail++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
    }
    static async Task<(List<GameReportItem> Games, DatHeader? Header)> Parse(Stream stream) =>
        await (Task<(List<GameReportItem>, DatHeader?)>)Method("CreateDatSummaryAsync").Invoke(null, new object[] { stream })!;
    static async Task<(List<GameReportItem> Games, DatHeader? Header)> ParseXml(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return await Parse(stream);
    }
    static async Task<List<GameReportItem>> Match(List<GameReportItem> dat, IEnumerable<string> titles)
    {
        string[] extensions = [".zip", ".7z", ".md"];
        var rows = titles.Select((name, i) => new GameMetadataRow { Path = Path.Combine("roms", name + extensions[i % 3]) }).ToList();
        return await (Task<List<GameReportItem>>)Method("CreateGamelistSummaryAsync").Invoke(null, new object[] { dat, rows, AppContext.BaseDirectory })!;
    }
    static string Report(List<GameReportItem> dat, List<GameReportItem> present, bool parents = false, bool clones = false, bool text = true) =>
        (string)Method("BuildMissingReport").Invoke(null, new object[] { dat, present, parents, clones, text, "fixture.dat" })!;
    static void Counts(string report, int parents, int clones)
    {
        Check(Regex.IsMatch(report, @"Total Missing Parents:\s+" + parents + @"\b"), "wrong parent count");
        Check(Regex.IsMatch(report, @"Total Missing Clones:\s+" + clones + @"\b"), "wrong clone count");
    }
    static XDocument Document(string path)
    {
        using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null });
        return XDocument.Load(reader);
    }
    static async Task<int> Main(string[] args)
    {
        List<GameReportItem> fixture;
        DatHeader? header;
        using (var stream = File.OpenRead(args[0])) (fixture, header) = await Parse(stream);
        const string parent = "Example, Inc. (USA) (v1.2)";
        const string europe = "Example, Inc. (Europe) (v1.1)";
        const string japan = "Example, Inc. (Japan) (v1.0)";
        await Test("adjacent header fields all parse without skipping siblings", () =>
        {
            Check(header?.Name == "Parent-Clone compatibility fixture" && header.Description == "Synthetic test data" && header.Version == "1.2" && header.Date == "2026-10-28" && header.Author == "Test author", "header fields missing");
            return Task.CompletedTask;
        });
        await Test("parent and clone titles retain punctuation and revisions", () =>
        {
            Check(fixture.Count == 7 && fixture.Select(g => g.Name).Distinct().Count() == 7, "entries lost or collided");
            Check(fixture.Single(g => g.Name == europe).CloneOf == parent && fixture.Single(g => g.Name == japan).CloneOf == parent, "clone reference truncated");
            Check(fixture.Single(g => g.Name == "Mr. Test (World)").Description == "Mr. Test (World)", "title changed");
            return Task.CompletedTask;
        });
        await Test("multiple ROMs and region releases remain one DAT game", () =>
        {
            Check(fixture.Count(g => g.Name == "Lock & Link (World)") == 1, "multiple ROMs became duplicate game entries");
            return Task.CompletedTask;
        });
        await Test("compressed and raw paths match full titles and identify the missing clone", async () =>
        {
            var present = await Match(fixture, new[] { parent, europe, "Mr. Test (World)" });
            Check(present.All(g => g.NotInDat == ""), "valid filenames reported not in DAT");
            var report = Report(fixture, present);
            Counts(report, 1, 1);
            Check(report.Contains(japan) && report.Contains("Parent: " + parent), "missing clone or complete parent absent");
            Check(!report.Contains(europe), "present clone reported missing");
        });
        await Test("empty gamelist reports all games and excludes No-Intro BIOS entries", () =>
        {
            var report = Report(fixture, new());
            Counts(report, 3, 2);
            Check(!report.Contains("[BIOS]"), "BIOS included");
            return Task.CompletedTask;
        });
        await Test("parents-only and clones-only report filters", () =>
        {
            Counts(Report(fixture, new(), parents: true), 3, 0);
            Counts(Report(fixture, new(), clones: true), 0, 2);
            return Task.CompletedTask;
        });
        await Test("CSV quotes punctuated titles and full parent references", () =>
        {
            var csv = Report(fixture, new(), text: false);
            Check(csv.Contains("\"" + europe + "\",\"" + parent + "\""), "CSV title or parent lost");
            Check(!csv.Contains("[BIOS]"), "BIOS leaked to CSV");
            return Task.CompletedTask;
        });
        await Test("complete gamelist produces no missing report", async () =>
        {
            Check(Report(fixture, await Match(fixture, fixture.Select(g => g.Name))) == "", "false missing games");
        });
        await Test("MAME driver, control, disk and exclusion metadata still parse", async () =>
        {
            var (games, mame) = await ParseXml("<mame build='0.test'><machine name='parent'><description>Parent</description></machine><machine name='child' cloneof='parent'><description>Child</description><driver status='preliminary'/><input><control type='joy' player='1' buttons='3'/></input><disk name='childdisk' status='nodump'/></machine><machine name='bios' isbios='yes'/><machine name='dev' isdevice='yes'/><machine name='soft'><softwarelist name='software'/></machine></mame>");
            var child = games.Single(g => g.Name == "child");
            Check(mame?.Name == "MAME" && mame.Version == "0.test", "MAME header changed");
            Check(child.CloneOf == "parent" && child.NonPlayable.Contains("Preliminary driver") && child.NonPlayable.Contains("nodump") && child.CHDRequired.Contains("childdisk.chd") && child.ButtonCount == 3 && child.ControlTypes.Contains("joy1"), "MAME metadata lost");
            Counts(Report(games, new()), 1, 1);
        });
        await Test("FBNeo-style game entries remain supported", async () =>
        {
            var (games, fb) = await ParseXml("<datafile><header><name>FBNeo</name><description>Arcade</description><version>test</version><author>FBNeo Team</author></header><game name='fbparent'><description>Parent</description><rom name='chip.bin'/></game><game name='fbclone' cloneof='fbparent'><description>Clone</description></game></datafile>");
            Check(games.Count == 2 && games[1].CloneOf == "fbparent" && fb?.Name == "FBNeo" && fb.Author == "FBNeo Team", "FBNeo import changed");
        });
        await Test("empty header does not consume subsequent game entries", async () =>
        {
            var (games, _) = await ParseXml("<datafile><header/><game name='Standalone (World)'/></datafile>");
            Check(games.Count == 1 && games[0].Name == "Standalone (World)", "header consumed game");
        });
        await Test("malformed XML fails without accepting partial results", async () =>
        {
            try { await ParseXml("<datafile><game name='broken'>"); throw new Exception("malformed XML accepted"); }
            catch (XmlException) { }
        });
        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
        {
            await Test("full supplied DAT preserves every entry and matches all full filenames", async () =>
            {
                var before = SHA256.HashData(File.ReadAllBytes(args[1]));
                using var stream = File.OpenRead(args[1]);
                var (games, sampleHeader) = await Parse(stream);
                var document = Document(args[1]);
                var expected = document.Root!.Elements("game").ToArray();
                Check(games.Count == expected.Length, "entry count differs");
                for (int i = 0; i < games.Count; i++)
                {
                    Check(games[i].Name == (string?)expected[i].Attribute("name"), "title differs at " + i);
                    Check(games[i].CloneOf == ((string?)expected[i].Attribute("cloneof") ?? ""), "parent differs at " + i);
                    Check(games[i].Description == (string?)expected[i].Element("description"), "description differs at " + i);
                }
                var h = document.Root!.Element("header")!;
                Check(sampleHeader is not null && sampleHeader.Name == (string?)h.Element("name") && sampleHeader.Version == (string?)h.Element("version") && sampleHeader.Author == (string?)h.Element("author") && sampleHeader.Date == (string?)h.Element("date") && sampleHeader.Description == (string?)h.Element("description"), "header differs");
                var present = await Match(games, games.Select(g => g.Name));
                Check(present.All(g => g.NotInDat == "") && Report(games, present) == "", "false misses");
                var removed = games.First(g => g.Name.Contains('.') && string.IsNullOrEmpty(g.NonPlayable));
                var report = Report(games, present.Where(g => g.Name != removed.Name).ToList());
                Check(report.Contains(removed.Name), "missing punctuated title not detected");
                Counts(report, string.IsNullOrEmpty(removed.CloneOf) ? 1 : 0, string.IsNullOrEmpty(removed.CloneOf) ? 0 : 1);
                Check(before.SequenceEqual(SHA256.HashData(File.ReadAllBytes(args[1]))), "sample modified");
                Console.WriteLine($"  {games.Count} entries; {games.Count(g => string.IsNullOrEmpty(g.CloneOf))} parents; {games.Count(g => !string.IsNullOrEmpty(g.CloneOf))} clones; {games.Count(g => g.NonPlayable.Contains("BIOS"))} BIOS entries.");
            });
        }
        Console.WriteLine($"{pass} passed; {fail} failed");
        return fail == 0 ? 0 : 1;
    }
}
'@ | Set-Content -LiteralPath (Join-Path $testRoot 'Program.cs')
    dotnet restore (Join-Path $testRoot 'DatTests.csproj') --configfile (Join-Path $testRoot 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw 'Could not prepare the isolated DAT test host.' }
    dotnet run --project (Join-Path $testRoot 'DatTests.csproj') --configuration Release --no-restore -- $fixturePath $samplePath
    if ($LASTEXITCODE -ne 0) { throw 'DAT regression tests failed.' }
}
finally {
    $cleanupPath = [IO.Path]::GetFullPath($testRoot)
    if ([IO.Path]::GetDirectoryName($cleanupPath) -eq [IO.Path]::TrimEndingDirectorySeparator($tempRoot) -and
        [IO.Path]::GetFileName($cleanupPath) -match '^GamelistDatTests-[0-9a-f]{32}$') {
        Remove-Item -LiteralPath $cleanupPath -Recurse -Force
    }
}
