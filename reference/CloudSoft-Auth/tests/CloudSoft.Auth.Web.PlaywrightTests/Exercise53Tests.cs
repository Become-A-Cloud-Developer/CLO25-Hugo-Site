using Microsoft.Playwright;

namespace CloudSoft.Auth.Web.PlaywrightTests;

/// <summary>
/// Exercise 5.3 — the admin user is seeded from configuration (AdminSeed:*)
/// via IdentitySeeder, and the seeder is idempotent. We can't easily
/// demonstrate idempotency across app restarts inside one Playwright run,
/// so we verify the observable state: admin exists, is in the Admin role,
/// and carries the Department claim from AdminSeed config.
/// </summary>
[Parallelizable(ParallelScope.Self)]
public class Exercise53Tests : TestBase
{
    [Test]
    public async Task Seeded_Admin_Has_Expected_Role_And_Department()
    {
        await TestHelpers.LoginAsAdminAsync(Page);
        await Page.GotoAsync("/WhoAmI");

        await Expect(Page.GetByTestId("whoami-name")).ToContainTextAsync("admin");
        await Expect(Page.GetByTestId("whoami-roles")).ToContainTextAsync("Admin");

        var claims = Page.GetByTestId("whoami-claims-table");
        await Expect(claims).ToContainTextAsync("Department");
        await Expect(claims).ToContainTextAsync("Engineering");
    }

    [Test]
    public async Task Seeded_Admin_Can_Reach_Policy_Gated_Engineering_Page()
    {
        await TestHelpers.LoginAsAdminAsync(Page);
        var response = await Page.GotoAsync("/Engineering");

        Assert.That(response?.Status, Is.EqualTo(200));
        await Expect(Page.GetByTestId("engineering-heading")).ToBeVisibleAsync();
    }
}
