namespace CrimsonBookStore.Models;

/// <summary>
/// Request model for admin rejecting a sell submission
/// Used for PUT /api/admin/sell-submissions/{submissionId}/reject endpoint
/// </summary>
public class RejectSubmissionRequest
{
    public string? Reason { get; set; } // Optional rejection reason
}

