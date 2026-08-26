using AdminPlatform.Modules.Organization.Application;
using AdminPlatform.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using OrganizationEntity = AdminPlatform.Modules.Organization.Domain.Organization;

namespace AdminPlatform.Modules.Organization.Infrastructure;

public sealed class OrganizationDbContext : DbContext, IOrganizationDbContext
{
    public const string Schema = "organization";

    public DbSet<OrganizationEntity> Organizations => Set<OrganizationEntity>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<UserDepartment> UserDepartments => Set<UserDepartment>();
    public DbSet<UserBrand> UserBrands => Set<UserBrand>();

    public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizationDbContext).Assembly);
    }
}
