using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/api/healthz")]
    public IActionResult GetHealthz()
    {
        var gitHash = Environment.GetEnvironmentVariable("GIT_HASH") ?? "unknown";
        return Ok(new { status = "Healthy", gitHash });
    }
}