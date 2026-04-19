using Microsoft.AspNetCore.Mvc;

namespace CloudSoft.Auth.Web.Controllers;

public class WhoAmIController : Controller
{
    public IActionResult Index() => View();
}
