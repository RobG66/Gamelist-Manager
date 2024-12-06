using System.Data;
using System.Text;
using System.Windows;

namespace GamelistManager.classes
{
    public static class GamelistLoader
    {
        public static DataSet LoadGamelist(string xmlFilePath)
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
                return null!;
            }

            if (sourceDataSet.Tables.Count == 0 || sourceDataSet.Tables["game"] == null)
            {
                return null!;
            }

            MetaDataList metaDataList = new MetaDataList();
            var metaDataDictionary = metaDataList.GetMetaDataDictionary();

            var viewableMetaData = metaDataDictionary.Values
                .Where(decl => decl.Viewable)
                .ToList();

            // Create a new DataTable
            DataTable newTable = new DataTable("game");

            DataColumn statusColumn = new DataColumn
            {
                ColumnName = "Status",
                DataType = typeof(string),
            };
            newTable.Columns.Add(statusColumn);
            newTable.Columns[0].SetOrdinal(0);

            int ordinal = 1;
            foreach (var metaDataDecl in viewableMetaData)
            {
                Type dataType = metaDataDecl.DataType == MetaDataType.Bool ? typeof(bool) : typeof(string);

                DataColumn column = new DataColumn
                {
                    ColumnName = metaDataDecl.Name,
                    DataType = dataType,
                };
                newTable.Columns.Add(column);
                newTable.Columns[ordinal].SetOrdinal(ordinal);
                ordinal++;
            }

            destinationDataSet.Tables.Add(newTable);
            destinationDataSet.AcceptChanges();

            // Track "Rom Path" to ensure no duplicates
            var uniqueRomPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow sourceRow in sourceDataSet.Tables["game"].Rows)
            {
                DataRow destinationRow = destinationDataSet.Tables[0].NewRow();

                foreach (var metaDataDecl in viewableMetaData)
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
                                destinationValue = Convert.ToBoolean(sourceValue);
                            }
                            destinationRow[destinationColumnName] = destinationValue;
                        }
                        else
                        {
                            if (sourceValue == DBNull.Value || string.IsNullOrEmpty(sourceValue.ToString()))
                            {
                                continue;
                            }

                            if (destinationColumnName == "Release Date")
                            {
                                string formattedDate = ISO8601Converter.ConvertFromISO8601(sourceValue.ToString());
                                if (!string.IsNullOrEmpty(formattedDate))
                                {
                                    destinationRow[destinationColumnName] = formattedDate;
                                }
                                continue;
                            }

                            destinationRow[destinationColumnName] = sourceValue == DBNull.Value ? string.Empty : sourceValue.ToString();
                        }
                    }
                    else
                    {
                        if (destinationDataSet.Tables[0].Columns[destinationColumnName]?.DataType == typeof(bool))
                        {
                            destinationRow[destinationColumnName] = false;
                        }
                    }
                }

                // Check and handle duplicates in "Rom Path"
                string? romPath = destinationRow["Rom Path"]?.ToString();
                if (!string.IsNullOrEmpty(romPath))
                {
                    if (uniqueRomPaths.Contains(romPath))
                    {
                        // Track duplicates for later warning
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
                StringBuilder warningMessage = new StringBuilder("The following duplicates were removed:\n");
                warningMessage.Append(string.Join(", ", duplicateRomPaths));

                MessageBox.Show(warningMessage.ToString(), "Duplicate Detected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return destinationDataSet;
        }
    }
}
