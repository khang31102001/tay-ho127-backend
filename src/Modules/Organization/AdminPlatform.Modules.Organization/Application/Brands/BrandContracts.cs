namespace AdminPlatform.Modules.Organization.Application.Brands;

public sealed record CreateBrandRequest(Guid OrganizationId, string Code, string Name);

public sealed record UpdateBrandRequest(string Name, bool IsActive);

public sealed record BrandResponse(Guid Id, Guid OrganizationId, string Code, string Name, bool IsActive, DateTime CreatedAtUtc);
