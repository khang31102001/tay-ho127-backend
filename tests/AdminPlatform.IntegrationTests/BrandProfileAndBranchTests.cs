using System.Net;
using System.Net.Http.Json;
using AdminPlatform.Modules.Organization.Application.BrandProfiles;
using AdminPlatform.Modules.Organization.Application.Brands;
using AdminPlatform.Modules.Organization.Application.Organizations;

namespace AdminPlatform.IntegrationTests;

[Collection("Api")]
public class BrandProfileAndBranchTests
{
    private readonly AdminPlatformApiFactory _factory;

    public BrandProfileAndBranchTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Brand_profile_is_one_record_edited_by_an_admin_and_read_publicly_with_the_primary_branch()
    {
        using var admin = await AuthTestHelper.CreateAdminClientAsync(_factory);
        using var anonymous = _factory.CreateClient();

        // The profile is a singleton that always exists; social links are validated.
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/organization/brand-profile")).StatusCode);
        var bad = new UpdateBrandProfileRequest("Brand", "Slogan", null, null, null, [new SocialLinkDto("facebook", "javascript:x", 1, true)], null, null, null, null, null);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/v1/organization/brand-profile", bad)).StatusCode);

        var tagline = $"Slogan {Guid.NewGuid():n}"[..16];
        var ok = new UpdateBrandProfileRequest("Bánh Cuốn Test", tagline, "Mô tả", "0312345678", "Công ty Test",
            [new SocialLinkDto("facebook", "https://facebook.com/test", 1, true)], null, null, null, null, null);
        var saved = await admin.PutAsJsonAsync("/api/v1/organization/brand-profile", ok);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        // A branch with its own address/hours becomes the primary one (only one can be).
        var suffix = Guid.NewGuid().ToString("n")[..8];
        var org = (await (await admin.PostAsJsonAsync("/api/v1/organizations", new CreateOrganizationRequest($"org{suffix}", "Org"))).Content.ReadFromJsonAsync<OrganizationResponse>())!;
        var first = (await (await admin.PostAsJsonAsync("/api/v1/brands", new CreateBrandRequest(org.Id, "a", "A"))).Content.ReadFromJsonAsync<BrandResponse>())!;
        var second = (await (await admin.PostAsJsonAsync("/api/v1/brands", new CreateBrandRequest(org.Id, "b", "B"))).Content.ReadFromJsonAsync<BrandResponse>())!;
        var contact = new BrandContactDto("0901112222", null, "a@x.vn", "1 Lê Lợi", "Bến Thành", "Quận 1", "TP.HCM", "07:00", "22:00", null);

        var firstPrimary = (await (await admin.PutAsJsonAsync($"/api/v1/brands/{first.Id}", new UpdateBrandRequest("A", true, contact, true))).Content.ReadFromJsonAsync<BrandResponse>())!;
        Assert.True(firstPrimary.IsPrimary);
        Assert.Equal("1 Lê Lợi", firstPrimary.Contact.AddressLine);

        var secondPrimary = (await (await admin.PutAsJsonAsync($"/api/v1/brands/{second.Id}", new UpdateBrandRequest("B", true, null, true))).Content.ReadFromJsonAsync<BrandResponse>())!;
        Assert.True(secondPrimary.IsPrimary);
        Assert.False((await admin.GetFromJsonAsync<BrandResponse>($"/api/v1/brands/{first.Id}"))!.IsPrimary);

        // A rename that omits the contact leaves the contact alone; bad hours are a 400.
        Assert.Equal("1 Lê Lợi", (await (await admin.PutAsJsonAsync($"/api/v1/brands/{first.Id}", new UpdateBrandRequest("A2", true))).Content.ReadFromJsonAsync<BrandResponse>())!.Contact.AddressLine);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/v1/brands/{first.Id}", new UpdateBrandRequest("A", true, contact with { OpenTime = "07:00", CloseTime = null }))).StatusCode);

        // The public website sees the identity plus the primary branch, without logging in.
        var publicBrand = (await anonymous.GetFromJsonAsync<PublicBrandResponse>("/api/v1/organization/public/brand"))!;
        Assert.Equal(tagline, publicBrand.Profile.Tagline);
        Assert.Equal("b", publicBrand.PrimaryBranch!.Code);

        // Editing is not anonymous.
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/organization/brand-profile")).StatusCode);
    }
}
