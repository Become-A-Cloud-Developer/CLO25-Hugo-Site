using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using CloudCi.Models;

namespace CloudCi.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly TelemetryClient _telemetry;

    public HomeController(ILogger<HomeController> logger, TelemetryClient telemetry)
    {
        _logger = logger;
        _telemetry = telemetry;
    }

    public IActionResult Index()
    {
        var hostName = Environment.MachineName;
        var buildShaRaw = Environment.GetEnvironmentVariable("BUILD_SHA");
        var buildSha = buildShaRaw ?? "local";

        if (string.IsNullOrEmpty(buildShaRaw))
        {
            _logger.LogWarning("BUILD_SHA env var is not set; falling back to {BuildSha}", buildSha);
        }

        // GetMetric returns a pre-aggregated metric that buckets values
        // client-side and ships one-minute summaries. Cheap at any volume.
        _telemetry.GetMetric("home-page-views").TrackValue(1);

        _logger.LogInformation(
            "Home page rendered for {HostName} build {BuildSha}",
            hostName,
            buildSha);

        _logger.LogDebug("Index() finished; returning View");

        ViewData["BuildSha"] = buildSha;
        ViewData["HostName"] = hostName;
        return View();
    }

    public IActionResult Boom()
    {
        _logger.LogError("About to throw an exception from /Home/Boom");
        throw new InvalidOperationException(
            "Boom! This is a deliberate failure for the Failures blade.");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
