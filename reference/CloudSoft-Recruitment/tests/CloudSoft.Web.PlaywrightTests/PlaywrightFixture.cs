using Microsoft.Playwright;

namespace CloudSoft.Web.PlaywrightTests;

[CollectionDefinition("Playwright")]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>;

public class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;

    public IBrowser Browser { get; private set; } = null!;
    public string BaseUrl { get; private set; } = null!;
    public bool IsAppRunning { get; private set; }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();

        var mode = Environment.GetEnvironmentVariable("PLAYWRIGHT_MODE") ?? "headless";
        BaseUrl = Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL") ?? "http://localhost:5161";

        var launchOptions = new BrowserTypeLaunchOptions();

        switch (mode.ToLowerInvariant())
        {
            case "headed":
                launchOptions.Headless = false;
                break;
            case "slowmo":
                launchOptions.Headless = false;
                var slowMoMs = int.TryParse(Environment.GetEnvironmentVariable("PLAYWRIGHT_SLOWMO"), out var ms) ? ms : 500;
                launchOptions.SlowMo = slowMoMs;
                break;
            case "headless":
            default:
                launchOptions.Headless = true;
                break;
        }

        Browser = await _playwright.Chromium.LaunchAsync(launchOptions);

        // Health check: verify the app is reachable
        try
        {
            var handler = new HttpClientHandler();
            if (BaseUrl.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase))
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
            var response = await httpClient.GetAsync(BaseUrl);
            IsAppRunning = response.IsSuccessStatusCode;
        }
        catch
        {
            IsAppRunning = false;
        }
    }

    public async Task<IBrowserContext> CreateContextAsync()
    {
        var options = new BrowserNewContextOptions();
        if (BaseUrl.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase))
        {
            options.IgnoreHTTPSErrors = true;
        }
        return await Browser.NewContextAsync(options);
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
            await Browser.CloseAsync();

        _playwright?.Dispose();
    }
}
