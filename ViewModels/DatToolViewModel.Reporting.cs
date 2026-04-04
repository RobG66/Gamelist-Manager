using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gamelist_Manager.ViewModels;

public partial class DatToolViewModel
{
    #region Report Helpers

    internal static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    // Builds a plain-text report of missing parents and clones, grouped and sorted alphabetically.
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
                EscapeCsv(p.Name),
                string.Empty,
                EscapeCsv(p.Description),
                string.IsNullOrEmpty(p.NonPlayable) ? "Yes" : "No",
                EscapeCsv(p.NonPlayable),
                EscapeCsv(p.CHDRequired)));
        }

        foreach (var group in missingClones
            .GroupBy(c => c.CloneOf, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var c in group.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine(string.Join(",",
                    "Clone",
                    EscapeCsv(c.Name),
                    EscapeCsv(c.CloneOf),
                    EscapeCsv(c.Description),
                    string.IsNullOrEmpty(c.NonPlayable) ? "Yes" : "No",
                    EscapeCsv(c.NonPlayable),
                    EscapeCsv(c.CHDRequired)));
            }
        }

        return sb.ToString();
    }

    #endregion
}
