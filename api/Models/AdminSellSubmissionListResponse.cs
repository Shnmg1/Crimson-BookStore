namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for admin sell submission list item
/// Used for GET /api/admin/sell-submissions endpoint
/// Includes user information for admin view
/// </summary>
public class AdminSellSubmissionListResponse
{
    public int SubmissionId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal AskingPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubmissionDate { get; set; }
}

