using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace CloudSoft.Web.Tests.IntegrationTests;

public class JwtAuthenticationTests : IDisposable
{
    private readonly CloudSoftWebApplicationFactory _factory;

    public JwtAuthenticationTests()
    {
        _factory = new CloudSoftWebApplicationFactory();
    }

    public void Dispose() => _factory.Dispose();

    private HttpClient CreateClient() =>
        _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task TokenEndpoint_ValidAdminCredentials_ReturnsToken()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/token", new
        {
            email = "admin@cloudsoft.com",
            password = "Admin123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.True(json.TryGetProperty("token", out var token));
        Assert.False(string.IsNullOrEmpty(token.GetString()));
        Assert.True(json.TryGetProperty("expiration", out _));
    }

    [Fact]
    public async Task TokenEndpoint_ValidCandidateCredentials_ReturnsToken()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/token", new
        {
            email = "candidate@test.com",
            password = "Candidate123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.True(json.TryGetProperty("token", out var token));
        Assert.False(string.IsNullOrEmpty(token.GetString()));
    }

    [Fact]
    public async Task TokenEndpoint_InvalidCredentials_Returns401()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/token", new
        {
            email = "admin@cloudsoft.com",
            password = "WrongPassword!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenEndpoint_NonexistentUser_Returns401()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/token", new
        {
            email = "nobody@cloudsoft.com",
            password = "SomePassword!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_ValidToken_ReturnsOk()
    {
        var client = CreateClient();
        var token = await client.GetJwtTokenAsync("admin@cloudsoft.com", "Admin123!");
        client.SetBearerToken(token);

        var response = await client.PostAsJsonAsync("/api/jobs", new
        {
            title = "JWT Test Job",
            description = "Testing JWT authentication",
            location = "Oslo",
            deadline = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd")
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_ExpiredToken_Returns401()
    {
        var client = CreateClient();

        // Create a manually expired token using the same test key
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("TestSecretKeyForIntegrationTestsThatIsAtLeast32Characters!"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiredToken = new JwtSecurityToken(
            issuer: "CloudSoft.Tests",
            audience: "CloudSoft.Tests",
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user"),
                new Claim(ClaimTypes.Email, "admin@cloudsoft.com"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Name, "Admin")
            },
            expires: DateTime.UtcNow.AddMinutes(-10),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(expiredToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

        var response = await client.PostAsJsonAsync("/api/jobs", new
        {
            title = "Expired Token Job",
            description = "This should fail",
            location = "Oslo",
            deadline = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd")
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_MalformedToken_Returns401()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-jwt-token");

        var response = await client.PostAsJsonAsync("/api/jobs", new
        {
            title = "Malformed Token Job",
            description = "This should fail",
            location = "Oslo",
            deadline = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd")
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
