using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CloudSoft.Web.Tests.IntegrationTests;

public class JobsApiTests : IDisposable
{
    private readonly CloudSoftWebApplicationFactory _factory;

    public JobsApiTests()
    {
        _factory = new CloudSoftWebApplicationFactory();
    }

    public void Dispose() => _factory.Dispose();

    private HttpClient CreateClient() =>
        _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    private async Task<HttpClient> CreateAuthenticatedAdminClient()
    {
        var client = CreateClient();
        var token = await client.GetJwtTokenAsync("admin@cloudsoft.com", "Admin123!");
        client.SetBearerToken(token);
        return client;
    }

    private async Task<HttpClient> CreateAuthenticatedCandidateClient()
    {
        var client = CreateClient();
        var token = await client.GetJwtTokenAsync("candidate@test.com", "Candidate123!");
        client.SetBearerToken(token);
        return client;
    }

    private static object CreateValidJobRequest() => new
    {
        title = "Test Developer",
        description = "Write tests for the application",
        location = "Oslo",
        deadline = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd")
    };

    [Fact]
    public async Task GetJobs_ReturnsOkWithEmptyList()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/jobs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var jobs = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.Equal(JsonValueKind.Array, jobs.ValueKind);
    }

    [Fact]
    public async Task GetJobs_AfterCreating_ReturnsJobInList()
    {
        var client = await CreateAuthenticatedAdminClient();

        await client.PostAsJsonAsync("/api/jobs", CreateValidJobRequest());

        var response = await client.GetAsync("/api/jobs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var jobs = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.True(jobs.GetArrayLength() > 0);
        Assert.Contains("Test Developer", jobs[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetJobById_ExistingJob_ReturnsOkWithJob()
    {
        var client = await CreateAuthenticatedAdminClient();

        var createResponse = await client.PostAsJsonAsync("/api/jobs", CreateValidJobRequest());
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<JsonElement>(createContent);
        var jobId = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/jobs/{jobId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var job = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.Equal("Test Developer", job.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetJobById_NonExisting_Returns404()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/jobs/nonexistent-id");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostJob_WithValidToken_Returns201()
    {
        var client = await CreateAuthenticatedAdminClient();

        var response = await client.PostAsJsonAsync("/api/jobs", CreateValidJobRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var job = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.Equal("Test Developer", job.GetProperty("title").GetString());
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task PostJob_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/jobs", CreateValidJobRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostJob_WithCandidateToken_Returns403()
    {
        var client = await CreateAuthenticatedCandidateClient();

        var response = await client.PostAsJsonAsync("/api/jobs", CreateValidJobRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostJob_InvalidData_Returns400()
    {
        var client = await CreateAuthenticatedAdminClient();

        var response = await client.PostAsJsonAsync("/api/jobs", new
        {
            title = "",
            description = "",
            location = "",
            deadline = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetApplicationsForJob_WithAdminToken_ReturnsOk()
    {
        var client = await CreateAuthenticatedAdminClient();

        var createResponse = await client.PostAsJsonAsync("/api/jobs", CreateValidJobRequest());
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<JsonElement>(createContent);
        var jobId = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/jobs/{jobId}/applications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var apps = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.Equal(JsonValueKind.Array, apps.ValueKind);
    }

    [Fact]
    public async Task GetApplicationsForJob_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/jobs/some-id/applications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
