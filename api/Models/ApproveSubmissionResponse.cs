namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for approving a sell submission
/// Used for PUT /api/admin/sell-submissions/{submissionId}/approve endpoint
/// </summary>
public class ApproveSubmissionResponse
{
    public int SubmissionId { get; set; }
    public int BookId { get; set; }
    public string Status { get; set; } = string.Empty;
}

