using Microsoft.Playwright;

namespace CloudSoft.Auth.Web.PlaywrightTests;

/// <summary>
/// Cross-exercise test helpers. Each method represents a user flow exercised by multiple exercises.
/// </summary>
public static class TestHelpers
{
    public static async Task LoginAsync(IPage page, string username, string password)
    {
        await page.GotoAsync("/Account/Login");
        await page.FillAsync("[data-testid='login-username']", username);
        await page.FillAsync("[data-testid='login-password']", password);
        await page.ClickAsync("[data-testid='login-submit']");
        await page.WaitForURLAsync(url => !url.Contains("/Account/Login"));
    }

    public static Task LoginAsAdminAsync(IPage page) =>
        LoginAsync(page, "admin", "admin");

    public static Task LoginAsCandidateAsync(IPage page) =>
        LoginAsync(page, "candidate", "candidate");

    public static async Task LogoutAsync(IPage page)
    {
        await page.GotoAsync("/WhoAmI");
        var logoutForm = page.Locator("[data-testid='logout-form']");
        if (await logoutForm.CountAsync() > 0)
        {
            await page.GetByTestId("logout-submit").ClickAsync();
            await page.WaitForURLAsync(url => !url.Contains("/WhoAmI"));
        }
    }
}
