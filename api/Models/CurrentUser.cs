namespace CrimsonBookStore.Models;

public class CurrentUser
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
}

