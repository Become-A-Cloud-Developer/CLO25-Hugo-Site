using Microsoft.Playwright;

namespace CloudSoft.Web.PlaywrightTests;

public static class LoginHelper
{
    public static async Task LoginAsync(this IPage page, string baseUrl, string email, string password)
    {
        await page.GotoAsync($"{baseUrl}/Account/Login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.FillAsync("#email", email);
        await page.FillAsync("#password", password);
        await page.ClickAsync("button[type='submit']:has-text('Sign In')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public static async Task LogoutAsync(this IPage page)
    {
        await page.ClickAsync("button:has-text('Logout')");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
