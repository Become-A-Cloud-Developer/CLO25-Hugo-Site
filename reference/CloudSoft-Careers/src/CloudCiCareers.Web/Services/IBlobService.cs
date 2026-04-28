namespace CloudCiCareers.Web.Services;

public interface IBlobService
{
    Task UploadAsync(string name, Stream content, CancellationToken ct);
    Task<Stream> OpenReadAsync(string name, CancellationToken ct);
    Task DeleteAsync(string name, CancellationToken ct);
}
