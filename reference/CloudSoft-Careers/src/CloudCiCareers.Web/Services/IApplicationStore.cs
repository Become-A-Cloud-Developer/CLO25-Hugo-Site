using CloudCiCareers.Web.Models;

namespace CloudCiCareers.Web.Services;

public interface IApplicationStore
{
    Task<IEnumerable<Application>> GetAllAsync(CancellationToken ct = default);
    Task<Application?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Application> CreateAsync(Application application, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(string id, ApplicationStatus newStatus, string? notes, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}
