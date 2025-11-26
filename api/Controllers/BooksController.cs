using Microsoft.AspNetCore.Mvc;
using CrimsonBookStore.Models;
using CrimsonBookStore.Services;
using CrimsonBookStore.Helpers;

namespace CrimsonBookStore.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly ISessionService _sessionService;

    public BooksController(IBookService bookService, ISessionService sessionService)
    {
        _bookService = bookService;
        _sessionService = sessionService;
    }

    private bool IsAdmin(CurrentUser? user)
    {
        return user != null && user.UserType == "Admin";
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
            // Validate pagination parameters
            if (page < 1)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Page number must be greater than 0",
                    statusCode = 400
                });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Page size must be between 1 and 100",
                    statusCode = 400
                });
            }

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
            // Validate ID
            if (id <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Invalid book ID",
                    statusCode = 400
                });
            }

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

    [HttpGet("copies/{isbn}/{edition}")]
    public async Task<IActionResult> GetBookCopies(string isbn, string edition)
    {
        try
        {
            var copies = await _bookService.GetBookCopiesAsync(isbn, edition);

            return Ok(new
            {
                success = true,
                data = copies
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while retrieving book copies",
                statusCode = 500
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateBook([FromBody] CreateBookRequest? request)
    {
        try
        {
            // Check authentication and admin access
            var currentUser = AuthHelper.GetCurrentUser(Request, _sessionService);
            if (currentUser == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    error = "Not authenticated",
                    statusCode = 401
                });
            }

            if (!IsAdmin(currentUser))
            {
                return StatusCode(403, new
                {
                    success = false,
                    error = "Admin access required",
                    statusCode = 403
                });
            }

            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Request body is required",
                    statusCode = 400
                });
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.ISBN))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ISBN is required",
                    statusCode = 400
                });
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Title is required",
                    statusCode = 400
                });
            }

            if (string.IsNullOrWhiteSpace(request.Author))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Author is required",
                    statusCode = 400
                });
            }

            if (string.IsNullOrWhiteSpace(request.Edition))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Edition is required",
                    statusCode = 400
                });
            }

            // Validate prices
            if (request.SellingPrice <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Selling price must be greater than 0",
                    statusCode = 400
                });
            }

            if (request.AcquisitionCost < 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Acquisition cost cannot be negative",
                    statusCode = 400
                });
            }

            // Validate business rule: SellingPrice > AcquisitionCost
            if (request.SellingPrice <= request.AcquisitionCost)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Selling price must be greater than acquisition cost",
                    statusCode = 400
                });
            }

            // Validate book condition
            if (!string.IsNullOrWhiteSpace(request.BookCondition) &&
                request.BookCondition != "New" && 
                request.BookCondition != "Good" && 
                request.BookCondition != "Fair")
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Book condition must be 'New', 'Good', or 'Fair'",
                    statusCode = 400
                });
            }

            try
            {
                var bookId = await _bookService.CreateBookAsync(request);

                return StatusCode(201, new
                {
                    success = true,
                    data = new
                    {
                        bookId = bookId
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    statusCode = 400
                });
            }
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while creating the book",
                statusCode = 500
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBook(int id, [FromBody] UpdateBookRequest? request)
    {
        try
        {
            // Check authentication and admin access
            var currentUser = AuthHelper.GetCurrentUser(Request, _sessionService);
            if (currentUser == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    error = "Not authenticated",
                    statusCode = 401
                });
            }

            if (!IsAdmin(currentUser))
            {
                return StatusCode(403, new
                {
                    success = false,
                    error = "Admin access required",
                    statusCode = 403
                });
            }

            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Request body is required",
                    statusCode = 400
                });
            }

            if (id <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Invalid book ID",
                    statusCode = 400
                });
            }

            // Validate prices if provided
            if (request.SellingPrice.HasValue && request.SellingPrice.Value <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Selling price must be greater than 0",
                    statusCode = 400
                });
            }

            // Validate book condition if provided
            if (!string.IsNullOrWhiteSpace(request.BookCondition) &&
                request.BookCondition != "New" && 
                request.BookCondition != "Good" && 
                request.BookCondition != "Fair")
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Book condition must be 'New', 'Good', or 'Fair'",
                    statusCode = 400
                });
            }

            // Validate status if provided
            if (!string.IsNullOrWhiteSpace(request.Status) &&
                request.Status != "Available" && 
                request.Status != "Sold")
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Status must be 'Available' or 'Sold'",
                    statusCode = 400
                });
            }

            try
            {
                var success = await _bookService.UpdateBookAsync(id, request);

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Book updated successfully"
                    });
                }

                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to update book",
                    statusCode = 500
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    error = ex.Message,
                    statusCode = 404
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    statusCode = 400
                });
            }
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while updating the book",
                statusCode = 500
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBook(int id)
    {
        try
        {
            // Check authentication and admin access
            var currentUser = AuthHelper.GetCurrentUser(Request, _sessionService);
            if (currentUser == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    error = "Not authenticated",
                    statusCode = 401
                });
            }

            if (!IsAdmin(currentUser))
            {
                return StatusCode(403, new
                {
                    success = false,
                    error = "Admin access required",
                    statusCode = 403
                });
            }

            if (id <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Invalid book ID",
                    statusCode = 400
                });
            }

            try
            {
                var success = await _bookService.DeleteBookAsync(id);

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Book deleted successfully"
                    });
                }

                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to delete book",
                    statusCode = 500
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    error = ex.Message,
                    statusCode = 404
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(409, new
                {
                    success = false,
                    error = ex.Message,
                    statusCode = 409
                });
            }
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while deleting the book",
                statusCode = 500
            });
        }
    }
}

