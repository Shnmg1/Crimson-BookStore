using Microsoft.AspNetCore.Mvc;
using CrimsonBookStore.Models;
using CrimsonBookStore.Services;
using CrimsonBookStore.Helpers;

namespace CrimsonBookStore.Controllers;

[ApiController]
[Route("api/admin/sell-submissions")]
public class AdminSellSubmissionsController : ControllerBase
{
    private readonly ISellSubmissionService _sellSubmissionService;
    private readonly ISessionService _sessionService;

    public AdminSellSubmissionsController(ISellSubmissionService sellSubmissionService, ISessionService sessionService)
    {
        _sellSubmissionService = sellSubmissionService;
        _sessionService = sessionService;
    }

    private bool IsAdmin(CurrentUser? user)
    {
        return user != null && user.UserType == "Admin";
    }

    [HttpGet]
    public async Task<IActionResult> GetAdminSubmissions(
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

            var submissions = await _sellSubmissionService.GetAdminSubmissionsAsync(status);

            // Simple pagination (for school project - keep it simple)
            var totalItems = submissions.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var skip = (page - 1) * pageSize;
            var pagedSubmissions = submissions.Skip(skip).Take(pageSize).ToList();

            return Ok(new
            {
                success = true,
                data = pagedSubmissions,
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
                error = "An error occurred while retrieving submissions",
                statusCode = 500
            });
        }
    }

    [HttpGet("{submissionId}")]
    public async Task<IActionResult> GetAdminSubmissionDetails(int submissionId)
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

            if (submissionId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Invalid submission ID",
                    statusCode = 400
                });
            }

            var submission = await _sellSubmissionService.GetAdminSubmissionDetailsAsync(submissionId);
            
            if (submission == null)
            {
                return NotFound(new
                {
                    success = false,
                    error = "Submission not found",
                    statusCode = 404
                });
            }

            return Ok(new
            {
                success = true,
                data = submission
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while retrieving submission details",
                statusCode = 500
            });
        }
    }

    [HttpPost("{submissionId}/negotiate")]
    public async Task<IActionResult> AdminNegotiate(int submissionId, [FromBody] AdminNegotiateRequest? request)
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

            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Request body is required",
                    statusCode = 400
                });
            }

            if (submissionId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Invalid submission ID",
                    statusCode = 400
                });
            }

            try
            {
                var response = await _sellSubmissionService.AdminNegotiateAsync(submissionId, currentUser.UserId, request);

                return StatusCode(201, new
                {
                    success = true,
                    data = response
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
                error = "An error occurred while processing the negotiation",
                statusCode = 500
            });
        }
    }

    [HttpPut("{submissionId}/approve")]
    public async Task<IActionResult> ApproveSubmission(int submissionId, [FromBody] ApproveSubmissionRequest? request)
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

            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Request body is required",
                    statusCode = 400
                });
            }

            if (submissionId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Invalid submission ID",
                    statusCode = 400
                });
            }

            try
            {
                var response = await _sellSubmissionService.ApproveSubmissionAsync(submissionId, currentUser.UserId, request);

                return Ok(new
                {
                    success = true,
                    data = response
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
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    statusCode = 400
                });
            }
        }
        catch (Exception ex)
        {
            // Log the actual exception for debugging
            Console.WriteLine($"Error approving submission: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            
            return StatusCode(500, new
            {
                success = false,
                error = $"An error occurred while approving the submission: {ex.Message}",
                statusCode = 500
            });
        }
    }

    [HttpPut("{submissionId}/reject")]
    public async Task<IActionResult> RejectSubmission(int submissionId, [FromBody] RejectSubmissionRequest? request)
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

            if (submissionId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Invalid submission ID",
                    statusCode = 400
                });
            }

            // Request body is optional for rejection (reason is optional)
            var rejectRequest = request ?? new RejectSubmissionRequest();

            try
            {
                var success = await _sellSubmissionService.RejectSubmissionAsync(submissionId, currentUser.UserId, rejectRequest);

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            submissionId = submissionId,
                            status = "Rejected"
                        }
                    });
                }

                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to reject submission",
                    statusCode = 500
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
                error = "An error occurred while rejecting the submission",
                statusCode = 500
            });
        }
    }
}

