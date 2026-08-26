namespace AdminPlatform.Modules.Organization.Application.Organizations;

public sealed record CreateOrganizationRequest(string Code, string Name);

public sealed record UpdateOrganizationRequest(string Name, bool IsActive);

public sealed record OrganizationResponse(Guid Id, string Code, string Name, bool IsActive, DateTime CreatedAtUtc);
