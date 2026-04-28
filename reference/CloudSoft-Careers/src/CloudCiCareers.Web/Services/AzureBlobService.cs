using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace CloudCiCareers.Web.Services;

public class AzureBlobService : IBlobService
{
    private readonly BlobContainerClient _container;

    public AzureBlobService(BlobServiceClient client, IConfiguration config)
    {
        var name = config["Storage:Container"]
            ?? throw new InvalidOperationException("Storage:Container not configured");
        _container = client.GetBlobContainerClient(name);
    }

    public async Task UploadAsync(string name, Stream content, CancellationToken ct)
    {
        await _container.GetBlobClient(name).UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = "application/pdf" },
            cancellationToken: ct);
    }

    public async Task<Stream> OpenReadAsync(string name, CancellationToken ct)
    {
        return await _container.GetBlobClient(name).OpenReadAsync(cancellationToken: ct);
    }

    public async Task DeleteAsync(string name, CancellationToken ct)
    {
        await _container.GetBlobClient(name).DeleteIfExistsAsync(cancellationToken: ct);
    }
}
