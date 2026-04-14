using CloudSoft.Web.Models;
using CloudSoft.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CloudSoft.Web.Controllers;

public class ApplicationController : Controller
{
    private readonly IApplicationService _applicationService;
    private readonly IJobService _jobService;
    private readonly IBlobService _blobService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ApplicationController> _logger;

    public ApplicationController(
        IApplicationService applicationService,
        IJobService jobService,
        IBlobService blobService,
        UserManager<ApplicationUser> userManager,
        ILogger<ApplicationController> logger)
    {
        _applicationService = applicationService;
        _jobService = jobService;
        _blobService = blobService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Apply(string id)
    {
        var job = await _jobService.GetJobByIdAsync(id);
        if (job == null)
        {
            return NotFound();
        }

        ViewBag.Job = job;
        return View(new Application { JobId = id });
    }

    [HttpPost]
    [Authorize(Roles = "Candidate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply([Bind("JobId,CoverLetter")] Application application, IFormFile? cvFile)
    {
        var job = await _jobService.GetJobByIdAsync(application.JobId);
        if (job == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        // Assemble all denormalized data server-side
        application.JobTitle = job.Title;
        application.CandidateId = user.Id;
        application.CandidateEmail = user.Email ?? string.Empty;
        application.CandidateName = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(application.CandidateName))
        {
            application.CandidateName = user.Email ?? "Unknown";
        }

        // Remove validation errors for server-populated fields
        ModelState.Remove("JobTitle");
        ModelState.Remove("CandidateId");
        ModelState.Remove("CandidateEmail");
        ModelState.Remove("CandidateName");

        // Validate and upload CV if provided
        if (cvFile != null)
        {
            var validationError = ValidatePdf(cvFile);
            if (validationError != null)
            {
                ModelState.AddModelError("cvFile", validationError);
            }
            else
            {
                // Read first 5 bytes to check PDF magic number
                using var checkStream = cvFile.OpenReadStream();
                var header = new byte[5];
                var bytesRead = await checkStream.ReadAsync(header.AsMemory(0, 5));
                if (bytesRead < 5 || System.Text.Encoding.ASCII.GetString(header) != "%PDF-")
                {
                    ModelState.AddModelError("cvFile", "File does not appear to be a valid PDF.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Job = job;
            return View(application);
        }

        // Upload CV
        if (cvFile != null)
        {
            var fileName = $"{Guid.NewGuid()}.pdf";
            using var uploadStream = cvFile.OpenReadStream();
            await _blobService.UploadAsync(uploadStream, fileName, "application/pdf");
            application.CvUrl = fileName;
        }

        var result = await _applicationService.ApplyAsync(application);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            ViewBag.Job = job;
            return View(application);
        }

        _logger.LogInformation("Application submitted for job {JobId} by candidate {CandidateId}", application.JobId, application.CandidateId);
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(MyApplications));
    }

    [HttpGet]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> MyApplications()
    {
        var user = await _userManager.GetUserAsync(User);
        var applications = await _applicationService.GetMyApplicationsAsync(user!.Id);
        return View(applications);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ForJob(string id)
    {
        var job = await _jobService.GetJobByIdAsync(id);
        if (job == null)
        {
            return NotFound();
        }

        ViewBag.Job = job;
        var applications = await _applicationService.GetApplicationsForJobAsync(id);
        return View(applications);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DownloadCv(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return NotFound();
        }

        var stream = await _blobService.DownloadAsync(fileName);
        if (stream == null)
        {
            return NotFound();
        }

        _logger.LogInformation("CV {FileName} downloaded by admin {UserId}", fileName, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        return File(stream, "application/pdf", fileName);
    }

    private static string? ValidatePdf(IFormFile file)
    {
        if (Path.GetExtension(file.FileName).ToLowerInvariant() != ".pdf")
        {
            return "Only PDF files are accepted.";
        }

        if (file.ContentType != "application/pdf")
        {
            return "Only PDF files are accepted.";
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return "File size must not exceed 5 MB.";
        }

        return null;
    }
}
