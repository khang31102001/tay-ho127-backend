using AdminPlatform.Common.Pagination;

namespace AdminPlatform.Modules.Identity.Application.Users;

public interface IUserService
{
    Task<PagedResult<UserResponse>> ListAsync(PagedRequest request, CancellationToken cancellationToken);

    Task<UserDetailsResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<UserDetailsResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<UserDetailsResponse> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken);

    Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken);

    Task ChangePasswordAsync(Guid id, ChangePasswordRequest request, CancellationToken cancellationToken);

    Task SetWorkingContextAsync(Guid id, SetWorkingContextRequest request, CancellationToken cancellationToken);
}
