namespace CrimsonBookStore.Models;

/// <summary>
/// Request model for adding a book to cart
/// Used for POST /api/cart endpoint
/// </summary>
public class AddToCartRequest
{
    public int BookId { get; set; }
}

