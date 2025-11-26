using CrimsonBookStore.Models;

namespace CrimsonBookStore.Services;

public interface ISessionService
{
    string CreateSession(int userId, string username, string userType);
    CurrentUser? GetUserFromToken(string token);
    void RemoveSession(string token);
    bool IsValidToken(string token);
}

