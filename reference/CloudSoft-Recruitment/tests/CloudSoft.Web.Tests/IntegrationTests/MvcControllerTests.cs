using System.Net;
using System.Text.RegularExpressions;

namespace CloudSoft.Web.Tests.IntegrationTests;

public class MvcControllerTests : IDisposable
{
    private readonly CloudSoftWebApplicationFactory _factory;

    public MvcControllerTests()
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

    // ── HomeController ──────────────────────────────────────────────

    [Fact]
    public async Task PrivacyPage_ReturnsOk()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/Home/Privacy");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ErrorPage_ReturnsOk()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/Home/Error");

        // Error page should render successfully (returns 500 status but renders the error view)
        // The Error action itself returns a view, so it returns 200
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── AccountController ───────────────────────────────────────────

    [Fact]
    public async Task Login_EmptyCredentials_ReturnsLoginPage()
    {
        var client = CreateClient();

        var (token, _) = await client.GetAntiforgeryTokensAsync("/Account/Login");

        var formData = new Dictionary<string, string>
        {
            ["email"] = "",
            ["password"] = "",
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Account/Login",
            new FormUrlEncodedContent(formData));

        // Should stay on login page (200), not redirect
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Email and password are required", html);
    }

    [Fact]
    public async Task Logout_Authenticated_RedirectsToHome()
    {
        var client = CreateClient();
        await client.LoginAsync("admin@cloudsoft.com", "Admin123!");

        var (token, _) = await client.GetAntiforgeryTokensAsync("/Job");

        var formData = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Account/Logout",
            new FormUrlEncodedContent(formData));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task AccessDenied_ReturnsOk()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/Account/AccessDenied");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── JobController ───────────────────────────────────────────────

    [Fact]
    public async Task CreateJob_InvalidModel_ReturnsCreateView()
    {
        var client = CreateClient();
        await client.LoginAsync("admin@cloudsoft.com", "Admin123!");

        var (token, _) = await client.GetAntiforgeryTokensAsync("/Job/Create");

        // Post with missing required fields (no Title, no Description)
        var formData = new Dictionary<string, string>
        {
            ["Title"] = "",
            ["Description"] = "",
            ["Location"] = "",
            ["Deadline"] = "",
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Job/Create",
            new FormUrlEncodedContent(formData));

        // Should return the form (200), not redirect
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EditJob_NonExistentId_ReturnsNotFound()
    {
        var client = CreateClient();
        await client.LoginAsync("admin@cloudsoft.com", "Admin123!");

        var response = await client.GetAsync("/Job/Edit/nonexistent-id-12345");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteJob_NonExistentId_ReturnsNotFound()
    {
        var client = CreateClient();
        await client.LoginAsync("admin@cloudsoft.com", "Admin123!");

        var response = await client.GetAsync("/Job/Delete/nonexistent-id-12345");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task JobDetails_NonExistentId_ReturnsNotFound()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/Job/Details/nonexistent-id-12345");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EditJob_ValidUpdate_RedirectsToIndex()
    {
        var client = CreateClient();
        await client.LoginAsync("admin@cloudsoft.com", "Admin123!");

        // Create a job first
        var (createToken, _) = await client.GetAntiforgeryTokensAsync("/Job/Create");

        var jobData = new Dictionary<string, string>
        {
            ["Title"] = "Original Title",
            ["Description"] = "Original Description",
            ["Location"] = "Oslo",
            ["Deadline"] = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
            ["__RequestVerificationToken"] = createToken
        };

        await client.PostAsync("/Job/Create", new FormUrlEncodedContent(jobData));

        // Get the job ID from the listing
        var listResponse = await client.GetAsync("/Job");
        var listHtml = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("Original Title", listHtml);

        var jobIdMatches = Regex.Matches(listHtml, @"/Job/Details/([^""]+)");
        Assert.True(jobIdMatches.Count > 0, "No job links found");
        var jobId = jobIdMatches[^1].Groups[1].Value;

        // Edit the job
        var (editToken, _) = await client.GetAntiforgeryTokensAsync($"/Job/Edit/{jobId}");

        var editData = new Dictionary<string, string>
        {
            ["Title"] = "Updated Title",
            ["Description"] = "Updated Description",
            ["Location"] = "Bergen",
            ["Deadline"] = DateTime.UtcNow.AddDays(60).ToString("yyyy-MM-dd"),
            ["__RequestVerificationToken"] = editToken
        };

        var editResponse = await client.PostAsync($"/Job/Edit/{jobId}",
            new FormUrlEncodedContent(editData));

        Assert.Equal(HttpStatusCode.Redirect, editResponse.StatusCode);
        Assert.Equal("/Job", editResponse.Headers.Location?.ToString());

        // Verify updated title appears in listing
        var updatedListResponse = await client.GetAsync("/Job");
        var updatedHtml = await updatedListResponse.Content.ReadAsStringAsync();
        Assert.Contains("Updated Title", updatedHtml);
    }

    // ── AccountController — login failure and lockout ──────────────

    [Fact]
    public async Task Login_WrongPassword_ShowsError()
    {
        var client = CreateClient();

        var (token, _) = await client.GetAntiforgeryTokensAsync("/Account/Login");

        var formData = new Dictionary<string, string>
        {
            ["email"] = "admin@cloudsoft.com",
            ["password"] = "WrongPassword!",
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Account/Login",
            new FormUrlEncodedContent(formData));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid login attempt", html);
    }

    [Fact]
    public async Task Login_RepeatedFailures_LocksAccount()
    {
        var client = CreateClient();

        // Fail 5 times to trigger lockout
        for (int i = 0; i < 5; i++)
        {
            var (token, _) = await client.GetAntiforgeryTokensAsync("/Account/Login");

            var formData = new Dictionary<string, string>
            {
                ["email"] = "admin@cloudsoft.com",
                ["password"] = "WrongPassword!",
                ["__RequestVerificationToken"] = token
            };

            await client.PostAsync("/Account/Login", new FormUrlEncodedContent(formData));
        }

        // 6th attempt should show lockout message
        var (lockoutToken, _) = await client.GetAntiforgeryTokensAsync("/Account/Login");

        var lockoutData = new Dictionary<string, string>
        {
            ["email"] = "admin@cloudsoft.com",
            ["password"] = "WrongPassword!",
            ["__RequestVerificationToken"] = lockoutToken
        };

        var response = await client.PostAsync("/Account/Login",
            new FormUrlEncodedContent(lockoutData));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("locked out", html);
    }

    // ── ApplicationController ───────────────────────────────────────

    [Fact]
    public async Task Apply_NonExistentJob_ReturnsNotFound()
    {
        var client = CreateClient();
        await client.LoginAsync("candidate@test.com", "Candidate123!");

        var response = await client.GetAsync("/Application/Apply/nonexistent-id-12345");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MyApplications_Unauthenticated_RedirectsToLogin()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/Application/MyApplications");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task ForJob_AsCandidate_RedirectsToAccessDenied()
    {
        var client = CreateClient();

        // Admin creates a job first
        await client.LoginAsync("admin@cloudsoft.com", "Admin123!");
        var (createToken, _) = await client.GetAntiforgeryTokensAsync("/Job/Create");

        var jobData = new Dictionary<string, string>
        {
            ["Title"] = "Test Job For Access",
            ["Description"] = "Testing access control",
            ["Location"] = "Oslo",
            ["Deadline"] = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
            ["__RequestVerificationToken"] = createToken
        };
        await client.PostAsync("/Job/Create", new FormUrlEncodedContent(jobData));

        // Get job ID
        var listHtml = await (await client.GetAsync("/Job")).Content.ReadAsStringAsync();
        var jobIdMatches = Regex.Matches(listHtml, @"/Job/Details/([^""]+)");
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

        // Candidate tries to access ForJob (admin-only)
        var response = await client.GetAsync($"/Application/ForJob/{jobId}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task DownloadCv_AsCandidate_RedirectsToAccessDenied()
    {
        var client = CreateClient();
        await client.LoginAsync("candidate@test.com", "Candidate123!");

        var response = await client.GetAsync("/Application/DownloadCv/test.pdf");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location?.ToString());
    }
}
