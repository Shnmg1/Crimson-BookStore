using CrimsonBookStore.Models;

namespace CrimsonBookStore.Services;

public interface ICartService
{
    Task<CartResponse> GetCartAsync(int userId);
    Task<int> AddToCartAsync(int userId, int bookId);
    Task<bool> RemoveFromCartAsync(int userId, int cartItemId);
    Task<bool> ClearCartAsync(int userId);
}

