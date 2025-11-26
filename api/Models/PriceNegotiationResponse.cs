namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for price negotiation information
/// Used in SellSubmissionResponse to show negotiation history
/// </summary>
public class PriceNegotiationResponse
{
    public int NegotiationId { get; set; }
    public string OfferedBy { get; set; } = string.Empty; // 'User' or 'Admin'
    public decimal OfferedPrice { get; set; }
    public DateTime OfferDate { get; set; }
    public string? OfferMessage { get; set; }
    public string OfferStatus { get; set; } = string.Empty; // 'Pending', 'Accepted', 'Rejected'
    public int RoundNumber { get; set; }
}

