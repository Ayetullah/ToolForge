using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityTools.Application.Features.Admin.Queries.GetAllUsers;
using UtilityTools.Application.Features.Admin.Queries.GetSystemStats;

namespace UtilityTools.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Route("api/v{version:apiVersion}/admin")] // ✅ Support both versioned and non-versioned routes
[ApiVersion("1.0")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("users")]
    public async Task<ActionResult<GetAllUsersResponse>> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? subscriptionTier = null)
    {
        var query = new GetAllUsersQuery
        {
            Page = page,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            SubscriptionTier = subscriptionTier
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<GetSystemStatsResponse>> GetSystemStats()
    {
        var query = new GetSystemStatsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

