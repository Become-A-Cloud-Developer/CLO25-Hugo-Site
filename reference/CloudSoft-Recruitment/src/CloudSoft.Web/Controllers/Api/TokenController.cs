using CloudSoft.Web.Models;
using CloudSoft.Web.Models.DTOs;
using CloudSoft.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CloudSoft.Web.Controllers.Api;

[ApiController]
[Route("api/token")]
[IgnoreAntiforgeryToken]
public class TokenController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<TokenController> _logger;

    public TokenController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService,
        ILogger<TokenController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateToken([FromBody] TokenRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Token request for non-existent user {Email}", request.Email);
            return Unauthorized(new { error = "Invalid credentials" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed token request for {Email}", request.Email);
            return Unauthorized(new { error = "Invalid credentials" });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "";
        var token = _jwtTokenService.GenerateToken(user.Id, user.Email!, role, user.DisplayName ?? user.Email!);

        _logger.LogInformation("Token issued for {Email} with role {Role}", request.Email, role);
        return Ok(new TokenResponse
        {
            Token = token,
            Expiration = DateTime.UtcNow.AddMinutes(60)
        });
    }
}
