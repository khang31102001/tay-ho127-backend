using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Organization.Application.UserScopes;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Organization.Api;

[ApiController]
[Route("api/v1/users/{userId:guid}/brands")]
public sealed class UserBrandsController : ControllerBase
{
    private readonly IUserScopeService _userScopeService;

    public UserBrandsController(IUserScopeService userScopeService)
    {
        _userScopeService = userScopeService;
    }

    [HttpGet]
    [RequirePermission(OrganizationPermissions.BrandsView)]
    [ProducesResponseType<IReadOnlyList<UserBrandResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserBrandResponse>>> List(Guid userId, CancellationToken cancellationToken)
    {
        return Ok(await _userScopeService.ListBrandsAsync(userId, cancellationToken));
    }

    [HttpPost]
    [RequirePermission(OrganizationPermissions.UsersManageBrands)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Assign(Guid userId, [FromBody] AssignBrandRequest request, CancellationToken cancellationToken)
    {
        await _userScopeService.AssignBrandAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{brandId:guid}")]
    [RequirePermission(OrganizationPermissions.UsersManageBrands)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(Guid userId, Guid brandId, CancellationToken cancellationToken)
    {
        await _userScopeService.RemoveBrandAsync(userId, brandId, cancellationToken);
        return NoContent();
    }
}
