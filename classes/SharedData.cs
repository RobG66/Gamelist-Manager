using System.Data;

namespace GamelistManager.classes
{
    // This is global data accessible by all forms, methods and classes
    public static class SharedData
    {
        private static DataSet dataSet = new DataSet();
        private static string xmlFilename = string.Empty;
        private static bool isDataChanged;
        private static string currentSystem = string.Empty;
        private static string programDirectory = string.Empty;
        private static ChangeTracker? changeTracker;
             
        public static void InitializeChangeTracker(DataTable table, int maxUndo)
        {
            changeTracker = new ChangeTracker(table, maxUndo);
        }
                
        public static ChangeTracker? ChangeTracker
        {
            get => changeTracker;
            set => changeTracker = value;
        }

        public static bool IsDataChanged
        {
            get { return isDataChanged; }
            set { isDataChanged = value; }
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

        public static string ProgramDirectory
        {
            get { return programDirectory; }
            set { programDirectory = value; }
        }

        public static string CurrentSystem
        {
            get { return currentSystem; }
            set { currentSystem = value; }
        }
    }
}
