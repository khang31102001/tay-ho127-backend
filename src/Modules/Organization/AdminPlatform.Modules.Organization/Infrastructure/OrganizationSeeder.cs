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

    /// <summary>Returns the sample Organization's id so the Migrator can pass it to modules that seed
    /// their own Organization-scoped sample data (e.g. Platform's sample FiscalYear).</summary>
    public static async Task<Guid> SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
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

        var mainBranch = await db.Brands.SingleOrDefaultAsync(
            b => b.OrganizationId == organization.Id && b.Code == SampleBrandCode, cancellationToken);
        if (mainBranch is null)
        {
            mainBranch = Brand.Create(organization.Id, SampleBrandCode, "Chi nhánh chính");
            db.Brands.Add(mainBranch);
        }

        // Contact of the real restaurant (from the website's former static data) so the site has an address from day one —
        // only while the branch has none, so an admin's edit is never overwritten.
        if (mainBranch.AddressLine is null)
        {
            mainBranch.UpdateContact(new BrandContact(
                "0900 127 127", null, null, "127 Đinh Tiên Hoàng", "Đa Kao", "Quận 1", "TP. Hồ Chí Minh", "06:00", "21:30",
                "Mở cửa tất cả các ngày trong tuần."));
        }

        // The website needs a primary branch: only when none exists yet, so an admin choice is never overridden.
        if (!await db.Brands.AnyAsync(b => b.IsPrimary, cancellationToken))
        {
            mainBranch.SetPrimary(true);
        }

        // The brand-wide identity (one row), created with the defaults only while absent.
        if (!await db.BrandProfiles.AnyAsync(cancellationToken))
        {
            db.BrandProfiles.Add(BrandProfile.Create(new BrandProfileDetails(
                "Bánh Cuốn Tây Hồ 127",
                "Bánh cuốn truyền thống, phục vụ nhanh, hương vị gia đình Bắc giữa Sài Gòn.",
                null, null, null,
                [
                    new SocialLink("website", "https://tayho127.com", 1, true),
                    new SocialLink("facebook", "https://www.facebook.com/tayho127", 2, true),
                    new SocialLink("instagram", "https://www.instagram.com/tayho127", 3, true),
                    new SocialLink("tiktok", "https://www.tiktok.com/@tayho127", 4, true),
                    new SocialLink("shopee", "https://shopee.vn/tayho127", 5, true),
                ],
                null, null, null, null, null)));
        }

        await db.SaveChangesAsync(cancellationToken);

        return organization.Id;
    }
}
