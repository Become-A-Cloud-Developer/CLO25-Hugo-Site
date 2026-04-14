using CloudSoft.Web.Models;

namespace CloudSoft.Web.Repositories;

public interface IApplicationRepository
{
    Task<IEnumerable<Application>> GetByCandidateIdAsync(string candidateId);
    Task<IEnumerable<Application>> GetByJobIdAsync(string jobId);
    Task<bool> AddAsync(Application application);
    Task<bool> HasAppliedAsync(string candidateId, string jobId);
}
