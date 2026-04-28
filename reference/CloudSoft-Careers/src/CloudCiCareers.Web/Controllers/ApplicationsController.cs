using CloudCiCareers.Web.Models;
using CloudCiCareers.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CloudCiCareers.Web.Controllers;

public class ApplicationsController : Controller
{
    private readonly IApplicationStore _store;
    private readonly IJobCatalog _catalog;
    private readonly IBlobService _blobs;

    public ApplicationsController(
        IApplicationStore store,
        IJobCatalog catalog,
        IBlobService blobs)
    {
        _store = store;
        _catalog = catalog;
        _blobs = blobs;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Catalog"] = _catalog;
        var all = await _store.GetAllAsync(ct);
        return View(all.ToList());
    }

    public async Task<IActionResult> Details(string id, CancellationToken ct)
    {
        var application = await _store.GetByIdAsync(id, ct);
        if (application is null)
        {
            return NotFound();
        }
        ViewData["Job"] = _catalog.GetById(application.JobId);
        return View(application);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(string id,
        ApplicationStatus newStatus, string? notes, CancellationToken ct)
    {
        if (!await _store.UpdateStatusAsync(id, newStatus, notes, ct))
        {
            return NotFound();
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        if (!await _store.DeleteAsync(id, ct))
        {
            return NotFound();
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Cv(string id, CancellationToken ct)
    {
        var application = await _store.GetByIdAsync(id, ct);
        if (application is null)
        {
            return NotFound();
        }
        var stream = await _blobs.OpenReadAsync(application.CvBlobName, ct);
        Response.Headers["Content-Disposition"] = "inline; filename=\"cv.pdf\"";
        return File(stream, "application/pdf");
    }
}
