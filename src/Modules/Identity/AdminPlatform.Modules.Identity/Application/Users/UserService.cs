using AdminPlatform.Common.Pagination;
using AdminPlatform.Modules.Identity.Domain;
using AdminPlatform.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Identity.Application.Users;

public sealed class UserService : IUserService
{
    private readonly IIdentityDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserScopeValidator _scopeValidator;

    public UserService(IIdentityDbContext db, IPasswordHasher passwordHasher, IUserScopeValidator scopeValidator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _scopeValidator = scopeValidator;
    }

    public async Task<PagedResult<UserResponse>> ListAsync(PagedRequest request, CancellationToken cancellationToken)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search}%";
            query = query.Where(u => EF.Functions.ILike(u.Email, pattern) || EF.Functions.ILike(u.FullName, pattern));
        }

        query = request.SortBy?.ToLowerInvariant() switch
        {
            "email" => request.IsDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "fullname" => request.IsDescending ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName),
            _ => request.IsDescending ? query.OrderByDescending(u => u.CreatedAtUtc) : query.OrderBy(u => u.CreatedAtUtc),
        };

        var projected = query.Select(u => new UserResponse(u.Id, u.Email, u.FullName, u.IsActive, u.CreatedAtUtc));
        return await projected.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<UserDetailsResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await FindOrThrowAsync(id, cancellationToken);
        return ToDetails(user);
    }

    public async Task<UserDetailsResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var alreadyExists = await _db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (alreadyExists)
        {
            throw new ConflictException($"A user with email '{email}' already exists.");
        }

        var user = User.Create(request.Email, _passwordHasher.Hash(request.Password), request.FullName);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDetails(user);
    }

    public async Task<UserDetailsResponse> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await FindOrThrowAsync(id, cancellationToken);

        user.UpdateProfile(request.FullName);
        if (request.IsActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToDetails(user);
    }

    public async Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await FindOrThrowAsync(id, cancellationToken);
        user.SetPasswordHash(_passwordHasher.Hash(request.NewPassword));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid id, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await FindOrThrowAsync(id, cancellationToken);
        if (!_passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            throw new AuthenticationFailedException("Current password is incorrect.");
        }

        user.SetPasswordHash(_passwordHasher.Hash(request.NewPassword));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetWorkingContextAsync(Guid id, SetWorkingContextRequest request, CancellationToken cancellationToken)
    {
        var user = await FindOrThrowAsync(id, cancellationToken);

        if (request.BrandId is { } brandId && !await _scopeValidator.HasBrandAccessAsync(id, brandId, cancellationToken))
        {
            throw new ForbiddenException("You do not have access to this brand.");
        }

        if (request.FiscalYearId is { } fiscalYearId && !await _scopeValidator.HasFiscalYearAccessAsync(id, fiscalYearId, cancellationToken))
        {
            throw new ForbiddenException("You do not have access to this fiscal year.");
        }

        user.SetWorkingContext(request.BrandId, request.FiscalYearId);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), id);

    private static UserDetailsResponse ToDetails(User user) => new(
        user.Id, user.Email, user.FullName, user.IsActive,
        user.CurrentBrandId, user.CurrentFiscalYearId, user.CreatedAtUtc, user.UpdatedAtUtc);
}
