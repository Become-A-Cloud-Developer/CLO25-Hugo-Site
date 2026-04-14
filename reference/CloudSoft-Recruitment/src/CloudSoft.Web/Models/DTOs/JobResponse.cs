namespace CloudSoft.Web.Models.DTOs;

public class JobResponse
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public DateTime PostedAt { get; set; }
    public string PostedByName { get; set; } = string.Empty;

    public static JobResponse FromJob(Job job) => new()
    {
        Id = job.Id ?? string.Empty,
        Title = job.Title,
        Description = job.Description,
        Location = job.Location,
        Deadline = job.Deadline,
        PostedAt = job.PostedAt,
        PostedByName = job.PostedByName
    };
}
