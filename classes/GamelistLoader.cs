using System.Data;

namespace GamelistManager.classes
{
    public static class GamelistLoader
    {
        public static DataSet LoadGamelist(string xmlFilePath)
        {
            DataSet destinationDataSet = new DataSet();
            DataSet sourceDataSet = new DataSet();

            try
            {
                sourceDataSet.ReadXml(xmlFilePath);
            }
            catch
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
            // Add columns to the new datatable
            foreach (var metaDataDecl in viewableMetaData)
            {
                // Check if the metadata type is bool, otherwise use string
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


            // Add the new DataTable to the destination DataSet
            destinationDataSet.Tables.Add(newTable);
            destinationDataSet.AcceptChanges();

            // Copy data from sourceDataSet to destinationDataSet
            foreach (DataRow sourceRow in sourceDataSet.Tables[0].Rows)
            {
                DataRow destinationRow = destinationDataSet.Tables[0].NewRow();

                foreach (var metaDataDecl in viewableMetaData)
                {
                    // Source column name is Type
                    // Destination column name is Name
                    // This is purely for aesthetics
                    // It's a little bit more work, but the datagrid looks better
                    string sourceColumnName = metaDataDecl.Type;
                    string destinationColumnName = metaDataDecl.Name;

                    // Check if the source column exists
                    if (sourceDataSet.Tables[0].Columns.Contains(sourceColumnName))
                    {
                        var sourceValue = sourceRow[sourceColumnName];

                        Type columnType = destinationDataSet.Tables[0].Columns[destinationColumnName]!.DataType;

                        // Handle different column types
                        if (columnType == typeof(bool))
                        {
                            bool destinationValue = false; // Default value for bool columns
                            if (sourceValue != DBNull.Value && sourceValue != null)
                            {
                                destinationValue = Convert.ToBoolean(sourceValue);
                            }
                            destinationRow[destinationColumnName] = destinationValue;
                        }
                        else
                        {
                            // Handle other column types and default to empty string if necessary
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
                destinationDataSet.Tables[0].Rows.Add(destinationRow);
            }
            return destinationDataSet;
        }
    }
}