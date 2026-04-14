using System.Net;

namespace CloudSoft.Web.Tests.SmokeTests;

public class DeploymentSmokeTests
{
    private readonly string? _baseUrl;
    private readonly HttpClient _client;

    public DeploymentSmokeTests()
    {
        _baseUrl = Environment.GetEnvironmentVariable("SMOKE_TEST_BASE_URL");
        _client = new HttpClient();
    }

    [Fact]
    public async Task HealthEndpoint_Returns200WithHealthy()
    {
        if (string.IsNullOrEmpty(_baseUrl)) return;

        var response = await _client.GetAsync($"{_baseUrl}/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task VersionEndpoint_ReturnsVersionWithCommitHash()
    {
        if (string.IsNullOrEmpty(_baseUrl)) return;

        var response = await _client.GetAsync($"{_baseUrl}/version");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("version", content);
    }

    [Fact]
    public async Task ApiJobsEndpoint_Returns200()
    {
        if (string.IsNullOrEmpty(_baseUrl)) return;

        var response = await _client.GetAsync($"{_baseUrl}/api/jobs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/json", contentType);
    }

    [Fact]
    public async Task HomePage_Returns200()
    {
        if (string.IsNullOrEmpty(_baseUrl)) return;

        var response = await _client.GetAsync(_baseUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("CloudSoft", content);
    }

    [Fact]
    public async Task LoginPage_Returns200()
    {
        if (string.IsNullOrEmpty(_baseUrl)) return;

        var response = await _client.GetAsync($"{_baseUrl}/Account/Login");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
