using AdminPlatform.SharedKernel;

namespace AdminPlatform.Modules.Organization.Domain;

public sealed class UserBrand : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid BrandId { get; private set; }

    private UserBrand()
    {
        // EF Core
    }

    public static UserBrand Create(Guid userId, Guid brandId)
    {
        return new UserBrand
        {
            Id = Guid.NewGuid(),
            UserId = Guard.NotEmpty(userId, nameof(userId)),
            BrandId = Guard.NotEmpty(brandId, nameof(brandId)),
        };
    }
}
