namespace CrimsonBookStore.Models;

/// <summary>
/// Represents a single item in the shopping cart
/// Used for cart responses
/// </summary>
public class CartItem
{
    public int CartItemId { get; set; }
    public int BookId { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public string BookCondition { get; set; } = string.Empty;
    public string? CourseMajor { get; set; }
    public DateTime AddedDate { get; set; }
}

