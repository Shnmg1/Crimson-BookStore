namespace CrimsonBookStore.Models;

/// <summary>
/// Entity model for PriceNegotiation table
/// Represents a single round of price negotiation between customer and admin
/// </summary>
public class PriceNegotiation
{
    public int NegotiationID { get; set; }
    public int SubmissionID { get; set; }
    public string OfferedBy { get; set; } = string.Empty; // 'User' or 'Admin'
    public decimal OfferedPrice { get; set; }
    public DateTime OfferDate { get; set; }
    public string? OfferMessage { get; set; }
    public string OfferStatus { get; set; } = "Pending"; // 'Pending', 'Accepted', 'Rejected'
    public int RoundNumber { get; set; }
}

