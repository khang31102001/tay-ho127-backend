namespace AdminPlatform.Modules.AccessControl.Application.UserRoles;

public sealed record UserRoleResponse(Guid RoleId, string RoleCode, string RoleName);

public sealed record AssignRoleRequest(Guid RoleId);
