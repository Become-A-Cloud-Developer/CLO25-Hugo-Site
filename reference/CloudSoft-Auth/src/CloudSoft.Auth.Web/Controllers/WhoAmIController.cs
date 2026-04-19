using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudSoft.Auth.Web.Controllers;

public class WhoAmIController : Controller
{
    public IActionResult Index() => View();

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public IActionResult TestPost(string message)
    {
        TempData["CsrfDemoMessage"] = $"Received: {message}";
        return RedirectToAction(nameof(Index));
    }
}
