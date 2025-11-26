namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for individual book copies
/// Used for GET /api/books/{isbn}/{edition}/copies endpoint
/// </summary>
public class BookCopyResponse
{
    public int BookId { get; set; }
    public decimal Price { get; set; }
    public string Condition { get; set; } = string.Empty;
}

