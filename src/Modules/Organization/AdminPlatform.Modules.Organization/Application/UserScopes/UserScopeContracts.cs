namespace AdminPlatform.Modules.Organization.Application.UserScopes;

public sealed record UserDepartmentResponse(Guid DepartmentId, string DepartmentCode, string DepartmentName);

public sealed record UserBrandResponse(Guid BrandId, string BrandCode, string BrandName);

public sealed record AssignDepartmentRequest(Guid DepartmentId);

public sealed record AssignBrandRequest(Guid BrandId);
