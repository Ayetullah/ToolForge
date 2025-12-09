using Microsoft.AspNetCore.Mvc;

namespace UtilityTools.Api.Controllers;

[ApiController]
[Route("/")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "UtilityTools API v1", status = "running" });
    }
}

