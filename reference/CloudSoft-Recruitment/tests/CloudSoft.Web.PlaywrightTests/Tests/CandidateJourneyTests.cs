using Microsoft.Playwright;

namespace CloudSoft.Web.PlaywrightTests.Tests;

[Collection("Playwright")]
public class CandidateJourneyTests
{
    private readonly PlaywrightFixture _fixture;

    public CandidateJourneyTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CandidateJourney_ApplyToJob()
    {
        // Skip if app is not running
        if (!_fixture.IsAppRunning) return;

        await using var context = await _fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        // Step 1: Login as admin and create a job
        await page.LoginAsync(_fixture.BaseUrl, "admin@cloudsoft.com", "Admin123!");

        await page.GotoAsync($"{_fixture.BaseUrl}/Job/Create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var jobTitle = $"Apply Test Job {uniqueId}";
        var deadline = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd");

        await page.FillAsync("#Title", jobTitle);
        await page.FillAsync("#Description", "Job for candidate application test");
        await page.FillAsync("#Location", "Oslo");
        await page.FillAsync("#Deadline", deadline);

        await page.ClickAsync("button:has-text('Post Job')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify job was created
        var jobCard = page.Locator($"text={jobTitle}");
        await Assertions.Expect(jobCard.First).ToBeVisibleAsync();

        // Step 2: Logout
        await page.LogoutAsync();

        // Step 3: Login as candidate
        await page.LoginAsync(_fixture.BaseUrl, "candidate@test.com", "Candidate123!");

        // Step 4: Navigate to job listing and find the job
        await page.GotoAsync($"{_fixture.BaseUrl}/Job");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click "View Details" for the specific job
        var card = page.Locator(".card").Filter(new() { HasText = jobTitle }).First;
        await card.Locator("a:has-text('View Details')").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Step 5: Click Apply
        await page.ClickAsync("a:has-text('Apply'), button:has-text('Apply')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Step 6: Fill in the application form
        await page.FillAsync("#CoverLetter", "I am passionate about this role");

        // Step 7: Submit the application
        await page.ClickAsync("button:has-text('Submit Application')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Step 8: Assert we're on MyApplications and the job title is visible
        Assert.Contains("MyApplications", page.Url);
        var appliedJob = page.Locator($"text={jobTitle}");
        await Assertions.Expect(appliedJob.First).ToBeVisibleAsync();
    }
}
