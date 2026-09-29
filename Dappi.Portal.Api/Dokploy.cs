using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dappi.Portal.Api;

// Dokploy's API: GET or POST /api/<router.procedure> with an x-api-key header.
public class Dokploy
{
    private readonly HttpClient _http;

    public Dokploy(IConfiguration config)
    {
        Url = config["Dokploy:Url"]!.TrimEnd('/');
        _http = new HttpClient { BaseAddress = new Uri(Url + "/api/") };
        _http.DefaultRequestHeaders.Add("x-api-key", config["Dokploy:ApiKey"]);
    }

    public string Url { get; }

    public async Task<JsonNode?> Get(string procedure, string query = "")
    {
        var response = await _http.GetAsync(procedure + query);
        return await Read(procedure, response);
    }

    public async Task<JsonNode?> Post(string procedure, object body)
    {
        // Dokploy ignores the body when the content type has "; charset=utf-8", which StringContent adds.
        var content = new StringContent(JsonSerializer.Serialize(body));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await _http.PostAsync(procedure, content);
        return await Read(procedure, response);
    }

    private static async Task<JsonNode?> Read(string procedure, HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Dokploy {procedure} failed ({(int)response.StatusCode}): {text}");
        }

        return text == "" ? null : JsonNode.Parse(text);
    }
}
