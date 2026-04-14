using CloudSoft.Web.Models;
using CloudSoft.Web.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CloudSoft.Web.Repositories;

public class MongoDbApplicationRepository : IApplicationRepository
{
    private readonly IMongoCollection<Application> _applications;

    public MongoDbApplicationRepository(IOptions<MongoDbOptions> options)
    {
        var mongoDbOptions = options.Value;
        var client = new MongoClient(mongoDbOptions.ConnectionString);
        var database = client.GetDatabase(mongoDbOptions.DatabaseName);
        _applications = database.GetCollection<Application>(mongoDbOptions.ApplicationsCollectionName);
    }

    public async Task<IEnumerable<Application>> GetByCandidateIdAsync(string candidateId)
    {
        return await _applications.Find(a => a.CandidateId == candidateId).ToListAsync();
    }

    public async Task<IEnumerable<Application>> GetByJobIdAsync(string jobId)
    {
        return await _applications.Find(a => a.JobId == jobId).ToListAsync();
    }

    public async Task<bool> AddAsync(Application application)
    {
        if (application == null)
            return false;

        try
        {
            await _applications.InsertOneAsync(application);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> HasAppliedAsync(string candidateId, string jobId)
    {
        return await _applications.CountDocumentsAsync(
            a => a.CandidateId == candidateId && a.JobId == jobId) > 0;
    }
}
