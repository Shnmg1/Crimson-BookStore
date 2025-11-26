namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for order creation
/// Used for POST /api/orders endpoint
/// </summary>
public class OrderCreateResponse
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "New";
    public decimal TotalAmount { get; set; }
    public List<OrderItemSummary> Items { get; set; } = new();
}

/// <summary>
/// Summary of order item for creation response
/// </summary>
public class OrderItemSummary
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal PriceAtSale { get; set; }
}

