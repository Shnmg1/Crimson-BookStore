namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for sell submission list item
/// Used for GET /api/sell-submissions endpoint
/// </summary>
public class SellSubmissionListResponse
{
    public int SubmissionId { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public decimal AskingPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubmissionDate { get; set; }
}

