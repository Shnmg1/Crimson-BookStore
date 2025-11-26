namespace CrimsonBookStore.Models;

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty; // 'New', 'Processing', 'Fulfilled', 'Cancelled'
}

