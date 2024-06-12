using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GamelistManager
{
    internal static class API_EmuMovies
    {
        private static readonly string apiURL = "http://api3.emumovies.com/api";
        private static readonly string apiKey = "";

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            return client;
        }

        public static async Task<string> AuthenticateAsync(string username, string password)
        {
            var credentials = new
            {
                username,
                password
            };

            string url = $"{apiURL}/User/authenticate";

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var jsonContent = JsonSerializer.Serialize(credentials, options);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            using (var client = CreateHttpClient())
            {
                HttpResponseMessage response = await client.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(responseBody))
                    {
                        JsonElement root = doc.RootElement;
                        JsonElement data = root.GetProperty("data");
                        JsonElement acessTokenElement = data.GetProperty("acessToken");
                        return acessTokenElement.GetString();
                    }
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
