namespace CrimsonBookStore.Models;

public class PaymentMethodResponse
{
    public int PaymentMethodId { get; set; }
    public string CardType { get; set; } = string.Empty;
    public string LastFourDigits { get; set; } = string.Empty;
    public string ExpirationDate { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedDate { get; set; }
    
    /// <summary>
    /// Display format: "Visa ending in 1234"
    /// </summary>
    public string DisplayName => $"{CardType} ending in {LastFourDigits}";
}

