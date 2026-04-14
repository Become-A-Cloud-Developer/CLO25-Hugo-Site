using CloudSoft.Web.Models.DTOs;
using CloudSoft.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudSoft.Web.Controllers.Api;

[ApiController]
[Route("api/jobs")]
[IgnoreAntiforgeryToken]
public class JobApiController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly IApplicationService _applicationService;
    private readonly ILogger<JobApiController> _logger;

    public JobApiController(IJobService jobService, IApplicationService applicationService, ILogger<JobApiController> logger)
    {
        _jobService = jobService;
        _applicationService = applicationService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var jobs = await _jobService.GetAllJobsAsync();
        return Ok(jobs.Select(JobResponse.FromJob));
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(string id)
    {
        var job = await _jobService.GetJobByIdAsync(id);
        if (job == null) return NotFound();
        return Ok(JobResponse.FromJob(job));
    }

    [HttpPost]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Bearer,Identity.Application")]
    public async Task<IActionResult> Create([FromBody] CreateJobRequest request)
    {
        var job = request.ToJob();
        job.PostedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        job.PostedByName = User.Identity?.Name ?? "API User";

        var result = await _jobService.CreateJobAsync(job);
        if (!result.IsSuccess) return BadRequest(new { error = result.Message });

        _logger.LogInformation("Job {JobTitle} created via API by {UserId}", job.Title, job.PostedByUserId);
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, JobResponse.FromJob(job));
    }

    [HttpGet("{id}/applications")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "Bearer,Identity.Application")]
    public async Task<IActionResult> GetApplications(string id)
    {
        var job = await _jobService.GetJobByIdAsync(id);
        if (job == null) return NotFound();

        var applications = await _applicationService.GetApplicationsForJobAsync(id);
        return Ok(applications.Select(ApplicationResponse.FromApplication));
    }
}
