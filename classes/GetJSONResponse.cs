using GamelistManager.classes.GamelistManager;
using System.Net.Http;

namespace GamelistManager.classes
{
    internal class GetJsonResponse
    {
        public async Task<string> GetJsonResponseAsync(string url)
        {
            // Use the singleton instance
            var client = HttpClientSingleton.Instance;

            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync(url);
                // Throws an exception if the status code is not 2xx
                response.EnsureSuccessStatusCode();
            }
            catch
            {
                return string.Empty;
            }

            // Try to read the response content
            string jsonResponse = await response.Content.ReadAsStringAsync();
            return jsonResponse;
        }
    }
}
