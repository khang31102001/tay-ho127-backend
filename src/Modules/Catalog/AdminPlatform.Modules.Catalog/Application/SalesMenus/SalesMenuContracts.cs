namespace AdminPlatform.Modules.Catalog.Application.SalesMenus;

public sealed record CreateSalesMenuRequest(string Code, string Name, bool IsActive);

/// <summary>Code is immutable after creation (the website looks menus up by it).</summary>
public sealed record UpdateSalesMenuRequest(string Name, bool IsActive);

public sealed record SalesMenuResponse(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
