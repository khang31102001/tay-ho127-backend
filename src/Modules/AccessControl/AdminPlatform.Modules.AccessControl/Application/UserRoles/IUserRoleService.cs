namespace AdminPlatform.Modules.AccessControl.Application.UserRoles;

public interface IUserRoleService
{
    Task<IReadOnlyList<UserRoleResponse>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task AssignAsync(Guid userId, AssignRoleRequest request, CancellationToken cancellationToken);

    Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken);
}
