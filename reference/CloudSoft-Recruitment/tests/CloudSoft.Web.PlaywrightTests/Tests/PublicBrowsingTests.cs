using Microsoft.Playwright;

namespace CloudSoft.Web.PlaywrightTests.Tests;

[Collection("Playwright")]
public class PublicBrowsingTests
{
    private readonly PlaywrightFixture _fixture;

    public PublicBrowsingTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HomePage_ShowsCloudSoftTitle()
    {
        // Skip if app is not running
        if (!_fixture.IsAppRunning) return;

        await using var context = await _fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(_fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var title = await page.TitleAsync();
        Assert.Contains("CloudSoft", title);

        var heading = page.Locator("h1").Filter(new() { HasText = "CloudSoft Recruitment" });
        await Assertions.Expect(heading.First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task JobListing_IsAccessible()
    {
        // Skip if app is not running
        if (!_fixture.IsAppRunning) return;

        await using var context = await _fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/Job");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var heading = page.GetByRole(AriaRole.Heading, new() { Name = "Job Listings" });
        await Assertions.Expect(heading).ToBeVisibleAsync();
    }
}
