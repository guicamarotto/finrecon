using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using FinRecon.Tests.Fixtures;

namespace FinRecon.Tests.Integration;

public class ReconciliationApiTests : IClassFixture<WebAppFixture>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ReconciliationApiTests(WebAppFixture factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_Returns200()
    {
        var response = await _client.GetAsync("/api/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostReconciliation_WithoutAuth_Returns401()
    {
        using var content = BuildCsvUpload("test.csv", "client_id,product_type,value\nC001,equity,1000");
        var response = await _client.PostAsync("/api/reconciliations", content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetReconciliation_WithUnknownId_Returns404()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/reconciliations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetList_Returns200WithPagedResult()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/reconciliations?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);
        result.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
    }

    // ── Helper methods ────────────────────────────────────────────────────────

    private async Task<string> GetAuthTokenAsync()
    {
        // Register a test user and get a token
        var email = $"test_{Guid.NewGuid():N}@finrecon.test";
        var registerBody = new StringContent(
            JsonSerializer.Serialize(new { email, password = "TestPass123!" }),
            Encoding.UTF8, "application/json");

        var registerResponse = await _client.PostAsync("/api/auth/register", registerBody);
        registerResponse.EnsureSuccessStatusCode();

        var registerJson = await JsonSerializer.DeserializeAsync<JsonElement>(
            await registerResponse.Content.ReadAsStreamAsync(), JsonOpts);

        return registerJson.GetProperty("token").GetString()!;
    }

    private static MultipartFormDataContent BuildCsvUpload(string filename, string csvContent)
    {
        var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        multipart.Add(fileContent, "file", filename);
        multipart.Add(new StringContent("2025-01-15"), "referenceDate");
        return multipart;
    }
}
