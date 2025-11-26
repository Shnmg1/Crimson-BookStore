namespace CrimsonBookStore.Models;

/// <summary>
/// Request model for creating an order from cart
/// Used for POST /api/orders endpoint
/// </summary>
public class CreateOrderRequest
{
    public int? PaymentMethodId { get; set; } // Nullable for one-time payments
}

