using System.Collections.Concurrent;
using CloudSoft.Web.Models;

namespace CloudSoft.Web.Repositories;

public class InMemoryJobRepository : IJobRepository
{
    private readonly ConcurrentDictionary<string, Job> _jobs = new();

    public Task<IEnumerable<Job>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<Job>>(_jobs.Values.ToList());
    }

    public Task<Job?> GetByIdAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
            return Task.FromResult<Job?>(null);

        _jobs.TryGetValue(id, out var job);
        return Task.FromResult(job);
    }

    public Task<bool> AddAsync(Job job)
    {
        if (job == null)
            return Task.FromResult(false);

        job.Id = Guid.NewGuid().ToString();
        return Task.FromResult(_jobs.TryAdd(job.Id, job));
    }

    public Task<bool> UpdateAsync(Job job)
    {
        if (job == null || string.IsNullOrEmpty(job.Id))
            return Task.FromResult(false);

        if (!_jobs.ContainsKey(job.Id))
            return Task.FromResult(false);

        _jobs[job.Id] = job;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
            return Task.FromResult(false);

        return Task.FromResult(_jobs.TryRemove(id, out _));
    }
}
