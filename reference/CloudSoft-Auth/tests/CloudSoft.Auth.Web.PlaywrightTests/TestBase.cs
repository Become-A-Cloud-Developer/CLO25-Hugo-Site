using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace CloudSoft.Auth.Web.PlaywrightTests;

public abstract class TestBase : PageTest
{
    protected static string BaseUrl =>
        Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL") ?? "http://localhost:5017";

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = BaseUrl,
        IgnoreHTTPSErrors = true,
    };

    [SetUp]
    public async Task SetUpContextStartFresh()
    {
        await Context.ClearCookiesAsync();
    }
}
