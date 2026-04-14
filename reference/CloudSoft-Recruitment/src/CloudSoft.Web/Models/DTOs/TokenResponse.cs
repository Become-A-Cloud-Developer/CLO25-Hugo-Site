namespace CloudSoft.Web.Models.DTOs;

public class TokenResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
}
