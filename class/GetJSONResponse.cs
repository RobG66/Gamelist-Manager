using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GamelistManager
{
    internal class GetJsonResponse
    {
        public async Task<List<string>> GetJsonResponseAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"HTTP request failed: {ex.Message}");
                    return null;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();

                try
                {
                    JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    JsonElement root = doc.RootElement;
                    JsonElement dataElement = root.GetProperty("data");

                    List<string> dataList = new List<string>();
                    foreach (JsonElement element in dataElement.EnumerateArray())
                    {
                        dataList.Add(element.GetString());
                    }
                    return dataList;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON parsing failed: {ex.Message}");
                    return null;
                }
            }
        }
    }
}
