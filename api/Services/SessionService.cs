using System.Collections.Concurrent;
using CrimsonBookStore.Models;

namespace CrimsonBookStore.Services;

public class SessionService : ISessionService
{
    // In-memory session storage (simple dictionary for demo purposes)
    private readonly ConcurrentDictionary<string, CurrentUser> _sessions = new();
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(24); // 24 hour session
    private readonly ConcurrentDictionary<string, DateTime> _sessionExpiry = new();

    public string CreateSession(int userId, string username, string userType)
    {
        // Generate a simple token (GUID for demo purposes)
        var token = Guid.NewGuid().ToString();

        var user = new CurrentUser
        {
            UserId = userId,
            Username = username,
            UserType = userType
        };

        _sessions[token] = user;
        _sessionExpiry[token] = DateTime.UtcNow.Add(_sessionTimeout);

        // Clean up expired sessions periodically (simple cleanup)
        CleanupExpiredSessions();

        return token;
    }

    public CurrentUser? GetUserFromToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        // Check if session exists and is not expired
        if (!_sessions.TryGetValue(token, out var user))
        {
            return null;
        }

        if (_sessionExpiry.TryGetValue(token, out var expiry) && DateTime.UtcNow > expiry)
        {
            // Session expired, remove it
            RemoveSession(token);
            return null;
        }

        return user;
    }

    public void RemoveSession(string token)
    {
        _sessions.TryRemove(token, out _);
        _sessionExpiry.TryRemove(token, out _);
    }

    public bool IsValidToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        if (!_sessions.ContainsKey(token))
        {
            return false;
        }

        if (_sessionExpiry.TryGetValue(token, out var expiry) && DateTime.UtcNow > expiry)
        {
            RemoveSession(token);
            return false;
        }

        return true;
    }

    private void CleanupExpiredSessions()
    {
        // Simple cleanup: remove expired sessions
        var expiredTokens = _sessionExpiry
            .Where(kvp => DateTime.UtcNow > kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var token in expiredTokens)
        {
            RemoveSession(token);
        }
    }
}

