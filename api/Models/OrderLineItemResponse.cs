namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for order line item with book details
/// Used in OrderResponse
/// </summary>
public class OrderLineItemResponse
{
    public int LineItemId { get; set; }
    public int BookId { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public decimal PriceAtSale { get; set; }
}

