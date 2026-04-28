namespace CloudCiCareers.Web.Models;

public record Job(
    int Id,
    string Title,
    string Department,
    string Description,
    DateTimeOffset Posted);
