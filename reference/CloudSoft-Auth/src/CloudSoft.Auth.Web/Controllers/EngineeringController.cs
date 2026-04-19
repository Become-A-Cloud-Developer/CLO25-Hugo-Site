using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudSoft.Auth.Web.Controllers;

[Authorize(Policy = "RequireEngineering")]
public class EngineeringController : Controller
{
    public IActionResult Index() => View();
}
