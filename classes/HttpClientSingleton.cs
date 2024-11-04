using System.Net.Http;
using System.Net.Http.Headers;

namespace GamelistManager.classes
{
    namespace GamelistManager
    {
        // Singleton HttpClient implementation
        internal static class HttpClientSingleton
        {
            private static readonly HttpClient _instance;

            // Static constructor to initialize the HttpClient with a timeout
            static HttpClientSingleton()
            {
                _instance = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(10) // Set the timeout to 10 seconds
                };
            }

            public static HttpClient Instance => _instance;

            // Method to set the Bearer token
            public static void SetBearerToken(string token)
            {
                if (string.IsNullOrEmpty(token))
                {
                    // If token is null or empty, remove the Authorization header
                    _instance.DefaultRequestHeaders.Authorization = null;
                }
                else
                {
                    // Set the Authorization header with Bearer token
                    _instance.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
        }
    }
}
