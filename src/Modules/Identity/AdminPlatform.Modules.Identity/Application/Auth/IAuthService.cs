namespace AdminPlatform.Modules.Identity.Application.Auth;

public interface IAuthService
{
    Task<TokenResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);

    Task<TokenResponse> RefreshAsync(string rawRefreshToken, string? deviceInfo, string? ipAddress, CancellationToken cancellationToken);

    Task LogoutAsync(string rawRefreshToken, CancellationToken cancellationToken);

    Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken);

    Task<MeResponse> GetMeAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SessionResponse>> GetSessionsAsync(Guid userId, CancellationToken cancellationToken);

    Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
}
