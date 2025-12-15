using GamelistManager.classes.helpers;
using System.Data;
using System.IO;
using System.Windows;

namespace GamelistManager.classes.gamelist
{
    public static class GamelistLoader
    {
        // Load an existing gamelist XML file into a DataSet.
        // Returns null if the file cannot be read or has no valid "game" table.
        public static DataSet? LoadGamelist(string xmlFilePath, bool ignoreDuplicates)
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

            // Track unique ROM paths
            var uniqueRomPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var duplicateRomPaths = new List<string>();

            int indexCounter = 1;
            DataTable destTable = destinationDataSet.Tables[0];

            var parentFolderPath = Path.GetDirectoryName(xmlFilePath);

            foreach (DataRow sourceRow in sourceTable.Rows)
            {
                DataRow destinationRow = destTable.NewRow();

                // Process metadata fields
                foreach (var metaDataDecl in viewableMetadata)
                {
                    string sourceColumnName = metaDataDecl.Type;
                    string destinationColumnName = metaDataDecl.Name;

                    if (sourceColumns.Contains(sourceColumnName))
                    {
                        var sourceValue = sourceRow[sourceColumnName];

                        if (metaDataDecl.DataType == MetaDataType.Bool)
                        {
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
                            if (sourceValue == DBNull.Value || sourceValue == null)
                                continue;

                            string stringValue = sourceValue.ToString()!;
                            if (string.IsNullOrEmpty(stringValue))
                                continue;

                            // Date conversions
                            if (destinationColumnName == "Release Date" || destinationColumnName == "Last Played")
                            {
                                string formattedDate = ISO8601Helper.ConvertFromISO8601(stringValue);
                                destinationRow[destinationColumnName] =
                                    !string.IsNullOrEmpty(formattedDate) ? formattedDate : stringValue;
                            }
                            else
                            {
                                destinationRow[destinationColumnName] = stringValue;
                            }
                        }
                    }
                    else
                    {
                        // Column missing in source - default false for bool
                        if (metaDataDecl.DataType == MetaDataType.Bool)
                            destinationRow[destinationColumnName] = false;
                    }
                }

                // Duplicate ROM-path detection
                string? normalizedFilePath =
                    PathHelper.ConvertGamelistPathToFullPath(destinationRow["Rom Path"].ToString()!, parentFolderPath!);

                if (!string.IsNullOrEmpty(normalizedFilePath))
                {
                    if (!uniqueRomPaths.Add(normalizedFilePath)) // duplicate detected
                    {
                        duplicateRomPaths.Add(normalizedFilePath);

                        if (ignoreDuplicates)
                            continue; // skip duplicate entirely
                        // else: keep duplicate
                    }
                }

                // Assign unique index
                destinationRow["Index"] = indexCounter++;
                destTable.Rows.Add(destinationRow);
            }

            // Duplicate warnings / logging
            if (duplicateRomPaths.Count > 0)
            {
                ShowDuplicateWarning(duplicateRomPaths, ignoreDuplicates);
            }

            return destinationDataSet;
        }

        private static void ShowDuplicateWarning(List<string> duplicates, bool removed)
        {
            var preview = string.Join(", ", duplicates.Take(10));

            string header = removed
                ? "Duplicate Entries Ignored"
                : "Duplicate Entries Detected";

            string body;

            if (removed)
            {
                body = $"The following duplicates were found and ignored:\n{preview}";
                if (duplicates.Count > 10)
                    body += $"\n... and {duplicates.Count - 10} more.";

                // Log removed duplicate paths
                WriteDuplicateLog(duplicates);
            }
            else
            {
                body =
                    "Duplicate item entries are detected.\n" +
                    "Use the 'Find Duplicates' tool to inspect and fix them.\n\n" +
                    preview;

                if (duplicates.Count > 10)
                    body += $"\n... and {duplicates.Count - 10} more.";
            }

            MessageBox.Show(body, header, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static void WriteDuplicateLog(List<string> duplicates)
        {
            try
            {
                string logDir = Path.Combine(Environment.CurrentDirectory, "logs");
                Directory.CreateDirectory(logDir);

                string logPath = Path.Combine(logDir, "duplicate_removed.log");

                using (StreamWriter writer = new StreamWriter(logPath, append: true))
                {
                    writer.WriteLine("=== Duplicate Entries Removed ===");
                    writer.WriteLine($"Timestamp: {DateTime.Now}");
                    writer.WriteLine("List of removed duplicate ROM paths:");

                    foreach (string entry in duplicates)
                        writer.WriteLine(entry);

                    writer.WriteLine();
                }
            }
            catch
            {
                // Silent fail – logging must not interrupt main workflow
            }
        }

        // Create a new, empty gamelist DataSet with the correct schema.
        public static DataSet CreateEmptyGamelist()
        {
            DataSet dataSet = new();
            dataSet.Tables.Add(CreateGameTable());
            return dataSet;
        }

        // Internal helper to build the "game" DataTable schema.
        // Includes a hidden "Index" column not written to XML.
        private static DataTable CreateGameTable()
        {
            var viewableMetaData = GamelistMetaData.GetMetaDataDictionary().Values
                .Where(decl => decl.Viewable)
                .ToList();

            DataTable table = new("game");

            // Hidden index column
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
                Type dataType = metaDataDecl.DataType == MetaDataType.Bool
                    ? typeof(bool)
                    : typeof(string);

                table.Columns.Add(new DataColumn
                {
                    ColumnName = metaDataDecl.Name,
                    DataType = dataType,
                });
            }

            return table;
        }
    }
}
