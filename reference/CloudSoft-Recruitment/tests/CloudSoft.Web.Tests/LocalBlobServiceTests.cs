using CloudSoft.Web.Services;

namespace CloudSoft.Web.Tests;

public class LocalBlobServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LocalBlobService _sut;

    public LocalBlobServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _sut = new LocalBlobService(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task UploadAsync_CreatesFileOnDisk()
    {
        var content = "hello world"u8.ToArray();
        using var stream = new MemoryStream(content);

        var result = await _sut.UploadAsync(stream, "test.txt", "text/plain");

        Assert.Equal("test.txt", result);
        Assert.True(File.Exists(Path.Combine(_tempDir, "test.txt")));
    }

    [Fact]
    public async Task DownloadAsync_ExistingFile_ReturnsStream()
    {
        var content = "file content"u8.ToArray();
        using var uploadStream = new MemoryStream(content);
        await _sut.UploadAsync(uploadStream, "download.txt", "text/plain");

        using var downloadStream = await _sut.DownloadAsync("download.txt");

        Assert.NotNull(downloadStream);
        using var reader = new StreamReader(downloadStream);
        var text = await reader.ReadToEndAsync();
        Assert.Equal("file content", text);
    }

    [Fact]
    public async Task DownloadAsync_NonExistingFile_ReturnsNull()
    {
        var result = await _sut.DownloadAsync("missing.txt");

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingFile_RemovesFile()
    {
        var content = "to delete"u8.ToArray();
        using var stream = new MemoryStream(content);
        await _sut.UploadAsync(stream, "delete-me.txt", "text/plain");
        Assert.True(File.Exists(Path.Combine(_tempDir, "delete-me.txt")));

        await _sut.DeleteAsync("delete-me.txt");

        Assert.False(File.Exists(Path.Combine(_tempDir, "delete-me.txt")));
    }

    [Fact]
    public async Task DeleteAsync_NonExistingFile_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(() => _sut.DeleteAsync("no-such-file.txt"));

        Assert.Null(exception);
    }
}
