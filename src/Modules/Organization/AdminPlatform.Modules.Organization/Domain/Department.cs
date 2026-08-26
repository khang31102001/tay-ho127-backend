using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Organization.Domain;

/// <summary>Self-referencing tree via ParentId (task requirement: "hỗ trợ cấu trúc cha–con").</summary>
public sealed class Department : CatalogEntity
{
    public Guid OrganizationId { get; private set; }
    public Guid? ParentId { get; private set; }

    private Department()
    {
        // EF Core
    }

    public static Department Create(Guid organizationId, string code, string name, Guid? parentId)
    {
        return new Department
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guard.NotEmpty(organizationId, nameof(organizationId)),
            Code = Guard.NotNullOrWhiteSpace(code, nameof(code)).Trim(),
            Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim(),
            ParentId = parentId,
            IsActive = true,
        };
    }

    public void Update(string name, bool isActive, Guid? parentId)
    {
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        IsActive = isActive;
        ParentId = parentId;
    }
}
