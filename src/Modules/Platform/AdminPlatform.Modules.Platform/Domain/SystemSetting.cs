using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Platform.Domain;

/// <summary>A key/value configuration entry, optionally scoped to one Organization (null = global default).</summary>
public sealed class SystemSetting : CatalogEntity
{
    public Guid? OrganizationId { get; private set; }
    public string Value { get; private set; } = string.Empty;

    private SystemSetting()
    {
        // EF Core
    }

    public static SystemSetting Create(string code, string name, string value, Guid? organizationId)
    {
        return new SystemSetting
        {
            Id = Guid.NewGuid(),
            Code = Guard.NotNullOrWhiteSpace(code, nameof(code)).Trim(),
            Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim(),
            Value = value,
            OrganizationId = organizationId,
            IsActive = true,
        };
    }

    public void Update(string name, string value, bool isActive)
    {
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        Value = value;
        IsActive = isActive;
    }
}
