using System.Net;

namespace GamelistManager.classes.helpers
{
    public static class UrlHelper
    {
        public static string UrlEncodeFileName(string fileName)
        {
            return WebUtility.UrlEncode(fileName);
        }
    }
}
