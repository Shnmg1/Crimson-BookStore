namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for negotiation actions
/// Used for POST /api/sell-submissions/{submissionId}/negotiate and admin negotiate endpoints
/// </summary>
public class NegotiateResponse
{
    public int? NegotiationId { get; set; }
    public int? RoundNumber { get; set; }
    public decimal? OfferedPrice { get; set; }
    public string? OfferStatus { get; set; }
    public string? Message { get; set; } // Success message
}

