using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CloudSoft.Web.Models;

public class Application
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [BsonElement("jobId")]
    public string JobId { get; set; } = string.Empty;

    [BsonElement("jobTitle")]
    public string JobTitle { get; set; } = string.Empty;

    [Required]
    [BsonElement("candidateId")]
    public string CandidateId { get; set; } = string.Empty;

    [BsonElement("candidateEmail")]
    public string CandidateEmail { get; set; } = string.Empty;

    [BsonElement("candidateName")]
    public string CandidateName { get; set; } = string.Empty;

    [Required]
    [StringLength(5000)]
    [BsonElement("coverLetter")]
    public string CoverLetter { get; set; } = string.Empty;

    [BsonElement("cvUrl")]
    public string? CvUrl { get; set; }

    [BsonElement("appliedAt")]
    public DateTime AppliedAt { get; set; }
}
