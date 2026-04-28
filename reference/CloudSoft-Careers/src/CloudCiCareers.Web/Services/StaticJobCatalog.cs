using CloudCiCareers.Web.Models;

namespace CloudCiCareers.Web.Services;

public class StaticJobCatalog : IJobCatalog
{
    private static readonly IReadOnlyList<Job> _jobs = new List<Job>
    {
        new(1, "Cloud Engineer", "Platform",
            "Design, deploy, and operate Azure infrastructure for the product platform.",
            DateTimeOffset.UtcNow.AddDays(-7)),
        new(2, "Backend Developer", "Engineering",
            "Build and own ASP.NET Core services that back the customer-facing apps.",
            DateTimeOffset.UtcNow.AddDays(-5)),
        new(3, "DevOps Specialist", "Platform",
            "Build and maintain CI/CD, observability, and incident-response tooling.",
            DateTimeOffset.UtcNow.AddDays(-3)),
        new(4, "Site Reliability Engineer", "Platform",
            "Define SLOs, run game days, and keep production boring.",
            DateTimeOffset.UtcNow.AddDays(-1)),
    };

    public IReadOnlyList<Job> GetAll() => _jobs;

    public Job? GetById(int id) => _jobs.FirstOrDefault(j => j.Id == id);
}
