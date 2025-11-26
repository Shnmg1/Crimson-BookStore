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

