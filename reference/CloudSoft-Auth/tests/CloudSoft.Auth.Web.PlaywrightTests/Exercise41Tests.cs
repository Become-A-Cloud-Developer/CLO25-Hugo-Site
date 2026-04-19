using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace CloudSoft.Auth.Web.PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
public class Exercise41Tests : TestBase
{
    [Test]
    public async Task Anonymous_WhoAmI_Page_Reports_NotAuthenticated()
    {
        await Page.GotoAsync("/WhoAmI");

        await Expect(Page.GetByTestId("whoami-authenticated"))
            .ToContainTextAsync("False");
        await Expect(Page.GetByTestId("whoami-name"))
            .ToContainTextAsync("(anonymous)");
        await Expect(Page.GetByTestId("whoami-authtype"))
            .ToContainTextAsync("(none)");
    }

    [Test]
    public async Task Footer_WhoAmI_Link_Navigates_To_The_Page()
    {
        await Page.GotoAsync("/");
        await Page.GetByTestId("footer-whoami").ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"/WhoAmI/?$"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Who Am I?" }))
            .ToBeVisibleAsync();
    }

    [Test]
    public async Task Login_As_Admin_Puts_Name_And_Scheme_On_WhoAmI()
    {
        await TestHelpers.LoginAsAdminAsync(Page);
        await Page.GotoAsync("/WhoAmI");

        await Expect(Page.GetByTestId("whoami-authenticated"))
            .ToContainTextAsync("True");
        await Expect(Page.GetByTestId("whoami-name"))
            .ToContainTextAsync("admin");
        // Any registered scheme is acceptable: "Cookies" before Exercise 5.1,
        // "Identity.Application" after Identity takes over.
        await Expect(Page.GetByTestId("whoami-authtype"))
            .Not.ToContainTextAsync("(none)");
    }

    [Test]
    public async Task Login_With_Bad_Credentials_Shows_Error()
    {
        await Page.GotoAsync("/Account/Login");
        await Page.GetByTestId("login-username").FillAsync("admin");
        await Page.GetByTestId("login-password").FillAsync("wrong");
        await Page.GetByTestId("login-submit").ClickAsync();

        await Expect(Page.GetByTestId("login-error")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Logout_Clears_Authentication_State()
    {
        await TestHelpers.LoginAsAdminAsync(Page);
        await TestHelpers.LogoutAsync(Page);

        await Page.GotoAsync("/WhoAmI");
        await Expect(Page.GetByTestId("whoami-authenticated"))
            .ToContainTextAsync("False");
    }
}
