using CloudSoft.Web.Models;
using CloudSoft.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CloudSoft.Web.Controllers;

public class JobController : Controller
{
    private readonly IJobService _jobService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<JobController> _logger;

    public JobController(IJobService jobService, UserManager<ApplicationUser> userManager, ILogger<JobController> logger)
    {
        _jobService = jobService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var jobs = await _jobService.GetAllJobsAsync();
        return View(jobs);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var job = await _jobService.GetJobByIdAsync(id);
        if (job == null)
        {
            return NotFound();
        }

        return View(job);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Description,Location,Deadline")] Job job)
    {
        if (!ModelState.IsValid)
        {
            return View(job);
        }

        var user = await _userManager.GetUserAsync(User);
        job.PostedByUserId = user!.Id;
        job.PostedByName = user.DisplayName ?? user.Email ?? "Admin";

        var result = await _jobService.CreateJobAsync(job);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(job);
        }

        _logger.LogInformation("Job {JobId} created by admin {UserId}", job.Id, user.Id);
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(string id)
    {
        var job = await _jobService.GetJobByIdAsync(id);
        if (job == null)
        {
            return NotFound();
        }

        return View(job);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("Title,Description,Location,Deadline")] Job job)
    {
        if (!ModelState.IsValid)
        {
            return View(job);
        }

        job.Id = id;

        var result = await _jobService.UpdateJobAsync(job);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(job);
        }

        _logger.LogInformation("Job {JobId} edited", id);
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var job = await _jobService.GetJobByIdAsync(id);
        if (job == null)
        {
            return NotFound();
        }

        return View(job);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var result = await _jobService.DeleteJobAsync(id);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("Job {JobId} deleted", id);
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
