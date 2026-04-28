using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CloudCiCareers.Web.Models;

public class Application
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    public int JobId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string CvBlobName { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public ApplicationStatus Status { get; set; }
    public string? Notes { get; set; }
}

public enum ApplicationStatus
{
    Submitted,
    UnderReview,
    Rejected,
    Hired,
}
