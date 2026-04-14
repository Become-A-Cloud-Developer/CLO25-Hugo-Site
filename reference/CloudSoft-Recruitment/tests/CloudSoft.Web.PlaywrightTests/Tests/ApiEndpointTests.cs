using Microsoft.Playwright;

namespace CloudSoft.Web.PlaywrightTests.Tests;

[Collection("Playwright")]
public class ApiEndpointTests
{
    private readonly PlaywrightFixture _fixture;

    public ApiEndpointTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ApiJobs_ReturnsJsonArray()
    {
        if (!_fixture.IsAppRunning) return;

        await using var context = await _fixture.CreateContextAsync();
        var request = context.APIRequest;

        var response = await request.GetAsync($"{_fixture.BaseUrl}/api/jobs");
        Assert.True(response.Ok);

        var body = await response.TextAsync();
        Assert.StartsWith("[", body.Trim());
    }

    [Fact]
    public async Task ApiJobs_AfterAdminCreatesJob_ReturnsJobInApi()
    {
        if (!_fixture.IsAppRunning) return;

        await using var context = await _fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        // Admin creates a job via MVC
        await page.LoginAsync(_fixture.BaseUrl, "admin@cloudsoft.com", "Admin123!");
        await page.GotoAsync($"{_fixture.BaseUrl}/Job/Create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var uniqueTitle = $"API Test Job {Guid.NewGuid().ToString()[..8]}";
        await page.FillAsync("#Title", uniqueTitle);
        await page.FillAsync("#Description", "Test job for API verification");
        await page.FillAsync("#Location", "Oslo");
        await page.FillAsync("#Deadline", DateTime.Now.AddDays(30).ToString("yyyy-MM-dd"));
        await page.ClickAsync("button:has-text('Post Job')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify via API
        var request = context.APIRequest;
        var response = await request.GetAsync($"{_fixture.BaseUrl}/api/jobs");
        var body = await response.TextAsync();
        Assert.Contains(uniqueTitle, body);
    }

    [Fact]
    public async Task ApiJobsPost_WithoutAuth_Returns401()
    {
        if (!_fixture.IsAppRunning) return;

        await using var context = await _fixture.CreateContextAsync();
        var request = context.APIRequest;

        var response = await request.PostAsync($"{_fixture.BaseUrl}/api/jobs", new()
        {
            DataObject = new
            {
                title = "Unauthorized Job",
                description = "Should fail",
                location = "Nowhere",
                deadline = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd")
            }
        });

        Assert.Equal(401, response.Status);
    }
}
