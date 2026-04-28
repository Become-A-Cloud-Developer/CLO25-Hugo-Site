using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CloudCiCareers.Web.Services;

public class CosmosHealthCheck : IHealthCheck
{
    private readonly Container _container;

    public CosmosHealthCheck(CosmosClient client, IConfiguration config)
    {
        var db = config["Cosmos:Database"]
            ?? throw new InvalidOperationException("Cosmos:Database not configured");
        var name = config["Cosmos:Container"]
            ?? throw new InvalidOperationException("Cosmos:Container not configured");
        _container = client.GetContainer(db, name);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await _container.ReadContainerAsync(cancellationToken: cts.Token);
            return HealthCheckResult.Healthy("Cosmos reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cosmos unreachable", ex);
        }
    }
}
