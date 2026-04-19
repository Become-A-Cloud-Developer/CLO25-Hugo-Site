using System.Security.Claims;
using CloudSoft.Auth.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudSoft.Auth.Web.Controllers;

public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        var user = DummyUsers.Find(username, password);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var (type, value) in user.Claims)
        {
            claims.Add(new Claim(type, value));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "WhoAmI");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action("Index", "WhoAmI");

        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }
}
