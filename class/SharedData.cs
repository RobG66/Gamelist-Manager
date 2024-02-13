using System.Data;

namespace GamelistManager
{
    public static class SharedData
    {
        private static readonly object datasetLock = new object();
        private static DataSet dataSet;
        private static string xmlFilename;
        static SharedData()
        {
            dataSet = new DataSet();
        }
        public static DataSet DataSet
        {
            get { return dataSet; }
            set { dataSet = value; }
        }
        public static string XMLFilename
        {
            get { return xmlFilename; }
            set { xmlFilename = value; }
        }
        public static object DatasetLock
        {
            get { return datasetLock; }
        }
    }
}
