using System.Collections.Generic;
using System.Data;

namespace GamelistManager
{
    public static class SharedData
    {
        private static readonly object dataLock = new object();
        private static DataSet dataSet;
        private static string xmlFilename;
        private static List<ScraperData> scrapedList = new List<ScraperData>();
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
        public static List<ScraperData> ScrapedList
        {
            get { return scrapedList; }
            set { scrapedList = value; }
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
        public static object DataLock
        {
            get { return dataLock; }
        }
    }
    public class ScraperData
    {
        public string Item { get; set; }
        public string ScrapeTime { get; set; }
        public string Source { get; set; }
    }
}
