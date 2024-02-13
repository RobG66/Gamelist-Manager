using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GamelistManager
{
    internal class XMLResponder
    {
        public async Task<XmlDocument> GetXMLResponseAsync(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] responseBytes = await client.GetByteArrayAsync(url);
                    string responseString = Encoding.UTF8.GetString(responseBytes);

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(responseString);

                    return xmlDoc;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
