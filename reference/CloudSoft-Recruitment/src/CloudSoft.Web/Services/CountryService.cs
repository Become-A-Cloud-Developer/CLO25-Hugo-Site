using System.Text.Json;

namespace CloudSoft.Web.Services;

// Registration needed in Program.cs:
// builder.Services.AddHttpClient<ICountryService, CountryService>(client =>
// {
//     client.BaseAddress = new Uri("https://restcountries.com/");
//     client.Timeout = TimeSpan.FromSeconds(5);
// });

public class CountryService : ICountryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CountryService> _logger;

    public CountryService(HttpClient httpClient, ILogger<CountryService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> SearchCountriesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<string>();

        try
        {
            var response = await _httpClient.GetAsync($"v3.1/name/{Uri.EscapeDataString(query)}?fields=name");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("REST Countries API returned {StatusCode} for query {Query}", response.StatusCode, query);
                return Enumerable.Empty<string>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var countries = JsonSerializer.Deserialize<List<CountryApiResponse>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return countries?.Select(c => c.Name?.Common ?? "").Where(n => !string.IsNullOrEmpty(n)).OrderBy(n => n)
                   ?? Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch countries for query {Query}", query);
            return Enumerable.Empty<string>();
        }
    }

    private class CountryApiResponse
    {
        public CountryName? Name { get; set; }
    }

    private class CountryName
    {
        public string Common { get; set; } = string.Empty;
    }
}
