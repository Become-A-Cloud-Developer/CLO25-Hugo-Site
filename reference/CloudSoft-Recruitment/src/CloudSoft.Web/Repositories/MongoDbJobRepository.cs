using CloudSoft.Web.Models;
using CloudSoft.Web.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CloudSoft.Web.Repositories;

public class MongoDbJobRepository : IJobRepository
{
    private readonly IMongoCollection<Job> _jobs;

    public MongoDbJobRepository(IOptions<MongoDbOptions> options)
    {
        var mongoDbOptions = options.Value;
        var client = new MongoClient(mongoDbOptions.ConnectionString);
        var database = client.GetDatabase(mongoDbOptions.DatabaseName);
        _jobs = database.GetCollection<Job>(mongoDbOptions.JobsCollectionName);
    }

    public async Task<IEnumerable<Job>> GetAllAsync()
    {
        return await _jobs.Find(_ => true).ToListAsync();
    }

    public async Task<Job?> GetByIdAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        return await _jobs.Find(j => j.Id == id).FirstOrDefaultAsync();
    }

    public async Task<bool> AddAsync(Job job)
    {
        if (job == null)
            return false;

        try
        {
            await _jobs.InsertOneAsync(job);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateAsync(Job job)
    {
        if (job == null || string.IsNullOrEmpty(job.Id))
            return false;

        try
        {
            var result = await _jobs.ReplaceOneAsync(j => j.Id == job.Id, job);
            return result.ModifiedCount > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        try
        {
            var result = await _jobs.DeleteOneAsync(j => j.Id == id);
            return result.DeletedCount > 0;
        }
        catch
        {
            return false;
        }
    }
}
