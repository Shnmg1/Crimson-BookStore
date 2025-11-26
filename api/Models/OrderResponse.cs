namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for detailed order information
/// Used for GET /api/orders/{orderId} endpoint
/// </summary>
public class OrderResponse
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<OrderLineItemResponse> Items { get; set; } = new();
    public PaymentResponse? Payment { get; set; }
}

