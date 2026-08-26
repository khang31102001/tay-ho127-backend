using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.AccessControl.Domain;

public sealed class Role : CatalogEntity
{
    private Role()
    {
        // EF Core
    }

    public static Role Create(string code, string name)
    {
        return new Role
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
