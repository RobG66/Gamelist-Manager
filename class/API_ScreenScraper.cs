using System.Threading.Tasks;
using System.Xml;

namespace GamelistManager
{
    internal static class API_ScreenScraper
    {
        private static readonly string apiURL = "https://api.screenscraper.fr/api2";
        private static readonly string devId = "";
        private static readonly string devPassword = "";
        private static readonly string software = "GamelistManager";

        public static async Task<int> GetMaxScrap(string username, string password)
        {
            XmlNode xmlData = await AuthenticateAsync(username, password);
            if (xmlData != null)
            {
                XmlNode maxThreadsNode = xmlData.SelectSingleNode("//ssuser/maxthreads");
                if (maxThreadsNode != null && int.TryParse(maxThreadsNode.InnerText, out int max))
                {
                    return max;
                }
            }
            return 1;
        }

        public static async Task<XmlNode> AuthenticateAsync(string username, string password)
        {
            string url = $"{apiURL}/ssuserInfos.php?devid={devId}&devpassword={devPassword}&softname={software}&output=xml&ssid={username}&sspassword={password}";
            XMLResponder responder = new XMLResponder();
            XmlNode xmlResponse = await responder.GetXMLResponseAsync(url);
            return xmlResponse;
        }
    }
}