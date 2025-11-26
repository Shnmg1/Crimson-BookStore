namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for detailed sell submission information
/// Used for GET /api/sell-submissions/{submissionId} endpoint
/// </summary>
public class SellSubmissionResponse
{
    public int SubmissionId { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public decimal AskingPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubmissionDate { get; set; }
    public List<PriceNegotiationResponse> Negotiations { get; set; } = new();
}

