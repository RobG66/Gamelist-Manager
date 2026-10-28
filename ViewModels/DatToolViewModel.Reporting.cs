using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gamelist_Manager.ViewModels;

public partial class DatToolViewModel
{
    #region Report Helpers

    internal static string BuildMissingReport(
        List<GameReportItem> datSummary,
        List<GameReportItem> gamelistSummary,
        bool filterParents,
        bool filterClones,
        bool isTextFormat,
        string datFileName)
    {
        var gamelistLookup = new HashSet<string>(
            gamelistSummary.Select(g => g.Name),
            FilePathHelper.PathComparer);

        var missingParents = new List<(string Name, string Description, string NonPlayable, string CHDRequired)>();
        var missingClones = new List<(string Name, string CloneOf, string Description, string NonPlayable, string CHDRequired)>();

        foreach (var datItem in datSummary)
        {
            if (gamelistLookup.Contains(datItem.Name)) continue;

            if (!string.IsNullOrEmpty(datItem.NonPlayable))
            {
                string np = datItem.NonPlayable.ToLowerInvariant();
                if (np.Contains("device") || np.Contains("bios") || np.Contains("software list"))
                    continue;
            }

            if (string.IsNullOrEmpty(datItem.CloneOf))
                missingParents.Add((datItem.Name, datItem.Description, datItem.NonPlayable, datItem.CHDRequired));
            else
                missingClones.Add((datItem.Name, datItem.CloneOf, datItem.Description, datItem.NonPlayable, datItem.CHDRequired));
        }

        if (filterParents)
            missingClones.Clear();
        else if (filterClones)
            missingParents.Clear();

        if (missingParents.Count == 0 && missingClones.Count == 0)
            return string.Empty;

        return isTextFormat
            ? GenerateTextReport(missingParents, missingClones, datFileName)
            : GenerateCsvReport(missingParents, missingClones, datFileName);
    }

    // Builds a plain-text report
    internal static string GenerateTextReport(
        List<(string Name, string Description, string NonPlayable, string CHDRequired)> missingParents,
        List<(string Name, string CloneOf, string Description, string NonPlayable, string CHDRequired)> missingClones,
        string datFileName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MISSING GAMES REPORT");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"DAT File: {datFileName}");
        sb.AppendLine();
        sb.AppendLine($"Total Missing Parents: {missingParents.Count}");
        sb.AppendLine($"Total Missing Clones:  {missingClones.Count}");
        sb.AppendLine($"Total Missing:         {missingParents.Count + missingClones.Count}");
        sb.AppendLine();

        if (missingParents.Count > 0)
        {
            sb.AppendLine("MISSING PARENTS (Alphabetical)");
            foreach (var p in missingParents.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine(p.Name);
                if (!string.IsNullOrEmpty(p.Description))
                    sb.AppendLine($"  Description:  {p.Description}");
                sb.AppendLine($"  Playable:      {(string.IsNullOrEmpty(p.NonPlayable) ? "Yes" : "No")}");
                if (!string.IsNullOrEmpty(p.NonPlayable))
                    sb.AppendLine($"  Status:        {p.NonPlayable}");
                if (!string.IsNullOrEmpty(p.CHDRequired))
                    sb.AppendLine($"  CHD Required:  {p.CHDRequired}");
                sb.AppendLine();
            }
        }

        if (missingClones.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("MISSING CLONES (Grouped by Parent, Alphabetical)");

            foreach (var group in missingClones
                .GroupBy(c => c.CloneOf, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"Parent: {group.Key}");
                foreach (var c in group.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"  {c.Name}");
                    if (!string.IsNullOrEmpty(c.Description))
                        sb.AppendLine($"    Description:  {c.Description}");
                    sb.AppendLine($"    Playable:      {(string.IsNullOrEmpty(c.NonPlayable) ? "Yes" : "No")}");
                    if (!string.IsNullOrEmpty(c.NonPlayable))
                        sb.AppendLine($"    Status:        {c.NonPlayable}");
                    if (!string.IsNullOrEmpty(c.CHDRequired))
                        sb.AppendLine($"    CHD Required:  {c.CHDRequired}");
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine("END OF REPORT");
        return sb.ToString();
    }

    // Builds a CSV report with columns: Type, Name, CloneOf, Description, Playable, Status, CHD Required.
    internal static string GenerateCsvReport(
        List<(string Name, string Description, string NonPlayable, string CHDRequired)> missingParents,
        List<(string Name, string CloneOf, string Description, string NonPlayable, string CHDRequired)> missingClones,
        string datFileName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Type,Name,CloneOf,Description,Playable,Status,CHD Required");

        foreach (var p in missingParents.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine(string.Join(",",
                    "Parent",
                    CsvHelper.EscapeCsv(p.Name),
                    string.Empty,
                    CsvHelper.EscapeCsv(p.Description),
                    string.IsNullOrEmpty(p.NonPlayable) ? "Yes" : "No",
                    CsvHelper.EscapeCsv(p.NonPlayable),
                    CsvHelper.EscapeCsv(p.CHDRequired)));
        }

        foreach (var group in missingClones
            .GroupBy(c => c.CloneOf, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var c in group.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine(string.Join(",",
                    "Clone",
                    CsvHelper.EscapeCsv(c.Name),
                    CsvHelper.EscapeCsv(c.CloneOf),
                    CsvHelper.EscapeCsv(c.Description),
                    string.IsNullOrEmpty(c.NonPlayable) ? "Yes" : "No",
                    CsvHelper.EscapeCsv(c.NonPlayable),
                    CsvHelper.EscapeCsv(c.CHDRequired)));
            }
        }

        return sb.ToString();
    }

    #endregion
}
