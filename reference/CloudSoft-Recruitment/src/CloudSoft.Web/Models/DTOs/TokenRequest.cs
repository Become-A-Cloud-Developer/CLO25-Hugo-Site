using System.ComponentModel.DataAnnotations;

namespace CloudSoft.Web.Models.DTOs;

public class TokenRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
