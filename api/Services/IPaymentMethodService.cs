using CrimsonBookStore.Models;

namespace CrimsonBookStore.Services;

public interface IPaymentMethodService
{
    Task<List<PaymentMethodResponse>> GetPaymentMethodsAsync(int userId);
    Task<int> CreatePaymentMethodAsync(int userId, CreatePaymentMethodRequest request);
    Task<bool> DeletePaymentMethodAsync(int userId, int paymentMethodId);
    Task<bool> SetDefaultPaymentMethodAsync(int userId, int paymentMethodId);
}

