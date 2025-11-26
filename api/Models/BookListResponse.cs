namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for grouped book listings (grouped by ISBN + Edition)
/// Used for GET /api/books endpoint
/// </summary>
public class BookListResponse
{
    public string ISBN { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int AvailableCount { get; set; }
    public List<string> AvailableConditions { get; set; } = new();
    public string? CourseMajor { get; set; }
}

