using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CloudSoft.Web.Options;
using CloudSoft.Web.Services;
using Microsoft.IdentityModel.Tokens;

namespace CloudSoft.Web.Tests;

public class JwtTokenServiceTests
{
    private readonly JwtOptions _options = new()
    {
        Key = "TestSecretKeyForUnitTestsThatIsAtLeast32Characters!",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        ExpirationMinutes = 60
    };

    private JwtTokenService CreateService() =>
        new(Microsoft.Extensions.Options.Options.Create(_options));

    [Fact]
    public void GenerateToken_ValidUser_ReturnsNonEmptyToken()
    {
        var service = CreateService();

        var token = service.GenerateToken("user-1", "test@example.com", "Admin", "Test User");

        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public void GenerateToken_ContainsExpectedClaims()
    {
        var service = CreateService();

        var token = service.GenerateToken("user-1", "test@example.com", "Admin", "Test User");

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("test@example.com", jwtToken.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("user-1", jwtToken.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("Test User", jwtToken.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("TestIssuer", jwtToken.Issuer);
        Assert.Contains("TestAudience", jwtToken.Audiences);
    }

    [Fact]
    public void GenerateToken_AdminRole_ContainsAdminRoleClaim()
    {
        var service = CreateService();

        var token = service.GenerateToken("user-1", "admin@example.com", "Admin", "Admin User");

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("Admin", jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void GenerateToken_TokenExpiresAfterConfiguredDuration()
    {
        var service = CreateService();
        var beforeGeneration = DateTime.UtcNow;

        var token = service.GenerateToken("user-1", "test@example.com", "Admin", "Test User");

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expectedExpiration = beforeGeneration.AddMinutes(_options.ExpirationMinutes);
        // Allow a small tolerance for test execution time
        Assert.True(jwtToken.ValidTo >= expectedExpiration.AddSeconds(-5));
        Assert.True(jwtToken.ValidTo <= expectedExpiration.AddSeconds(5));
    }

    [Fact]
    public void ValidateToken_ExpiredToken()
    {
        var expiredOptions = new JwtOptions
        {
            Key = "TestSecretKeyForUnitTestsThatIsAtLeast32Characters!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = -1
        };
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(expiredOptions));

        var token = service.GenerateToken("user-1", "test@example.com", "Admin", "Test User");

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = expiredOptions.Issuer,
            ValidAudience = expiredOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(expiredOptions.Key)),
            ClockSkew = TimeSpan.Zero
        };

        Assert.Throws<SecurityTokenExpiredException>(() =>
            handler.ValidateToken(token, validationParameters, out _));
    }
}
