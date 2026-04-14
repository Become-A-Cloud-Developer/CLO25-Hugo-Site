namespace CloudSoft.Web.Services;

public class LocalBlobService : IBlobService
{
    private readonly string _storagePath;

    public LocalBlobService(string storagePath)
    {
        _storagePath = storagePath;
        Directory.CreateDirectory(_storagePath);
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        var filePath = Path.Combine(_storagePath, fileName);
        using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream);
        return fileName;
    }

    public Task<Stream?> DownloadAsync(string fileName)
    {
        var filePath = Path.Combine(_storagePath, fileName);
        if (!File.Exists(filePath))
            return Task.FromResult<Stream?>(null);

        Stream stream = File.OpenRead(filePath);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string fileName)
    {
        var filePath = Path.Combine(_storagePath, fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }
}
