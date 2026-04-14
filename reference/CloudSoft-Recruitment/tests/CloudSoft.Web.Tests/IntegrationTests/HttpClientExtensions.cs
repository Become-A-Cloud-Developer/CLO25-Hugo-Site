using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace CloudSoft.Web.Tests.IntegrationTests;

public static partial class HttpClientExtensions
{
    public static async Task<(string token, string cookieToken)> GetAntiforgeryTokensAsync(
        this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        var match = AntiforgeryInputRegex().Match(html);
        var formToken = match.Success ? match.Groups[1].Value : string.Empty;

        var cookieToken = string.Empty;
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies)
            {
                if (cookie.Contains(".AspNetCore.Antiforgery"))
                {
                    cookieToken = cookie;
                    break;
                }
            }
        }

        return (formToken, cookieToken);
    }

    public static async Task<HttpClient> LoginAsync(
        this HttpClient client, string email, string password)
    {
        var (token, _) = await client.GetAntiforgeryTokensAsync("/Account/Login");

        var formData = new Dictionary<string, string>
        {
            ["email"] = email,
            ["password"] = password,
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync("/Account/Login",
            new FormUrlEncodedContent(formData));

        return client;
    }

    public static async Task<string> GetJwtTokenAsync(this HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/token", new { email, password });
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var json = System.Text.Json.JsonDocument.Parse(content);
        return json.RootElement.GetProperty("token").GetString()!;
    }

    public static void SetBearerToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [GeneratedRegex("""<input[^>]*name="__RequestVerificationToken"[^>]*value="([^"]*)"[^>]*>""")]
    private static partial Regex AntiforgeryInputRegex();
}
