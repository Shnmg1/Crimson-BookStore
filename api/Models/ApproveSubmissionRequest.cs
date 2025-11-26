namespace CrimsonBookStore.Models;

/// <summary>
/// Request model for admin approving a sell submission
/// Used for PUT /api/admin/sell-submissions/{submissionId}/approve endpoint
/// </summary>
public class ApproveSubmissionRequest
{
    public decimal SellingPrice { get; set; } // Must be > AcquisitionCost (from accepted negotiation)
}

