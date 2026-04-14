namespace CloudSoft.Web.Services;

public class DisabledCountryService : ICountryService
{
    public Task<IEnumerable<string>> SearchCountriesAsync(string query)
    {
        return Task.FromResult(Enumerable.Empty<string>());
    }
}
