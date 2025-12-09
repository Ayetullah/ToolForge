using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityTools.Application.Features.Auth.Commands.ForgotPassword;
using UtilityTools.Application.Features.Auth.Commands.Login;
using UtilityTools.Application.Features.Auth.Commands.Logout;
using UtilityTools.Application.Features.Auth.Commands.RefreshToken;
using UtilityTools.Application.Features.Auth.Commands.Register;
using UtilityTools.Application.Features.Auth.Commands.ResendVerification;
using UtilityTools.Application.Features.Auth.Commands.ResetPassword;
using UtilityTools.Application.Features.Auth.Commands.VerifyEmail;

namespace UtilityTools.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")] // ✅ Support both versioned and non-versioned routes
[ApiVersion("1.0")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        return Created($"/api/users/{result.UserId}", result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh([FromBody] RefreshTokenCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout([FromBody] LogoutCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<VerifyEmailResponse>> VerifyEmail([FromBody] VerifyEmailCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("resend-verification")]
    public async Task<ActionResult<ResendVerificationResponse>> ResendVerification([FromBody] ResendVerificationCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}

