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
    private readonly ICustomerPhoneVerifier _phoneVerifier;
    private readonly PhoneVerificationOptions _phoneVerification;

    public CustomerAuthService(
        ICustomerDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IGoogleIdTokenVerifier googleIdTokenVerifier,
        IDateTimeProvider dateTimeProvider,
        IOptions<JwtOptions> jwtOptions,
        ICustomerPhoneVerifier phoneVerifier,
        IOptions<PhoneVerificationOptions> phoneVerification)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _googleIdTokenVerifier = googleIdTokenVerifier;
        _dateTimeProvider = dateTimeProvider;
        _jwtOptions = jwtOptions.Value;
        _phoneVerifier = phoneVerifier;
        _phoneVerification = phoneVerification.Value;
    }

    public async Task<RegistrationOtpResponse> RequestRegistrationOtpAsync(
        RequestRegistrationOtpRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        if (!_phoneVerification.Required)
        {
            throw new BusinessRuleValidationException("Tính năng xác minh số điện thoại khi đăng ký chưa được bật.");
        }

        var phone = NormalizeRequiredPhone(request.Phone);
        var email = request.Email.Trim().ToLowerInvariant();

        // Checked before a code is sent: a number or email that is already taken would waste a paid message, and the
        // user can fix it straight away instead of finding out only after typing the code.
        await EnsureContactAvailableAsync(email, phone, cancellationToken);

        var issued = await _phoneVerifier.SendRegistrationCodeAsync(phone, ipAddress, cancellationToken);
        return new RegistrationOtpResponse(issued.ChallengeId, issued.ExpiresAtUtc, issued.ResendAfterSeconds);
    }

    public async Task<CustomerTokenResponse> RegisterAsync(RegisterCustomerRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var verificationRequired = _phoneVerification.Required;

        // With verification on the number must be a real Vietnamese number (a code is about to go to it); with it
        // off the old, lenient behaviour is kept exactly.
        var phone = verificationRequired ? NormalizeRequiredPhone(request.Phone) : VietnamesePhone.NormalizeLenient(request.Phone);

        if (verificationRequired && (request.OtpChallengeId is null || string.IsNullOrWhiteSpace(request.OtpCode)))
        {
            throw new BusinessRuleValidationException("Vui lòng xác minh số điện thoại bằng mã OTP trước khi đăng ký.");
        }

        // Duplicates are checked BEFORE the code is checked, so a taken email never uses up a correct code.
        await EnsureContactAvailableAsync(email, phone, cancellationToken);

        DateTime? phoneVerifiedAtUtc = null;
        if (verificationRequired)
        {
            var outcome = await _phoneVerifier.VerifyRegistrationCodeAsync(
                request.OtpChallengeId!.Value, phone!, request.OtpCode!.Trim(), cancellationToken);
            if (outcome != PhoneVerificationOutcome.Verified)
            {
                throw new BusinessRuleValidationException(OtpFailureMessage(outcome));
            }

            phoneVerifiedAtUtc = _dateTimeProvider.UtcNow;
        }

        var customer = Domain.Customer.Create(request.FullName, phone, email, phoneVerifiedAtUtc);
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

    /// <summary>Email and phone are unique across customers; checked up front so the caller gets a readable 409 instead of
    /// a raw constraint violation. A null phone has nothing to clash with.</summary>
    private async Task EnsureContactAvailableAsync(string email, string? phone, CancellationToken cancellationToken)
    {
        var emailTaken = await _db.CustomerAuthIdentities
            .AnyAsync(i => i.Provider == CustomerAuthProvider.Local && i.Email == email, cancellationToken);
        if (emailTaken)
        {
            throw new ConflictException($"A customer with email '{email}' already exists.");
        }

        if (phone is not null && await _db.Customers.AnyAsync(c => c.Phone == phone, cancellationToken))
        {
            throw new ConflictException($"A customer with phone '{phone}' already exists.");
        }
    }

    private static string NormalizeRequiredPhone(string? phone)
    {
        if (!VietnamesePhone.TryNormalize(phone, out var normalized))
        {
            throw new BusinessRuleValidationException("Số điện thoại không đúng định dạng.");
        }

        return normalized;
    }

    /// <summary>One message per outcome, all in the user's language and none revealing more than it must.</summary>
    private static string OtpFailureMessage(PhoneVerificationOutcome outcome) => outcome switch
    {
        PhoneVerificationOutcome.Expired => "Mã xác minh đã hết hạn. Vui lòng yêu cầu mã mới.",
        PhoneVerificationOutcome.TooManyAttempts => "Bạn đã nhập sai quá số lần cho phép. Vui lòng yêu cầu mã mới.",
        _ => "Mã xác minh không đúng hoặc đã được sử dụng.",
    };

    private static CustomerMeResponse ToMeResponse(Domain.Customer customer) => new(
        customer.Id,
        customer.CustomerCode,
        customer.FullName,
        customer.Phone,
        customer.Email,
        customer.AvatarMediaId,
        customer.Status.ToString(),
        customer.IsPhoneVerified);
}
