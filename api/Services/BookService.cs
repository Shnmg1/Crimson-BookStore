using System.Data;
using CrimsonBookStore.Models;

namespace CrimsonBookStore.Services;

public class BookService : IBookService
{
    private readonly IDatabaseService _databaseService;

    public BookService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<List<BookListResponse>> GetAvailableBooksAsync(string? search = null, string? isbn = null, string? courseMajor = null)
    {
        var query = @"
            SELECT 
                ISBN,
                Edition,
                Title,
                Author,
                MIN(SellingPrice) as min_price,
                MAX(SellingPrice) as max_price,
                COUNT(*) as available_count,
                GROUP_CONCAT(DISTINCT BookCondition ORDER BY 
                    CASE BookCondition 
                        WHEN 'New' THEN 1 
                        WHEN 'Good' THEN 2 
                        WHEN 'Fair' THEN 3 
                    END) as available_conditions,
                MIN(CourseMajor) as course_major
            FROM Book
            WHERE Status = 'Available'";

        var parameters = new Dictionary<string, object>();

        // Add search filter if provided
        if (!string.IsNullOrWhiteSpace(search))
        {
            query += " AND (Title LIKE @Search OR Author LIKE @Search OR ISBN = @Search OR CourseMajor LIKE @Search)";
            parameters.Add("@Search", $"%{search}%");
        }

        // Add ISBN filter if provided
        if (!string.IsNullOrWhiteSpace(isbn))
        {
            query += " AND ISBN = @ISBN";
            parameters.Add("@ISBN", isbn);
        }

        // Add course/major filter if provided
        if (!string.IsNullOrWhiteSpace(courseMajor))
        {
            query += " AND CourseMajor = @CourseMajor";
            parameters.Add("@CourseMajor", courseMajor);
        }

        query += @"
            GROUP BY ISBN, Edition, Title, Author
            HAVING available_count > 0
            ORDER BY Title, Edition";

        var dataTable = await _databaseService.ExecuteQueryAsync(query, parameters.Count > 0 ? parameters : null);

        var books = new List<BookListResponse>();

        foreach (DataRow row in dataTable.Rows)
        {
            var book = new BookListResponse
            {
                ISBN = row["ISBN"].ToString() ?? string.Empty,
                Edition = row["Edition"].ToString() ?? string.Empty,
                Title = row["Title"].ToString() ?? string.Empty,
                Author = row["Author"].ToString() ?? string.Empty,
                MinPrice = Convert.ToDecimal(row["min_price"]),
                MaxPrice = Convert.ToDecimal(row["max_price"]),
                AvailableCount = Convert.ToInt32(row["available_count"]),
                CourseMajor = row["course_major"] == DBNull.Value ? null : row["course_major"].ToString()
            };

            // Parse available conditions from GROUP_CONCAT result
            if (row["available_conditions"] != DBNull.Value && row["available_conditions"] != null)
            {
                var conditionsStr = row["available_conditions"].ToString() ?? string.Empty;
                book.AvailableConditions = conditionsStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .ToList();
            }

            books.Add(book);
        }

        return books;
    }

    public async Task<BookDetailResponse?> GetBookByIdAsync(int bookId)
    {
        var query = @"
            SELECT 
                b.BookID,
                b.ISBN,
                b.Title,
                b.Author,
                b.Edition,
                b.SellingPrice,
                b.BookCondition,
                b.CourseMajor,
                b.Status,
                (SELECT COUNT(*) 
                 FROM Book b2 
                 WHERE b2.ISBN = b.ISBN 
                   AND b2.Edition = b.Edition 
                   AND b2.Status = 'Available') as available_count
            FROM Book b
            WHERE b.BookID = @BookID AND b.Status = 'Available'";

        var parameters = new Dictionary<string, object>
        {
            { "@BookID", bookId }
        };

        var dataTable = await _databaseService.ExecuteQueryAsync(query, parameters);

        if (dataTable.Rows.Count == 0)
        {
            return null;
        }

        var row = dataTable.Rows[0];

        return new BookDetailResponse
        {
            BookId = Convert.ToInt32(row["BookID"]),
            ISBN = row["ISBN"].ToString() ?? string.Empty,
            Title = row["Title"].ToString() ?? string.Empty,
            Author = row["Author"].ToString() ?? string.Empty,
            Edition = row["Edition"].ToString() ?? string.Empty,
            SellingPrice = Convert.ToDecimal(row["SellingPrice"]),
            BookCondition = row["BookCondition"].ToString() ?? string.Empty,
            CourseMajor = row["CourseMajor"] == DBNull.Value ? null : row["CourseMajor"].ToString(),
            Status = row["Status"].ToString() ?? string.Empty,
            AvailableCount = Convert.ToInt32(row["available_count"])
        };
    }

    public async Task<List<BookListResponse>> SearchBooksAsync(string searchTerm)
    {
        // SearchBooks is essentially the same as GetAvailableBooks with search parameter
        return await GetAvailableBooksAsync(search: searchTerm);
    }

    public async Task<int> CreateBookAsync(CreateBookRequest request)
    {
        // Validate: SellingPrice > AcquisitionCost
        if (request.SellingPrice <= request.AcquisitionCost)
        {
            throw new ArgumentException("SellingPrice must be greater than AcquisitionCost");
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.ISBN) ||
            string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Author) ||
            string.IsNullOrWhiteSpace(request.Edition) ||
            string.IsNullOrWhiteSpace(request.BookCondition))
        {
            throw new ArgumentException("ISBN, Title, Author, Edition, and BookCondition are required");
        }

        // Validate BookCondition
        if (!new[] { "New", "Good", "Fair" }.Contains(request.BookCondition))
        {
            throw new ArgumentException("BookCondition must be 'New', 'Good', or 'Fair'");
        }

        // Validate prices
        if (request.SellingPrice <= 0)
        {
            throw new ArgumentException("SellingPrice must be greater than 0");
        }

        if (request.AcquisitionCost < 0)
        {
            throw new ArgumentException("AcquisitionCost cannot be negative");
        }

        var query = @"
            INSERT INTO Book (
                ISBN, Title, Author, Edition, 
                SellingPrice, AcquisitionCost, BookCondition, 
                CourseMajor, Status, CreatedDate
            )
            VALUES (
                @ISBN, @Title, @Author, @Edition,
                @SellingPrice, @AcquisitionCost, @BookCondition,
                @CourseMajor, 'Available', NOW()
            )";

        var parameters = new Dictionary<string, object>
        {
            { "@ISBN", request.ISBN },
            { "@Title", request.Title },
            { "@Author", request.Author },
            { "@Edition", request.Edition },
            { "@SellingPrice", request.SellingPrice },
            { "@AcquisitionCost", request.AcquisitionCost },
            { "@BookCondition", request.BookCondition },
            { "@CourseMajor", request.CourseMajor ?? (object)DBNull.Value }
        };

        await _databaseService.ExecuteNonQueryAsync(query, parameters);

        // Get the last insert ID
        var lastIdQuery = "SELECT LAST_INSERT_ID()";
        var lastId = await _databaseService.ExecuteScalarAsync(lastIdQuery);
        
        return Convert.ToInt32(lastId ?? 0);
    }

    public async Task<bool> UpdateBookAsync(int bookId, UpdateBookRequest request)
    {
        // Check if book exists
        var existingBook = await GetBookByIdAsync(bookId);
        if (existingBook == null)
        {
            // Try to get book even if not available (for admin to update)
            var checkQuery = "SELECT BookID, SellingPrice, AcquisitionCost FROM Book WHERE BookID = @BookID";
            var checkParams = new Dictionary<string, object> { { "@BookID", bookId } };
            var checkResult = await _databaseService.ExecuteQueryAsync(checkQuery, checkParams);
            
            if (checkResult.Rows.Count == 0)
            {
                throw new KeyNotFoundException("Book not found");
            }
        }

        // Build update query dynamically based on what's provided
        var updateFields = new List<string>();
        var parameters = new Dictionary<string, object> { { "@BookID", bookId } };

        if (request.SellingPrice.HasValue)
        {
            updateFields.Add("SellingPrice = @SellingPrice");
            parameters.Add("@SellingPrice", request.SellingPrice.Value);
        }

        if (request.AcquisitionCost.HasValue)
        {
            updateFields.Add("AcquisitionCost = @AcquisitionCost");
            parameters.Add("@AcquisitionCost", request.AcquisitionCost.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.BookCondition))
        {
            if (!new[] { "New", "Good", "Fair" }.Contains(request.BookCondition))
            {
                throw new ArgumentException("BookCondition must be 'New', 'Good', or 'Fair'");
            }
            updateFields.Add("BookCondition = @BookCondition");
            parameters.Add("@BookCondition", request.BookCondition);
        }

        // CourseMajor: if provided (not null), update it (empty string = NULL)
        if (request.CourseMajor != null)
        {
            updateFields.Add("CourseMajor = @CourseMajor");
            parameters.Add("@CourseMajor", string.IsNullOrWhiteSpace(request.CourseMajor) ? (object)DBNull.Value : request.CourseMajor);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!new[] { "Available", "Sold" }.Contains(request.Status))
            {
                throw new ArgumentException("Status must be 'Available' or 'Sold'");
            }
            updateFields.Add("Status = @Status");
            parameters.Add("@Status", request.Status);
        }

        if (updateFields.Count == 0)
        {
            throw new ArgumentException("At least one field must be provided for update");
        }

        // Validate: If both SellingPrice and AcquisitionCost are being updated, ensure SellingPrice > AcquisitionCost
        if (request.SellingPrice.HasValue && request.AcquisitionCost.HasValue)
        {
            if (request.SellingPrice.Value <= request.AcquisitionCost.Value)
            {
                throw new ArgumentException("SellingPrice must be greater than AcquisitionCost");
            }
        }
        else if (request.SellingPrice.HasValue)
        {
            // Get current AcquisitionCost to validate
            var getAcqQuery = "SELECT AcquisitionCost FROM Book WHERE BookID = @BookID";
            var acqResult = await _databaseService.ExecuteQueryAsync(getAcqQuery, new Dictionary<string, object> { { "@BookID", bookId } });
            if (acqResult.Rows.Count > 0)
            {
                var currentAcqCost = Convert.ToDecimal(acqResult.Rows[0]["AcquisitionCost"]);
                if (request.SellingPrice.Value <= currentAcqCost)
                {
                    throw new ArgumentException("SellingPrice must be greater than AcquisitionCost");
                }
            }
        }
        else if (request.AcquisitionCost.HasValue)
        {
            // Get current SellingPrice to validate
            var getSellQuery = "SELECT SellingPrice FROM Book WHERE BookID = @BookID";
            var sellResult = await _databaseService.ExecuteQueryAsync(getSellQuery, new Dictionary<string, object> { { "@BookID", bookId } });
            if (sellResult.Rows.Count > 0)
            {
                var currentSellPrice = Convert.ToDecimal(sellResult.Rows[0]["SellingPrice"]);
                if (currentSellPrice <= request.AcquisitionCost.Value)
                {
                    throw new ArgumentException("SellingPrice must be greater than AcquisitionCost");
                }
            }
        }

        var query = $"UPDATE Book SET {string.Join(", ", updateFields)} WHERE BookID = @BookID";

        var rowsAffected = await _databaseService.ExecuteNonQueryAsync(query, parameters);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteBookAsync(int bookId)
    {
        // Check if book is in an active order (cannot delete)
        var checkOrderQuery = @"
            SELECT COUNT(*) as order_count
            FROM OrderLineItem oli
            JOIN PurchaseOrder po ON oli.OrderID = po.OrderID
            WHERE oli.BookID = @BookID 
              AND po.Status != 'Cancelled'";

        var checkParams = new Dictionary<string, object> { { "@BookID", bookId } };
        var checkResult = await _databaseService.ExecuteQueryAsync(checkOrderQuery, checkParams);
        
        if (checkResult.Rows.Count > 0 && Convert.ToInt32(checkResult.Rows[0]["order_count"]) > 0)
        {
            throw new InvalidOperationException("Cannot delete book that is in an active order");
        }

        // Check if book exists
        var existsQuery = "SELECT BookID FROM Book WHERE BookID = @BookID";
        var existsResult = await _databaseService.ExecuteQueryAsync(existsQuery, checkParams);
        
        if (existsResult.Rows.Count == 0)
        {
            throw new KeyNotFoundException("Book not found");
        }

        // Delete the book
        var deleteQuery = "DELETE FROM Book WHERE BookID = @BookID";
        var rowsAffected = await _databaseService.ExecuteNonQueryAsync(deleteQuery, checkParams);
        
        return rowsAffected > 0;
    }
}

