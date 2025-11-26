namespace CrimsonBookStore.Models;

/// <summary>
/// Request model for admin making a counter-offer in price negotiation
/// Used for POST /api/admin/sell-submissions/{submissionId}/negotiate endpoint
/// </summary>
public class AdminNegotiateRequest
{
    public decimal OfferedPrice { get; set; }
    public string? OfferMessage { get; set; }
}

