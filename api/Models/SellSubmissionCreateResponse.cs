namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for creating a sell submission
/// Used for POST /api/sell-submissions endpoint
/// </summary>
public class SellSubmissionCreateResponse
{
    public int SubmissionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubmissionDate { get; set; }
}

