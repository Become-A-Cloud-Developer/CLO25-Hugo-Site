using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudSoft.Auth.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminOnlyController : Controller
{
    public IActionResult Index() => View();
}
