using GamelistManager.classes.GamelistManager;

namespace GamelistManager.classes
{
    internal static class GetJSONResponse
    {
        public static async Task<string> GetJsonResponseAsync(string bearerToken, string url)
        {
            // Use the singleton instance
            var client = HttpClientSingleton.Instance;
            HttpClientSingleton.SetBearerToken(bearerToken);

            try
            {
                var response = await client.GetAsync(url);
                // Throws an exception if the status code is not 2xx
                response.EnsureSuccessStatusCode();

                // Read and return the response content
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
