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
    /// <summary>Rate-limiter policy (registered by the Host) for the endpoint that sends a paid one-time code. Separate
    /// from "auth" so sign-ups do not eat the login budget; the per-phone / per-day limits sit in the OTP service.</summary>
    public const string OtpRequestRateLimitPolicy = "customer-otp-request";

    private readonly ICustomerAuthService _authService;

    public CustomerAuthController(ICustomerAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Step 1 of registering with phone verification: sends a one-time code to the phone. 400 when verification is
    /// off or the phone is not a Vietnamese number, 409 when the phone or email is already registered, 429 (with
    /// Retry-After) when a sending limit is hit, 503 when the code could not be sent.</summary>
    [HttpPost("register/otp")]
    [AllowAnonymous]
    [EnableRateLimiting(OtpRequestRateLimitPolicy)]
    [ProducesResponseType<RegistrationOtpResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RegistrationOtpResponse>> RequestRegistrationOtp(
        [FromBody] RequestRegistrationOtpRequest request, CancellationToken cancellationToken) =>
        Ok(await _authService.RequestRegistrationOtpAsync(request, RemoteIpAddress, cancellationToken));

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<CustomerTokenResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerTokenResponse>> Register([FromBody] RegisterCustomerRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, RemoteIpAddress, cancellationToken);
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
