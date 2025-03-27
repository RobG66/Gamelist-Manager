using System.Data;

namespace GamelistManager.classes
{
    // This is global data accessible by all forms, methods, and classes
    public static class SharedData
    {
        private static DataSet _dataSet = new DataSet();
        private static string _xmlFilename = string.Empty;
        private static bool _isDataChanged;
        private static string _currentSystem = string.Empty;
        private static string _programDirectory = string.Empty;
        private static bool _metaDataEditMode = false;
        private static ChangeTracker? _changeTracker;


        /// <summary>
        /// Initializes the ChangeTracker and GamelistMetaData objects.
        /// </summary>
        public static void InitializeChangeTracker()
        {
            _changeTracker = new ChangeTracker();
        }

        /// <summary>
        /// Gets the ChangeTracker instance.
        /// </summary>
        public static ChangeTracker? ChangeTracker
        {
            get => _changeTracker;
            set => _changeTracker = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the data has changed.
        /// </summary>
        public static bool IsDataChanged
        {
            get => _isDataChanged;
            set => _isDataChanged = value;
        }

        /// <summary>
        /// Gets or sets the current DataSet.
        /// </summary>
        public static DataSet DataSet
        {
            get => _dataSet;
            set => _dataSet = value;
        }

        /// <summary>
        /// Gets or sets the current XML filename.
        /// </summary>
        public static string XMLFilename
        {
            get => _xmlFilename;
            set => _xmlFilename = value;
        }

        /// <summary>
        /// Gets or sets the current program directory.
        /// </summary>
        public static string ProgramDirectory
        {
            get => _programDirectory;
            set => _programDirectory = value;
        }

        /// <summary>
        /// Gets or sets the current system.
        /// </summary>
        public static string CurrentSystem
        {
            get => _currentSystem;
            set => _currentSystem = value;
        }

        public static bool MetaDataEditMode
        {
            get => _metaDataEditMode;
            set => _metaDataEditMode = value;
        }

    }
}
