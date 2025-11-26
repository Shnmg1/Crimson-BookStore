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
}

