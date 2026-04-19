using CloudSoft.Auth.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudSoft.Auth.Web.Controllers;

public class DiagnosticsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public DiagnosticsController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet]
    public IActionResult Store()
    {
        ViewBag.ConfiguredProvider = _config["IdentityStore:Provider"] ?? "InMemory";
        ViewBag.ProviderName = _db.Database.ProviderName ?? "(unknown)";
        ViewBag.IsRelational = _db.Database.IsRelational();
        return View();
    }
}
