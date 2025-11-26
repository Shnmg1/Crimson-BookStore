namespace CrimsonBookStore.Models;

/// <summary>
/// Request model for customer responding to a price negotiation
/// Used for POST /api/sell-submissions/{submissionId}/negotiate endpoint
/// </summary>
public class NegotiateRequest
{
    public string Action { get; set; } = string.Empty; // 'accept', 'reject', or 'counter'
    public int? NegotiationId { get; set; } // Required for 'accept' or 'reject' actions
    public decimal? OfferedPrice { get; set; } // Required for 'counter' action
    public string? OfferMessage { get; set; } // Optional message for counter-offer
}

