using AdminPlatform.Modules.Customer.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AdminPlatform.Modules.Customer.Api;

[ApiController]
[Route("api/v1/customer/auth")]
[EnableRateLimiting("auth")]
public sealed class CustomerAuthController : ControllerBase
{
    private readonly ICustomerAuthService _authService;

    public CustomerAuthController(ICustomerAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<CustomerTokenResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerTokenResponse>> Register([FromBody] RegisterCustomerRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, RemoteIpAddress, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Registration step 1. Sends a one-time code to the phone; no account exists yet. 429 when the number
    /// asks for codes too often.</summary>
    [HttpPost("register/request-otp")]
    [AllowAnonymous]
    [ProducesResponseType<RegistrationOtpResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RegistrationOtpResponse>> RequestRegistrationOtp([FromBody] RequestRegistrationOtpRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RequestRegistrationOtpAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>Registration step 2. A correct code creates the account (phone verified) and signs it in.</summary>
    [HttpPost("register/verify")]
    [AllowAnonymous]
    [ProducesResponseType<CustomerTokenResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerTokenResponse>> VerifyRegistrationOtp([FromBody] VerifyRegistrationOtpRequest request, CancellationToken cancellationToken)
    {
        var effectiveRequest = request with { DeviceInfo = request.DeviceInfo ?? Request.Headers["User-Agent"].ToString() };
        var response = await _authService.VerifyRegistrationOtpAsync(effectiveRequest, RemoteIpAddress, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<CustomerTokenResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerTokenResponse>> Login([FromBody] CustomerLoginRequest request, CancellationToken cancellationToken)
    {
        var effectiveRequest = request with { DeviceInfo = request.DeviceInfo ?? Request.Headers["User-Agent"].ToString() };
        var response = await _authService.LoginAsync(effectiveRequest, RemoteIpAddress, cancellationToken);
        return Ok(response);
    }

    [HttpPost("google")]
    [AllowAnonymous]
    [ProducesResponseType<CustomerTokenResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerTokenResponse>> Google([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        var effectiveRequest = request with { DeviceInfo = request.DeviceInfo ?? Request.Headers["User-Agent"].ToString() };
        var response = await _authService.GoogleLoginAsync(effectiveRequest, RemoteIpAddress, cancellationToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<CustomerTokenResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerTokenResponse>> Refresh([FromBody] CustomerRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshAsync(request.RefreshToken, Request.Headers["User-Agent"].ToString(), RemoteIpAddress, cancellationToken);
        return Ok(response);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] CustomerRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    private string? RemoteIpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();
}
