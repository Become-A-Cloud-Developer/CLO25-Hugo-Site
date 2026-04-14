using CloudSoft.Web.Models;

namespace CloudSoft.Web.Services;

public interface IApplicationService
{
    Task<OperationResult> ApplyAsync(Application application);
    Task<IEnumerable<Application>> GetMyApplicationsAsync(string candidateId);
    Task<IEnumerable<Application>> GetApplicationsForJobAsync(string jobId);
}
