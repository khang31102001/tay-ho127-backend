using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.AccessControl.Domain;

/// <summary>Code is the permission's identity (e.g. "users.view") — checked against JWT `permission`
/// claims by <see cref="AdminPlatform.Common.Security.PermissionAuthorizationHandler"/>.</summary>
public sealed class Permission : CatalogEntity
{
    private Permission()
    {
        // EF Core
    }

    public static Permission Create(string code, string name)
    {
        return new Permission
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
