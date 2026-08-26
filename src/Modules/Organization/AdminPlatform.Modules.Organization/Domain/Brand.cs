using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Organization.Domain;

public sealed class Brand : CatalogEntity
{
    public Guid OrganizationId { get; private set; }

    private Brand()
    {
        // EF Core
    }

    public static Brand Create(Guid organizationId, string code, string name)
    {
        return new Brand
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guard.NotEmpty(organizationId, nameof(organizationId)),
            Code = Guard.NotNullOrWhiteSpace(code, nameof(code)).Trim(),
            Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim(),
            IsActive = true,
        };
    }

    public void Update(string name, bool isActive)
    {
        Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        IsActive = isActive;
    }
}
