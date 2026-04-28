using CloudCiApi.Dtos;
using CloudCiApi.Models;
using CloudCiApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudCiApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuotesController : ControllerBase
{
    private readonly IQuoteStore _store;

    public QuotesController(IQuoteStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<IEnumerable<QuoteDto>> GetAll()
    {
        var dtos = _store.GetAll().Select(ToDto);
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public ActionResult<QuoteDto> GetById(int id)
    {
        var quote = _store.GetById(id);
        if (quote is null)
        {
            return NotFound();
        }
        return Ok(ToDto(quote));
    }

    [HttpPost]
    public ActionResult<QuoteDto> Create([FromBody] CreateQuoteRequest request)
    {
        var quote = _store.Add(request.Author, request.Text);
        var dto = ToDto(quote);

        return CreatedAtAction(
            nameof(GetById),
            new { id = quote.Id },
            dto);
    }

    private static QuoteDto ToDto(Quote q) => new()
    {
        Id = q.Id,
        Author = q.Author,
        Text = q.Text,
        CreatedAt = q.CreatedAt,
    };
}
