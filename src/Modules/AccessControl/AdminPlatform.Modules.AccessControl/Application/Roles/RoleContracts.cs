namespace AdminPlatform.Modules.AccessControl.Application.Roles;

public sealed record CreateRoleRequest(string Code, string Name);

public sealed record UpdateRoleRequest(string Name, bool IsActive);

public sealed record RoleResponse(Guid Id, string Code, string Name, bool IsActive, DateTime CreatedAtUtc);

public sealed record AssignPermissionsRequest(IReadOnlyCollection<Guid> PermissionIds);
