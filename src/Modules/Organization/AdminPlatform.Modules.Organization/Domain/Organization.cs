using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Organization.Domain;

public sealed class Organization : CatalogEntity
{
    private Organization()
    {
        // EF Core
    }

    public static Organization Create(string code, string name)
    {
        return new Organization
        {
            Id = Guid.NewGuid(),
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
