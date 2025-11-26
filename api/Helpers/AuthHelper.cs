using CrimsonBookStore.Models;
using CrimsonBookStore.Services;

namespace CrimsonBookStore.Helpers;

public static class AuthHelper
{
    /// <summary>
    /// Gets the current authenticated user from the Authorization header token.
    /// Returns null if no valid token is found.
    /// </summary>
    public static CurrentUser? GetCurrentUser(HttpRequest request, ISessionService sessionService)
    {
        // Extract token from Authorization header: "Bearer {token}"
        var authHeader = request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader))
        {
            return null;
        }

        var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader.Substring("Bearer ".Length).Trim()
            : authHeader.Trim();

        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        return sessionService.GetUserFromToken(token);
    }
}

