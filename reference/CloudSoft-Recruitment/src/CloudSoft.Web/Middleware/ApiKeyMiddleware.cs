using System.Security.Claims;

namespace CloudSoft.Web.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeaderName = "X-API-Key";

    public ApiKeyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        if (context.Request.Path.StartsWithSegments("/api") &&
            context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedKey))
        {
            var configuredKey = configuration["ApiKey"];
            if (!string.IsNullOrEmpty(configuredKey) && extractedKey == configuredKey)
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim(ClaimTypes.Name, "ApiKeyUser"),
                    new Claim(ClaimTypes.NameIdentifier, "api-key-user")
                };
                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey"));
            }
        }

        await _next(context);
    }
}
