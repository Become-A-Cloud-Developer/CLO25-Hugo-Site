using CloudSoft.Auth.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudSoft.Auth.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UserAdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAdminController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var rows = new List<UserRow>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            rows.Add(new UserRow(
                user.Id,
                user.UserName ?? "(unknown)",
                user.Email,
                roles.ToArray()));
        }
        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Promote(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is not null && currentUser.Id == user.Id)
        {
            TempData["UserAdminMessage"] = "You cannot modify your own roles.";
            return RedirectToAction(nameof(Index));
        }

        if (!await _userManager.IsInRoleAsync(user, "Admin"))
        {
            await _userManager.AddToRoleAsync(user, "Admin");
            TempData["UserAdminMessage"] = $"{user.UserName} is now Admin.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Demote(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is not null && currentUser.Id == user.Id)
        {
            TempData["UserAdminMessage"] = "You cannot demote yourself.";
            return RedirectToAction(nameof(Index));
        }

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            await _userManager.RemoveFromRoleAsync(user, "Admin");
            TempData["UserAdminMessage"] = $"{user.UserName} is no longer Admin.";
        }

        return RedirectToAction(nameof(Index));
    }

    public record UserRow(string Id, string Username, string? Email, string[] Roles);
}
