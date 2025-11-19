using GamelistManager.classes.helpers;
using System.Data;
using System.Windows;

namespace GamelistManager.classes.gamelist;

public static class GamelistLoader
{
    /// <summary>
    /// Load an existing gamelist XML file into a DataSet.
    /// Returns null if the file cannot be read or has no valid "game" table.
    /// </summary>
    public static DataSet? LoadGamelist(string xmlFilePath)
    {
        DataSet sourceDataSet = new();

        try
        {
            sourceDataSet.ReadXml(xmlFilePath);
        }
        catch
        {
            return null;
        }

        DataTable? sourceTable = sourceDataSet.Tables["game"];

        if (sourceTable == null || sourceDataSet == null || sourceDataSet.Tables.Count == 0 || sourceDataSet.Tables["game"] == null)
        {
            return null;
        }

        DataSet destinationDataSet = new();
        DataTable newTable = CreateGameTable();
        destinationDataSet.Tables.Add(newTable);

        // Pre-cache viewable metadata to avoid repeated LINQ queries
        var viewableMetadata = GamelistMetaData.GetMetaDataDictionary().Values
            .Where(d => d.Viewable)
            .ToList();

        // Pre-build column existence lookup for O(1) access
        var sourceColumns = new HashSet<string>(
            sourceTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName),
            StringComparer.OrdinalIgnoreCase
        );

        // Track unique Rom Paths
        var uniqueRomPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateRomPaths = new List<string>();

        int indexCounter = 1;
        DataTable destTable = destinationDataSet.Tables[0];

        // Get target column info once
        var destColumns = destTable.Columns;

        foreach (DataRow sourceRow in sourceTable.Rows)
        {
            DataRow destinationRow = destTable.NewRow();

            // Process all metadata columns
            foreach (var metaDataDecl in viewableMetadata)
            {
                string sourceColumnName = metaDataDecl.Type;
                string destinationColumnName = metaDataDecl.Name;

                // Fast column existence check
                if (sourceColumns.Contains(sourceColumnName))
                {
                    var sourceValue = sourceRow[sourceColumnName];

                    if (metaDataDecl.DataType == MetaDataType.Bool)
                    {
                        // Handle boolean
                        bool destinationValue = false;
                        if (sourceValue != DBNull.Value && sourceValue != null)
                        {
                            try
                            {
                                destinationValue = Convert.ToBoolean(sourceValue);
                            }
                            catch
                            {
                                destinationValue = false;
                            }
                        }
                        destinationRow[destinationColumnName] = destinationValue;
                    }
                    else
                    {
                        // Handle string values
                        if (sourceValue == DBNull.Value || sourceValue == null)
                            continue;

                        string stringValue = sourceValue.ToString()!;
                        if (string.IsNullOrEmpty(stringValue))
                            continue;

                        // Handle date conversions
                        if (destinationColumnName == "Release Date" || destinationColumnName == "Last Played")
                        {
                            string formattedDate = ISO8601Helper.ConvertFromISO8601(stringValue);
                            destinationRow[destinationColumnName] = !string.IsNullOrEmpty(formattedDate)
                                ? formattedDate
                                : stringValue;
                        }
                        else
                        {
                            destinationRow[destinationColumnName] = stringValue;
                        }
                    }
                }
                else
                {
                    // Column missing in source - set default for bool
                    if (metaDataDecl.DataType == MetaDataType.Bool)
                        destinationRow[destinationColumnName] = false;
                }
            }

            // Check for duplicate Rom Path
            string? romPath = destinationRow["Rom Path"]?.ToString();
            if (!string.IsNullOrEmpty(romPath))
            {
                if (!uniqueRomPaths.Add(romPath)) // Add returns false if already exists
                {
                    duplicateRomPaths.Add(romPath);
                    continue; // Skip duplicate
                }
            }

            // Assign unique index
            destinationRow["Index"] = indexCounter++;
            destTable.Rows.Add(destinationRow);
        }

        // Show duplicate warning if needed
        if (duplicateRomPaths.Count > 0)
        {
            ShowDuplicateWarning(duplicateRomPaths);
        }

        return destinationDataSet;
    }

    private static void ShowDuplicateWarning(List<string> duplicates)
    {
        var preview = string.Join(", ", duplicates.Take(10));
        string warningMessage = $"The following duplicates were found and ignored:\n{preview}";

        if (duplicates.Count > 10)
            warningMessage += $"\n... and {duplicates.Count - 10} more.";

        MessageBox.Show(warningMessage, "Duplicate Detected", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <summary>
    /// Create a new, empty gamelist DataSet with the correct schema.
    /// </summary>
    public static DataSet CreateEmptyGamelist()
    {
        DataSet dataSet = new();
        dataSet.Tables.Add(CreateGameTable());
        return dataSet;
    }

    /// <summary>
    /// Internal helper to build the "game" DataTable schema.
    /// Includes a hidden "Index" column not written to XML.
    /// </summary>
    private static DataTable CreateGameTable()
    {
        var viewableMetaData = GamelistMetaData.GetMetaDataDictionary().Values
            .Where(decl => decl.Viewable)
            .ToList();

        DataTable table = new("game");

        // Hidden index column (not written to XML)
        table.Columns.Add(new DataColumn
        {
            ColumnName = "Index",
            DataType = typeof(int),
            ColumnMapping = MappingType.Hidden
        });

        // Status column
        table.Columns.Add(new DataColumn
        {
            ColumnName = "Status",
            DataType = typeof(string),
        });

        // All viewable metadata columns
        foreach (var metaDataDecl in viewableMetaData)
        {
            Type dataType = metaDataDecl.DataType == MetaDataType.Bool ? typeof(bool) : typeof(string);

            table.Columns.Add(new DataColumn
            {
                ColumnName = metaDataDecl.Name,
                DataType = dataType,
            });
        }

        return table;
    }
}