using Microsoft.AspNetCore.Mvc;
using CrimsonBookStore.Models;
using CrimsonBookStore.Services;

namespace CrimsonBookStore.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<IActionResult> GetBooks(
        [FromQuery] string? search = null,
        [FromQuery] string? isbn = null,
        [FromQuery] string? courseMajor = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var books = await _bookService.GetAvailableBooksAsync(search, isbn, courseMajor);

            // Simple pagination (for school project - keep it simple)
            var totalItems = books.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var skip = (page - 1) * pageSize;
            var pagedBooks = books.Skip(skip).Take(pageSize).ToList();

            return Ok(new
            {
                success = true,
                data = pagedBooks,
                pagination = new
                {
                    page = page,
                    pageSize = pageSize,
                    totalItems = totalItems,
                    totalPages = totalPages
                }
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while retrieving books",
                statusCode = 500
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBookById(int id)
    {
        try
        {
            var book = await _bookService.GetBookByIdAsync(id);

            if (book == null)
            {
                return NotFound(new
                {
                    success = false,
                    error = "Book not found",
                    statusCode = 404
                });
            }

            return Ok(new
            {
                success = true,
                data = book
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while retrieving the book",
                statusCode = 500
            });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchBooks([FromQuery] string q)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Search term is required",
                    statusCode = 400
                });
            }

            var books = await _bookService.SearchBooksAsync(q);

            return Ok(new
            {
                success = true,
                data = books
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while searching books",
                statusCode = 500
            });
        }
    }

    [HttpGet("stock/{isbn}/{edition}")]
    public async Task<IActionResult> GetStockCount(string isbn, string edition)
    {
        try
        {
            // Get books with this ISBN and Edition to calculate stock
            var books = await _bookService.GetAvailableBooksAsync(isbn: isbn);
            
            // Find the matching edition
            var matchingBook = books.FirstOrDefault(b => 
                b.ISBN.Equals(isbn, StringComparison.OrdinalIgnoreCase) && 
                b.Edition.Equals(edition, StringComparison.OrdinalIgnoreCase));

            if (matchingBook == null)
            {
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        isbn = isbn,
                        edition = edition,
                        stockCount = 0
                    }
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    isbn = matchingBook.ISBN,
                    edition = matchingBook.Edition,
                    stockCount = matchingBook.AvailableCount
                }
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while retrieving stock count",
                statusCode = 500
            });
        }
    }
}

