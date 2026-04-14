using CloudSoft.Web.Models;
using CloudSoft.Web.Repositories;
using CloudSoft.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CloudSoft.Web.Tests;

public class ApplicationServiceTests
{
    private readonly IApplicationService _sut;
    private readonly InMemoryJobRepository _jobRepository;
    private readonly IJobService _jobService;

    public ApplicationServiceTests()
    {
        var applicationRepository = new InMemoryApplicationRepository();
        _sut = new ApplicationService(applicationRepository, NullLogger<ApplicationService>.Instance);
        _jobRepository = new InMemoryJobRepository();
        _jobService = new JobService(_jobRepository, NullLogger<JobService>.Instance);
    }

    private async Task<Job> CreateTestJob()
    {
        var job = new Job
        {
            Title = "Test Developer",
            Description = "Test description",
            Location = "Oslo",
            Deadline = DateTime.UtcNow.AddDays(30)
        };
        await _jobService.CreateJobAsync(job);
        var jobs = await _jobService.GetAllJobsAsync();
        return jobs.First();
    }

    [Fact]
    public async Task ApplyAsync_FirstApplication_ReturnsSuccess()
    {
        var job = await CreateTestJob();
        var application = new Application
        {
            JobId = job.Id!,
            JobTitle = job.Title,
            CandidateId = "candidate-1",
            CandidateEmail = "candidate@example.com",
            CandidateName = "Test Candidate",
            CoverLetter = "I am very interested in this position."
        };

        var result = await _sut.ApplyAsync(application);

        Assert.True(result.IsSuccess);
        Assert.Contains("successfully", result.Message);
    }

    [Fact]
    public async Task ApplyAsync_DuplicateApplication_ReturnsFailure()
    {
        var job = await CreateTestJob();
        var application1 = new Application
        {
            JobId = job.Id!,
            JobTitle = job.Title,
            CandidateId = "candidate-1",
            CandidateEmail = "candidate@example.com",
            CandidateName = "Test Candidate",
            CoverLetter = "First application."
        };
        await _sut.ApplyAsync(application1);

        var application2 = new Application
        {
            JobId = job.Id!,
            JobTitle = job.Title,
            CandidateId = "candidate-1",
            CandidateEmail = "candidate@example.com",
            CandidateName = "Test Candidate",
            CoverLetter = "Second application."
        };
        var result = await _sut.ApplyAsync(application2);

        Assert.False(result.IsSuccess);
        Assert.Contains("already applied", result.Message);
    }

    [Fact]
    public async Task GetMyApplicationsAsync_ReturnsOnlyCandidatesApplications()
    {
        var job = await CreateTestJob();

        var app1 = new Application
        {
            JobId = job.Id!, JobTitle = job.Title,
            CandidateId = "candidate-1", CandidateEmail = "c1@test.com",
            CandidateName = "Candidate 1", CoverLetter = "Letter 1"
        };
        var app2 = new Application
        {
            JobId = job.Id!, JobTitle = job.Title,
            CandidateId = "candidate-2", CandidateEmail = "c2@test.com",
            CandidateName = "Candidate 2", CoverLetter = "Letter 2"
        };
        await _sut.ApplyAsync(app1);
        await _sut.ApplyAsync(app2);

        var myApps = await _sut.GetMyApplicationsAsync("candidate-1");

        Assert.Single(myApps);
        Assert.Equal("candidate-1", myApps.First().CandidateId);
    }

    [Fact]
    public async Task GetApplicationsForJobAsync_ReturnsOnlyThatJobsApplications()
    {
        var job1 = await CreateTestJob();
        // Create a second job — use the returned object's Id directly
        var job2 = new Job
        {
            Title = "Another Role",
            Description = "Another description",
            Location = "Bergen",
            Deadline = DateTime.UtcNow.AddDays(30)
        };
        await _jobService.CreateJobAsync(job2);

        var app1 = new Application
        {
            JobId = job1.Id!, JobTitle = job1.Title,
            CandidateId = "candidate-1", CandidateEmail = "c1@test.com",
            CandidateName = "Candidate 1", CoverLetter = "Letter for job 1"
        };
        var app2 = new Application
        {
            JobId = job2.Id!, JobTitle = job2.Title,
            CandidateId = "candidate-2", CandidateEmail = "c2@test.com",
            CandidateName = "Candidate 2", CoverLetter = "Letter for job 2"
        };
        await _sut.ApplyAsync(app1);
        await _sut.ApplyAsync(app2);

        var jobApps = await _sut.GetApplicationsForJobAsync(job1.Id!);

        Assert.Single(jobApps);
        Assert.Equal(job1.Id, jobApps.First().JobId);
    }
}
