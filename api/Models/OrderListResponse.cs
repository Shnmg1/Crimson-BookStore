namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for order list (summary)
/// Used for GET /api/orders endpoint
/// </summary>
public class OrderListResponse
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
}

