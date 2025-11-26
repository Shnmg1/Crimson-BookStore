namespace CrimsonBookStore.Models;

/// <summary>
/// Entity model for OrderLineItem table
/// Represents a single book in an order
/// </summary>
public class OrderLineItem
{
    public int LineItemId { get; set; }
    public int OrderId { get; set; }
    public int BookId { get; set; }
    public decimal PriceAtSale { get; set; }
}

