using System.Data;
using System.Text;
using System.Windows;

namespace GamelistManager.classes
{
    public static class GamelistLoader
    {
        /// <summary>
        /// Load an existing gamelist XML file into a DataSet.
        /// Returns null if the file cannot be read or has no valid "game" table.
        /// </summary>
        public static DataSet? LoadGamelist(string xmlFilePath)
        {
            DataSet destinationDataSet = new DataSet();
            DataSet sourceDataSet = new DataSet();
            List<string> duplicateRomPaths = new List<string>();

            try
            {
                sourceDataSet.ReadXml(xmlFilePath);
            }
            catch
            {
                return null;
            }

            if (sourceDataSet.Tables.Count == 0 || sourceDataSet.Tables["game"] == null)
            {
                return null;
            }

            // Build schema
            DataTable newTable = CreateGameTable();
            destinationDataSet.Tables.Add(newTable);

            // Track "Rom Path" to ensure no duplicates
            var uniqueRomPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow sourceRow in sourceDataSet.Tables["game"].Rows)
            {
                DataRow destinationRow = destinationDataSet.Tables[0].NewRow();

                foreach (var metaDataDecl in GamelistMetaData.GetMetaDataDictionary().Values.Where(d => d.Viewable))
                {
                    string sourceColumnName = metaDataDecl.Type;
                    string destinationColumnName = metaDataDecl.Name;

                    if (sourceDataSet.Tables["game"].Columns.Contains(sourceColumnName))
                    {
                        var sourceValue = sourceRow[sourceColumnName];
                        Type columnType = destinationDataSet.Tables[0].Columns[destinationColumnName]!.DataType;

                        if (columnType == typeof(bool))
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
                            if (sourceValue == DBNull.Value || string.IsNullOrEmpty(sourceValue.ToString()))
                                continue;

                            if (destinationColumnName == "Release Date" || destinationColumnName == "Last Played")
                            {
                                string formattedDate = ISO8601Converter.ConvertFromISO8601(sourceValue.ToString()!);
                                if (!string.IsNullOrEmpty(formattedDate))
                                    destinationRow[destinationColumnName] = formattedDate;
                                else
                                    destinationRow[destinationColumnName] = sourceValue.ToString();
                                continue;
                            }

                            destinationRow[destinationColumnName] = sourceValue.ToString();
                        }
                    }
                    else
                    {
                        // If column is missing in source but exists in schema, set default
                        if (destinationDataSet.Tables[0].Columns[destinationColumnName]?.DataType == typeof(bool))
                            destinationRow[destinationColumnName] = false;
                    }
                }

                // Check and handle duplicates in "Rom Path"
                string? romPath = destinationRow["Rom Path"]?.ToString();
                if (!string.IsNullOrEmpty(romPath))
                {
                    if (uniqueRomPaths.Contains(romPath))
                    {
                        duplicateRomPaths.Add(romPath);
                        continue; // Skip duplicate
                    }
                    uniqueRomPaths.Add(romPath);
                }

                destinationDataSet.Tables[0].Rows.Add(destinationRow);
            }

            // Show a warning if duplicates were removed
            if (duplicateRomPaths.Count > 0)
            {
                var preview = string.Join(", ", duplicateRomPaths.Take(10));
                string warningMessage = $"The following duplicates were found and ignored:\n{preview}";
                if (duplicateRomPaths.Count > 10)
                    warningMessage += $"\n... and {duplicateRomPaths.Count - 10} more.";
                MessageBox.Show(warningMessage, "Duplicate Detected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return destinationDataSet;
        }

        /// <summary>
        /// Create a new, empty gamelist DataSet with the correct schema.
        /// </summary>
        public static DataSet CreateEmptyGamelist()
        {
            DataSet dataSet = new DataSet();
            dataSet.Tables.Add(CreateGameTable());
            return dataSet;
        }

        /// <summary>
        /// Internal helper to build the "game" DataTable schema.
        /// </summary>
        private static DataTable CreateGameTable()
        {
            var viewableMetaData = GamelistMetaData.GetMetaDataDictionary().Values
                .Where(decl => decl.Viewable)
                .ToList();

            DataTable table = new DataTable("game");

            // Status column first
            DataColumn statusColumn = new DataColumn
            {
                ColumnName = "Status",
                DataType = typeof(string),
            };
            table.Columns.Add(statusColumn);

            foreach (var metaDataDecl in viewableMetaData)
            {
                Type dataType = metaDataDecl.DataType == MetaDataType.Bool ? typeof(bool) : typeof(string);

                DataColumn column = new DataColumn
                {
                    ColumnName = metaDataDecl.Name,
                    DataType = dataType,
                };
                table.Columns.Add(column);
            }
            return table;
        }
    }
}
