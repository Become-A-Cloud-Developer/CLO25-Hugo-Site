using System.Net;
using System.Text.Json;
using Xunit;

namespace CloudSoft.Web.Tests.IntegrationTests;

public class VersionEndpointTests : IDisposable
{
    private readonly CloudSoftWebApplicationFactory _factory;

    public VersionEndpointTests()
    {
        _factory = new CloudSoftWebApplicationFactory();
    }

    [Fact]
    public async Task VersionEndpoint_ReturnsOk()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync("/version");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VersionEndpoint_ReturnsVersionString()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync("/version");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("version", content);

        var json = JsonDocument.Parse(content);
        var version = json.RootElement.GetProperty("version").GetString();
        Assert.False(string.IsNullOrEmpty(version));
    }

    [Fact]
    public async Task VersionEndpoint_NoAuthRequired()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync("/version");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}
