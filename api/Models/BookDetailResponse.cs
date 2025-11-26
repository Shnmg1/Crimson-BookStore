namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for single book details
/// Used for GET /api/books/{bookId} endpoint
/// </summary>
public class BookDetailResponse
{
    public int BookId { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public string BookCondition { get; set; } = string.Empty;
    public string? CourseMajor { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AvailableCount { get; set; } // Stock count for this ISBN+Edition
}

