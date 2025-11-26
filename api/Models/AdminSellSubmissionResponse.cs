namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for admin viewing sell submission details
/// Used for GET /api/admin/sell-submissions/{submissionId} endpoint
/// </summary>
public class AdminSellSubmissionResponse
{
    public int SubmissionId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public decimal AskingPrice { get; set; }
    public string PhysicalCondition { get; set; } = string.Empty;
    public string? CourseMajor { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubmissionDate { get; set; }
    public List<PriceNegotiationResponse> Negotiations { get; set; } = new();
}

