namespace CrimsonBookStore.Models;

public class PaymentMethod
{
    public int PaymentMethodID { get; set; }
    public int UserID { get; set; }
    public string CardType { get; set; } = string.Empty; // Visa, MasterCard, American Express, etc.
    public string LastFourDigits { get; set; } = string.Empty;
    public string ExpirationDate { get; set; } = string.Empty; // MM/YYYY format
    public bool IsDefault { get; set; }
    public DateTime CreatedDate { get; set; }
}

