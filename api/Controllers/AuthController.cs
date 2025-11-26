using Microsoft.AspNetCore.Mvc;
using CrimsonBookStore.Models;
using CrimsonBookStore.Services;
using CrimsonBookStore.Helpers;

namespace CrimsonBookStore.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ISessionService _sessionService;

    public AuthController(IAuthService authService, ISessionService sessionService)
    {
        _authService = authService;
        _sessionService = sessionService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Username is required",
                    statusCode = 400
                });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Email is required",
                    statusCode = 400
                });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Password is required",
                    statusCode = 400
                });
            }

            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "First name and last name are required",
                    statusCode = 400
                });
            }

            // Validate UserType
            if (!string.IsNullOrEmpty(request.UserType) && 
                request.UserType != "Customer" && request.UserType != "Admin")
            {
                return BadRequest(new
                {
                    success = false,
                    error = "UserType must be either 'Customer' or 'Admin'",
                    statusCode = 400
                });
            }

            var authResponse = await _authService.RegisterAsync(request);

            return StatusCode(201, new
            {
                success = true,
                data = new
                {
                    userId = authResponse.UserId,
                    username = authResponse.Username,
                    email = authResponse.Email,
                    userType = authResponse.UserType
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            // Handle duplicate username/email
            return BadRequest(new
            {
                success = false,
                error = ex.Message,
                statusCode = 400
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while registering the user",
                statusCode = 500
            });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Username is required",
                    statusCode = 400
                });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Password is required",
                    statusCode = 400
                });
            }

            var authResponse = await _authService.LoginAsync(request);

            if (authResponse == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    error = "Invalid username or password",
                    statusCode = 401
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    userId = authResponse.UserId,
                    username = authResponse.Username,
                    userType = authResponse.UserType,
                    token = authResponse.Token
                }
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while logging in",
                statusCode = 500
            });
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        try
        {
            // Get token from Authorization header
            var authHeader = Request.Headers["Authorization"].ToString();
            var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader.Substring("Bearer ".Length).Trim()
                : authHeader.Trim();
            
            if (!string.IsNullOrEmpty(token))
            {
                _sessionService.RemoveSession(token);
            }

            return Ok(new
            {
                success = true,
                message = "Logged out successfully"
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while logging out",
                statusCode = 500
            });
        }
    }
}

