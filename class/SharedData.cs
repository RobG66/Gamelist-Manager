using System.Data;

namespace GamelistManager
{
    // This is global data accessible by all forms, methods and classes
    public static class SharedData
    {
        public static readonly object lockObject = new object();
        private static DataSet dataSet;
        private static string xmlFilename;
        private static bool isDataChanged;
      
        public static bool IsDataChanged
        {
            get { return isDataChanged; }
            set { isDataChanged = value; }
        }

        static SharedData()
        {
            dataSet = new DataSet();
        }

        public static DataSet DataSet
        {
            get
            {
                return dataSet;
            }

            set
            {
                dataSet = value;
            }
        }

        public static string XMLFilename
        {
            get { return xmlFilename; }
            set { xmlFilename = value; }
        }

    }
}
