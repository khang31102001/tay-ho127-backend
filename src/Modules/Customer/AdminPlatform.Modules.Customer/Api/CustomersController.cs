using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Customer.Application.Auth;
using AdminPlatform.Modules.Customer.Application.Customers;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Customer.Api;

[ApiController]
[Route("api/v1/customers/me")]
[RequireAccountType(AccountTypes.Customer)]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerAuthService _authService;
    private readonly ICustomerProfileService _profileService;

    public CustomersController(ICustomerAuthService authService, ICustomerProfileService profileService)
    {
        _authService = authService;
        _profileService = profileService;
    }

    [HttpGet]
    [ProducesResponseType<CustomerMeResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerMeResponse>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _authService.GetMeAsync(User.GetUserId(), cancellationToken));
    }

    [HttpPut]
    [ProducesResponseType<CustomerProfileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerProfileResponse>> UpdateProfile([FromBody] UpdateCustomerProfileRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _profileService.UpdateProfileAsync(User.GetUserId(), request, cancellationToken));
    }

    [HttpPut("avatar")]
    [ProducesResponseType<CustomerProfileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerProfileResponse>> UpdateAvatar([FromBody] UpdateCustomerAvatarRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _profileService.UpdateAvatarAsync(User.GetUserId(), request, cancellationToken));
    }
}
