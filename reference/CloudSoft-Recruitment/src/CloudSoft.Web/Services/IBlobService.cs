namespace CloudSoft.Web.Services;

public interface IBlobService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType);
    Task<Stream?> DownloadAsync(string fileName);
    Task DeleteAsync(string fileName);
}
