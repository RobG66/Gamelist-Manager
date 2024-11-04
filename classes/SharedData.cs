using System.Data;

namespace GamelistManager.classes
{
    // This is global data accessible by all forms, methods and classes
    public static class SharedData
    {
        private static DataSet _dataSet = new DataSet();
        private static string _xmlFilename = string.Empty;
        private static bool _isDataChanged;
        private static string _currentSystem = string.Empty;
        private static string _programDirectory = string.Empty;
        private static ChangeTracker? _changeTracker;
                  
        public static void InitializeChangeTracker()
        {
            _changeTracker = new ChangeTracker();
        }
                        
        public static ChangeTracker? ChangeTracker
        {
            get => _changeTracker;
            set => _changeTracker = value;
        }

        public static bool IsDataChanged
        {
            get { return _isDataChanged; }
            set { _isDataChanged = value; }
        }
        
        public static DataSet DataSet
        {
            get { return _dataSet; }
            set { _dataSet = value; }
        }

        public static string XMLFilename
        {
            get { return _xmlFilename; }
            set { _xmlFilename = value; }
        }

        public static string ProgramDirectory
        {
            get { return _programDirectory; }
            set { _programDirectory = value; }
        }

        public static string CurrentSystem
        {
            get { return _currentSystem; }
            set { _currentSystem = value; }
        }
    }
}
