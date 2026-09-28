using AdminPlatform.Modules.Organization.Application;
using AdminPlatform.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.Modules.Organization.Infrastructure;

/// <summary>Idempotent DEMO data (Migrator `seed-demo` only): a small department tree under the sample
/// Organization's root department, two extra brands, and department/brand scopes for each demo persona.
/// Run after the base <see cref="OrganizationSeeder"/>. Upserted by Code; nothing is ever removed.</summary>
public static class OrganizationDemoSeeder
{
    private static readonly (string Code, string Name, string ParentCode)[] DemoDepartments =
    [
        ("sales", "Sales", OrganizationSeeder.SampleDepartmentCode),
        ("marketing", "Marketing", OrganizationSeeder.SampleDepartmentCode),
        ("it", "IT", OrganizationSeeder.SampleDepartmentCode),
    ];

    private static readonly (string Code, string Name)[] DemoBrands =
    [
        ("premium", "Tay Ho 127 Premium"),
        ("online", "Tay Ho 127 Online"),
    ];

    private static readonly (string Persona, string[] DepartmentCodes, string[] BrandCodes)[] PersonaScopes =
    [
        ("manager", [OrganizationSeeder.SampleDepartmentCode], [OrganizationSeeder.SampleBrandCode, "premium", "online"]),
        ("staff", ["sales"], [OrganizationSeeder.SampleBrandCode]),
        ("viewer", ["marketing"], ["online"]),
    ];

    public static async Task SeedAsync(
        IServiceProvider services,
        Guid sampleOrganizationId,
        IReadOnlyDictionary<string, Guid> userIdsByPersona,
        CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IOrganizationDbContext>();

        var departments = await db.Departments
            .Where(d => d.OrganizationId == sampleOrganizationId)
            .ToDictionaryAsync(d => d.Code, cancellationToken);
        foreach (var (code, name, parentCode) in DemoDepartments)
        {
            if (!departments.ContainsKey(code))
            {
                var department = Department.Create(sampleOrganizationId, code, name, departments[parentCode].Id);
                db.Departments.Add(department);
                departments[code] = department;
            }
        }

        var brands = await db.Brands
            .Where(b => b.OrganizationId == sampleOrganizationId)
            .ToDictionaryAsync(b => b.Code, cancellationToken);
        foreach (var (code, name) in DemoBrands)
        {
            if (!brands.ContainsKey(code))
            {
                var brand = Brand.Create(sampleOrganizationId, code, name);
                db.Brands.Add(brand);
                brands[code] = brand;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var (persona, departmentCodes, brandCodes) in PersonaScopes)
        {
            if (!userIdsByPersona.TryGetValue(persona, out var userId))
            {
                continue;
            }

            foreach (var departmentId in departmentCodes.Select(code => departments[code].Id))
            {
                if (!await db.UserDepartments.AnyAsync(ud => ud.UserId == userId && ud.DepartmentId == departmentId, cancellationToken))
                {
                    db.UserDepartments.Add(UserDepartment.Create(userId, departmentId));
                }
            }

            foreach (var brandId in brandCodes.Select(code => brands[code].Id))
            {
                if (!await db.UserBrands.AnyAsync(ub => ub.UserId == userId && ub.BrandId == brandId, cancellationToken))
                {
                    db.UserBrands.Add(UserBrand.Create(userId, brandId));
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
