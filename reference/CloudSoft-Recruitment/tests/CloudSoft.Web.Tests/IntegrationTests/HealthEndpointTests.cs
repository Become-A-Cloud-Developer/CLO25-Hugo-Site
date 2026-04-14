using System.Net;
using Xunit;

namespace CloudSoft.Web.Tests.IntegrationTests;

public class HealthEndpointTests : IDisposable
{
    private readonly CloudSoftWebApplicationFactory _factory;

    public HealthEndpointTests()
    {
        _factory = new CloudSoftWebApplicationFactory();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsJsonWithStatus()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("status", content);
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task HealthEndpoint_NoAuthRequired()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync("/health");
        Assert.NotEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}
