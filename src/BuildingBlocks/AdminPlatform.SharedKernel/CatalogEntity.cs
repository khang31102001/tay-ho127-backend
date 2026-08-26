namespace AdminPlatform.SharedKernel;

/// <summary>
/// Base for master/catalog-style entities (Organizations, Departments, Brands,
/// Roles, Permissions, FiscalYears, Menus, SystemSettings, ...): identified by
/// a stable business Code, human-readable Name, and an enable/disable status.
/// </summary>
public abstract class CatalogEntity : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
