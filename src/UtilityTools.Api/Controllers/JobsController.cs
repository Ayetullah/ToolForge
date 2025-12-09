using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityTools.Application.Features.Jobs.Queries.GetJobStatus;

namespace UtilityTools.Api.Controllers;

[ApiController]
[Route("api/jobs")]
[Route("api/v{version:apiVersion}/jobs")] // ✅ Support both versioned and non-versioned routes
[ApiVersion("1.0")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public JobsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{jobId}/status")]
    public async Task<ActionResult<GetJobStatusResponse>> GetJobStatus(Guid jobId)
    {
        var query = new GetJobStatusQuery { JobId = jobId };
        var result = await _mediator.Send(query);
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            return NotFound(new { error = result.ErrorMessage });
        }
        return Ok(result);
    }
}

