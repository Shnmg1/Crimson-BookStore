namespace CrimsonBookStore.Models;

public class CreatePaymentMethodRequest
{
    public string CardType { get; set; } = string.Empty;
    public string LastFourDigits { get; set; } = string.Empty;
    public string ExpirationDate { get; set; } = string.Empty; // MM/YYYY format
    public bool IsDefault { get; set; }
}

