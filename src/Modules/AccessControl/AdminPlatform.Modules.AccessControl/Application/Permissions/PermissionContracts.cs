namespace AdminPlatform.Modules.AccessControl.Application.Permissions;

public sealed record CreatePermissionRequest(string Code, string Name);

public sealed record UpdatePermissionRequest(string Name, bool IsActive);

public sealed record PermissionResponse(Guid Id, string Code, string Name, bool IsActive);
