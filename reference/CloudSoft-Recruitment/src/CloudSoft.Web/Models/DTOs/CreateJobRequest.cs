using System.ComponentModel.DataAnnotations;

namespace CloudSoft.Web.Models.DTOs;

public class CreateJobRequest
{
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(5000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime Deadline { get; set; }

    public Job ToJob() => new()
    {
        Title = Title,
        Description = Description,
        Location = Location,
        Deadline = Deadline
    };
}
