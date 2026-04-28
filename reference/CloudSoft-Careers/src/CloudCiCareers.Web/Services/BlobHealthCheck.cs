using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CloudCiCareers.Web.Services;

public class BlobHealthCheck : IHealthCheck
{
    private readonly BlobContainerClient _container;

    public BlobHealthCheck(BlobServiceClient client, IConfiguration config)
    {
        var name = config["Storage:Container"]
            ?? throw new InvalidOperationException("Storage:Container not configured");
        _container = client.GetBlobContainerClient(name);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            var exists = await _container.ExistsAsync(cts.Token);
            return exists.Value
                ? HealthCheckResult.Healthy("Blob container reachable")
                : HealthCheckResult.Unhealthy("Blob container missing");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Blob unreachable", ex);
        }
    }
}
