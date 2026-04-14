using Microsoft.Playwright;

namespace CloudSoft.Web.PlaywrightTests.Tests;

[Collection("Playwright")]
public class HealthAndVersionTests
{
    private readonly PlaywrightFixture _fixture;

    public HealthAndVersionTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyStatus()
    {
        if (!_fixture.IsAppRunning) return;

        await using var context = await _fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        var response = await page.GotoAsync($"{_fixture.BaseUrl}/health");
        Assert.NotNull(response);
        Assert.Equal(200, response.Status);

        var content = await page.ContentAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task VersionEndpoint_ReturnsVersionInfo()
    {
        if (!_fixture.IsAppRunning) return;

        await using var context = await _fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        var response = await page.GotoAsync($"{_fixture.BaseUrl}/version");
        Assert.NotNull(response);
        Assert.Equal(200, response.Status);

        var content = await page.ContentAsync();
        Assert.Contains("version", content);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsJsonContentType()
    {
        if (!_fixture.IsAppRunning) return;

        await using var context = await _fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        var response = await page.GotoAsync($"{_fixture.BaseUrl}/health");
        Assert.NotNull(response);
        var contentType = response.Headers["content-type"];
        Assert.Contains("application/json", contentType);
    }
}
