using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CloudSoft.Web.Models;

public class Job
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [Required]
    [StringLength(100)]
    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(5000)]
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [BsonElement("location")]
    public string Location { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [BsonElement("deadline")]
    public DateTime Deadline { get; set; }

    [BsonElement("postedAt")]
    public DateTime PostedAt { get; set; }

    [BsonElement("postedByUserId")]
    public string PostedByUserId { get; set; } = string.Empty;

    [BsonElement("postedByName")]
    public string PostedByName { get; set; } = string.Empty;
}
