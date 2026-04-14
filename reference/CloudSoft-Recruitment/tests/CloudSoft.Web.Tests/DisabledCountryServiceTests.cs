using CloudSoft.Web.Services;

namespace CloudSoft.Web.Tests;

public class DisabledCountryServiceTests
{
    [Fact]
    public async Task SearchCountriesAsync_AlwaysReturnsEmpty()
    {
        var sut = new DisabledCountryService();

        var result = await sut.SearchCountriesAsync("Norway");

        Assert.Empty(result);
    }
}
