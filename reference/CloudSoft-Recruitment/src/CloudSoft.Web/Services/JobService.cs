using CloudSoft.Web.Models;
using CloudSoft.Web.Repositories;
using Microsoft.Extensions.Logging;

namespace CloudSoft.Web.Services;

public class JobService : IJobService
{
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<JobService> _logger;

    public JobService(IJobRepository jobRepository, ILogger<JobService> logger)
    {
        _jobRepository = jobRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Job>> GetAllJobsAsync()
    {
        return await _jobRepository.GetAllAsync();
    }

    public async Task<Job?> GetJobByIdAsync(string id)
    {
        return await _jobRepository.GetByIdAsync(id);
    }

    public async Task<OperationResult> CreateJobAsync(Job job)
    {
        if (job.Deadline <= DateTime.UtcNow)
        {
            return OperationResult.Failure("Deadline must be in the future.");
        }

        job.PostedAt = DateTime.UtcNow;

        var success = await _jobRepository.AddAsync(job);
        if (success)
        {
            _logger.LogInformation("Job created: {JobTitle} by {UserId}", job.Title, job.PostedByUserId);
            return OperationResult.Success("Job posted successfully.");
        }

        return OperationResult.Failure("Failed to create job.");
    }

    public async Task<OperationResult> UpdateJobAsync(Job job)
    {
        var existing = await _jobRepository.GetByIdAsync(job.Id!);
        if (existing == null)
        {
            _logger.LogWarning("Job not found: {JobId}", job.Id);
            return OperationResult.Failure("Job not found.");
        }

        existing.Title = job.Title;
        existing.Description = job.Description;
        existing.Location = job.Location;
        existing.Deadline = job.Deadline;

        var success = await _jobRepository.UpdateAsync(existing);
        if (success)
        {
            _logger.LogInformation("Job updated: {JobId}", job.Id);
            return OperationResult.Success("Job updated successfully.");
        }

        return OperationResult.Failure("Failed to update job.");
    }

    public async Task<OperationResult> DeleteJobAsync(string id)
    {
        var existing = await _jobRepository.GetByIdAsync(id);
        if (existing == null)
        {
            _logger.LogWarning("Job not found: {JobId}", id);
            return OperationResult.Failure("Job not found.");
        }

        var success = await _jobRepository.DeleteAsync(id);
        if (success)
        {
            _logger.LogInformation("Job deleted: {JobId}", id);
            return OperationResult.Success("Job deleted successfully.");
        }

        return OperationResult.Failure("Failed to delete job.");
    }
}
