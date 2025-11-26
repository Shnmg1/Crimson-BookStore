namespace CrimsonBookStore.Models;

public class AuthResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string UserType { get; set; } = string.Empty;
    public string? Token { get; set; }
}

