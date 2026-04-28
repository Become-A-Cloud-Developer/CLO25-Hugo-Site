using CloudCiApi.Models;

namespace CloudCiApi.Services;

public class InMemoryQuoteStore : IQuoteStore
{
    private readonly List<Quote> _quotes = new();
    private int _nextId = 1;
    private readonly object _gate = new();

    public InMemoryQuoteStore()
    {
        Add("Edsger W. Dijkstra",
            "Simplicity is prerequisite for reliability.");
        Add("Grace Hopper",
            "The most dangerous phrase in the language is 'we've always done it this way.'");
        Add("Alan Kay",
            "The best way to predict the future is to invent it.");
        Add("Linus Torvalds",
            "Talk is cheap. Show me the code.");
    }

    public IReadOnlyList<Quote> GetAll()
    {
        lock (_gate)
        {
            return _quotes.ToList();
        }
    }

    public Quote? GetById(int id)
    {
        lock (_gate)
        {
            return _quotes.FirstOrDefault(q => q.Id == id);
        }
    }

    public Quote Add(string author, string text)
    {
        lock (_gate)
        {
            var quote = new Quote
            {
                Id = _nextId++,
                Author = author,
                Text = text,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            _quotes.Add(quote);
            return quote;
        }
    }
}
