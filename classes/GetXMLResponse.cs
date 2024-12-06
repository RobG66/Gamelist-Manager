using GamelistManager.classes.GamelistManager;
using System.Text;

namespace GamelistManager.classes
{
    internal static class GetXMLResponse
    {
        // Static method to get XML response asynchronously
        public static async Task<string> GetXMLResponseAsync(string bearerToken, string url)
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
                return Encoding.UTF8.GetString(responseBytes);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
