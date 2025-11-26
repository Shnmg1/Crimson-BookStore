using CrimsonBookStore.Models;

namespace CrimsonBookStore.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    
    // Admin method
    Task<List<AdminUserListResponse>> GetAdminUsersAsync(string? userType = null);
}

