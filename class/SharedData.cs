using System.Collections.Generic;
using System.Data;
using System.Text;

namespace GamelistManager
{
    public static class SharedData
    {
        public static readonly object lockObject = new object();
        private static DataSet dataSet;
        private static string xmlFilename;
        private static bool isDataChanged;
        private static Dictionary<string, string> mediaTypePaths;

        private static readonly string[] mediaTypes = new string[]
        {
              "image",
              "marquee",
              "thumbnail",
              "fanart",
              "titleshot",
              "manual",
              "magazine",
              "map",
              "bezel",
              "boxback",
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
            mediaTypePaths = new Dictionary<string, string>();
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

        public static Dictionary<string, string> MediaTypePaths
        {
            get
            {
                return mediaTypePaths;
            }

            set
            {
                lock (lockObject)
                {
                    mediaTypePaths = value;
                }
            }
        }

        public static void SetMediaTypePath(string mediaType, string path)
        {
            if (!mediaTypePaths.ContainsKey(mediaType))
            {
                mediaTypePaths.Add(mediaType, path);
            }
            else
            {
                mediaTypePaths[mediaType] = path;
            }
        }

        public static string GetMediaTypePath(string mediaType)
        {
            if (mediaTypePaths.TryGetValue(mediaType, out string path))
            {
                return path;
            }
            return null;
        }

        public static void ConfigureMediaPaths() {

            string regValue = RegistryManager.ReadRegistryValue(null, "MediaPaths");

            if (string.IsNullOrEmpty(regValue))
            {
                // First time setup
                foreach (var mediaType in GamelistManager.SharedData.MediaTypes)
                {
                    SharedData.SetMediaTypePath(mediaType, "./images");
                }
                SharedData.SetMediaTypePath("video", "./videos");
                SharedData.SetMediaTypePath("manual", "./manuals");

                StringBuilder sb = new StringBuilder();

                foreach (var mediaType in GamelistManager.SharedData.MediaTypes)
                {
                    string path = GamelistManager.SharedData.GetMediaTypePath(mediaType);
                    if (path != null)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append(",");
                        }
                        sb.Append($"{mediaType}={path}");
                    }
                }

                RegistryManager.WriteRegistryValue(null,"MediaPaths",sb.ToString());
            }

            // Load media paths
            regValue = RegistryManager.ReadRegistryValue(null, "MediaPaths");

            var pairs = regValue.Split(',');

            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    string mediaType = keyValue[0].Trim();
                    string path = keyValue[1].Trim();
                    SetMediaTypePath(mediaType, path);
                }
            }
        }
    }
}
