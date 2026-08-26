using AdminPlatform.Modules.Organization.Application;
using AdminPlatform.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrganizationEntity = AdminPlatform.Modules.Organization.Domain.Organization;

namespace AdminPlatform.Modules.Organization.Infrastructure;

/// <summary>Idempotent sample data: one Organization, one root Department, one Brand — upserted by Code.</summary>
public static class OrganizationSeeder
{
    public const string SampleOrganizationCode = "hq";
    public const string SampleDepartmentCode = "general";
    public const string SampleBrandCode = "main";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IOrganizationDbContext>();

        var organization = await db.Organizations.SingleOrDefaultAsync(o => o.Code == SampleOrganizationCode, cancellationToken);
        if (organization is null)
        {
            organization = OrganizationEntity.Create(SampleOrganizationCode, "Head Office");
            db.Organizations.Add(organization);
            await db.SaveChangesAsync(cancellationToken);
        }

        var departmentExists = await db.Departments.AnyAsync(
            d => d.OrganizationId == organization.Id && d.Code == SampleDepartmentCode, cancellationToken);
        if (!departmentExists)
        {
            db.Departments.Add(Department.Create(organization.Id, SampleDepartmentCode, "General", null));
        }

        var brandExists = await db.Brands.AnyAsync(
            b => b.OrganizationId == organization.Id && b.Code == SampleBrandCode, cancellationToken);
        if (!brandExists)
        {
            db.Brands.Add(Brand.Create(organization.Id, SampleBrandCode, "Main Brand"));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
