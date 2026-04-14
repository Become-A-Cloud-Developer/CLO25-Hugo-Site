using CloudSoft.Web.Models;
using CloudSoft.Web.Repositories;
using CloudSoft.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CloudSoft.Web.Tests;

public class JobServiceTests
{
    private readonly IJobService _sut;

    public JobServiceTests()
    {
        var repository = new InMemoryJobRepository();
        _sut = new JobService(repository, NullLogger<JobService>.Instance);
    }

    [Fact]
    public async Task CreateJobAsync_ValidJob_ReturnsSuccess()
    {
        var job = new Job
        {
            Title = "Software Developer",
            Description = "Build amazing software",
            Location = "Oslo",
            Deadline = DateTime.UtcNow.AddDays(30)
        };

        var result = await _sut.CreateJobAsync(job);

        Assert.True(result.IsSuccess);
        Assert.Contains("successfully", result.Message);
    }

    [Fact]
    public async Task CreateJobAsync_PastDeadline_ReturnsFailure()
    {
        var job = new Job
        {
            Title = "Software Developer",
            Description = "Build amazing software",
            Location = "Oslo",
            Deadline = DateTime.UtcNow.AddDays(-1)
        };

        var result = await _sut.CreateJobAsync(job);

        Assert.False(result.IsSuccess);
        Assert.Contains("future", result.Message);
    }

    [Fact]
    public async Task GetAllJobsAsync_ReturnsAllJobs()
    {
        var job1 = new Job { Title = "Dev 1", Description = "Desc 1", Location = "Oslo", Deadline = DateTime.UtcNow.AddDays(30) };
        var job2 = new Job { Title = "Dev 2", Description = "Desc 2", Location = "Bergen", Deadline = DateTime.UtcNow.AddDays(30) };
        await _sut.CreateJobAsync(job1);
        await _sut.CreateJobAsync(job2);

        var jobs = await _sut.GetAllJobsAsync();

        Assert.Equal(2, jobs.Count());
    }

    [Fact]
    public async Task GetJobByIdAsync_ExistingId_ReturnsJob()
    {
        var job = new Job { Title = "Dev", Description = "Desc", Location = "Oslo", Deadline = DateTime.UtcNow.AddDays(30) };
        await _sut.CreateJobAsync(job);
        var allJobs = await _sut.GetAllJobsAsync();
        var id = allJobs.First().Id!;

        var found = await _sut.GetJobByIdAsync(id);

        Assert.NotNull(found);
        Assert.Equal("Dev", found.Title);
    }

    [Fact]
    public async Task GetJobByIdAsync_NonExistingId_ReturnsNull()
    {
        var found = await _sut.GetJobByIdAsync("non-existing-id");

        Assert.Null(found);
    }

    [Fact]
    public async Task UpdateJobAsync_ExistingJob_ReturnsSuccess()
    {
        var job = new Job { Title = "Dev", Description = "Desc", Location = "Oslo", Deadline = DateTime.UtcNow.AddDays(30) };
        await _sut.CreateJobAsync(job);
        var allJobs = await _sut.GetAllJobsAsync();
        var existing = allJobs.First();

        existing.Title = "Senior Dev";
        var result = await _sut.UpdateJobAsync(existing);

        Assert.True(result.IsSuccess);
        var updated = await _sut.GetJobByIdAsync(existing.Id!);
        Assert.Equal("Senior Dev", updated!.Title);
    }

    [Fact]
    public async Task UpdateJobAsync_NonExistingJob_ReturnsFailure()
    {
        var job = new Job { Id = "non-existing-id", Title = "Dev", Description = "Desc", Location = "Oslo", Deadline = DateTime.UtcNow.AddDays(30) };

        var result = await _sut.UpdateJobAsync(job);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task DeleteJobAsync_ExistingJob_ReturnsSuccess()
    {
        var job = new Job { Title = "Dev", Description = "Desc", Location = "Oslo", Deadline = DateTime.UtcNow.AddDays(30) };
        await _sut.CreateJobAsync(job);
        var allJobs = await _sut.GetAllJobsAsync();
        var id = allJobs.First().Id!;

        var result = await _sut.DeleteJobAsync(id);

        Assert.True(result.IsSuccess);
        var deleted = await _sut.GetJobByIdAsync(id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteJobAsync_NonExistingJob_ReturnsFailure()
    {
        var result = await _sut.DeleteJobAsync("non-existing-id");

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Message);
    }
}
