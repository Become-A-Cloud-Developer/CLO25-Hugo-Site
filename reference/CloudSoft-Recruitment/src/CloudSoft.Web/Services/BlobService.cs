using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CloudSoft.Web.Options;
using Microsoft.Extensions.Options;

namespace CloudSoft.Web.Services;

public class BlobService : IBlobService
{
    private readonly BlobContainerClient _containerClient;

    public BlobService(IOptions<BlobStorageOptions> options)
    {
        var blobOptions = options.Value;
        var serviceClient = new BlobServiceClient(blobOptions.ConnectionString);
        _containerClient = serviceClient.GetBlobContainerClient(blobOptions.ContainerName);
        _containerClient.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });
        return fileName;
    }

    public async Task<Stream?> DownloadAsync(string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);

        if (!await blobClient.ExistsAsync())
            return null;

        var response = await blobClient.DownloadStreamingAsync();
        return response.Value.Content;
    }

    public async Task DeleteAsync(string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        await blobClient.DeleteIfExistsAsync();
    }
}
