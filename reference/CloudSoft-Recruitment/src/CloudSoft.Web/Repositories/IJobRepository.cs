using CloudSoft.Web.Models;

namespace CloudSoft.Web.Repositories;

public interface IJobRepository
{
    Task<IEnumerable<Job>> GetAllAsync();
    Task<Job?> GetByIdAsync(string id);
    Task<bool> AddAsync(Job job);
    Task<bool> UpdateAsync(Job job);
    Task<bool> DeleteAsync(string id);
}
