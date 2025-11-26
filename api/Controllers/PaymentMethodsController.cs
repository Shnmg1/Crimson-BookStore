using Microsoft.AspNetCore.Mvc;
using CrimsonBookStore.Models;
using CrimsonBookStore.Services;
using CrimsonBookStore.Helpers;

namespace CrimsonBookStore.Controllers;

[ApiController]
[Route("api/payment-methods")]
public class PaymentMethodsController : ControllerBase
{
    private readonly IPaymentMethodService _paymentMethodService;
    private readonly ISessionService _sessionService;

    public PaymentMethodsController(IPaymentMethodService paymentMethodService, ISessionService sessionService)
    {
        _paymentMethodService = paymentMethodService;
        _sessionService = sessionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaymentMethods()
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

            var paymentMethods = await _paymentMethodService.GetPaymentMethodsAsync(currentUser.UserId);

            return Ok(new
            {
                success = true,
                data = paymentMethods
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while retrieving payment methods",
                statusCode = 500
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreatePaymentMethod([FromBody] CreatePaymentMethodRequest? request)
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

            if (string.IsNullOrWhiteSpace(request.CardType))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Card type is required",
                    statusCode = 400
                });
            }

            if (string.IsNullOrWhiteSpace(request.LastFourDigits))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Last four digits are required",
                    statusCode = 400
                });
            }

            // Validate last four digits format (exactly 4 numeric digits)
            if (request.LastFourDigits.Length != 4 || !request.LastFourDigits.All(char.IsDigit))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Last four digits must be exactly 4 numeric digits",
                    statusCode = 400
                });
            }

            if (string.IsNullOrWhiteSpace(request.ExpirationDate))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Expiration date is required",
                    statusCode = 400
                });
            }

            // Validate expiration date format (MM/YYYY)
            var expirationParts = request.ExpirationDate.Split('/');
            if (expirationParts.Length != 2)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Expiration date must be in MM/YYYY format (e.g., 12/2025)",
                    statusCode = 400
                });
            }

            if (!int.TryParse(expirationParts[0], out int month) || month < 1 || month > 12)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Expiration month must be between 01 and 12",
                    statusCode = 400
                });
            }

            if (!int.TryParse(expirationParts[1], out int year) || year < 2000 || year > 2099)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Expiration year must be between 2000 and 2099",
                    statusCode = 400
                });
            }

            try
            {
                var paymentMethodId = await _paymentMethodService.CreatePaymentMethodAsync(currentUser.UserId, request);

                return StatusCode(201, new
                {
                    success = true,
                    data = new
                    {
                        paymentMethodId = paymentMethodId
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    statusCode = 400
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    statusCode = 500
                });
            }
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while creating the payment method",
                statusCode = 500
            });
        }
    }

    [HttpPut("{paymentMethodId}/default")]
    public async Task<IActionResult> SetDefaultPaymentMethod(int paymentMethodId)
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

            if (paymentMethodId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Invalid payment method ID",
                    statusCode = 400
                });
            }

            try
            {
                var success = await _paymentMethodService.SetDefaultPaymentMethodAsync(currentUser.UserId, paymentMethodId);

                if (!success)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "Payment method not found",
                        statusCode = 404
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Default payment method updated successfully"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    error = ex.Message,
                    statusCode = 404
                });
            }
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while setting default payment method",
                statusCode = 500
            });
        }
    }

    [HttpDelete("{paymentMethodId}")]
    public async Task<IActionResult> DeletePaymentMethod(int paymentMethodId)
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

            if (paymentMethodId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Invalid payment method ID",
                    statusCode = 400
                });
            }

            try
            {
                var deleted = await _paymentMethodService.DeletePaymentMethodAsync(currentUser.UserId, paymentMethodId);

                if (!deleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "Payment method not found",
                        statusCode = 404
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Payment method deleted successfully"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    error = ex.Message,
                    statusCode = 404
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
                error = "An error occurred while deleting the payment method",
                statusCode = 500
            });
        }
    }
}

