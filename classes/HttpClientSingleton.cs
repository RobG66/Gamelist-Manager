using System.Net.Http;

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
        }
    }
}
