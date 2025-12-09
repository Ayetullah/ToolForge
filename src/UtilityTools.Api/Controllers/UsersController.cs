using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityTools.Application.Features.Users.Queries.GetUsageStatistics;
using UtilityTools.Application.Features.Users.Queries.GetUserProfile;

namespace UtilityTools.Api.Controllers;

[ApiController]
[Route("api/users")]
[Route("api/v{version:apiVersion}/users")] // ✅ Support both versioned and non-versioned routes
[ApiVersion("1.0")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("profile")]
    [ResponseCache(Duration = 30)] // ✅ Cache for 30 seconds (varies by user via Authorization header)
    public async Task<ActionResult<GetUserProfileResponse>> GetProfile()
    {
        var query = new GetUserProfileQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("usage-statistics")]
    public async Task<ActionResult<GetUsageStatisticsResponse>> GetUsageStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var query = new GetUsageStatisticsQuery
        {
            StartDate = startDate,
            EndDate = endDate
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

