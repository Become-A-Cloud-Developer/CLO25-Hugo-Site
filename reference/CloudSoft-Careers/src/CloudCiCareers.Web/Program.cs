using System.Text.Json;
using Azure.Identity;
using Azure.Storage.Blobs;
using CloudCiCareers.Web.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddSingleton<IJobCatalog, StaticJobCatalog>();

// If Cosmos:Endpoint is configured, register the cloud implementations
// authenticated by the Container App's managed identity. Otherwise fall back
// to the dev-friendly in-memory + local-file pair so `dotnet run` Just Works.
var cosmosEndpoint = builder.Configuration["Cosmos:Endpoint"];
var hasCloud = !string.IsNullOrWhiteSpace(cosmosEndpoint);

if (hasCloud)
{
    builder.Services.AddSingleton(_ =>
        new CosmosClient(cosmosEndpoint, new DefaultAzureCredential()));
    builder.Services.AddSingleton(_ =>
        new BlobServiceClient(
            new Uri(builder.Configuration["Storage:BlobEndpoint"]
                ?? throw new InvalidOperationException("Storage:BlobEndpoint not configured")),
            new DefaultAzureCredential()));
    builder.Services.AddSingleton<IApplicationStore, CosmosApplicationStore>();
    builder.Services.AddSingleton<IBlobService, AzureBlobService>();
}
else
{
    builder.Services.AddSingleton<IApplicationStore, InMemoryApplicationStore>();
    builder.Services.AddSingleton<IBlobService, LocalFileBlobService>();
}

var healthChecks = builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy());
if (hasCloud)
{
    healthChecks
        .AddCheck<CosmosHealthCheck>("cosmos", tags: new[] { "ready" })
        .AddCheck<BlobHealthCheck>("blob", tags: new[] { "ready" });
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = c => c.Tags.Contains("ready"),
});
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteJsonResponse,
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Jobs}/{action=Index}/{id?}");

app.Run();

static Task WriteJsonResponse(HttpContext ctx, HealthReport report)
{
    ctx.Response.ContentType = "application/json";
    var payload = JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            duration_ms = (int)e.Value.Duration.TotalMilliseconds,
        }),
    });
    return ctx.Response.WriteAsync(payload);
}
