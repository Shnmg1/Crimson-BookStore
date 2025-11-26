namespace CrimsonBookStore.Models;

/// <summary>
/// Response model for shopping cart
/// Used for GET /api/cart endpoint
/// </summary>
public class CartResponse
{
    public List<CartItem> Items { get; set; } = new();
    public decimal Total { get; set; }
}

