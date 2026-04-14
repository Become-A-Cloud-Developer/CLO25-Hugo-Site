namespace CloudSoft.Web.Services;

public interface ICountryService
{
    Task<IEnumerable<string>> SearchCountriesAsync(string query);
}
