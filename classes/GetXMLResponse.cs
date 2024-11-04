using GamelistManager.classes.GamelistManager;
using System.Text;

namespace GamelistManager.classes
{
    internal class GetXMLResponse
    {
        // Method to get XML response asynchronously
        public async Task<string> GetXMLResponseAsync(string bearerToken,string url)
        {
            var client = HttpClientSingleton.Instance; // Use the singleton instance
            HttpClientSingleton.SetBearerToken(bearerToken);

            try
            {
                byte[] responseBytes = await client.GetByteArrayAsync(url);
                if (responseBytes == null)
                {
                    return string.Empty;
                }
                string responseString = Encoding.UTF8.GetString(responseBytes);
                return responseString;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
