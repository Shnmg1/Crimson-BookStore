namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for payment information
/// Used in OrderResponse
/// </summary>
public class PaymentResponse
{
    public int PaymentId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; } // e.g., "Visa ending in 1234"
}

