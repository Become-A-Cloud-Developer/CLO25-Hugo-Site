using System.Security.Claims;
using CloudSoft.Web.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace CloudSoft.Web.Tests;

public class ApiKeyMiddlewareTests
{
    private class CallTracker { public bool NextCalled; }

    private static (ApiKeyMiddleware middleware, CallTracker tracker) CreateMiddleware()
    {
        var tracker = new CallTracker();
        var middleware = new ApiKeyMiddleware(_ =>
        {
            tracker.NextCalled = true;
            return Task.CompletedTask;
        });
        return (middleware, tracker);
    }

    private static IConfiguration BuildConfig(string? apiKey)
    {
        var dict = new Dictionary<string, string?>();
        if (apiKey is not null)
            dict["ApiKey"] = apiKey;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }

    [Fact]
    public async Task ApiRoute_ValidApiKey_SetsAdminPrincipal()
    {
        var (middleware, tracker) = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/jobs";
        context.Request.Headers["X-API-Key"] = "my-secret-key";
        var config = BuildConfig("my-secret-key");

        await middleware.InvokeAsync(context, config);

        Assert.True(tracker.NextCalled);
        Assert.True(context.User.Identity?.IsAuthenticated);
        Assert.True(context.User.IsInRole("Admin"));
        Assert.Equal("ApiKeyUser", context.User.Identity?.Name);
    }

    [Fact]
    public async Task ApiRoute_InvalidApiKey_DoesNotSetPrincipal()
    {
        var (middleware, tracker) = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/jobs";
        context.Request.Headers["X-API-Key"] = "wrong-key";
        var config = BuildConfig("my-secret-key");

        await middleware.InvokeAsync(context, config);

        Assert.True(tracker.NextCalled);
        Assert.False(context.User.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public async Task ApiRoute_NoApiKeyHeader_PassesThrough()
    {
        var (middleware, tracker) = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/jobs";
        var config = BuildConfig("my-secret-key");

        await middleware.InvokeAsync(context, config);

        Assert.True(tracker.NextCalled);
        Assert.False(context.User.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public async Task NonApiRoute_ApiKeyHeader_Ignored()
    {
        var (middleware, tracker) = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Path = "/Job";
        context.Request.Headers["X-API-Key"] = "my-secret-key";
        var config = BuildConfig("my-secret-key");

        await middleware.InvokeAsync(context, config);

        Assert.True(tracker.NextCalled);
        Assert.False(context.User.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public async Task ApiRoute_NoConfiguredKey_PassesThrough()
    {
        var (middleware, tracker) = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/jobs";
        context.Request.Headers["X-API-Key"] = "some-key";
        var config = BuildConfig("");

        await middleware.InvokeAsync(context, config);

        Assert.True(tracker.NextCalled);
        Assert.False(context.User.Identity?.IsAuthenticated ?? false);
    }
}
