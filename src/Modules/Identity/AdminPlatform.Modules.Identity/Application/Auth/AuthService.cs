using System.Security.Claims;
using AdminPlatform.Common.Abstractions;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Identity.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AdminPlatform.Modules.Identity.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IIdentityDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserPermissionsProvider _permissionsProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        IIdentityDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IUserPermissionsProvider permissionsProvider,
        IDateTimeProvider dateTimeProvider,
        IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _permissionsProvider = permissionsProvider;
        _dateTimeProvider = dateTimeProvider;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new AuthenticationFailedException("Invalid email or password.");
        }

        return await IssueTokenPairAsync(user, request.DeviceInfo, ipAddress, cancellationToken);
    }

    public async Task<TokenResponse> RefreshAsync(string rawRefreshToken, string? deviceInfo, string? ipAddress, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(rawRefreshToken);
        var existing = await _db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        var now = _dateTimeProvider.UtcNow;

        if (existing is null)
        {
            throw new AuthenticationFailedException("Invalid refresh token.");
        }

        if (existing.IsRevoked)
        {
            // Reuse detection: a token that was already rotated/revoked was presented again.
            // Assume the token family is compromised and kill every session for this user.
            await RevokeAllForUserAsync(existing.UserId, now, cancellationToken);
            throw new AuthenticationFailedException("Refresh token has already been used. All sessions were revoked.");
        }

        if (existing.IsExpired(now))
        {
            throw new AuthenticationFailedException("Refresh token has expired.");
        }

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == existing.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new AuthenticationFailedException("Invalid refresh token.");
        }

        return await IssueTokenPairAsync(user, deviceInfo, ipAddress, cancellationToken, existing);
    }

    public async Task LogoutAsync(string rawRefreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(rawRefreshToken);
        var existing = await _db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        if (existing is null || existing.IsRevoked)
        {
            return;
        }

        existing.Revoke(_dateTimeProvider.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task LogoutAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        await RevokeAllForUserAsync(userId, _dateTimeProvider.UtcNow, cancellationToken);
    }

    public async Task<MeResponse> GetMeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        var permissions = await _permissionsProvider.GetPermissionsAsync(userId, cancellationToken);

        return new MeResponse(
            user.Id,
            user.Email,
            user.FullName,
            permissions.Roles,
            permissions.Permissions,
            user.CurrentBrandId,
            user.CurrentFiscalYearId);
    }

    public async Task<IReadOnlyList<SessionResponse>> GetSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _db.RefreshTokens
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.IssuedAtUtc)
            .Select(t => new SessionResponse(t.Id, t.DeviceInfo, t.IpAddress, t.IssuedAtUtc, t.ExpiresAtUtc, t.RevokedAtUtc != null))
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var token = await _db.RefreshTokens.SingleOrDefaultAsync(t => t.Id == sessionId && t.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Session", sessionId);

        token.Revoke(_dateTimeProvider.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<TokenResponse> IssueTokenPairAsync(
        User user,
        string? deviceInfo,
        string? ipAddress,
        CancellationToken cancellationToken,
        RefreshToken? rotateFrom = null)
    {
        var now = _dateTimeProvider.UtcNow;
        var permissions = await _permissionsProvider.GetPermissionsAsync(user.Id, cancellationToken);

        var claims = new List<Claim>
        {
            new(AppClaimTypes.UserId, user.Id.ToString()),
            new(AppClaimTypes.Email, user.Email),
        };
        claims.AddRange(permissions.Roles.Select(role => new Claim(AppClaimTypes.Role, role)));
        claims.AddRange(permissions.Permissions.Select(permission => new Claim(AppClaimTypes.Permission, permission)));
        if (user.CurrentBrandId is { } brandId)
        {
            claims.Add(new Claim(AppClaimTypes.CurrentBrandId, brandId.ToString()));
        }
        if (user.CurrentFiscalYearId is { } fiscalYearId)
        {
            claims.Add(new Claim(AppClaimTypes.CurrentFiscalYearId, fiscalYearId.ToString()));
        }

        var accessToken = _jwtTokenService.CreateAccessToken(claims);

        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var tokenHash = _jwtTokenService.HashRefreshToken(rawRefreshToken);
        var refreshTokenEntity = RefreshToken.Issue(
            user.Id, tokenHash, now, TimeSpan.FromDays(_jwtOptions.RefreshTokenDays), deviceInfo, ipAddress);

        rotateFrom?.Revoke(now, refreshTokenEntity.Id);

        _db.RefreshTokens.Add(refreshTokenEntity);
        await _db.SaveChangesAsync(cancellationToken);

        return new TokenResponse(accessToken.Value, accessToken.ExpiresAtUtc, rawRefreshToken, refreshTokenEntity.ExpiresAtUtc);
    }

    private async Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(nowUtc);
        }

        if (activeTokens.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
