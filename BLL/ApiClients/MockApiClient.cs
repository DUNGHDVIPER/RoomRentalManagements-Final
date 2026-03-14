using System.Net.Http.Json;

namespace BLL.ApiClients;

public class MockApiClient
{
    private readonly HttpClient _http;

    public MockApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(endpoint, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }
}
