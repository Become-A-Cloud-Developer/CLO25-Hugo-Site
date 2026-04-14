using Microsoft.Playwright;

namespace CloudSoft.Web.PlaywrightTests.Tests;

[Collection("Playwright")]
public class RoleEnforcementTests
{
    private readonly PlaywrightFixture _fixture;

    public RoleEnforcementTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CandidateCannotAccessAdmin()
    {
        // Skip if app is not running
        if (!_fixture.IsAppRunning) return;

        await using var context = await _fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        // Login as candidate
        await page.LoginAsync(_fixture.BaseUrl, "candidate@test.com", "Candidate123!");

        // Try to access admin-only page
        await page.GotoAsync($"{_fixture.BaseUrl}/Job/Create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Should be redirected to AccessDenied
        Assert.Contains("/Account/AccessDenied", page.Url);
    }
}
