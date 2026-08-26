namespace AdminPlatform.Modules.Platform.Application.SystemSettings;

public sealed record CreateSystemSettingRequest(string Code, string Name, string Value, Guid? OrganizationId);

public sealed record UpdateSystemSettingRequest(string Name, string Value, bool IsActive);

public sealed record SystemSettingResponse(Guid Id, string Code, string Name, string Value, bool IsActive, Guid? OrganizationId);
