using Microsoft.AspNetCore.Mvc;
using CrimsonBookStore.Models;
using CrimsonBookStore.Services;
using CrimsonBookStore.Helpers;

namespace CrimsonBookStore.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ISessionService _sessionService;

    public OrdersController(IOrderService orderService, ISessionService sessionService)
    {
        _orderService = orderService;
        _sessionService = sessionService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest? request)
    {
        try
        {
            // Check authentication
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

            // PaymentMethodId is optional (nullable for one-time payments)
            var paymentMethodId = request?.PaymentMethodId;

            try
            {
                var order = await _orderService.CreateOrderAsync(currentUser.UserId, paymentMethodId);

                return StatusCode(201, new
                {
                    success = true,
                    data = order
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    statusCode = 400
                });
            }
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while creating the order",
                statusCode = 500
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] string? status = null)
    {
        try
        {
            // Check authentication
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

            var orders = await _orderService.GetUserOrdersAsync(currentUser.UserId, status);

            return Ok(new
            {
                success = true,
                data = orders
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

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrderDetails(int orderId)
    {
        try
        {
            // Check authentication
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

            if (orderId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Invalid order ID",
                    statusCode = 400
                });
            }

            try
            {
                var order = await _orderService.GetOrderDetailsAsync(orderId, currentUser.UserId);

                if (order == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "Order not found",
                        statusCode = 404
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = order
                });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new
                {
                    success = false,
                    error = "You do not have access to this order",
                    statusCode = 403
                });
            }
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while retrieving order details",
                statusCode = 500
            });
        }
    }
}

