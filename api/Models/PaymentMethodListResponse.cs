namespace CrimsonBookStore.Models;

public class PaymentMethodListResponse
{
    public List<PaymentMethodResponse> PaymentMethods { get; set; } = new();
}

