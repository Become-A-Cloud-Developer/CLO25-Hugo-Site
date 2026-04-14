using CloudSoft.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CloudSoft.Web.Tests;

public class CountryServiceTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage? _response;
        private readonly bool _throwException;

        public FakeHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public FakeHttpMessageHandler(bool throwException = true)
        {
            _throwException = throwException;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_throwException) throw new HttpRequestException("Connection failed");
            return Task.FromResult(_response!);
        }
    }

    private static CountryService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://restcountries.com/") };
        return new CountryService(httpClient, NullLogger<CountryService>.Instance);
    }

    [Fact]
    public async Task SearchCountriesAsync_ValidQuery_ReturnsMatchingCountries()
    {
        var json = """[{"name":{"common":"Norway"}},{"name":{"common":"Northern Mariana Islands"}}]""";
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
        var service = CreateService(handler);
        var result = await service.SearchCountriesAsync("nor");
        Assert.Contains("Norway", result);
    }

    [Fact]
    public async Task SearchCountriesAsync_NoResults_ReturnsEmptyList()
    {
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound,
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        });
        var service = CreateService(handler);
        var result = await service.SearchCountriesAsync("zzzzzzz");
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCountriesAsync_ApiThrowsException_ReturnsEmptyList()
    {
        var handler = new FakeHttpMessageHandler(throwException: true);
        var service = CreateService(handler);
        var result = await service.SearchCountriesAsync("nor");
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCountriesAsync_ApiReturnsServerError_ReturnsEmptyList()
    {
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        });
        var service = CreateService(handler);
        var result = await service.SearchCountriesAsync("nor");
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCountriesAsync_EmptyQuery_ReturnsEmptyList()
    {
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
        });
        var service = CreateService(handler);
        var result = await service.SearchCountriesAsync("");
        Assert.Empty(result);
    }
}
