namespace AdminPlatform.Modules.Organization.Application.Departments;

public sealed record CreateDepartmentRequest(Guid OrganizationId, string Code, string Name, Guid? ParentId);

public sealed record UpdateDepartmentRequest(string Name, bool IsActive, Guid? ParentId);

public sealed record DepartmentResponse(
    Guid Id, Guid OrganizationId, string Code, string Name, bool IsActive, Guid? ParentId, DateTime CreatedAtUtc);

public sealed record DepartmentTreeNode(
    Guid Id, string Code, string Name, bool IsActive, IReadOnlyList<DepartmentTreeNode> Children);
