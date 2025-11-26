using Microsoft.AspNetCore.Mvc;
using CrimsonBookStore.Services;

namespace CrimsonBookStore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IDatabaseService _databaseService;

    public HealthController(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var dbConnected = await _databaseService.TestConnectionAsync();
        
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            database = dbConnected ? "connected" : "disconnected"
        });
    }
}

