using System.Data;
using CrimsonBookStore.Models;

namespace CrimsonBookStore.Services;

public class AuthService : IAuthService
{
    private readonly IDatabaseService _databaseService;
    private readonly ISessionService _sessionService;

    public AuthService(IDatabaseService databaseService, ISessionService sessionService)
    {
        _databaseService = databaseService;
        _sessionService = sessionService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if username already exists
        var usernameCheckQuery = "SELECT COUNT(*) FROM `User` WHERE Username = @Username";
        var usernameExists = await _databaseService.ExecuteScalarAsync(
            usernameCheckQuery,
            new Dictionary<string, object> { { "@Username", request.Username } }
        );

        if (Convert.ToInt32(usernameExists) > 0)
        {
            throw new InvalidOperationException("Username already exists");
        }

        // Check if email already exists
        var emailCheckQuery = "SELECT COUNT(*) FROM `User` WHERE Email = @Email";
        var emailExists = await _databaseService.ExecuteScalarAsync(
            emailCheckQuery,
            new Dictionary<string, object> { { "@Email", request.Email } }
        );

        if (Convert.ToInt32(emailExists) > 0)
        {
            throw new InvalidOperationException("Email already exists");
        }

        // Insert new user (storing password as plain text for demo purposes)
        var insertQuery = @"
            INSERT INTO `User` (Username, Email, Password, FirstName, LastName, Phone, Address, UserType, CreatedDate)
            VALUES (@Username, @Email, @Password, @FirstName, @LastName, @Phone, @Address, @UserType, NOW())";

        var parameters = new Dictionary<string, object>
        {
            { "@Username", request.Username },
            { "@Email", request.Email },
            { "@Password", request.Password },
            { "@FirstName", request.FirstName },
            { "@LastName", request.LastName },
            { "@UserType", request.UserType ?? "Customer" }
        };

        if (!string.IsNullOrEmpty(request.Phone))
        {
            parameters.Add("@Phone", request.Phone);
        }
        else
        {
            parameters.Add("@Phone", DBNull.Value);
        }

        if (!string.IsNullOrEmpty(request.Address))
        {
            parameters.Add("@Address", request.Address);
        }
        else
        {
            parameters.Add("@Address", DBNull.Value);
        }

        await _databaseService.ExecuteNonQueryAsync(insertQuery, parameters);

        // Get the created user
        var getUserQuery = "SELECT UserID, Username, Email, UserType FROM `User` WHERE Username = @Username";
        var userData = await _databaseService.ExecuteQueryAsync(
            getUserQuery,
            new Dictionary<string, object> { { "@Username", request.Username } }
        );

        if (userData.Rows.Count == 0)
        {
            throw new InvalidOperationException("Failed to retrieve created user");
        }

        var row = userData.Rows[0];
        return new AuthResponse
        {
            UserId = Convert.ToInt32(row["UserID"]),
            Username = row["Username"].ToString() ?? string.Empty,
            Email = row["Email"].ToString(),
            UserType = row["UserType"].ToString() ?? "Customer"
        };
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        // Get user by username
        var query = @"
            SELECT UserID, Username, Email, Password, UserType 
            FROM `User` 
            WHERE Username = @Username";

        var userData = await _databaseService.ExecuteQueryAsync(
            query,
            new Dictionary<string, object> { { "@Username", request.Username } }
        );

        if (userData.Rows.Count == 0)
        {
            return null; // User not found
        }

        var row = userData.Rows[0];
        var storedPassword = row["Password"].ToString() ?? string.Empty;

        // Simple password comparison (plain text for demo purposes)
        if (request.Password != storedPassword)
        {
            return null; // Invalid password
        }

        // Create session and return user info with token
        var userId = Convert.ToInt32(row["UserID"]);
        var username = row["Username"].ToString() ?? string.Empty;
        var userType = row["UserType"].ToString() ?? "Customer";
        
        var token = _sessionService.CreateSession(userId, username, userType);

        return new AuthResponse
        {
            UserId = userId,
            Username = username,
            Email = row["Email"].ToString(),
            UserType = userType,
            Token = token
        };
    }
}

