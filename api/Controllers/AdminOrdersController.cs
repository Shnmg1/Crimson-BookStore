using Microsoft.AspNetCore.Mvc;
using CrimsonBookStore.Models;
using CrimsonBookStore.Services;
using CrimsonBookStore.Helpers;

namespace CrimsonBookStore.Controllers;

[ApiController]
[Route("api/admin/orders")]
public class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ISessionService _sessionService;

    public AdminOrdersController(IOrderService orderService, ISessionService sessionService)
    {
        _orderService = orderService;
        _sessionService = sessionService;
    }

    private bool IsAdmin(CurrentUser? user)
    {
        return user != null && user.UserType == "Admin";
    }

    [HttpGet]
    public async Task<IActionResult> GetAdminOrders(
        [FromQuery] string? status = null,
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

            var orders = await _orderService.GetAdminOrdersAsync(status);

            // Simple pagination (for school project - keep it simple)
            var totalItems = orders.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var skip = (page - 1) * pageSize;
            var pagedOrders = orders.Skip(skip).Take(pageSize).ToList();

            return Ok(new
            {
                success = true,
                data = pagedOrders,
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
                error = "An error occurred while retrieving orders",
                statusCode = 500
            });
        }
    }
}

