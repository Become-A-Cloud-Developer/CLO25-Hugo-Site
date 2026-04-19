using Microsoft.Playwright;

namespace CloudSoft.Auth.Web.PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
public class Exercise42Tests : TestBase
{
    [Test]
    public async Task Admin_Login_Shows_Admin_Role_On_WhoAmI()
    {
        await TestHelpers.LoginAsAdminAsync(Page);
        await Page.GotoAsync("/WhoAmI");

        await Expect(Page.GetByTestId("whoami-roles")).ToContainTextAsync("Admin");
    }

    [Test]
    public async Task Candidate_Login_Shows_Candidate_Role_On_WhoAmI()
    {
        await TestHelpers.LoginAsCandidateAsync(Page);
        await Page.GotoAsync("/WhoAmI");

        await Expect(Page.GetByTestId("whoami-roles")).ToContainTextAsync("Candidate");
    }

    [Test]
    public async Task Admin_Can_Access_AdminOnly_Page()
    {
        await TestHelpers.LoginAsAdminAsync(Page);
        var response = await Page.GotoAsync("/AdminOnly");

        Assert.That(response?.Status, Is.EqualTo(200));
        await Expect(Page.GetByTestId("adminonly-heading")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Candidate_Is_Redirected_To_AccessDenied_On_AdminOnly()
    {
        await TestHelpers.LoginAsCandidateAsync(Page);
        await Page.GotoAsync("/AdminOnly");

        // Cookie AccessDeniedPath = /Account/AccessDenied
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(@"/Account/AccessDenied"));
        await Expect(Page.GetByTestId("access-denied-heading")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Anonymous_User_Hitting_AdminOnly_Is_Sent_To_Login()
    {
        await Page.GotoAsync("/AdminOnly");

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(@"/Account/Login"));
    }

    [Test]
    public async Task Admin_Link_On_WhoAmI_Is_Hidden_For_NonAdmin()
    {
        await TestHelpers.LoginAsCandidateAsync(Page);
        await Page.GotoAsync("/WhoAmI");

        await Expect(Page.GetByTestId("whoami-adminlink")).Not.ToBeVisibleAsync();
    }
}
