using CrimsonBookStore.Models;

namespace CrimsonBookStore.Services;

public interface IOrderService
{
    Task<OrderCreateResponse> CreateOrderAsync(int userId, int? paymentMethodId);
    Task<List<OrderListResponse>> GetUserOrdersAsync(int userId, string? status = null);
    Task<OrderResponse?> GetOrderDetailsAsync(int orderId, int userId);
}

