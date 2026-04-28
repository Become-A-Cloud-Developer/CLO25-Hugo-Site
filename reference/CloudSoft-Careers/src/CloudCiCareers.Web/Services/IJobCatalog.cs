using CloudCiCareers.Web.Models;

namespace CloudCiCareers.Web.Services;

public interface IJobCatalog
{
    IReadOnlyList<Job> GetAll();
    Job? GetById(int id);
}
