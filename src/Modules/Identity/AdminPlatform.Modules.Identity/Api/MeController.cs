using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Identity.Application.Auth;
using AdminPlatform.Modules.Identity.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Identity.Api;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    public MeController(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    [HttpGet]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _authService.GetMeAsync(User.GetUserId(), cancellationToken));
    }

    [HttpGet("sessions")]
    [ProducesResponseType<IReadOnlyList<SessionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SessionResponse>>> GetSessions(CancellationToken cancellationToken)
    {
        return Ok(await _authService.GetSessionsAsync(User.GetUserId(), cancellationToken));
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        await _authService.RevokeSessionAsync(User.GetUserId(), sessionId, cancellationToken);
        return NoContent();
    }

    [HttpPut("working-context")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetWorkingContext([FromBody] SetWorkingContextRequest request, CancellationToken cancellationToken)
    {
        await _userService.SetWorkingContextAsync(User.GetUserId(), request, cancellationToken);
        return NoContent();
    }

    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await _userService.ChangePasswordAsync(User.GetUserId(), request, cancellationToken);
        return NoContent();
    }
}
