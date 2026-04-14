using Microsoft.Playwright;

namespace CloudSoft.Web.PlaywrightTests.Tests;

[Collection("Playwright")]
public class AdminJourneyTests
{
    private readonly PlaywrightFixture _fixture;

    public AdminJourneyTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AdminJourney_CreateEditDeleteJob()
    {
        // Skip if app is not running
        if (!_fixture.IsAppRunning) return;

        await using var context = await _fixture.CreateContextAsync();
        var page = await context.NewPageAsync();

        // Login as admin
        await page.LoginAsync(_fixture.BaseUrl, "admin@cloudsoft.com", "Admin123!");

        // Navigate to create job
        await page.GotoAsync($"{_fixture.BaseUrl}/Job/Create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Fill the job form
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var jobTitle = $"Playwright Test Job {uniqueId}";
        var deadline = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd");

        await page.FillAsync("#Title", jobTitle);
        await page.FillAsync("#Description", "Automated test job");
        await page.FillAsync("#Location", "Oslo");
        await page.FillAsync("#Deadline", deadline);

        // Submit the form
        await page.ClickAsync("button:has-text('Post Job')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert redirected to /Job and job title is visible
        Assert.Contains("/Job", page.Url);
        var jobCard = page.Locator($"text={jobTitle}");
        await Assertions.Expect(jobCard.First).ToBeVisibleAsync();

        // Click "View Details" for this specific job
        var card = page.Locator(".card").Filter(new() { HasText = jobTitle }).First;
        await card.Locator("a:has-text('View Details')").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click Edit
        await page.ClickAsync("a:has-text('Edit'), button:has-text('Edit')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Change the title
        var editedTitle = $"Edited {jobTitle}";
        await page.FillAsync("#Title", editedTitle);

        // Submit the edit
        await page.ClickAsync("button:has-text('Update Job')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert redirected to /Job listing and edited title is visible
        Assert.Contains("/Job", page.Url);
        var editedText = page.Locator($"text={editedTitle}");
        await Assertions.Expect(editedText.First).ToBeVisibleAsync();

        // Navigate to details page for the edited job
        var editedCard = page.Locator(".card").Filter(new() { HasText = editedTitle }).First;
        await editedCard.Locator("a:has-text('View Details')").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click Delete
        await page.ClickAsync("a:has-text('Delete')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Confirm deletion
        await page.ClickAsync("button:has-text('Confirm Delete')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert redirected to /Job and edited title is gone
        Assert.Contains("/Job", page.Url);
        var deletedJob = page.Locator($"text={editedTitle}");
        await Assertions.Expect(deletedJob).ToHaveCountAsync(0);
    }
}
