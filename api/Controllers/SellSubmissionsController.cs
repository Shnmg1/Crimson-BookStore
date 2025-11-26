using Microsoft.AspNetCore.Mvc;
using CrimsonBookStore.Models;
using CrimsonBookStore.Services;
using CrimsonBookStore.Helpers;

namespace CrimsonBookStore.Controllers;

[ApiController]
[Route("api/sell-submissions")]
public class SellSubmissionsController : ControllerBase
{
    private readonly ISellSubmissionService _sellSubmissionService;
    private readonly ISessionService _sessionService;

    public SellSubmissionsController(ISellSubmissionService sellSubmissionService, ISessionService sessionService)
    {
        _sellSubmissionService = sellSubmissionService;
        _sessionService = sessionService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSellSubmission([FromBody] CreateSellSubmissionRequest? request)
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

            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Request body is required",
                    statusCode = 400
                });
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.ISBN))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "ISBN is required",
                    statusCode = 400
                });
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Title is required",
                    statusCode = 400
                });
            }

            if (string.IsNullOrWhiteSpace(request.Author))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Author is required",
                    statusCode = 400
                });
            }

            if (string.IsNullOrWhiteSpace(request.Edition))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Edition is required",
                    statusCode = 400
                });
            }

            if (string.IsNullOrWhiteSpace(request.PhysicalCondition))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Physical condition is required",
                    statusCode = 400
                });
            }

            // Validate physical condition
            if (request.PhysicalCondition != "New" && 
                request.PhysicalCondition != "Good" && 
                request.PhysicalCondition != "Fair")
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Physical condition must be 'New', 'Good', or 'Fair'",
                    statusCode = 400
                });
            }

            // Validate asking price
            if (request.AskingPrice <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Asking price must be greater than 0",
                    statusCode = 400
                });
            }

            try
            {
                var submission = await _sellSubmissionService.CreateSellSubmissionAsync(currentUser.UserId, request);

                return StatusCode(201, new
                {
                    success = true,
                    data = submission
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
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "An error occurred while creating the submission",
                statusCode = 500
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetSellSubmissions([FromQuery] string? status = null)
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

            var submissions = await _sellSubmissionService.GetUserSubmissionsAsync(currentUser.UserId, status);

            return Ok(new
            {
                success = true,
                data = submissions
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
    public async Task<IActionResult> GetSubmissionDetails(int submissionId)
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
                var submission = await _sellSubmissionService.GetSubmissionDetailsAsync(submissionId, currentUser.UserId);

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
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new
                {
                    success = false,
                    error = "You do not have access to this submission",
                    statusCode = 403
                });
            }
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
    public async Task<IActionResult> Negotiate(int submissionId, [FromBody] NegotiateRequest? request)
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

            // Validate action
            if (string.IsNullOrWhiteSpace(request.Action))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Action is required (accept, reject, or counter)",
                    statusCode = 400
                });
            }

            var validActions = new[] { "accept", "reject", "counter" };
            if (!validActions.Contains(request.Action.ToLower()))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Action must be 'accept', 'reject', or 'counter'",
                    statusCode = 400
                });
            }

            // Validate counter-offer requirements
            if (request.Action.ToLower() == "counter")
            {
                if (!request.OfferedPrice.HasValue || request.OfferedPrice.Value <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Offered price is required and must be greater than 0 for counter-offers",
                        statusCode = 400
                    });
                }
            }

            // Validate accept/reject requirements
            if ((request.Action.ToLower() == "accept" || request.Action.ToLower() == "reject") &&
                !request.NegotiationId.HasValue)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Negotiation ID is required for accept or reject actions",
                    statusCode = 400
                });
            }

            try
            {
                var response = await _sellSubmissionService.NegotiateAsync(submissionId, currentUser.UserId, request);

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
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new
                {
                    success = false,
                    error = "You do not have access to this submission",
                    statusCode = 403
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
}

