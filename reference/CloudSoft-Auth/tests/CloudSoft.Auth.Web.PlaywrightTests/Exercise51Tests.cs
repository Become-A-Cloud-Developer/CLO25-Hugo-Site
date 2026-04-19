using Microsoft.Playwright;

namespace CloudSoft.Auth.Web.PlaywrightTests;

/// <summary>
/// Exercise 5.1 — ASP.NET Core Identity replaces the hand-rolled cookie
/// sign-in. UserManager/SignInManager manage users; EF Core InMemory stores
/// them in the same process. The Who Am I page should look identical to the
/// end of Chapter 4 except the authentication scheme is now
/// "Identity.Application" (Identity's default application cookie).
/// </summary>
[Parallelizable(ParallelScope.Self)]
public class Exercise51Tests : TestBase
{
    [Test]
    public async Task Authentication_Scheme_Is_Identity_Application()
    {
        await TestHelpers.LoginAsAdminAsync(Page);
        await Page.GotoAsync("/WhoAmI");

        await Expect(Page.GetByTestId("whoami-authtype"))
            .ToContainTextAsync("Identity.Application");
    }

    [Test]
    public async Task Role_Claim_Flows_From_Identity_UserRoles_Table()
    {
        await TestHelpers.LoginAsAdminAsync(Page);
        await Page.GotoAsync("/WhoAmI");

        await Expect(Page.GetByTestId("whoami-roles")).ToContainTextAsync("Admin");
    }

    [Test]
    public async Task Custom_Claim_Flows_From_Identity_UserClaims_Table()
    {
        await TestHelpers.LoginAsAdminAsync(Page);
        await Page.GotoAsync("/WhoAmI");

        var table = Page.GetByTestId("whoami-claims-table");
        await Expect(table).ToContainTextAsync("Department");
        await Expect(table).ToContainTextAsync("Engineering");
    }

    [Test]
    public async Task NonExistent_User_Cannot_Sign_In()
    {
        await Page.GotoAsync("/Account/Login");
        await Page.GetByTestId("login-username").FillAsync("ghost");
        await Page.GetByTestId("login-password").FillAsync("whatever");
        await Page.GetByTestId("login-submit").ClickAsync();

        await Expect(Page.GetByTestId("login-error")).ToBeVisibleAsync();
    }
}
