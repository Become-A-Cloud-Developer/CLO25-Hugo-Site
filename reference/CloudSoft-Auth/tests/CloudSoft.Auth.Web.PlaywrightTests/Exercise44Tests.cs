using Microsoft.Playwright;

namespace CloudSoft.Auth.Web.PlaywrightTests;

/// <summary>
/// Exercise 4.4 adds Google OAuth conditionally. With no ClientId configured (the
/// default), the Google button is hidden and all previous tests still pass. We do
/// NOT verify the live Google consent round-trip — that requires a real Google
/// Cloud project and passing the consent screen, out of scope for automated tests.
/// </summary>
[Parallelizable(ParallelScope.Self)]
public class Exercise44Tests : TestBase
{
    [Test]
    public async Task Google_Login_Button_Hidden_When_ClientId_Unset()
    {
        await Page.GotoAsync("/Account/Login");
        await Expect(Page.GetByTestId("external-login-google")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task Classic_Login_Still_Works_With_Google_Integration_Present()
    {
        // Regression: adding the Google hooks should not break cookie-based login.
        await TestHelpers.LoginAsAdminAsync(Page);
        await Page.GotoAsync("/WhoAmI");
        await Expect(Page.GetByTestId("whoami-authenticated")).ToContainTextAsync("True");
    }
}
