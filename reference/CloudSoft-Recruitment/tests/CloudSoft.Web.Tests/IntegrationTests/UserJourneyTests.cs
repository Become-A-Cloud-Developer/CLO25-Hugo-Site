using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace CloudSoft.Web.Tests.IntegrationTests;

public class UserJourneyTests : IDisposable
{
    private readonly CloudSoftWebApplicationFactory _factory;

    public UserJourneyTests()
    {
        _factory = new CloudSoftWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task HomePage_ReturnsSuccessAndContainsCloudSoft()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("CloudSoft", html);
    }

    [Fact]
    public async Task JobIndex_IsPublic_ReturnsSuccess()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/Job");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task JobCreate_Unauthenticated_RedirectsToLogin()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/Job/Create");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task AdminLogin_ValidCredentials_Succeeds()
    {
        var client = CreateClient();

        await client.LoginAsync("admin@cloudsoft.com", "Admin123!");
        var response = await client.GetAsync("/Job/Create");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminJourney_CreateJob_AppearsInListing()
    {
        var client = CreateClient();
        await client.LoginAsync("admin@cloudsoft.com", "Admin123!");

        // Get Create form and antiforgery token
        var (token, _) = await client.GetAntiforgeryTokensAsync("/Job/Create");

        // Post new job
        var formData = new Dictionary<string, string>
        {
            ["Title"] = "Integration Test Developer",
            ["Description"] = "Write integration tests all day",
            ["Location"] = "Oslo",
            ["Deadline"] = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
            ["__RequestVerificationToken"] = token
        };

        var createResponse = await client.PostAsync("/Job/Create",
            new FormUrlEncodedContent(formData));

        // Should redirect to job listing
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        Assert.Equal("/Job", createResponse.Headers.Location?.ToString());

        // Follow redirect and verify job appears
        var listResponse = await client.GetAsync("/Job");
        var html = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("Integration Test Developer", html);
    }

    [Fact]
    public async Task CandidateLogin_ValidCredentials_Succeeds()
    {
        var client = CreateClient();

        await client.LoginAsync("candidate@test.com", "Candidate123!");
        var response = await client.GetAsync("/Application/MyApplications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CandidateJourney_ApplyToJob_AppearsInMyApplications()
    {
        var client = CreateClient();

        // Admin creates a job first
        await client.LoginAsync("admin@cloudsoft.com", "Admin123!");
        var (createToken, _) = await client.GetAntiforgeryTokensAsync("/Job/Create");

        var jobData = new Dictionary<string, string>
        {
            ["Title"] = "Cloud Engineer",
            ["Description"] = "Build cloud infrastructure",
            ["Location"] = "Bergen",
            ["Deadline"] = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
            ["__RequestVerificationToken"] = createToken
        };
        await client.PostAsync("/Job/Create", new FormUrlEncodedContent(jobData));

        // Get the job ID — find the Details link near "Cloud Engineer"
        var listResponse = await client.GetAsync("/Job");
        var listHtml = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("Cloud Engineer", listHtml);

        var jobIdMatches = System.Text.RegularExpressions.Regex.Matches(
            listHtml, @"/Job/Details/([^""]+)");
        Assert.True(jobIdMatches.Count > 0, "No job links found");
        var jobId = jobIdMatches[^1].Groups[1].Value;

        // Logout admin, login as candidate
        var (logoutToken, _) = await client.GetAntiforgeryTokensAsync("/Job");
        await client.PostAsync("/Account/Logout",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = logoutToken
            }));
        await client.LoginAsync("candidate@test.com", "Candidate123!");

        // Apply to the job
        var (applyToken, _) = await client.GetAntiforgeryTokensAsync($"/Application/Apply/{jobId}");

        var applyData = new Dictionary<string, string>
        {
            ["JobId"] = jobId,
            ["CoverLetter"] = "I am very passionate about cloud engineering.",
            ["__RequestVerificationToken"] = applyToken
        };

        var applyResponse = await client.PostAsync($"/Application/Apply/{jobId}",
            new FormUrlEncodedContent(applyData));

        Assert.Equal(HttpStatusCode.Redirect, applyResponse.StatusCode);
        var applyRedirectLocation = applyResponse.Headers.Location?.ToString() ?? "";
        Assert.Contains("MyApplications", applyRedirectLocation);

        // Verify application appears in My Applications
        var myAppsResponse = await client.GetAsync("/Application/MyApplications");
        Assert.Equal(HttpStatusCode.OK, myAppsResponse.StatusCode);
        var myAppsHtml = await myAppsResponse.Content.ReadAsStringAsync();
        Assert.Contains("Cloud Engineer", myAppsHtml);
    }

    [Fact]
    public async Task CandidateCannotAccessAdminRoutes()
    {
        var client = CreateClient();
        await client.LoginAsync("candidate@test.com", "Candidate123!");

        var response = await client.GetAsync("/Job/Create");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task AdminCanViewApplicationsForJob()
    {
        var client = CreateClient();

        // Admin creates a job
        await client.LoginAsync("admin@cloudsoft.com", "Admin123!");
        var (createToken, _) = await client.GetAntiforgeryTokensAsync("/Job/Create");

        var jobData = new Dictionary<string, string>
        {
            ["Title"] = "DevOps Specialist",
            ["Description"] = "CI/CD pipelines and monitoring",
            ["Location"] = "Trondheim",
            ["Deadline"] = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
            ["__RequestVerificationToken"] = createToken
        };
        await client.PostAsync("/Job/Create", new FormUrlEncodedContent(jobData));

        // Get job ID — find the link that's near "DevOps Specialist"
        var listHtml = await (await client.GetAsync("/Job")).Content.ReadAsStringAsync();
        Assert.Contains("DevOps Specialist", listHtml);

        // Find all job detail links and pick the last one (most recently added)
        var jobIdMatches = System.Text.RegularExpressions.Regex.Matches(
            listHtml, @"/Job/Details/([^""]+)");
        Assert.True(jobIdMatches.Count > 0, "No job links found");
        var jobId = jobIdMatches[^1].Groups[1].Value;

        // View applications page (should be empty but accessible)
        var response = await client.GetAsync($"/Application/ForJob/{jobId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Applications for", html);
    }
}
