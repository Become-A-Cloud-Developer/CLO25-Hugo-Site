using Microsoft.Playwright;

namespace CloudSoft.Auth.Web.PlaywrightTests;

/// <summary>
/// Exercise 5.2 introduces a feature flag that chooses the EF Core provider
/// for the Identity store. The default configuration is InMemory; these tests
/// verify the flag reaches the DbContext and report the active provider via a
/// diagnostics endpoint.
///
/// Tests reflect the configured store via the IDENTITY_STORE_UNDER_TEST env
/// var set by the harness script. Run ./run-playwright-tests.sh to exercise
/// InMemory, and ./run-playwright-tests-sqlite.sh to exercise SQLite.
/// </summary>
[Parallelizable(ParallelScope.Self)]
public class Exercise52Tests : TestBase
{
    private static string StoreUnderTest =>
        Environment.GetEnvironmentVariable("IDENTITY_STORE_UNDER_TEST") ?? "InMemory";

    [Test]
    public async Task Diagnostics_Reports_Configured_Provider()
    {
        await Page.GotoAsync("/Diagnostics/Store");

        await Expect(Page.GetByTestId("diag-configured-provider"))
            .ToContainTextAsync(StoreUnderTest);
    }

    [Test]
    public async Task Diagnostics_EF_Provider_Matches_Configured_Store()
    {
        await Page.GotoAsync("/Diagnostics/Store");

        var expectedEfProvider = StoreUnderTest.Equals("SQLite", StringComparison.OrdinalIgnoreCase)
            ? "Sqlite"
            : "InMemory";

        await Expect(Page.GetByTestId("diag-provider-name"))
            .ToContainTextAsync(expectedEfProvider);
    }

    [Test]
    public async Task Admin_Seeded_User_Can_Log_In_Under_Current_Store()
    {
        // Whichever store is active, the seeder populated admin/admin.
        await TestHelpers.LoginAsAdminAsync(Page);
        await Page.GotoAsync("/WhoAmI");

        await Expect(Page.GetByTestId("whoami-authenticated")).ToContainTextAsync("True");
        await Expect(Page.GetByTestId("whoami-name")).ToContainTextAsync("admin");
    }
}
