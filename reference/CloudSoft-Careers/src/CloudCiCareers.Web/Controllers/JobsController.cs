using CloudCiCareers.Web.Models;
using CloudCiCareers.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CloudCiCareers.Web.Controllers;

public class JobsController : Controller
{
    private readonly IJobCatalog _catalog;
    private readonly IApplicationStore _store;
    private readonly IBlobService _blobs;

    public JobsController(
        IJobCatalog catalog,
        IApplicationStore store,
        IBlobService blobs)
    {
        _catalog = catalog;
        _store = store;
        _blobs = blobs;
    }

    public IActionResult Index()
    {
        return View(_catalog.GetAll());
    }

    public IActionResult Apply(int id)
    {
        var job = _catalog.GetById(id);
        if (job is null)
        {
            return NotFound();
        }
        ViewData["Job"] = job;
        return View(new ApplyForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(int id, ApplyForm form, IFormFile? cv,
        CancellationToken ct)
    {
        var job = _catalog.GetById(id);
        if (job is null)
        {
            return NotFound();
        }
        ViewData["Job"] = job;

        if (cv is null || cv.Length == 0)
        {
            ModelState.AddModelError(nameof(cv),
                "Please attach your CV as a PDF file.");
        }
        else if (!PdfValidation.IsPdf(cv.OpenReadStream()))
        {
            ModelState.AddModelError(nameof(cv),
                "The uploaded file is not a valid PDF document.");
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var blobName = $"{Guid.NewGuid():n}.pdf";

        await using (var upload = cv!.OpenReadStream())
        {
            await _blobs.UploadAsync(blobName, upload, ct);
        }

        var application = new Application
        {
            JobId = job.Id,
            ApplicantName = form.Name,
            ApplicantEmail = form.Email,
            CvBlobName = blobName,
            Status = ApplicationStatus.Submitted,
        };

        var saved = await _store.CreateAsync(application, ct);
        TempData["Thanks"] = "Thanks for applying! We'll be in touch.";

        return RedirectToAction("Details", "Applications",
            new { id = saved.Id });
    }
}
