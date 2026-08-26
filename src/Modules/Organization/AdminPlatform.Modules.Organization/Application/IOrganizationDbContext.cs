using AdminPlatform.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;

namespace AdminPlatform.Modules.Organization.Application;

public interface IOrganizationDbContext
{
    DbSet<Domain.Organization> Organizations { get; }
    DbSet<Department> Departments { get; }
    DbSet<Brand> Brands { get; }
    DbSet<UserDepartment> UserDepartments { get; }
    DbSet<UserBrand> UserBrands { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
