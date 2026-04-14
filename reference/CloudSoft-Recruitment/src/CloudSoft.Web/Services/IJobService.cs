using CloudSoft.Web.Models;

namespace CloudSoft.Web.Services;

public interface IJobService
{
    Task<IEnumerable<Job>> GetAllJobsAsync();
    Task<Job?> GetJobByIdAsync(string id);
    Task<OperationResult> CreateJobAsync(Job job);
    Task<OperationResult> UpdateJobAsync(Job job);
    Task<OperationResult> DeleteJobAsync(string id);
}
