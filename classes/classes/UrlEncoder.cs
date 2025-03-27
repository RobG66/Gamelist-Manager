using System.Net;

namespace GamelistManager.classes
{
    public static class UrlEncoder
    {
        public static string UrlEncodeFileName(string fileName)
        {
            return WebUtility.UrlEncode(fileName);
        }
    }
}
