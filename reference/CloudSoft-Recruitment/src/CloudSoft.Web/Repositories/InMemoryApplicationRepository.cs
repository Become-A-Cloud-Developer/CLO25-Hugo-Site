using System.Collections.Concurrent;
using CloudSoft.Web.Models;

namespace CloudSoft.Web.Repositories;

public class InMemoryApplicationRepository : IApplicationRepository
{
    private readonly ConcurrentDictionary<string, Application> _applications = new();

    public Task<IEnumerable<Application>> GetByCandidateIdAsync(string candidateId)
    {
        var results = _applications.Values.Where(a => a.CandidateId == candidateId).ToList();
        return Task.FromResult<IEnumerable<Application>>(results);
    }

    public Task<IEnumerable<Application>> GetByJobIdAsync(string jobId)
    {
        var results = _applications.Values.Where(a => a.JobId == jobId).ToList();
        return Task.FromResult<IEnumerable<Application>>(results);
    }

    public Task<bool> AddAsync(Application application)
    {
        if (application == null)
            return Task.FromResult(false);

        application.Id = Guid.NewGuid().ToString();
        return Task.FromResult(_applications.TryAdd(application.Id, application));
    }

    public Task<bool> HasAppliedAsync(string candidateId, string jobId)
    {
        var exists = _applications.Values.Any(a => a.CandidateId == candidateId && a.JobId == jobId);
        return Task.FromResult(exists);
    }
}
