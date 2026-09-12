namespace AdminPlatform.Modules.Customer.Application.Auth;

public interface ICustomerAuthService
{
    Task<CustomerTokenResponse> RegisterAsync(RegisterCustomerRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<CustomerTokenResponse> LoginAsync(CustomerLoginRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<CustomerTokenResponse> GoogleLoginAsync(GoogleLoginRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<CustomerTokenResponse> RefreshAsync(string rawRefreshToken, string? deviceInfo, string? ipAddress, CancellationToken cancellationToken);

    Task LogoutAsync(string rawRefreshToken, CancellationToken cancellationToken);

    Task<CustomerMeResponse> GetMeAsync(Guid customerId, CancellationToken cancellationToken);
}
