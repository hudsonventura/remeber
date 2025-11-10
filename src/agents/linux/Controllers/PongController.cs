using Microsoft.AspNetCore.Mvc;

namespace agent.Controllers;

[ApiController]
[Route("[controller]")]
public class PongController : ControllerBase
{
    private readonly ILogger<PongController> _logger;

    public PongController(ILogger<PongController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("Pong endpoint called");
        return Ok(new { message = "Pong", timestamp = DateTime.UtcNow });
    }
}
