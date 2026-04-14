using CloudSoft.Web.Models;
using CloudSoft.Web.Repositories;
using Microsoft.Extensions.Logging;

namespace CloudSoft.Web.Services;

public class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(IApplicationRepository applicationRepository, ILogger<ApplicationService> logger)
    {
        _applicationRepository = applicationRepository;
        _logger = logger;
    }

    public async Task<OperationResult> ApplyAsync(Application application)
    {
        var hasApplied = await _applicationRepository.HasAppliedAsync(application.CandidateId, application.JobId);
        if (hasApplied)
        {
            _logger.LogWarning("Duplicate application attempt by {CandidateId} for job {JobId}", application.CandidateId, application.JobId);
            return OperationResult.Failure("You have already applied to this job.");
        }

        application.AppliedAt = DateTime.UtcNow;

        var success = await _applicationRepository.AddAsync(application);
        if (success)
        {
            _logger.LogInformation("Application submitted by {CandidateId} for job {JobId}", application.CandidateId, application.JobId);
            return OperationResult.Success("Application submitted successfully.");
        }

        return OperationResult.Failure("Failed to submit application.");
    }

    public async Task<IEnumerable<Application>> GetMyApplicationsAsync(string candidateId)
    {
        return await _applicationRepository.GetByCandidateIdAsync(candidateId);
    }

    public async Task<IEnumerable<Application>> GetApplicationsForJobAsync(string jobId)
    {
        return await _applicationRepository.GetByJobIdAsync(jobId);
    }
}
