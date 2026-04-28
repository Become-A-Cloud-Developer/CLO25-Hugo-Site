using CloudCiApi.Models;

namespace CloudCiApi.Services;

public interface IQuoteStore
{
    IReadOnlyList<Quote> GetAll();
    Quote? GetById(int id);
    Quote Add(string author, string text);
}
