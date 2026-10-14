namespace AdminPlatform.Modules.Customer.Application.Auth;

public interface ICustomerAuthService
{
    Task<CustomerTokenResponse> RegisterAsync(RegisterCustomerRequest request, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>Registration step 1: validates the sign-up, stores it pending and sends a one-time code to the phone.</summary>
    Task<RegistrationOtpResponse> RequestRegistrationOtpAsync(RequestRegistrationOtpRequest request, CancellationToken cancellationToken);

    /// <summary>Registration step 2: checks the code and, when right, creates the account (phone marked verified) and signs it in.</summary>
    Task<CustomerTokenResponse> VerifyRegistrationOtpAsync(VerifyRegistrationOtpRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<CustomerTokenResponse> LoginAsync(CustomerLoginRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<CustomerTokenResponse> GoogleLoginAsync(GoogleLoginRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<CustomerTokenResponse> RefreshAsync(string rawRefreshToken, string? deviceInfo, string? ipAddress, CancellationToken cancellationToken);

    Task LogoutAsync(string rawRefreshToken, CancellationToken cancellationToken);

    Task<CustomerMeResponse> GetMeAsync(Guid customerId, CancellationToken cancellationToken);
}
