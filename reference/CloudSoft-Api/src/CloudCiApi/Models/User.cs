namespace CloudCiApi.Models;

public record User(string Username, string Password, string? Role = null);
