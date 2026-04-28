using System.Collections.Concurrent;
using CloudCiCareers.Web.Models;

namespace CloudCiCareers.Web.Services;

public class InMemoryApplicationStore : IApplicationStore
{
    private readonly ConcurrentDictionary<string, Application> _applications = new();

    public Task<IEnumerable<Application>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IEnumerable<Application>>(
            _applications.Values.OrderByDescending(a => a.SubmittedAt).ToList());

    public Task<Application?> GetByIdAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(_applications.TryGetValue(id, out var application) ? application : null);

    public Task<Application> CreateAsync(Application application, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(application.Id))
        {
            application.Id = Guid.NewGuid().ToString("n");
        }
        if (application.SubmittedAt == default)
        {
            application.SubmittedAt = DateTimeOffset.UtcNow;
        }
        _applications[application.Id] = application;
        return Task.FromResult(application);
    }

    public Task<bool> UpdateStatusAsync(string id, ApplicationStatus newStatus, string? notes,
        CancellationToken ct = default)
    {
        if (!_applications.TryGetValue(id, out var application))
        {
            return Task.FromResult(false);
        }
        application.Status = newStatus;
        application.Notes = notes;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(_applications.TryRemove(id, out _));
}
