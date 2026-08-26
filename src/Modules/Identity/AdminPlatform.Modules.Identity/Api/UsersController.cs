using AdminPlatform.Common.Pagination;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Identity.Application.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Identity.Api;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [RequirePermission(IdentityPermissions.UsersView)]
    [ProducesResponseType<PagedResult<UserResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserResponse>>> List([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _userService.ListAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(IdentityPermissions.UsersView)]
    [ProducesResponseType<UserDetailsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDetailsResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _userService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(IdentityPermissions.UsersCreate)]
    [ProducesResponseType<UserDetailsResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserDetailsResponse>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var created = await _userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(IdentityPermissions.UsersUpdate)]
    [ProducesResponseType<UserDetailsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDetailsResponse>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _userService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/reset-password")]
    [RequirePermission(IdentityPermissions.UsersResetPassword)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _userService.ResetPasswordAsync(id, request, cancellationToken);
        return NoContent();
    }
}
