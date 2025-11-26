using Microsoft.AspNetCore.Mvc;
using CrimsonBookStore.Models;
using CrimsonBookStore.Services;
using CrimsonBookStore.Helpers;

namespace CrimsonBookStore.Controllers;

[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ISessionService _sessionService;

    public AdminUsersController(IAuthService authService, ISessionService sessionService)
    {
        _authService = authService;
        _sessionService = sessionService;
    }

    private bool IsAdmin(CurrentUser? user)
    {
        return user != null && user.UserType == "Admin";
    }

    [HttpGet]
    public async Task<IActionResult> GetAdminUsers(
        [FromQuery] string? userType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // Check authentication and admin access
            var currentUser = AuthHelper.GetCurrentUser(Request, _sessionService);
            if (currentUser == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    error = "Not authenticated",
                    statusCode = 401
                });
            }

            if (!IsAdmin(currentUser))
            {
                return StatusCode(403, new
                {
                    success = false,
                    error = "Admin access required",
                    statusCode = 403
                });
            }

            // Validate userType if provided
            if (!string.IsNullOrWhiteSpace(userType) && 
                userType != "Customer" && userType != "Admin")
            {
                return BadRequest(new
                {
                    success = false,
                    error = "userType must be 'Customer' or 'Admin'",
                    statusCode = 400
                });
            }

            var users = await _authService.GetAdminUsersAsync(userType);

            // Simple pagination (for school project - keep it simple)
            var totalItems = users.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var skip = (page - 1) * pageSize;
            var pagedUsers = users.Skip(skip).Take(pageSize).ToList();

            return Ok(new
            {
                success = true,
                data = pagedUsers,
                pagination = new
                {
                    page = page,
                    pageSize = pageSize,
                    totalItems = totalItems,
                    totalPages = totalPages
                }
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while retrieving users",
                statusCode = 500
            });
        }
    }
}

