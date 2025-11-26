namespace CrimsonBookStore.Models;

/// <summary>
/// Entity model for PurchaseOrder table
/// Represents a customer order
/// </summary>
public class Order
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "New"; // 'New', 'Processing', 'Fulfilled', 'Cancelled'
    public decimal TotalAmount { get; set; }
}

