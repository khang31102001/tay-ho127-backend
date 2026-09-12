using System.Security.Claims;
using AdminPlatform.Common.Abstractions;
using AdminPlatform.Common.Security;
using AdminPlatform.Modules.Customer.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AdminPlatform.Modules.Customer.Application.Auth;

public sealed class CustomerAuthService : ICustomerAuthService
{
    private readonly ICustomerDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IGoogleIdTokenVerifier _googleIdTokenVerifier;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly JwtOptions _jwtOptions;

    public CustomerAuthService(
        ICustomerDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IGoogleIdTokenVerifier googleIdTokenVerifier,
        IDateTimeProvider dateTimeProvider,
        IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _googleIdTokenVerifier = googleIdTokenVerifier;
        _dateTimeProvider = dateTimeProvider;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<CustomerTokenResponse> RegisterAsync(RegisterCustomerRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var phone = request.Phone.Trim();

        var emailTaken = await _db.CustomerAuthIdentities
            .AnyAsync(i => i.Provider == CustomerAuthProvider.Local && i.Email == email, cancellationToken);
        if (emailTaken)
        {
            throw new ConflictException($"A customer with email '{email}' already exists.");
        }

        var phoneTaken = await _db.Customers.AnyAsync(c => c.Phone == phone, cancellationToken);
        if (phoneTaken)
        {
            throw new ConflictException($"A customer with phone '{phone}' already exists.");
        }

        var customer = Domain.Customer.Create(request.FullName, phone, email);
        var identity = CustomerAuthIdentity.CreateLocal(customer.Id, email, _passwordHasher.Hash(request.Password));

        _db.Customers.Add(customer);
        _db.CustomerAuthIdentities.Add(identity);
        await _db.SaveChangesAsync(cancellationToken);

        return await IssueTokenPairAsync(customer, deviceInfo: null, ipAddress, cancellationToken);
    }

    public async Task<CustomerTokenResponse> LoginAsync(CustomerLoginRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var identity = await _db.CustomerAuthIdentities
            .SingleOrDefaultAsync(i => i.Provider == CustomerAuthProvider.Local && i.Email == email, cancellationToken);

        if (identity is null || identity.PasswordHash is null || !_passwordHasher.Verify(identity.PasswordHash, request.Password))
        {
            throw new AuthenticationFailedException("Invalid email or password.");
        }

        var customer = await _db.Customers.SingleOrDefaultAsync(c => c.Id == identity.CustomerId, cancellationToken);
        if (customer is null || customer.Status != CustomerStatus.Active)
        {
            throw new AuthenticationFailedException("Invalid email or password.");
        }

        return await IssueTokenPairAsync(customer, request.DeviceInfo, ipAddress, cancellationToken);
    }

    public async Task<CustomerTokenResponse> GoogleLoginAsync(GoogleLoginRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var payload = await _googleIdTokenVerifier.VerifyAsync(request.IdToken, cancellationToken);
        if (!payload.EmailVerified)
        {
            throw new AuthenticationFailedException("Google email is not verified.");
        }

        var googleEmail = payload.Email.Trim().ToLowerInvariant();

        var googleIdentity = await _db.CustomerAuthIdentities
            .SingleOrDefaultAsync(i => i.Provider == CustomerAuthProvider.Google && i.ProviderUserId == payload.Subject, cancellationToken);

        Domain.Customer customer;
        if (googleIdentity is not null)
        {
            customer = await _db.Customers.SingleOrDefaultAsync(c => c.Id == googleIdentity.CustomerId, cancellationToken)
                ?? throw new AuthenticationFailedException("Customer account no longer exists.");
        }
        else
        {
            // Account linking (§5): only link to an existing Local account when Google itself has verified
            // ownership of that exact email — never trust an unverified or client-supplied email for this,
            // otherwise anyone could claim someone else's account by asserting their email.
            var localIdentity = await _db.CustomerAuthIdentities
                .SingleOrDefaultAsync(i => i.Provider == CustomerAuthProvider.Local && i.Email == googleEmail, cancellationToken);

            if (localIdentity is not null)
            {
                customer = await _db.Customers.SingleOrDefaultAsync(c => c.Id == localIdentity.CustomerId, cancellationToken)
                    ?? throw new AuthenticationFailedException("Customer account no longer exists.");
            }
            else
            {
                customer = Domain.Customer.Create(string.IsNullOrWhiteSpace(payload.Name) ? "Google Customer" : payload.Name, phone: null, email: googleEmail);
                _db.Customers.Add(customer);
            }

            _db.CustomerAuthIdentities.Add(CustomerAuthIdentity.CreateGoogle(customer.Id, payload.Subject, googleEmail));
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (customer.Status != CustomerStatus.Active)
        {
            throw new AuthenticationFailedException("This customer account is inactive.");
        }

        return await IssueTokenPairAsync(customer, request.DeviceInfo, ipAddress, cancellationToken);
    }

    public async Task<CustomerTokenResponse> RefreshAsync(string rawRefreshToken, string? deviceInfo, string? ipAddress, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(rawRefreshToken);
        var existing = await _db.CustomerRefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        var now = _dateTimeProvider.UtcNow;

        if (existing is null)
        {
            throw new AuthenticationFailedException("Invalid refresh token.");
        }

        if (existing.IsRevoked)
        {
            await RevokeAllForCustomerAsync(existing.CustomerId, now, cancellationToken);
            throw new AuthenticationFailedException("Refresh token has already been used. All sessions were revoked.");
        }

        if (existing.IsExpired(now))
        {
            throw new AuthenticationFailedException("Refresh token has expired.");
        }

        var customer = await _db.Customers.SingleOrDefaultAsync(c => c.Id == existing.CustomerId, cancellationToken);
        if (customer is null || customer.Status != CustomerStatus.Active)
        {
            throw new AuthenticationFailedException("Invalid refresh token.");
        }

        return await IssueTokenPairAsync(customer, deviceInfo, ipAddress, cancellationToken, existing);
    }

    public async Task LogoutAsync(string rawRefreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(rawRefreshToken);
        var existing = await _db.CustomerRefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        if (existing is null || existing.IsRevoked)
        {
            return;
        }

        existing.Revoke(_dateTimeProvider.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CustomerMeResponse> GetMeAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers.SingleOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Customer), customerId);

        return ToMeResponse(customer);
    }

    private async Task<CustomerTokenResponse> IssueTokenPairAsync(
        Domain.Customer customer,
        string? deviceInfo,
        string? ipAddress,
        CancellationToken cancellationToken,
        CustomerRefreshToken? rotateFrom = null)
    {
        var now = _dateTimeProvider.UtcNow;

        var claims = new List<Claim>
        {
            new(AppClaimTypes.UserId, customer.Id.ToString()),
            new(AppClaimTypes.AccountType, AccountTypes.Customer),
        };
        if (customer.Email is { } email)
        {
            claims.Add(new Claim(AppClaimTypes.Email, email));
        }

        var accessToken = _jwtTokenService.CreateAccessToken(claims);

        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var tokenHash = _jwtTokenService.HashRefreshToken(rawRefreshToken);
        var refreshTokenEntity = CustomerRefreshToken.Issue(
            customer.Id, tokenHash, now, TimeSpan.FromDays(_jwtOptions.RefreshTokenDays), deviceInfo, ipAddress);

        rotateFrom?.Revoke(now, refreshTokenEntity.Id);

        _db.CustomerRefreshTokens.Add(refreshTokenEntity);
        await _db.SaveChangesAsync(cancellationToken);

        return new CustomerTokenResponse(accessToken.Value, accessToken.ExpiresAtUtc, rawRefreshToken, refreshTokenEntity.ExpiresAtUtc);
    }

    private async Task RevokeAllForCustomerAsync(Guid customerId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var activeTokens = await _db.CustomerRefreshTokens
            .Where(t => t.CustomerId == customerId && t.RevokedAtUtc == null)
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

    private static CustomerMeResponse ToMeResponse(Domain.Customer customer) => new(
        customer.Id,
        customer.CustomerCode,
        customer.FullName,
        customer.Phone,
        customer.Email,
        customer.AvatarMediaId,
        customer.Status.ToString());
}
