using System.Data;

namespace GamelistManager
{
    public static class SharedData
    {
        private static readonly object dataLock = new object();
        private static DataSet dataSet;
        private static string xmlFilename;
        private static bool isDataChanged;
        private static string[] mediaTypes = new string[]
        {     "image",
              "marquee",
              "thumbnail",
              "fanart",
              "titleshot",
              "manual",
              "magazine",
              "map",
              "bezel",
              "boxback",
              "fanart",
              "video"
         };

        public static string[] MediaTypes
        {
            get { return mediaTypes; }
        }

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
                lock (dataLock)
                {
                    return dataSet;
                }
            }

            set
            {
                lock (dataLock)
                {
                    dataSet = value;
                }
            }
        }
        public static string XMLFilename
        {
            get { return xmlFilename; }
            set { xmlFilename = value; }
        }
    }
}
