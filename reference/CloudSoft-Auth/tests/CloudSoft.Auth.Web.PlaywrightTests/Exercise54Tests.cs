using Microsoft.Playwright;

namespace CloudSoft.Auth.Web.PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
public class Exercise54Tests : TestBase
{
    private static string RandomUsername() =>
        "newuser-" + Guid.NewGuid().ToString("N")[..8];

    [Test]
    public async Task New_User_Registers_As_Candidate_And_Signs_In()
    {
        var username = RandomUsername();

        await Page.GotoAsync("/Account/Register");
        await Page.GetByTestId("register-username").FillAsync(username);
        await Page.GetByTestId("register-email").FillAsync($"{username}@example.com");
        await Page.GetByTestId("register-password").FillAsync("newpass");
        await Page.GetByTestId("register-submit").ClickAsync();

        await Page.WaitForURLAsync(url => url.Contains("/WhoAmI"));

        await Expect(Page.GetByTestId("whoami-name")).ToContainTextAsync(username);
        await Expect(Page.GetByTestId("whoami-roles")).ToContainTextAsync("Candidate");
    }

    [Test]
    public async Task Registered_Candidate_Cannot_Access_AdminOnly()
    {
        var username = RandomUsername();

        await Page.GotoAsync("/Account/Register");
        await Page.GetByTestId("register-username").FillAsync(username);
        await Page.GetByTestId("register-email").FillAsync($"{username}@example.com");
        await Page.GetByTestId("register-password").FillAsync("newpass");
        await Page.GetByTestId("register-submit").ClickAsync();

        await Page.WaitForURLAsync(url => url.Contains("/WhoAmI"));

        await Page.GotoAsync("/AdminOnly");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/AccessDenied"));
    }

    [Test]
    public async Task Admin_Can_Promote_A_Candidate()
    {
        // Register a new candidate we can promote (use a random username to
        // keep tests independent of prior runs under SQLite).
        var username = RandomUsername();
        await Page.GotoAsync("/Account/Register");
        await Page.GetByTestId("register-username").FillAsync(username);
        await Page.GetByTestId("register-email").FillAsync($"{username}@example.com");
        await Page.GetByTestId("register-password").FillAsync("newpass");
        await Page.GetByTestId("register-submit").ClickAsync();
        await Page.WaitForURLAsync(url => url.Contains("/WhoAmI"));
        await TestHelpers.LogoutAsync(Page);

        await TestHelpers.LoginAsAdminAsync(Page);
        await Page.GotoAsync("/UserAdmin");

        var rolesCell = Page.GetByTestId($"useradmin-roles-{username}");
        await Expect(rolesCell).ToContainTextAsync("Candidate");
        await Expect(rolesCell).Not.ToContainTextAsync("Admin");

        await Page.GetByTestId($"promote-{username}").ClickAsync();
        await Page.WaitForURLAsync(url => url.Contains("/UserAdmin"));

        var rolesAfter = Page.GetByTestId($"useradmin-roles-{username}");
        await Expect(rolesAfter).ToContainTextAsync("Admin");
    }

    [Test]
    public async Task Admin_Cannot_Demote_Self_From_User_Admin_Page()
    {
        await TestHelpers.LoginAsAdminAsync(Page);
        await Page.GotoAsync("/UserAdmin");

        // The row for the current admin user must not expose a Demote button.
        var selfRow = Page.GetByTestId("useradmin-row-admin");
        await Expect(selfRow).ToContainTextAsync("(you)");
        await Expect(selfRow).ToContainTextAsync("(cannot modify self)");

        await Expect(Page.GetByTestId("demote-admin")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task UserAdmin_Is_Not_Reachable_By_Candidate()
    {
        await TestHelpers.LoginAsCandidateAsync(Page);
        await Page.GotoAsync("/UserAdmin");

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/AccessDenied"));
    }

    [Test]
    public async Task Login_Page_Links_To_Register()
    {
        await Page.GotoAsync("/Account/Login");
        await Page.GetByTestId("login-register-link").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/Register"));
    }
}
