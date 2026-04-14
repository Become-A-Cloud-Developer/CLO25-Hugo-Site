namespace CloudSoft.Web.Models.DTOs;

public class ApplicationResponse
{
    public string Id { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public string CoverLetter { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }

    public static ApplicationResponse FromApplication(Application application) => new()
    {
        Id = application.Id ?? string.Empty,
        JobId = application.JobId,
        JobTitle = application.JobTitle,
        CandidateEmail = application.CandidateEmail,
        CandidateName = application.CandidateName,
        CoverLetter = application.CoverLetter,
        AppliedAt = application.AppliedAt
    };
}
