using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Gamelist_Manager.ViewModels;

public partial class DatToolViewModel
{
    #region Processing Pipeline

    private async Task ProcessDatStreamAsync(Stream xmlStream, string datFileName, Process? mameProcess = null)
    {
        DatFileName = datFileName;

        try
        {
            var (datEntries, datHeader) = await CreateDatSummaryAsync(xmlStream);
            xmlStream.Dispose();

            if (datEntries == null || datEntries.Count == 0)
            {
                await Views.ThreeButtonDialogView.ShowAsync(new Views.ThreeButtonDialogConfig
                {
                    Title = "No Data",
                    Message = "No entries were found in the DAT file.",
                    IconTheme = Views.DialogIconTheme.Info,
                    Button1Text = string.Empty,
                    Button2Text = string.Empty,
                    Button3Text = "OK"
                });
                return;
            }

            _datSummary.Clear();
            _datSummary.AddRange(datEntries);
            _datHeader = datHeader;
            IsReportComboEnabled = false;

            try
            {
                UpdateDatSummaryCounts();
                UpdateDatHeaderInfo();

                var gamelistSnapshot = _sharedData.GamelistData?.ToList() ?? [];
                string romDirectory = _sharedData.CurrentRomFolder!;

                _gamelistSummary.Clear();
                _gamelistSummary.AddRange(
                    await CreateGamelistSummaryAsync(_datSummary, gamelistSnapshot, romDirectory));
                // TODO: Review this code
                UpdateGamelistSummaryCounts(IncludeHidden);
            }
            finally
            {
                IsReportComboEnabled = true;
            }

            IsIncludeHiddenEnabled = true;
            IsCsvOutputEnabled = true;
            IsFindMissingEnabled = true;
        }
        finally
        {
            mameProcess?.Dispose();
            if (mameProcess == _mameProcess)
                _mameProcess = null;
        }
    }

    #endregion

    #region Gamelist Cross-Reference

    private static async Task<List<GameReportItem>> CreateGamelistSummaryAsync(
        List<GameReportItem> datSummary,
        List<GameMetadataRow> gamelistSnapshot,
        string romDirectory)
    {
        return await Task.Run(() =>
        {
            var datLookup = datSummary.ToDictionary(d => d.Name, d => d, FilePathHelper.PathComparer);

            return gamelistSnapshot.Select(row =>
            {
                var romName = FilePathHelper.NormalizeRomName(row.Path);

                if (datLookup.TryGetValue(romName, out var datItem))
                {
                    string chdStatus = string.Empty;

                    if (!string.IsNullOrEmpty(datItem.CHDRequired))
                    {
                        string chdPath = Path.Combine(romDirectory, romName);
                        bool chdExists = Directory.Exists(chdPath) &&
                                          Directory.GetFiles(chdPath, "*.chd").Length > 0;
                        chdStatus = chdExists
                            ? $"{datItem.CHDRequired} (OK)"
                            : $"{datItem.CHDRequired} (Missing)";
                    }

                    return new GameReportItem
                    {
                        Name = romName,
                        CloneOf = datItem.CloneOf,
                        Description = datItem.Description,
                        NonPlayable = datItem.NonPlayable,
                        CHDRequired = chdStatus,
                    };
                }

                return new GameReportItem { Name = romName, NotInDat = "Not In DAT" };
            }).ToList();
        });
    }

    #endregion

    #region DAT XML Parsing

    private static async Task<(List<GameReportItem>, DatHeader?)> CreateDatSummaryAsync(Stream xmlStream)
    {
        return await Task.Run(() =>
        {
            var list = new List<GameReportItem>();
            DatHeader? header = null;
            bool headerParsed = false;

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null,
                IgnoreWhitespace = true,
                IgnoreComments = true
            };

            using var reader = XmlReader.Create(xmlStream, settings);

            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                    continue;

                string elementName = reader.Name.ToLowerInvariant();

                if (elementName == "mame" && !headerParsed)
                {
                    header = new DatHeader
                    {
                        Name = "MAME",
                        Version = reader.GetAttribute("build") ?? string.Empty,
                        Description = $"MAME {reader.GetAttribute("build") ?? string.Empty}",
                        Author = "MAME Team"
                    };
                    headerParsed = true;
                    continue;
                }

                if (elementName == "header" && !headerParsed)
                {
                    header = ParseDatHeaderInline(reader);
                    headerParsed = true;
                    continue;
                }

                if (elementName is not ("machine" or "game"))
                    continue;

                string? nameAttr = reader.GetAttribute("name");
                if (string.IsNullOrEmpty(nameAttr))
                    continue;

                using var subtree = reader.ReadSubtree();
                subtree.Read();

                var item = ParseSingleMachine(subtree);
                if (item != null)
                    list.Add(item);
            }

            return (list, header);
        });
    }

    private static GameReportItem? ParseSingleMachine(XmlReader reader)
    {
        string name = FilePathHelper.NormalizeRomName(reader.GetAttribute("name") ?? string.Empty);
        string cloneOf = FilePathHelper.NormalizeRomName(reader.GetAttribute("cloneof") ?? string.Empty);
        string runnable = reader.GetAttribute("runnable") ?? "yes";
        string isBios = reader.GetAttribute("isbios") ?? "no";
        string isDevice = reader.GetAttribute("isdevice") ?? "no";
        string isMechanical = reader.GetAttribute("ismechanical") ?? "no";

        var nonPlayable = new List<string>();
        var diskStatus = new List<string>();
        string description = string.Empty;
        bool needsCHD = false;
        bool hasSoftwareList = false;

        if (runnable.Equals("no", StringComparison.OrdinalIgnoreCase)) nonPlayable.Add("Not runnable");
        if (isBios.Equals("yes", StringComparison.OrdinalIgnoreCase)) nonPlayable.Add("BIOS");
        if (isDevice.Equals("yes", StringComparison.OrdinalIgnoreCase)) nonPlayable.Add("Device");
        if (isMechanical.Equals("yes", StringComparison.OrdinalIgnoreCase)) nonPlayable.Add("Mechanical");

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
                continue;

            switch (reader.Name.ToLowerInvariant())
            {
                case "disk":
                    {
                        var (diskStatusText, nonPlayableReason, requiresCHD) = ResolveDiskStatus(reader);
                        diskStatus.Add(diskStatusText);
                        if (requiresCHD) needsCHD = true;
                        if (!string.IsNullOrEmpty(nonPlayableReason)) nonPlayable.Add(nonPlayableReason);
                        break;
                    }
                case "driver":
                    if ((reader.GetAttribute("status") ?? string.Empty)
                        .Equals("preliminary", StringComparison.OrdinalIgnoreCase))
                        nonPlayable.Add("Preliminary driver");
                    break;
                case "description":
                    description = reader.ReadElementContentAsString().Trim();
                    break;
                case "softwarelist":
                    hasSoftwareList = true;
                    break;
                case "device":
                    if ((reader.GetAttribute("type") ?? string.Empty)
                        .Equals("software_list", StringComparison.OrdinalIgnoreCase))
                        hasSoftwareList = true;
                    break;
            }
        }

        if (hasSoftwareList)
            nonPlayable.Add("Software list");

        return new GameReportItem
        {
            Name = name,
            CloneOf = cloneOf,
            Description = description,
            CHDRequired = needsCHD ? string.Join(", ", diskStatus) : string.Empty,
            NonPlayable = nonPlayable.Count > 0 ? string.Join(", ", nonPlayable) : string.Empty,
        };
    }

    private static (string DiskStatus, string? NonPlayableReason, bool RequiresCHD) ResolveDiskStatus(XmlReader reader)
    {
        string diskName = reader.GetAttribute("name") ?? string.Empty;
        string statusAttr = reader.GetAttribute("status") ?? string.Empty;
        string optionalAttr = reader.GetAttribute("optional") ?? string.Empty;

        bool required = string.IsNullOrEmpty(optionalAttr) ||
                          !optionalAttr.Equals("yes", StringComparison.OrdinalIgnoreCase);
        string chdFile = $"{diskName}.chd";
        string diskStatus = string.IsNullOrEmpty(statusAttr) ? chdFile : $"{chdFile} ({statusAttr})";

        string? nonPlayableReason = statusAttr.Equals("nodump", StringComparison.OrdinalIgnoreCase)
            ? $"Disk {diskName} nodump"
            : null;

        return (diskStatus, nonPlayableReason, required);
    }

    private static DatHeader ParseDatHeaderInline(XmlReader reader)
    {
        var header = new DatHeader();
        int depth = reader.Depth;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
                break;

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            switch (reader.Name.ToLowerInvariant())
            {
                case "name": header.Name = reader.ReadElementContentAsString().Trim(); break;
                case "version": header.Version = reader.ReadElementContentAsString().Trim(); break;
                case "author": header.Author = reader.ReadElementContentAsString().Trim(); break;
                case "date": header.Date = reader.ReadElementContentAsString().Trim(); break;
                case "description": header.Description = reader.ReadElementContentAsString().Trim(); break;
            }
        }

        return header;
    }

    #endregion

    #region MAME Streaming

    private static Task<(Stream? Stream, Process? Process)> GetMameListXmlStreamAsync(
        string mamePath, string arguments)
    {
        return Task.Run<(Stream?, Process?)>(() =>
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = mamePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            var process = new Process { StartInfo = startInfo };
            process.Start();
            return (process.StandardOutput.BaseStream, (Process?)process);
        });
    }

    #endregion

    #region UI Update Methods

    private void UpdateDatHeaderInfo()
    {
        DatInfoName = OrDash(_datHeader?.Name);
        DatInfoVersion = OrDash(_datHeader?.Version);
        DatInfoAuthor = OrDash(_datHeader?.Author);
        DatInfoDate = OrDash(_datHeader?.Date);
        DatInfoDescription = OrDash(_datHeader?.Description);
    }

    private void UpdateDatSummaryCounts()
    {
        if (_datSummary.Count == 0)
        {
            DatTotal = DatParents = DatClones = DatCHD = DatNonPlayable = DatPlayable = "0";
            return;
        }

        int total = _datSummary.Count;
        int parents = _datSummary.Count(g => string.IsNullOrEmpty(g.CloneOf));
        int clones = _datSummary.Count(g => !string.IsNullOrEmpty(g.CloneOf));
        int needsChd = _datSummary.Count(g => !string.IsNullOrEmpty(g.CHDRequired));
        int nonPlayable = _datSummary.Count(g => !string.IsNullOrEmpty(g.NonPlayable));

        DatTotal = total.ToString();
        DatParents = parents.ToString();
        DatClones = clones.ToString();
        DatCHD = needsChd.ToString();
        DatNonPlayable = nonPlayable.ToString();
        DatPlayable = (total - nonPlayable).ToString();
    }

    private void UpdateGamelistSummaryCounts(bool includeHidden)
    {
        if (_gamelistSummary.Count == 0)
        {
            GamelistTotal = GamelistParents = GamelistClones = GamelistCHD =
                GamelistNonPlayable = GamelistMissingParents = GamelistMissingClones = GamelistNotInDat = "—";
            return;
        }

        HashSet<string>? hiddenSet = null;
        if (!includeHidden)
        {
            hiddenSet = new HashSet<string>(
                (_sharedData.GamelistData ?? [])
                    .Where(r => r.Hidden)
                    .Select(r => FilePathHelper.NormalizeRomName(r.Path)),
                FilePathHelper.PathComparer);
        }

        var filtered = _gamelistSummary
            .Where(g => includeHidden || (hiddenSet != null && !hiddenSet.Contains(g.Name)))
            .ToList();

        var datLookup = _datSummary.ToDictionary(d => d.Name, d => d, FilePathHelper.PathComparer);
        var gamelistLookup = new HashSet<string>(filtered.Select(g => g.Name), FilePathHelper.PathComparer);

        int datParentsTotal = 0, datParentsPresent = 0;
        int datClonesTotal = 0, datClonesPresent = 0;

        foreach (var datItem in _datSummary)
        {
            bool isParent = string.IsNullOrEmpty(datItem.CloneOf);
            bool present = gamelistLookup.Contains(datItem.Name);

            if (isParent) { datParentsTotal++; if (present) datParentsPresent++; }
            else { datClonesTotal++; if (present) datClonesPresent++; }
        }

        GamelistTotal = filtered.Count.ToString();
        GamelistParents = filtered.Count(g => datLookup.TryGetValue(g.Name, out var d) && string.IsNullOrEmpty(d.CloneOf)).ToString();
        GamelistClones = filtered.Count(g => datLookup.TryGetValue(g.Name, out var d) && !string.IsNullOrEmpty(d.CloneOf)).ToString();
        GamelistCHD = filtered.Count(g => !string.IsNullOrEmpty(g.CHDRequired)).ToString();
        GamelistNonPlayable = filtered.Count(g => !string.IsNullOrEmpty(g.NonPlayable)).ToString();
        GamelistNotInDat = filtered.Count(g => !datLookup.ContainsKey(g.Name)).ToString();
        GamelistMissingParents = Math.Max(0, datParentsTotal - datParentsPresent).ToString();
        GamelistMissingClones = Math.Max(0, datClonesTotal - datClonesPresent).ToString();
    }

    private static string OrDash(string? value) =>
        string.IsNullOrEmpty(value) ? "—" : value;

    #endregion
}
