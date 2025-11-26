namespace CrimsonBookStore.Models;

/// <summary>
/// Request model for creating a sell submission
/// Used for POST /api/sell-submissions endpoint
/// </summary>
public class CreateSellSubmissionRequest
{
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public string PhysicalCondition { get; set; } = string.Empty; // 'New', 'Good', 'Fair'
    public string? CourseMajor { get; set; }
    public decimal AskingPrice { get; set; }
}

