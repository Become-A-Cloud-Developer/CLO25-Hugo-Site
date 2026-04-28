namespace CloudCiCareers.Web.Services;

public class LocalFileBlobService : IBlobService
{
    private readonly string _root;

    public LocalFileBlobService(IWebHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(_root);
    }

    public async Task UploadAsync(string name, Stream content, CancellationToken ct)
    {
        var path = Path.Combine(_root, name);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, ct);
    }

    public Task<Stream> OpenReadAsync(string name, CancellationToken ct)
    {
        var path = Path.Combine(_root, name);
        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string name, CancellationToken ct)
    {
        var path = Path.Combine(_root, name);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.CompletedTask;
    }
}
