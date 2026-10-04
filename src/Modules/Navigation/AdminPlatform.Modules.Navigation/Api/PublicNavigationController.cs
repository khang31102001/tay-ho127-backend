using AdminPlatform.Modules.Navigation.Application.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPlatform.Modules.Navigation.Api;

/// <summary>Anonymous read-only website menus (header, footer, mobile) for the public site. The admin sidebar and any
/// gated item are never returned here.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/navigation/public")]
public sealed class PublicNavigationController : ControllerBase
{
    private readonly IPublicNavigationService _publicNavigationService;

    public PublicNavigationController(IPublicNavigationService publicNavigationService)
    {
        _publicNavigationService = publicNavigationService;
    }

    /// <summary>`location` is header, footer or mobile. 404 for any other value.</summary>
    [HttpGet("{location}")]
    [ProducesResponseType<PublicNavigationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicNavigationResponse>> GetByLocation(string location, CancellationToken cancellationToken)
    {
        return Ok(await _publicNavigationService.GetByLocationAsync(location, cancellationToken));
    }
}
