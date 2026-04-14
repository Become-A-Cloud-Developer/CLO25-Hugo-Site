using Microsoft.Playwright;

namespace CloudSoft.Web.PlaywrightTests.Tests;

[Collection("Playwright")]
public class AuthRedirectTests
{
    private readonly PlaywrightFixture _fixture;

    public AuthRedirectTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task JobCreate_RedirectsToLogin()
    {
        // Skip if app is not running
        if (!_fixture.IsAppRunning) return;

        await using var context = await _fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/Job/Create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.Contains("/Account/Login", page.Url);
    }
}
