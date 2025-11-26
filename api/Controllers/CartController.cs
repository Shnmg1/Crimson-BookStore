using Microsoft.AspNetCore.Mvc;
using CrimsonBookStore.Models;
using CrimsonBookStore.Services;
using CrimsonBookStore.Helpers;

namespace CrimsonBookStore.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ISessionService _sessionService;

    public CartController(ICartService cartService, ISessionService sessionService)
    {
        _cartService = cartService;
        _sessionService = sessionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
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

            var cart = await _cartService.GetCartAsync(currentUser.UserId);

            return Ok(new
            {
                success = true,
                data = cart.Items,
                total = cart.Total
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while retrieving the cart",
                statusCode = 500
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
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

            // Validate request
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Request body is required",
                    statusCode = 400
                });
            }

            if (request.BookId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "BookId is required and must be greater than 0",
                    statusCode = 400
                });
            }

            try
            {
                var cartItemId = await _cartService.AddToCartAsync(currentUser.UserId, request.BookId);

                return StatusCode(201, new
                {
                    success = true,
                    data = new
                    {
                        cartItemId = cartItemId,
                        message = "Book added to cart"
                    }
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new
                {
                    success = false,
                    error = "Book not found",
                    statusCode = 404
                });
            }
            catch (InvalidOperationException ex)
            {
                // Check if it's a stock availability issue
                if (ex.Message.Contains("not available") || ex.Message.Contains("out of stock") || ex.Message.Contains("stock"))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = ex.Message,
                        statusCode = 400
                    });
                }
                
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
                error = "An error occurred while adding the book to cart",
                statusCode = 500
            });
        }
    }

    [HttpDelete("{cartItemId}")]
    public async Task<IActionResult> RemoveFromCart(int cartItemId)
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

            if (cartItemId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Invalid cart item ID",
                    statusCode = 400
                });
            }

            var removed = await _cartService.RemoveFromCartAsync(currentUser.UserId, cartItemId);

            if (!removed)
            {
                return NotFound(new
                {
                    success = false,
                    error = "Cart item not found",
                    statusCode = 404
                });
            }

            return Ok(new
            {
                success = true,
                message = "Item removed from cart"
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while removing the item from cart",
                statusCode = 500
            });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
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

            await _cartService.ClearCartAsync(currentUser.UserId);

            return Ok(new
            {
                success = true,
                message = "Cart cleared"
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while clearing the cart",
                statusCode = 500
            });
        }
    }
}

