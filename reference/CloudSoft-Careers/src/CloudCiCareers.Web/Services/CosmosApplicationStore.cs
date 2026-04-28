using System.Net;
using CloudCiCareers.Web.Models;
using Microsoft.Azure.Cosmos;

namespace CloudCiCareers.Web.Services;

public class CosmosApplicationStore : IApplicationStore
{
    private readonly Container _container;

    public CosmosApplicationStore(CosmosClient client, IConfiguration config)
    {
        var db = config["Cosmos:Database"]
            ?? throw new InvalidOperationException("Cosmos:Database not configured");
        var name = config["Cosmos:Container"]
            ?? throw new InvalidOperationException("Cosmos:Container not configured");
        _container = client.GetContainer(db, name);
    }

    public async Task<IEnumerable<Application>> GetAllAsync(CancellationToken ct = default)
    {
        var query = _container.GetItemQueryIterator<Application>(
            new QueryDefinition("SELECT * FROM c ORDER BY c.SubmittedAt DESC"));
        var results = new List<Application>();
        while (query.HasMoreResults)
        {
            foreach (var item in await query.ReadNextAsync(ct))
            {
                results.Add(item);
            }
        }
        return results;
    }

    public async Task<Application?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var r = await _container.ReadItemAsync<Application>(
                id, new PartitionKey(id), cancellationToken: ct);
            return r.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Application> CreateAsync(Application application, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(application.Id))
        {
            application.Id = Guid.NewGuid().ToString("n");
        }
        if (application.SubmittedAt == default)
        {
            application.SubmittedAt = DateTimeOffset.UtcNow;
        }
        var r = await _container.CreateItemAsync(application, new PartitionKey(application.Id),
            cancellationToken: ct);
        return r.Resource;
    }

    public async Task<bool> UpdateStatusAsync(string id, ApplicationStatus newStatus, string? notes,
        CancellationToken ct = default)
    {
        try
        {
            await _container.PatchItemAsync<Application>(
                id, new PartitionKey(id),
                patchOperations: new[]
                {
                    PatchOperation.Replace("/Status", newStatus.ToString()),
                    PatchOperation.Replace("/Notes", notes),
                },
                cancellationToken: ct);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        try
        {
            await _container.DeleteItemAsync<Application>(id, new PartitionKey(id),
                cancellationToken: ct);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
