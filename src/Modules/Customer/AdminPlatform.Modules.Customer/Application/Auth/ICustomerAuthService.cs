namespace AdminPlatform.Modules.Customer.Application.Auth;

public interface ICustomerAuthService
{
    /// <summary>Step 1 of registering with phone verification: checks the phone and email are free, then sends a code to the
    /// phone. 400 when verification is switched off or the phone is not a Vietnamese number, 409 when the phone or email is
    /// taken, 429 when a sending limit is hit, 503 when the code could not be sent.</summary>
    Task<RegistrationOtpResponse> RequestRegistrationOtpAsync(RequestRegistrationOtpRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<CustomerTokenResponse> RegisterAsync(RegisterCustomerRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<CustomerTokenResponse> LoginAsync(CustomerLoginRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<CustomerTokenResponse> GoogleLoginAsync(GoogleLoginRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<CustomerTokenResponse> RefreshAsync(string rawRefreshToken, string? deviceInfo, string? ipAddress, CancellationToken cancellationToken);

    Task LogoutAsync(string rawRefreshToken, CancellationToken cancellationToken);

    Task<CustomerMeResponse> GetMeAsync(Guid customerId, CancellationToken cancellationToken);
}
